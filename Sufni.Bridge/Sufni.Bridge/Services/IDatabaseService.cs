using System.Collections.Generic;
using System.Threading.Tasks;
using Sufni.Bridge.Models;
using Sufni.Bridge.Models.Telemetry;
using Calibration = Sufni.Bridge.Models.Calibration;
using Linkage = Sufni.Bridge.Models.Linkage;

namespace Sufni.Bridge.Services;

public interface IDatabaseService
{
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
    public Task DeleteSessionAsync(int id);
}