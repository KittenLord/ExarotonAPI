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
            await Exaroton.LoginAsync(token);

            var server = await Server.GetServerAsync("cRrm2IIXvY06BqPX");
            var account = await Account.GetAccount();
            //await server.StartServerAsync();

            var response = server.Status.ToString();

            Console.WriteLine(response);
            //return;




            // Websocket API
            Exaroton.CreateWebSocketAPIClient(serverid, token); // just for showcase
            var cl = Exaroton.GetWebSocketAPIClient(serverid);

            cl.OnMessage_ConsoleStream += (sender, e) =>
            {
                Console.WriteLine(e.Value);
            };

            await cl.Start();

            await Task.Delay(500);
            // possibly implement some message queue
            cl.StartStream(StreamType.Console, 500);

            await Task.Delay(-1);
        }
    }
}