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
            



            // HTTP API
            ExarotonClient client = new ExarotonClient(token);
            await client.LoginAsync();

            var account = await Account.GetAccount();

            var servers = await account.GetServers();
            var server = servers[0];

            Console.WriteLine(server.Name);
            Console.WriteLine(server.Status);

            //return;




            // Websocket API
            var wsc = client.CreateWebsocketClient(serverid);
            wsc.SetConsoleLines(500);
            wsc.OnMessage_ConsoleStream += (sender, e) =>
            {
                Console.WriteLine(e.Value);
            };

            // await wsc.Start();
            await wsc.StartStream(StreamType.Console); // child-proof starting the socket

            while(true)
            {
                string text = Console.ReadLine() ?? "";
                if(text == "") continue;

                await wsc.SendCommandToConsoleStream(text); // maybe create response finder, but im not sure, if minecraft organizes them properly
            }
        }
    }
}