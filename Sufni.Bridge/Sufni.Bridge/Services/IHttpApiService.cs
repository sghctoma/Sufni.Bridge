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
    public Task<SynchronizationData> PullSyncAsync(int since = 0);
    public Task PushSyncAsync(SynchronizationData syncData);
    public Task<List<Guid>> GetIncompleteSessionIdsAsync();
    public Task<byte[]?> GetSessionPsstAsync(Guid id);
    public Task PatchSessionPsstAsync(Guid id, byte[] data);
}
