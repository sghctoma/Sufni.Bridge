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
    public Task ImportSession(TelemetryFile session, int setupId);
}
