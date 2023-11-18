using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sufni.Bridge.Models;

public class TelemetryFile
{
    public string FileName => fileInfo.Name;
    public string FullName => fileInfo.FullName;
    public DateTime StartTime { get; }
    public string Name { get; set; }
    public string Duration => duration.ToString("hh\\:mm\\:ss");
    public bool ShouldBeImported { get; set; }
    public bool Imported { get; set; }
    public string Base64Data => Convert.ToBase64String(File.ReadAllBytes(FullName));
    
    public string Description { get; set; } = "";

    private readonly FileInfo fileInfo;
    private readonly TimeSpan duration;
    
    #region Native interop

    private struct GeneratePsstReturn
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        // Struct used as return value from a native function, so the fields *are* assigned to.
        public IntPtr DataPointer;
        public int DataSize;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }

    [DllImport("gosst", EntryPoint = "GeneratePsst", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    private static extern GeneratePsstReturn GeneratePsstNative(byte[] data, int dataSize, byte[] linkage, int linkageSize,
        byte[] calibrations, int calibrationsSize);

    #endregion
    
    public TelemetryFile(FileInfo fileInfo)
    {
        this.fileInfo = fileInfo;

        using var stream = File.Open(this.fileInfo.FullName, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadBytes(3);
        var version = reader.ReadByte();
        if (!magic.SequenceEqual("SST"u8.ToArray()) || version != 3)
        {
            throw new FormatException("Not an SST file");
        }

        var sampleRate = reader.ReadUInt16();
        var count = (this.fileInfo.Length - 16 /* sizeof(header) */) / 4 /* sizeof(record) */;
        duration = TimeSpan.FromSeconds((double)count / sampleRate);
        ShouldBeImported = duration.TotalSeconds >= 5;

        reader.ReadUInt16(); // padding

        StartTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64()).DateTime;
        Name = FileName;
    }
    
    public byte[] GeneratePsst(byte[] linkage, byte[] calibrations)
    {
        var rawData = File.ReadAllBytes(FullName);
        var psst = GeneratePsstNative(
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
}