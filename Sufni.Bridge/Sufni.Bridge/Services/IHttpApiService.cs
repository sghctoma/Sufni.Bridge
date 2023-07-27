using Sufni.Bridge.Models;
using System;
using System.Threading.Tasks;

namespace Sufni.Bridge.Services;

internal interface IHttpApiService
{ 
    public Task<string> InitAsync(string url, string refreshToken);
    public Task<string> RegisterAsync(string url, string username, string password);
    public Task Unregister(string refreshToken);
}
