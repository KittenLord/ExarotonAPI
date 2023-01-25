using System;
using Newtonsoft.Json;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using Exaroton;

namespace Exaroton
{
    public static class Program
    {
        public static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();

        public static async Task MainAsync()
        {
            var token = File.ReadAllText(@"secret\token.txt");
            var serverid = File.ReadAllText(@"secret\server.txt");
            


            //
            //// HTTP API
            //

            ExarotonClient client = new ExarotonClient(token);
            await client.LoginAsync();

            var account = await Account.GetAccount();

            var servers = await account.GetServers();
            var server = servers[0];
            server.SetWebsocketClient(client); // required for getting player's coordinates and executing commands which need response
                                               // you don't need to re-add it for server.GetUpdate(), it already does so

            Console.WriteLine(server.Name);
            Console.WriteLine(server.Status);
            //return;





            //
            //// Websocket API
            //

            var wsc = client.AddWebsocketClient(serverid);
            wsc.SetConsoleLines(500);
            wsc.OnMessage_ConsoleStream += (sender, e) =>
            {
                Console.WriteLine(e.Value);
            };

            // await wsc.Start();                      // fool-proof starting the socket
            await wsc.StartStream(StreamType.Console); // in StartStream()

            while(true)
            {
                await Task.Delay(500);



                //
                //// Monitoring players
                //

                var players = await server.GetPlayersAsync();

                foreach(var player in players) 
                {
                    var c = await player.GetCoordinatesAsync();
                    
                    Console.WriteLine($"{player.Name}: X-{c.X}, Y-{c.Y}, Z-{c.Z}");
                }



                //
                //// Executing commands and getting player's coordinates by request
                //

                // string text = Console.ReadLine() ?? ""; 
                // if(text == "") continue;

                // if(text.StartsWith("get"))
                // {
                //     if(text.Split(" ").Length != 2) continue;
                //     var username = text.Split(" ")[1];

                //     var user = server.Players.Find(p => p.Name == username);
                //     if(user is null) continue;

                //     var coordinates = await user.GetCoordinatesAsync();

                //     Console.WriteLine($"{coordinates.X}x {coordinates.Y}y {coordinates.Z}z");
                //     continue;
                // }

                // string commandResponse = await wsc.SendCommandToConsoleStream(text);
                // Console.WriteLine(commandResponse);
            }
        }
    }
}