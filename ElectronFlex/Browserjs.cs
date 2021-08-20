using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ElectronFlex
{
    public static class BrowserJs
    {
        private static IdGenerator s_idGenerator = new IdGenerator();
        private static InvokeTaskManager s_taskManager = new InvokeTaskManager();
        
        public static Task Invoke(string jsCode)
        {
            return Invoke<IgnoreReturn>(jsCode);
        }
        
        public static Task<T> Invoke<T>(string jsCode)
        {
            var pack = new Pack
            {
                Id = s_idGenerator.Next(),
                Type = PackType.InvokeCode,
                Content = jsCode
            };
            var task = s_taskManager.Invoke<T>(pack);
            Send(pack);

            return task;
        }

        public static void Loop()
        {
            var stream = Config.WebSocketStream;
            while (true)
            {
                if (!stream.HasSizeForRead(sizeof(int)))
                {
                    Thread.Sleep(20);
                    continue;
                }
                
                var size = stream.ReadInt32();
                if (!stream.HasSizeForRead(size))
                {
                    stream.UnReadInt32();
                    Thread.Sleep(20);
                    continue;
                }

                var packBuff = stream.ReadBytes(size);
                var pack = Pack.Decode(packBuff);
                Console.WriteLine($">>>>>>> WebSocket.Recv: {pack}");

                try
                {
                    switch (pack.Type)
                    {
                        case PackType.InvokeCode:
                            InvokeFromBrowser(pack);
                            break;
                        case PackType.InvokeResult:
                            s_taskManager.Result(pack);
                            break;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine($"BrowserJs.Loop Error: {err}");
                }
            }
        }

        private static void InvokeFromBrowser(Pack pack)
        {
            var result = s_taskManager.DoInvoke(pack);
            Send(new Pack
            {
                Id = pack.Id,
                Type = PackType.InvokeResult,
                Content = JsonConvert.SerializeObject(result)
            });
        }

        public static void WriteLine(string? line)
        {
            line = line?.TrimEnd('\n');
            line = line?.TrimEnd('\r');
            var pack = new Pack
            {
                Id = s_idGenerator.Next(),
                Type = PackType.ConsoleOutput,
                Content = line
            };

            Send(pack);
        }

        private static void Send(Pack pack)
        {
            Console.WriteLine($"<<<<<<< WebSocket.Send: {pack}");
            var ipPort = Config.WebSocketServer.ListClients().FirstOrDefault();
            Config.WebSocketServer.SendAsync(ipPort, pack.Encode());
        }
    }
}