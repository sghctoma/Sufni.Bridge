using System;
using System.Runtime.InteropServices;

namespace Sufni.Bridge.Models;

public interface ITelemetryFile
{
    public string Name { get; set; }
    public string FileName { get; }
    public bool ShouldBeImported { get; set; }
    public bool Imported { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; init; }
    public string Duration { get; init; }
    public string Base64Data { get; }

    public byte[] GeneratePsst(byte[] linkage, byte[] calibrations);
    public void OnImported();
    
    #region Native interop

    protected struct GeneratePsstReturn
    {
        // Struct used as return value from a native function, so the fields *are* assigned to.
        // ReSharper disable UnassignedField.Global
        public IntPtr DataPointer;
        public int DataSize;
    }

    [DllImport("gosst", EntryPoint = "GeneratePsst", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    protected static extern GeneratePsstReturn GeneratePsstNative(byte[] data, int dataSize, byte[] linkage, int linkageSize,
        byte[] calibrations, int calibrationsSize);

    #endregion
}