﻿using System;
using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sufni.Bridge.Services;

internal interface IHttpApiService
{ 
    public Task<string> RefreshTokensAsync(string url, string refreshToken);
    public Task<string> RegisterAsync(string url, string username, string password);
    public Task UnregisterAsync(string refreshToken);
    public Task<List<Session>> GetSessionsAsync();
    public Task<Guid> PutProcessedSessionAsync(string name, string description, byte[] data);
    public Task<SynchronizationData> PullSyncAsync(int since = 0);
    public Task PushSyncAsync(SynchronizationData syncData);
}
