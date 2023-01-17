using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
//using WebSocketSharp;
using Newtonsoft.Json;
using Exaroton;
using Newtonsoft.Json.Linq;

namespace Exaroton.Internal
{


    public class WebSocketAPIClient
    {
        private string[] _streamNames = new string[] { StreamNames.StatusStream, StreamNames.ConsoleStream, StreamNames.TickStream, StreamNames.StatsStream, StreamNames.HeapStream };
        private string GetStreamName(StreamType t)
        {
            return _streamNames[(int)t];
        }


        private Client _client;
        private bool HasStarted = false;




        public async Task Start()
        {
            // thank you stackoverflow
            //_client.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            //_client.SetCredentials(,)
            //_client.Protocol
            await _client.Connect();
        }

        public async Task Stop()
        {
            await _client.Stop();
            HasStarted = false;
        }


        private Dictionary<string, StreamInfo> Streams { get; set; } = new Dictionary<string, StreamInfo>();
        private static class StreamNames
        {
            public const string StatusStream = "status";
            public const string ConsoleStream = "console";
            public const string TickStream = "tick";
            public const string StatsStream = "stats";
            public const string HeapStream = "heap";
        }


        private void Send(WebSocketMessage msg)
        {
            if(!HasStarted) throw new Exception();

            var str = msg.Serialize();
            //Program.Logs += str + "\n";

            _client.Send(str);
        }

        private void SendToStreamUnsafe(StreamInfo stream, string type, object obj)
        {
            var msg = WebSocketMessage.Create(type, stream.StreamName, obj);
            
            Send(msg);
        }

        public void StartStream(StreamType stream, int consoleLines = 0)
        {
            StartStream(GetStreamName(stream), consoleLines);
        }
        public void StopStream(StreamType stream)
        {
            StopStream(GetStreamName(stream));
        }

        private void StartStream(string stream, int consoleLines = 0)
        {
            if(consoleLines < 0 || consoleLines > 500) throw new ArgumentOutOfRangeException();

            object? obj = null;
            if(stream == StreamNames.ConsoleStream) obj = new ConsoleArg(consoleLines);
            var msg = Streams[stream].GetSubscriptionMessage(obj);
            

            Send(msg);
        }
        private void StopStream(string stream)
        {
            var msg = Streams[stream].GetUnsubscriptionMessage();

            Send(msg);

            Streams[stream].IsRunning = false;
        }


        public event Action OnConnectionEstablished = () => {};
        // public event Action OnServerConnected = () => {};
        // public event Action<string> OnServerDisconnected = (reason) => {}; 



        public event EventHandler<StreamEventArgs<Server>> OnMessage_StatusStream = (s, e) => {};
        public event EventHandler<StreamEventArgs<string>> OnMessage_ConsoleStream = (s, e) => {};
        public event EventHandler<StreamEventArgs<TickInfo>> OnMessage_TickStream = (s, e) => {};
        public event EventHandler<StreamEventArgs<ServerStats>> OnMessage_StatsStream = (s, e) => {};
        public event EventHandler<StreamEventArgs<HeapMemoryUsage>> OnMessage_HeapStream = (s, e) => {};




        private StreamEventArgs<T> UpdateStreamValues<T>(string name, WebSocketMessage msg)
        {
            Streams[name].PreviousValue = Streams[name].CurrentValue;

            //Console.WriteLine(msg.Data);
            
            if(name == StreamNames.ConsoleStream)
                Streams[name].CurrentValue = msg.Data;
            else
                Streams[name].CurrentValue = JsonConvert.DeserializeObject<T>(msg.Data);

            return new StreamEventArgs<T>((T?)Streams[name].CurrentValue, (T?)Streams[name].PreviousValue);
        }

        private void ReceivedStatusMessage(WebSocketMessage msg)
        {
            var e = UpdateStreamValues<Server>(StreamNames.StatusStream, msg);
            OnMessage_StatusStream.Invoke(null, e);
        }

        private void ReceivedConsoleMessage(WebSocketMessage msg)
        {
            var e = UpdateStreamValues<string>(StreamNames.ConsoleStream, msg);
            OnMessage_ConsoleStream.Invoke(null, e);
        }
        private void ReceivedTickMessage(WebSocketMessage msg)
        {
            var e = UpdateStreamValues<TickInfo>(StreamNames.TickStream, msg);
            OnMessage_TickStream.Invoke(null, e);
        }
        private void ReceivedStatsMessage(WebSocketMessage msg)
        {
            var e = UpdateStreamValues<ServerStats>(StreamNames.StatsStream, msg);
            OnMessage_StatsStream.Invoke(null, e);
        }
        private void ReceivedHeapMessage(WebSocketMessage msg)
        {
            var e = UpdateStreamValues<HeapMemoryUsage>(StreamNames.HeapStream, msg);
            OnMessage_HeapStream.Invoke(null, e);
        }


        public void SendCommandToConsoleStream(string command)
        {
            var msg = WebSocketMessage.Create("command", StreamNames.ConsoleStream, command);
            if(!Streams[StreamNames.ConsoleStream].IsRunning) throw new Exception();

            Send(msg);
        }

        

        public WebSocketAPIClient(string server, string token)
        {
             _client = new Client(server, token);//GetWebSocketAPIUrl(server));
             //_client.
             //Console.WriteLine(string.Join(" ", _client.Credentials.Roles));
            // _client.CustomHeaders = new Dictionary<string, string>
            // {
            //     { "Authorization", $"Bearer {token}" }
            // };
            _client.OnMessage += OnMessage;
            //_client.SetCredentials()

            // StatusStream = new Stream<Server>(StreamNames.StatsStream);
            // ConsoleStream = new Stream<string>(StreamNames.ConsoleStream);
            // TickStream = new Stream<TickInfo>(StreamNames.TickStream);
            // StatsStream = new Stream<ServerStats>(StreamNames.StatsStream);
            // HeapStream = new Stream<HeapMemoryUsage>(StreamNames.HeapStream);

            Streams.Add(StreamNames.StatusStream, new StreamInfo(StreamNames.StatusStream));
            Streams[StreamNames.StatusStream].IsRunning = true;
            Streams.Add(StreamNames.ConsoleStream, new StreamInfo(StreamNames.ConsoleStream));
            Streams.Add(StreamNames.TickStream, new StreamInfo(StreamNames.TickStream));
            Streams.Add(StreamNames.StatsStream, new StreamInfo(StreamNames.StatsStream));
            Streams.Add(StreamNames.HeapStream, new StreamInfo(StreamNames.HeapStream));
        }

        private void OnMessage(string str)
        {
            //Program.Logs += str + "\n";
            var msg = JsonConvert.DeserializeObject<WebSocketMessage>(str, new JsonConverterObjectToString());

            if(msg is null) throw new Exception();

            if(msg.Type == "ready")
            {
                HasStarted = true;
                OnConnectionEstablished.Invoke();
            }

            if(msg.Type == "started")
            {
                if(Streams.ContainsKey(msg.Stream))
                {
                    Streams[msg.Stream].IsRunning = true;
                }
            }

            if(msg.Stream != "" && msg.Type != "started")
            {
                if(msg.Stream == StreamNames.StatusStream) ReceivedStatusMessage(msg);
                if(msg.Stream == StreamNames.ConsoleStream) ReceivedConsoleMessage(msg);
                if(msg.Stream == StreamNames.TickStream) ReceivedTickMessage(msg);
                if(msg.Stream == StreamNames.StatsStream) ReceivedStatsMessage(msg);
                if(msg.Stream == StreamNames.HeapStream) ReceivedHeapMessage(msg);
            }
        }
    }
}