using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Sufni.Bridge.Models;

public static class SstTcpClient
{
    public static async Task<byte[]> GetFile(IPEndPoint ipEndPoint, int fileId)
    {
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(ipEndPoint);
        
        // Get identifier as little-endian byte array
        var id = BitConverter.GetBytes(fileId);
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
        var size = BitConverter.ToInt32(sizeBuffer.AsSpan()[..4]); // We won't be able to process file larger than
                                                                     // int max anyway, so this is OK.
        
        // Send header OK signal
        await client.SendAsync(new byte[] { 0x04, 0x00, 0x00, 0x00 });
        
        // Receive data
        var buffer = new byte[size];
        var totalRead = 0;
        do
        {
            // Receiving the entire file fails without setting a cap on size with one Receive call. On my FreeBSD
            // box, the Python client (test_utils/tcpserver_client.py in the sst repo) reads 1460 (MSS) bytes of data
            // in one go most of the times, and it did not fail yet - hence the 1460 bytes cap.
            var read = client.Receive(buffer, totalRead, Math.Min(1460, size - totalRead), SocketFlags.None);
            if (read == 0)
            {
                throw new Exception("Server closed connection while receiveing data.");
            }
            totalRead += read;
        } while (totalRead != size);
        
        // Send file received signal. Server will close connection after receiving this.
        await client.SendAsync(new byte[] { 0x05, 0x00, 0x00, 0x00 });
        
        return buffer;
    }
}