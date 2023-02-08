using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Exaroton.Internal;
using Newtonsoft.Json;
using Exaroton;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Exaroton
{
    public class StreamEventArgs<T> : EventArgs
    {
        public T? Value { get; private set; }
        public T? PreviousValue { get; private set; }

        public StreamEventArgs(T? value, T?previousValue)
        {
            Value = value;
            PreviousValue = previousValue;
        }
    }


    public class ExarotonWebsocketClient
    {
        private string[] _streamNames = new string[] { StreamNames.StatusStream, StreamNames.ConsoleStream, StreamNames.TickStream, StreamNames.StatsStream, StreamNames.HeapStream };
        private string GetStreamName(StreamType t)
        {
            return _streamNames[(int)t];
        }


        private Client _client;
        public bool IsRunning { get; private set; } = false;
        private int consoleLinesRequest = 0;


        private async Task<bool> WaitUntil(Func<bool> condition, int msFrequency = 50, int sTimeout = 15)
        {
            var t = Task.Run(async () => {
               while(!condition()) await Task.Delay(msFrequency); 
            });

            return t == await Task.WhenAny(t, Task.Delay(sTimeout * 1000));
        }


        public async Task Start()
        {
            await _client.ConnectAsync();

            var started = await WaitUntil(() => IsRunning, 50, 10);
            if(!started) throw new Exception("Couldn't start websocket client.");
        }

        public async Task Stop()
        {
            await _client.Stop();
            IsRunning = false;
        }

        public void SetConsoleLines(int i)
        {
            if(i < 0 || i > 500) throw new ArgumentException();

            consoleLinesRequest = i;
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


        private async Task Send(WebSocketMessage msg)
        {
            if(!IsRunning) await Start();

            var str = msg.Serialize();

            _client.Send(str);
        }

        private async Task SendToStream(StreamType stream, WebSocketMessage msg)
        {
            await SendToStream(GetStreamName(stream), msg);
        }

        private async Task SendToStream(string stream, WebSocketMessage msg)
        {
            if(!Streams[stream].IsRunning) await StartStream(stream);

            await Send(msg);
        }

        public async Task<bool> StartStream(StreamType stream)
        {
            return await StartStream(GetStreamName(stream));
        }
        public async Task StopStream(StreamType stream)
        {
            await StopStream(GetStreamName(stream));
        }
        public bool IsStreamRunning(StreamType stream)
        {
            return Streams[GetStreamName(stream)].IsRunning;
        }

        private async Task<bool> StartStream(string stream)
        {
            object? obj = null;
            if(stream == StreamNames.ConsoleStream) obj = new ConsoleArg(consoleLinesRequest);
            var msg = Streams[stream].GetSubscriptionMessage(obj);
            
            await Send(msg);

            var started = await WaitUntil(() => Streams[stream].IsRunning);

            //if(!started) throw new Exception("Couldn't start the stream");
            return started;
        }
        private async Task StopStream(string stream)
        {
            var msg = Streams[stream].GetUnsubscriptionMessage();

            await Send(msg);

            Streams[stream].IsRunning = false;
        }

        public async Task<string> SendCommandToConsoleStream(string command, string wantedContent = "")
        {
            var msg = WebSocketMessage.Create("command", StreamNames.ConsoleStream, command);
            
            await SendToStream(StreamType.Console, msg);

            lastConsoleOutput = "";
            while(!lastConsoleOutput.Contains(wantedContent) || lastConsoleOutput == "") await Task.Delay(2);

            var l = lastConsoleOutput;
            lastConsoleOutput = "";
            return l;
        }

        private string lastConsoleOutput = "";


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



        private void OnMessage(string str)
        {
            if(str == "" && !IsRunning)
            {
                throw new Exception("Couldn't start websocket client. You are probably using wrong token/server id.");
            }

            var msg = JsonConvert.DeserializeObject<WebSocketMessage>(str, new JsonConverterObjectToString());

            if(msg is null) return;

            if(msg.Type == "ready")
            {
                IsRunning = true;
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

        // Studying console output makes me think that all messages coming from Exaroton fit in this regex
        Regex IsValidMessage = new Regex(@"\[\d\d:\d\d:\d\d\] \[.*\] \[minecraft\/DedicatedServer\]: .*");
        Regex IsUserSent = new Regex(@"\[\d\d:\d\d:\d\d\] \[.*\] \[minecraft\/DedicatedServer\]: <.*>");
        Regex IsAdminSent = new Regex(@"\[\d\d:\d\d:\d\d\] \[.*\] \[minecraft\/DedicatedServer\]: \[.*\]");

        private bool IsConsoleResponseAuthentic(string response)
        {
            return IsValidMessage.IsMatch(response) && !IsUserSent.IsMatch(response) && !IsAdminSent.IsMatch(response);
        }
        

        public ExarotonWebsocketClient(string server, string token)
        {
             _client = new Client(server, token);
            _client.OnMessage += OnMessage;

            Streams.Add(StreamNames.StatusStream, new StreamInfo(StreamNames.StatusStream));
            Streams[StreamNames.StatusStream].IsRunning = true;

            Streams.Add(StreamNames.ConsoleStream, new StreamInfo(StreamNames.ConsoleStream));
            Streams.Add(StreamNames.TickStream, new StreamInfo(StreamNames.TickStream));
            Streams.Add(StreamNames.StatsStream, new StreamInfo(StreamNames.StatsStream));
            Streams.Add(StreamNames.HeapStream, new StreamInfo(StreamNames.HeapStream));

            OnMessage_ConsoleStream += (s, e) => {
                if(IsConsoleResponseAuthentic(e.Value ?? ""))
                    lastConsoleOutput = e.Value ?? "";
            };
        }
    }
}