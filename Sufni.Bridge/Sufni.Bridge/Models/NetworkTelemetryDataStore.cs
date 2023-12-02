using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sufni.Bridge.Models;

public class NetworkTelemetryDataStore : ITelemetryDataStore
{
    public string Name { get; }
    public string? BoardId { get; private set; }
    private readonly IPEndPoint ipEndPoint;
    public readonly Task Initialization;
    
    public async Task<List<ITelemetryFile>> GetFiles()
    {
        var directoryInfo = await SstTcpClient.GetFile(ipEndPoint, 0);
        var recordCount = directoryInfo.Length / 25; // 9 characters + size (long) + timestamp (long)
        using var memoryStream = new MemoryStream(directoryInfo);
        using var reader = new BinaryReader(memoryStream);
        var boardId = reader.ReadBytes(8);
        BoardId = Convert.ToHexString(boardId).ToLower();
        var sampleRate = reader.ReadUInt16();

        var files = new List<ITelemetryFile>();
        for (var i = 0; i < recordCount; i++)
        {
            var name = Encoding.ASCII.GetString(reader.ReadBytes(9));
            var size = reader.ReadUInt64();
            var timestamp = reader.ReadUInt64();

            files.Add(new NetworkTelemetryFile(ipEndPoint, sampleRate, name, size, timestamp));
        }

        return files;
    }
    
    public NetworkTelemetryDataStore(IPAddress address, int port)
    {
        ipEndPoint = new IPEndPoint(address, port);
        Name = $"gosst://{ipEndPoint.Address}:{ipEndPoint.Port}";

        // We need this to set BoardId
        Initialization = GetFiles();
    }
}