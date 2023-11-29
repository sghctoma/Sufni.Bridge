using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sufni.Bridge.Models.Telemetry;
using Calibration = Sufni.Bridge.Models.Calibration;
using Linkage = Sufni.Bridge.Models.Linkage;

namespace Sufni.Bridge.Services;

internal interface IHttpApiService
{ 
    public Task<string> RefreshTokensAsync(string url, string refreshToken);
    public Task<string> RegisterAsync(string url, string username, string password);
    public Task UnregisterAsync(string refreshToken);
    public Task<List<Board>> GetBoardsAsync();
    public Task PutBoardAsync(Board board);
    public Task<List<Linkage>> GetLinkagesAsync();
    public Task<int> PutLinkageAsync(Linkage linkage);
    public Task DeleteLinkageAsync(int id);
    public Task<List<CalibrationMethod>> GetCalibrationMethodsAsync();
    public Task<List<Calibration>> GetCalibrationsAsync();
    public Task<int> PutCalibrationAsync(Calibration calibration);
    public Task DeleteCalibrationAsync(int id);
    public Task<List<Setup>> GetSetupsAsync();
    public Task<int> PutSetupAsync(Setup setup);
    public Task DeleteSetupAsync(int id);
    public Task<List<Session>> GetSessionsAsync();
    public Task<TelemetryData> GetSessionPsstAsync(int id);
    public Task<int> PutSessionAsync(Session session);
    public Task<int> PutProcessedSessionAsync(string name, string description, byte[] data);
    public Task DeleteSessionAsync(int id);
}
