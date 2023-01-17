using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Exaroton;
using Newtonsoft.Json.Linq;

namespace Exaroton.Internal
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

    public class WebSocketMessage
    {
        [JsonProperty("type")] public string Type { get; private set; }
        [JsonProperty("stream")] public string Stream { get; private set; }
        [JsonProperty("data")] public string Data { get; private set; }

        public T? GetValue<T>()
        {
            return JsonConvert.DeserializeObject<T>(Data);
        }


        // json deserialize won't put doublequotes if data was of string type, and "serializing" it back will ultimately fail
        [JsonIgnore] private bool IsUserCreated = false;


        [JsonConstructor]
        private WebSocketMessage(string type, string stream, string data)
        {
            if(type is null) type = "";
            if(stream is null) stream = "";
            if(data is null) data = "";

            Type = type;
            Stream = stream;
            Data = data;
        }

        // public static StreamMessage Deserialize(string json)
        // {
        //     var sm = JsonConvert.DeserializeObject<StreamMessage>(json, new JsonConverterObjectToString());

        //     JObject.Parse(json).
        //     token.
        // }

        public static WebSocketMessage Create(string type, string stream, object? data)
        {
            var _data = JsonConvert.SerializeObject(data);
            //Console.WriteLine(_data);

            var sm = new WebSocketMessage(type, stream, _data);
            sm.IsUserCreated = true;

            return sm;
        }

        public static string Serialize(WebSocketMessage sm)
        {
            if(!sm.IsUserCreated) throw new Exception("Can't serialize StreamMessage initialized by JSON.");

            var type = "\"" + sm.Type + "\"";
            var stream = "\"" + sm.Stream + "\"";
            var data = sm.Data;

            var typeLabel = "\"type\"";
            var streamLabel = "\"stream\"";
            var dataLabel = "\"data\"";

            var streamstr = streamLabel + ":" + stream + ",";
            var typestr = typeLabel + ":" + type;
            var datastr = data == "\"\"" ? "" : "," + dataLabel + ":" + data;

            return "{" + streamstr + typestr + datastr + "}";
        }

        public string Serialize()
        {
            return Serialize(this);
        }

        public static bool operator ==(WebSocketMessage a, WebSocketMessage b)
        {
            return a.Type == b.Type && a.Stream == b.Stream && a.Data == b.Data;
        }

        public static bool operator !=(WebSocketMessage a, WebSocketMessage b)
        {
            return !(a == b);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if(obj is WebSocketMessage sm) return sm == this;

            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public class StreamInfo
    {
        public string StreamName { get; private set; }
        public bool IsRunning { get; set; } = false;

        public object? CurrentValue { get; set; }
        public object? PreviousValue { get; set; }


        public const string SubscribeType = "start";
        public const string UnsubscribeType = "stop";
        public const string SuccessType = "started";


        public WebSocketMessage GetSubscriptionMessage(object? arg = null) => WebSocketMessage.Create(SubscribeType, StreamName, arg ?? "");
        public WebSocketMessage GetUnsubscriptionMessage() => WebSocketMessage.Create(UnsubscribeType, StreamName, "");
        public WebSocketMessage GetSuccessfulSubscriptionMessage() => WebSocketMessage.Create(SuccessType, StreamName, "");

        public StreamInfo(string streamName)
        {
            StreamName = streamName;
        }
    }
}