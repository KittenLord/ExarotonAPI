using System;
using System.Net;
using Newtonsoft.Json;
using Exaroton.Internal;
using Exaroton.Cast;

namespace Exaroton
{
    public enum ServerStatus
    {
        Offline = 0,
        Online = 1,
        Starting = 2,
        Stopping = 3,
        Restarting = 4,
        Saving = 5,
        Loading = 6,
        Crashed = 7,
        Pending = 8,
        Preparing = 10
    }

    public class Server
    {
        [JsonProperty("id")] public string ServerID { get; private set; }
        [JsonProperty("name")] public string Name { get; private set; }
        [JsonProperty("address")] public string Address { get; private set; }
        [JsonProperty("motd")] public string MessageOfTheDay { get; private set; }
        [JsonProperty("status")] public ServerStatus Status { get; private set; }
        [JsonProperty("host")] public string? Host { get; private set; }
        [JsonProperty("port")] public int? Port { get; private set; }
        [JsonProperty("software")] public ServerSoftware Software { get; private set; }
        [JsonProperty("players")] public PlayerList PlayersList { get; private set; }
        [JsonProperty("shared")] public bool Shared { get; private set; }


        #region Internal Websocket
        [JsonIgnore] private ExarotonWebsocketClient? Client { get; set; } = null;
        public void SetWebsocketClient(ExarotonClient client)
        {
            SetWebsocketClient(client.CreateWebsocketClient(ServerID));
        }

        private void SetWebsocketClient(ExarotonWebsocketClient client)
        {
            Client = client;
            SetupWebsocketClient();
        }

        private async void SetupWebsocketClient()
        {
            if(Client is null) throw new Exception();
            Client.SetConsoleLines(500);

            await Client.Start();

            if(!Client.IsStreamRunning(StreamType.Console))
                await Client.StartStream(StreamType.Console);
        }
        #endregion Internal Websocket
        

        [JsonIgnore] public bool IsOnline => Status == ServerStatus.Online;
        [JsonIgnore] public string? FullHost 
        {
            get
            {
                if(Host is null || Port is null) return null;

                return Host + ":" + Port.ToString();
            }
        }
        [JsonIgnore] public List<string> PlayerUsernames => PlayersList.Players;
        [JsonIgnore] public List<MinecraftPlayer> Players => PlayerUsernames.Select(p => new MinecraftPlayer(p, ServerID, () => this)).ToList();

        public static async Task<Server> GetServerAsync(string serverID)
        {
            Response response = await APIClient.GetRequestAsync("https://api.exaroton.com/v1/servers/" + serverID);

            var server = response.BuildData<Server>();

            return server;
        }
        public async Task<Server> GetUpdateAsync()
        {
            var update = await GetServerAsync(this.ServerID);
            update.Client = Client;
            return update;
        }
        
        #region Commands

        public async Task ExecuteCommandAsync(string command)
        {
            StringCommand c = new StringCommand(command);
            var json = JsonConvert.SerializeObject(c);

            await APIClient.PostRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/command/", json);
        }

        public async Task<string> ExecuteCommandWithResponseAsync(string command, string expectedContent = "")
        {
            if(Client is null) throw new Exception("Cannot await for response without a Websocket client initialized.");
            if(!Client.IsRunning) throw new Exception("Cannot await for response without a Websocket client running.");
            if(!Client.IsStreamRunning(StreamType.Console)) throw new Exception("Cannot await for response without the Console stream running.");

            var data = await Client.SendCommandToConsoleStream(command, expectedContent);

            return data;
        }
        
        public async Task<T> ExecuteDataCommandAsync<T>(string command, string expectedContent = "")
        {
            var data = await ExecuteCommandWithResponseAsync(command, expectedContent);

            return data.ConvertData<T>();
        }

        public async Task<T> ExecuteDataCommandAsync<T>(string command, Func<string, T> converter, string wantedContent = "")
        {
            string data = await ExecuteDataCommandAsync<string>(command, wantedContent);

            T result = converter(data);

            return result;
        }

        public async Task ExecuteCommandAsync(IMinecraftCommand command)
        {
            await ExecuteCommandAsync(command.Build());
        }

        #endregion Commands

        #region Logs
        public async Task<string> GetLogs()
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/logs/");

            var logs = response.BuildData<string>();

            return logs;
        }

        public async Task<SharedLogs> ShareLogs()
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/logs/share/");

            var logs = response.BuildData<SharedLogs>();

