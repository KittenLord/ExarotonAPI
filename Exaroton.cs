using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exaroton.Internal;

namespace Exaroton
{
    public enum StreamType
    {
        Status = 0,
        Console = 1,
        Tick = 2,
        Stats = 3,
        Heap = 4
    }

    
    public static class Exaroton
    {
        public static async Task<bool> LoginAsync(string token)
        {
            APIClient.SetToken(token);

            try
            {
                await APIClient.GetRequestAsync("https://api.exaroton.com/v1/account/");
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static Dictionary<string, WebSocketAPIClient> _websockets = new Dictionary<string, WebSocketAPIClient>();

        public static WebSocketAPIClient CreateWebSocketAPIClient(string ServerID, string Token)
        {
            if(_websockets.ContainsKey(ServerID)) throw new Exception();
            var ws = new WebSocketAPIClient(ServerID, Token);
            
            _websockets.Add(ServerID, ws);

            return ws;
        }

        public static WebSocketAPIClient GetWebSocketAPIClient(string ServerID)
        {
            if(!_websockets.ContainsKey(ServerID)) throw new Exception();

            return _websockets[ServerID];
        }
    }
}