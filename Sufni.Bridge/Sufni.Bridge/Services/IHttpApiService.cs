using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sufni.Bridge.Services;

internal interface IHttpApiService
{ 
    public Task<string> RefreshTokensAsync(string url, string refreshToken);
    public Task<string> RegisterAsync(string url, string username, string password);
    public Task Unregister(string refreshToken);
    public Task<List<Board>> GetBoards();
    public Task<List<Linkage>> GetLinkages();
    public Task<int> PutLinkage(Linkage linkage);
    public Task DeleteLinkage(int id);
    public Task<List<CalibrationMethod>> GetCalibrationMethods();
    public Task<List<Calibration>> GetCalibrations();
    public Task<int> PutCalibration(Calibration calibration);
    public Task DeleteCalibration(int id);
    public Task<List<Setup>> GetSetups();
    public Task<int> PutSetup(Setup setup);
    public Task DeleteSetup(int id);
    public Task ImportSession(TelemetryFile session, int setupId);
}
