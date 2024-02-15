using System;
using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Services;

internal interface IHttpApiService
{ 
    public Task<string> RefreshTokensAsync(string url, string refreshToken);
    public Task<string> RegisterAsync(string url, string username, string password);
    public Task UnregisterAsync(string refreshToken);
    public Task<List<Board>> GetBoardsAsync();
    public Task PutBoardAsync(Board board);
    public Task<List<Linkage>> GetLinkagesAsync();
    public Task<Guid> PutLinkageAsync(Linkage linkage);
    public Task DeleteLinkageAsync(Guid id);
    public Task<List<CalibrationMethod>> GetCalibrationMethodsAsync();
    public Task<List<Calibration>> GetCalibrationsAsync();
    public Task<Guid> PutCalibrationAsync(Calibration calibration);
    public Task DeleteCalibrationAsync(Guid id);
    public Task<List<Setup>> GetSetupsAsync();
    public Task<Guid> PutSetupAsync(Setup setup);
    public Task DeleteSetupAsync(Guid id);
    public Task<List<Session>> GetSessionsAsync();
    public Task<TelemetryData> GetSessionPsstAsync(Guid id);
    public Task<Guid> PutSessionAsync(Session session);
    public Task<Guid> PutProcessedSessionAsync(string name, string description, byte[] data);
    public Task DeleteSessionAsync(Guid id);
}
