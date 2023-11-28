using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sufni.Bridge.Models;

public class NetworkTelemetryDataStore : ITelemetryDataStore
{
    public string Name { get; }
    public IList<ITelemetryFile> Files { get; } = new List<ITelemetryFile>();
    public string? BoardId { get; private set; }
    public Task Initialization { get; }

    private readonly IPEndPoint ipEndPoint;

    private async Task<byte[]> ReadDirectoryInfo()
    {
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(ipEndPoint);
        
        // Request directory info "file"
        await client.SendAsync(new byte[]
        {
            0x03, 0x00, 0x00, 0x00, // 3: file request command
            0x00, 0x00, 0x00, 0x00  // 0: directory info identifier
        }, SocketFlags.None);
        
        // Receive directory info size
        var sizeBuffer = new byte[8];
        await client.ReceiveAsync(sizeBuffer);
        var size = BitConverter.ToUInt64(sizeBuffer);
        
        // Send header OK signal
        await client.SendAsync(new byte[] { 0x04, 0x00, 0x00, 0x00 });
        
        // Receive directory info data
        var buffer = new byte[size];
        await client.ReceiveAsync(buffer);
        
        // Send file received signal
        await client.SendAsync(new byte[] { 0x05, 0x00, 0x00, 0x00 });
        
        client.Shutdown(SocketShutdown.Both);

        return buffer;
    }
    
    private async Task ProcessDirectoryInfo()
    {
        var directoryInfo = await ReadDirectoryInfo();
        var recordCount = directoryInfo.Length / 25; // 9 characters + size (long) + timestamp (long)
        using var memoryStream = new MemoryStream(directoryInfo);
        using var reader = new BinaryReader(memoryStream);
        var boardId = reader.ReadBytes(8);
        BoardId = Convert.ToHexString(boardId);
        var sampleRate = reader.ReadUInt16();

        for (var i = 0; i < recordCount; i++)
        {
            var name = Encoding.ASCII.GetString(reader.ReadBytes(9));
            var size = reader.ReadUInt64();
            var timestamp = reader.ReadUInt64();

            Files.Add(new NetworkTelemetryFile(ipEndPoint, sampleRate, name, size, timestamp));
        }
    }
    
    public NetworkTelemetryDataStore(IPAddress address, int port)
    {
        ipEndPoint = new IPEndPoint(address, port);
        Name = $"gosst://{ipEndPoint.Address}:{ipEndPoint.Port}";

        Initialization = ProcessDirectoryInfo();
    }
}