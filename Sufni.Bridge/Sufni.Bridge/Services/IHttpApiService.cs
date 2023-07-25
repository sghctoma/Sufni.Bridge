using Avalonia.Controls.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Sufni.Bridge.Services;

internal interface IHttpApiService
{ 
    public Task<bool> IsRegisteredAsync();
    public Task<JsonObject?> RegisterAsync(string url, string username, string password);
}
