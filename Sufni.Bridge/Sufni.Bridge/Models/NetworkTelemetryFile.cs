using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sufni.Bridge.Models;

public class NetworkTelemetryFile : ITelemetryFile
{
    public string Name { get; set; }
    public string FileName { get; }
    public bool ShouldBeImported { get; set; }
    public bool Imported { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; init; }
    public string Duration { get; init; }

    private readonly IPEndPoint ipEndPoint;
    
    private async Task<byte[]> ReadContent()
    {
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(ipEndPoint);
        
        // Get identifier as little-endian byte array
        var idString = FileName[..5].TrimStart('0');
        var idUint = uint.Parse(idString);
        var id = BitConverter.GetBytes(idUint);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(id);
        }
        
        // Request file
        await client.SendAsync(new byte[]
        {
            0x03, 0x00, 0x00, 0x00,    // 3: file request command
            id[0], id[1], id[2], id[3] // file id
        }, SocketFlags.None);
        
        // Receive size
        var sizeBuffer = new byte[8];
        await client.ReceiveAsync(sizeBuffer);
        var size = BitConverter.ToUInt64(sizeBuffer);
        
        // Send header OK signal
        await client.SendAsync(new byte[] { 0x04, 0x00, 0x00, 0x00 });
        
        // Receive data
        var buffer = new byte[size];
        await client.ReceiveAsync(buffer);
        
        // Send file received signal. Server will close connection after receiving this.
        await client.SendAsync(new byte[] { 0x05, 0x00, 0x00, 0x00 });
        
        return buffer;
    } 
    
    public async Task<byte[]> GeneratePsstAsync(byte[] linkage, byte[] calibrations)
    {
        var rawData = await ReadContent();
        var psst = ITelemetryFile.GeneratePsstNative(
            rawData, rawData.Length, 
            linkage, linkage.Length,
            calibrations, calibrations.Length);
        if (psst.DataSize < 0)
        {
            throw new Exception("SST => PSST conversion failed.");
        }

        var psstBytes = new byte[psst.DataSize];
        Marshal.Copy(psst.DataPointer, psstBytes, 0, psst.DataSize);

        return psstBytes;
    }

    public void OnImported()
    {
        Imported = true;
    }
    
    public NetworkTelemetryFile(IPEndPoint source, ushort sampleRate, string name, ulong size, ulong timestamp)
    {
        var count = (size - 16 /* sizeof(header) */) / 4 /* sizeof(record) */;
        var duration = TimeSpan.FromSeconds((double)count / sampleRate);
        ShouldBeImported = duration.TotalSeconds >= 5;
        StartTime = DateTimeOffset.FromUnixTimeSeconds((int)timestamp).DateTime;
        Duration = duration.ToString(@"hh\:mm\:ss");
        Name = name;
        FileName = name;
        Description = $"Imported from {name}";
        ipEndPoint = source;
    }
}