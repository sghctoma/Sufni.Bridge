using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sufni.Bridge.Models;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Services;

public interface IDatabaseService
{
    public Task<List<Board>> GetBoardsAsync();
    public Task PutBoardAsync(Board board);
    public Task<List<Linkage>> GetLinkagesAsync();
    public Task<Linkage?> GetLinkageAsync(Guid? id);
    public Task<Guid?> PutLinkageAsync(Linkage linkage);
    public Task DeleteLinkageAsync(Guid? id);
    public Task<List<CalibrationMethod>> GetCalibrationMethodsAsync();
    public Task<CalibrationMethod?> GetCalibrationMethodAsync(Guid? id);
    public Task<List<Calibration>> GetCalibrationsAsync();
    public Task<Calibration?> GetCalibrationAsync(Guid? id);
    public Task<Guid?> PutCalibrationAsync(Calibration calibration);
    public Task DeleteCalibrationAsync(Guid? id);
    public Task<List<Setup>> GetSetupsAsync();
    public Task<Setup?> GetSetupAsync(Guid? id);
    public Task<Guid?> PutSetupAsync(Setup setup);
    public Task DeleteSetupAsync(Guid? id);
    public Task<List<Session>> GetSessionsAsync();
    public Task<TelemetryData?> GetSessionPsstAsync(Guid? id);
    public Task<byte[]?> GetSessionRawPsstAsync(Guid? id);
    public Task<Guid?> PutSessionAsync(Session session);
    public Task DeleteSessionAsync(Guid? id);
}