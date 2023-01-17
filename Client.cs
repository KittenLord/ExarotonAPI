using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;

#nullable disable

namespace Exaroton.Internal
{
    internal class Client
    {
        private const string webSocketAPIUrl = "wss://api.exaroton.com/v1/servers/{0}/websocket";
        private ClientWebSocket _client;
        private string Url { get; init; }
        public bool IsRunning { get; private set; }

        public async Task Connect()
        {
            await _client.ConnectAsync(new Uri(Url), new CancellationToken());
            IsRunning = true;
            Loop();
        }

        public void Send(string msg)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            _client.SendAsync(bytes, WebSocketMessageType.Text, true, new CancellationToken());
        }

        public async Task Stop()
        {
            IsRunning = false;
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", new CancellationToken());
        }
        
        private static async Task<String> ReadString(ClientWebSocket ws)
        {
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);

            WebSocketReceiveResult result = null;

            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(ms, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        private async void Loop()
        {
            while(IsRunning)
            {
                var msg = await ReadString(_client);
                OnMessage.Invoke(msg);
            }
        }

        public event Action<string> OnMessage = (msg) => {};

        public Client(string server, string token)
        {
            _client = new ClientWebSocket();
            Url = string.Format(webSocketAPIUrl, server);
            _client.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            //_client.
        }
    }
}