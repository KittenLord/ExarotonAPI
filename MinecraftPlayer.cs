using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;

namespace Exaroton
{
    public class MinecraftPlayer
    {
        public string Name { get; init; }
        public string ServerID { get; init; }

        private Func<Server>? Server { get; init; }

        private async Task<T> GetPlayerData<T>(string command, string wantedContent)
        {
            if(Server is null) throw new Exception();

            var server = Server();
            if(server is null) throw new Exception();
            //if(!server.PlayersList.Players.Contains(Name)) throw new Exception();

            var value = await server.ExecuteDataCommandAsync<T>(command, wantedContent);

            return value;
        }
        
        public async Task<Vector3> GetCoordinatesAsync()
        {
            var x = (float)await GetPlayerData<double>($"data get entity {Name} Pos[0]", $"{Name} has the following entity data: ");
            var y = (float)await GetPlayerData<double>($"data get entity {Name} Pos[1]", $"{Name} has the following entity data: ");
            var z = (float)await GetPlayerData<double>($"data get entity {Name} Pos[2]", $"{Name} has the following entity data: ");

            return new Vector3(x, y, z);
        }

        public MinecraftPlayer(string name, string serverID)
        {
            Name = name;
            ServerID = serverID;
            Server = null;
        }
        internal MinecraftPlayer(string name, string serverID, Func<Server> getServer)
        {
            Name = name;
            ServerID = serverID;
            Server = getServer;
        }
    }
}