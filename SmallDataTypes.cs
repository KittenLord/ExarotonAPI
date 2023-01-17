using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Exaroton
{
    class RamAmount
    {
        [JsonProperty("ram")]
        public int Ram { get; private set; }

        [JsonConstructor]
        public RamAmount(int ram)
        {
            Ram = ram;
        }
    }

    class MessageOfTheDay
    {
        [JsonProperty("motd")]
        public string Message { get; private set; }

        [JsonConstructor]
        public MessageOfTheDay(string message)
        {
            Message = message;
        }
    }

    class StringCommand
    {
        [JsonProperty("command")]
        public string Command { get; private set; }


        [JsonConstructor]
        public StringCommand(string command)
        {
            Command = command;
        }
    }

    class PlayersListRequest
    {
        [JsonProperty("entries")] public List<string> Players { get; private set; } = new List<string>();

        public PlayersListRequest(List<string> players)
        {
            Players = players;
        }
    }

    public class TickInfo
    {
        [JsonProperty("entries")] public double AverageTickTime { get; private set; }

        [JsonConstructor]
        private TickInfo(double averageTickTime)
        {
            AverageTickTime = averageTickTime;
        }
    }

    public class ServerStats
    {
        [JsonProperty("memory")] public ServerMemoryUsage MemoryUsage { get; private set; }

        [JsonConstructor]
        private ServerStats(ServerMemoryUsage memoryUsage)
        {
            MemoryUsage = memoryUsage;
        }
    }

    public class ServerMemoryUsage
    {
        [JsonProperty("percent")] public double Percent { get; private set; }
        [JsonProperty("usage")] public int Usage { get; private set; }

        [JsonConstructor]
        private ServerMemoryUsage(double percent, int usage)
        {
            Percent = percent;
            Usage = usage;
        }
    }

    public class HeapMemoryUsage
    {
        [JsonProperty("usage")] public int Usage { get; private set; }

        [JsonConstructor]
        private HeapMemoryUsage(int usage)
        {
            Usage = usage;
        }
    }

    public class ConsoleArg
    {
        [JsonProperty("tail")] public int Tail { get; private set; }

        [JsonConstructor]
        public ConsoleArg(int tail)
        {
            Tail = tail;
        }
    }
}