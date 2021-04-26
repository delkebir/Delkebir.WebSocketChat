using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using WebSocketState = System.Net.WebSockets.WebSocketState;

namespace Delkebir.WebSocketChat.Console
{



    class Program
    {
        static void Main(string[] args)
        {
            // here you can add as many consolles as you want, they are separated from eachother
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Count() <= 3)  // 2 consoles will appear
            {
                Process.Start("Delkebir.WebSocketChat.Console.exe");
            }

            StartWebsockets().GetAwaiter().GetResult();
        }

        public static async Task StartWebsockets()
        {
            var tokSrc = new CancellationTokenSource();
            var client = new ClientWebSocket();
            await client.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);
            System.Console.WriteLine($"Session is on");
            System.Console.WriteLine($"Web socket connection established @ {DateTime.UtcNow:F} ");

            var send = Task.Run(async () =>
            {
                string message;
                System.Console.Write($"Type a msg : ");
                while ((message = System.Console.ReadLine()) != null && message != string.Empty)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                }
                await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            });
            var receive = ReceiveAsync(client);
            await Task.WhenAll(send, receive);

            System.Console.WriteLine(@"You can't send msg anymore, Type ""exit<Enter>"" to quit... ");
            for (var inp = System.Console.ReadLine(); inp != "exit"; inp = System.Console.ReadLine())
            {
                await client.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(inp)),
                            WebSocketMessageType.Text,
                            true,
                            tokSrc.Token
                        );
                System.Console.WriteLine("Session off");
            }

            if (client.State == WebSocketState.Open)
            {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", tokSrc.Token);
            }
            tokSrc.Dispose();
            System.Console.WriteLine("WebSocket CLOSED");
        }

        public static async Task ReceiveAsync(ClientWebSocket client)
        {
            var buffer = new byte[1024 * 4];

            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                System.Console.Write($"Client 2 receive msg : ");
                System.Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
    }


}
