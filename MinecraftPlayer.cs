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

        private async Task<T> GetPlayerData<T>(string command)
        {
            var server = await Server.GetServerAsync(ServerID);
            if(!server.PlayersList.Players.Contains(Name)) throw new Exception();

            var value = await server.ExecuteDataCommandAsync<T>(command);

            return value;
        }
        
        public async Task<Vector3> GetCoordinatesAsync()
        {
            var x = await GetPlayerData<float>($"data get entity {Name} Pos[0]");
            var y = await GetPlayerData<float>($"data get entity {Name} Pos[1]");
            var z = await GetPlayerData<float>($"data get entity {Name} Pos[2]");

            return new Vector3(x, y, z);
        }

        public MinecraftPlayer(string name, string serverID)
        {
            Name = name;
            ServerID = serverID;
        }
    }
}