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

    
    public class ExarotonClient
    {
        public string Token { get; init; }

        public async Task<bool> LoginAsync()
        {
            APIClient.SetToken(Token);

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

        private Dictionary<string, ExarotonWebsocketClient> _websockets = new Dictionary<string, ExarotonWebsocketClient>();

        public ExarotonWebsocketClient AddWebsocketClient(string ServerID)
        {
            if(_websockets.ContainsKey(ServerID)) throw new Exception("Websocket client has been already created for this server!");
            var ws = new ExarotonWebsocketClient(ServerID, Token);
            
            _websockets.Add(ServerID, ws);
            return ws;
        }

        public ExarotonWebsocketClient CreateWebsocketClient(string ServerID)
        {
            return new ExarotonWebsocketClient(ServerID, Token);
        }

        public ExarotonWebsocketClient GetWebsocketClient(string ServerID)
        {
            if(!_websockets.ContainsKey(ServerID)) throw new Exception("There is no websocket client created for this server!");

            return _websockets[ServerID];
        }

        public ExarotonClient(string token)
        {
            Token = token;
        }
    }
}