using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TraderConsole
{
    public class Program
    {
        private static readonly object ConsoleLock = new object();
        static readonly UTF8Encoding Encoder = new UTF8Encoding();
        private const int ReceiveChunkSize = 256;
        private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(10000);
        private static readonly Uri ExchangeUrl = new Uri("wss://api.hitbtc.com/api/2/ws");

        // ReSharper disable once UnusedParameter.Local
        public static void Main(string[] args)
        {
            Thread.Sleep(1000);
            Connect(ExchangeUrl).Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static async Task Connect(Uri uri)
        {
            ClientWebSocket webSocket = null;

            try
            {
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(uri, CancellationToken.None);
                await Task.WhenAll(Receive(webSocket), Send(webSocket));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
            finally
            {
                webSocket?.Dispose();
                Console.WriteLine();

                lock (ConsoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("WebSocket closed.");
                    Console.ResetColor();
                }
            }
        }

        private static async Task Send(ClientWebSocket webSocket)
        {
            var requestObj = new WebSocketRequest
            {
                Method = "subscribeCandles",
                Params = new
                {
                    symbol = "ETHBTC",
                    period = "M1"
                },
                Id = Guid.NewGuid().ToString()
            };

            //var requestObj = new WebSocketRequest
            //{
            //    Method = "login",
            //    Params = new
            //    {
            //        algo = "BASIC",
            //        pKey = "da245ebae3a09d5887dec1d777666151",
            //        sKey = "b51e7a24599fd681febbfdf0972dbfc2"
            //    }
            //};

            byte[] buffer = Encoder.GetBytes(JsonConvert.SerializeObject(requestObj));
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

            LogStatus(false, buffer);

            //while (webSocket.State == WebSocketState.Open)
            //{
            //    LogStatus(false, buffer);
            //    await Task.Delay(Delay);
            //}
        }


        private static async Task Receive(ClientWebSocket webSocket)
        {
            byte[] buffer = new byte[ReceiveChunkSize];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    LogStatus(true, buffer);
                }
            }
        }

        private static void LogStatus(bool receiving, byte[] buffer)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = receiving ? ConsoleColor.Green : ConsoleColor.Gray;
                Console.WriteLine(Encoder.GetString(buffer));
                Console.ResetColor();
            }
        }
    }

    public class WebSocketRequest
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object Params { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