            return logs;
        }
        #endregion Logs

        #region Settings
        public async Task<int> GetRamAmountAsync()
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/options/ram/");

            var ram = response.BuildData<RamAmount>();

            return ram.Ram;
        }

        public async Task<bool> SetRamAmountAsync(int amount)
        {
            if(amount < 2 || amount > 16) throw new ArgumentException();

            RamAmount r = new RamAmount(amount);
            string json = JsonConvert.SerializeObject(r);
            var response = await APIClient.PostRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/options/ram/", json);

            return response.IsSuccess;
        }

        public async Task<string> GetMessageOfTheDayAsync()
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/options/motd/");

            var motd = JsonConvert.DeserializeObject<MessageOfTheDay>(response.Data);

            return motd?.Message ?? "";
        }

        public async Task<bool> SetMessageOfTheDayAsync(string message)
        {
            var motd = new MessageOfTheDay(message);

            var json = JsonConvert.SerializeObject(motd);

            var response = await APIClient.PostRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/options/motd/", json);

            return response.IsSuccess;
        }
        #endregion Settings

        #region Server operations
        public async Task<bool> StartServerAsync()
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/start/");

            return response.IsSuccess;
        }

        public async Task<bool> StopServerAsync()
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/stop/");

            return response.IsSuccess;
        }

        public async Task<bool> RestartServerAsync()
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/restart/");

            return response.IsSuccess;
        }
        #endregion Server operations

        #region Player lists
        private readonly string[] listNames = new string[] { "whitelist", "ops", "banned-players", "banned-ips" };
        public async Task<List<string>> GetAvailablePlayerListsNames()
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/playerlists/");

            var list = response.BuildData<List<string>>();

            return list;
        }
        public async Task<List<PlayerListType>> GetAvailablePlayerLists()
        {
            var list = await GetAvailablePlayerListsNames();

            var l = list.Select(el => GetPlayerlistType(el)).ToList();

            return l;
        }

        public async Task<List<string>> GetPlayerListUsernames(PlayerListType list)
        {
            return await GetPlayerListUsernames(GetPlayerlistName(list));
        }
        private async Task<List<string>> GetPlayerListUsernames(string listName)
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/playerlists/{listName}");

            var list = response.BuildData<List<string>>();

            return list;
        }


        public async Task<bool> AddUsernameToPlayerlist(PlayerListType list, params string[] usernames)
        {
            if(list == PlayerListType.BannedIPs) throw new ArgumentException();
            if(usernames.Any(username => username.Contains(" "))) throw new ArgumentException(); // TODO: Create 'bool ValidateUsername(string username)'

            return await AddStringsToPlayerlist(GetPlayerlistName(list), usernames.ToList());
        }
        public async Task<bool> AddIPsToBanlist(params string[] ips)
        {
            if(!ips.All(ip => IPAddress.TryParse(ip, out _))) throw new ArgumentException();

            return await AddStringsToPlayerlist(GetPlayerlistName(PlayerListType.BannedIPs), ips.ToList());
        }


        public async Task<bool> RemoveUsernameFromPlayerlist(PlayerListType list, params string[] usernames)
        {
            if(list == PlayerListType.BannedIPs) throw new ArgumentException();
            if(usernames.Any(username => username.Contains(" "))) throw new ArgumentException(); // TODO: Create 'bool ValidateUsername(string username)'

            return await RemoveStringsFromPlayerlist(GetPlayerlistName(list), usernames.ToList());
        }
        public async Task<bool> RemoveIPsFromBanlist(params string[] ips)
        {
            if(!ips.All(ip => IPAddress.TryParse(ip, out _))) throw new ArgumentException();

            return await RemoveStringsFromPlayerlist(GetPlayerlistName(PlayerListType.BannedIPs), ips.ToList());
        }



        private async Task<bool> AddStringsToPlayerlist(string list, List<string> strings)
        {
            var entries = new PlayersListRequest(strings);
            var json = JsonConvert.SerializeObject(entries);
            var response = await APIClient.PutRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/playerlists/{list}/", json);

            return response.IsSuccess;
        }
        private async Task<bool> RemoveStringsFromPlayerlist(string list, List<string> strings)
        {
            var entries = new PlayersListRequest(strings);
            var json = JsonConvert.SerializeObject(entries);
            var response = await APIClient.DeleteRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/playerlists/{list}/", json);

            return response.IsSuccess;
        }

        public async Task<List<string>> GetPlayersUsernamesAsync()
        {
            var s = await GetUpdateAsync();
            return s.PlayerUsernames;
        }

        public async Task<List<MinecraftPlayer>> GetPlayersAsync()
        {
            var s = await GetUpdateAsync();
            return s.Players;
        }

        

        private string GetPlayerlistName(PlayerListType list)
        {
            return listNames[(int)list];
        }
        private PlayerListType GetPlayerlistType(string name)
        {
            if(!listNames.Contains(name)) throw new ArgumentException();

            return (PlayerListType)listNames.ToList().IndexOf(name);
        }
        
        #endregion Player lists

        #region Files
        // cant test this rn due to This is only possible after you have purchased your first credits
        public async Task<string> ReadFileDataAsync(string filePath)
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/files/data/{filePath}/");

            return response.Data;
        }

        public async Task<FileInfo> ReadFileInfoAsync(string filePath)
        {
            var response = await APIClient.GetRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/files/info/{filePath}/");

            var fileInfo = response.BuildData<FileInfo>();

            return fileInfo;
        }

        // cant test this rn due to This is only possible after you have purchased your first credits
        public async Task<bool> WriteFileAsync(string filePath, string data)
        {
            var response = await APIClient.PutRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/files/data/{filePath}/", data);

            return response.IsSuccess;
        }

        // possibly change to Task<bool>
        public async Task<string> DeleteFileAsync(string filePath)
        {
            var response = await APIClient.DeleteRequestAsync($"https://api.exaroton.com/v1/servers/{ServerID}/files/data/{filePath}/");

            return response.Data;
            // return response.IsSuccess;
        }

        public static class Files
        {
            public const string ServerProperties = "server.properties";
            public const string Whitelist = "whitelist.json";
            public const string BannedIPs = "banned-ips.json";
        }
        #endregion Files



        [JsonConstructor]
        private Server(string serverID, string name, string address, string messageOfTheDay, ServerStatus status, string? host, int? port, ServerSoftware software, PlayerList players, bool shared)
        {
            ServerID = serverID;
            Name = name;
            Address = address;
            MessageOfTheDay = messageOfTheDay;
            Status = status;
            Host = host;
            Port = port;
            Software = software;
            PlayersList = players;
            Shared = shared;
        }
    }

    public enum PlayerListType
    {
        Whitelist,
        Ops,
        BannedPlayers,
        BannedIPs
    }

    public struct ServerSoftware
    {
        [JsonProperty("id")] public string Id { get; private set; }
        [JsonProperty("name")] public string Name { get; private set; }
        [JsonProperty("version")] public string Version { get; private set; }

        [JsonConstructor]
        private ServerSoftware(string id, string name, string version)
        {
            Id = id;
            Name = name;
            Version = version;
        }
    }

    // possibly TODO: make separate classes for files and directories, but tbh thats hardly needed
    public class FileInfo
    {
        [JsonProperty("path")] public string Path { get; private set; }
        [JsonProperty("name")] public string Name { get; private set; }
        [JsonProperty("isTextFile")] public bool IsTextFile { get; private set; }
        [JsonProperty("isConfigFile")] public bool IsConfigFile { get; private set; }
        [JsonProperty("isDirectory")] public bool IsDirectory { get; private set; }
        [JsonProperty("isLog")] public bool IsLog { get; private set; }
        [JsonProperty("isReadable")] public bool IsReadable { get; private set; }
        [JsonProperty("isWriteable")] public bool IsWriteable { get; private set; }
        [JsonProperty("size")] public int Size { get; private set; }
        [JsonProperty("children")] public List<FileInfo> Children { get; private set; } = new List<FileInfo>();


        [JsonConstructor]
        private FileInfo(string path, string name, bool isTextFile, bool isConfigFile, bool isDirectory, bool isLog, bool isReadable, bool isWriteable, int size, List<FileInfo> children)
        {
            Path = path;
            Name = name;
            IsTextFile = isTextFile;
            IsConfigFile = isConfigFile;
            IsDirectory = isDirectory;
            IsLog = isLog;
            IsReadable = isReadable;
            IsWriteable = isWriteable;
            Size = size;
            Children = children;

            if(children is null) Children = new List<FileInfo>();
        }
    }

    public class PlayerList
    {
        [JsonProperty("max")] public int Capacity { get; private set; }
        [JsonProperty("count")] public int Count { get; private set; }
        [JsonProperty("list")] public List<string> Players { get; private set; } = new List<string>();

        [JsonConstructor]
        private PlayerList(int capacity, int count, List<string> players)
        {
            Capacity = capacity;
            Count = count;
            Players = players;
        }
    }

    public class SharedLogs
    {
        [JsonProperty("id")] public string LogID { get; private set; }
        [JsonProperty("url")] public string LogUrl { get; private set; }
        [JsonProperty("raw")] public string LogRawUrl { get; private set; }

        [JsonConstructor]
        private SharedLogs(string logID, string logUrl, string logRawUrl)
        {
            LogID = logID;
            LogUrl = logUrl;
            LogRawUrl = logRawUrl;
        }
    }
}