using System.Collections.Generic;
using System.Threading.Tasks;
using Sufni.Bridge.Models;

namespace Sufni.Bridge.Services;

public class HttpApiServiceStub : IHttpApiService
{
    public async Task<string> RefreshTokensAsync(string url, string refreshToken)
    {
        return refreshToken;
    }

    public async Task<string> RegisterAsync(string url, string username, string password)
    {
        return "MOCK_TOKEN";
    }

    public async Task Unregister(string refreshToken)
    {
    }

    public async Task<List<Board>> GetBoards()
    {
        return new List<Board>()
        {
            new Board("0000000000000000", 0),
            new Board("0000000000000001", 1),
            new Board("0011223344556677", 2),
            new Board("0000000000000003", 3),
        };
    }

    public async Task ImportSession(TelemetryFile session, int setupId)
    {
    }
}