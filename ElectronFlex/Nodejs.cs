using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ElectronFlex
{
    class IgnoreReturn
    {}

    public static class NodeJs
    {
        private static IdGenerator s_idGenerator = new IdGenerator();
        private static InvokeTaskManager s_taskManager = new InvokeTaskManager();
        
        public static Task Invoke(string jsCode)
        {
            return Invoke<IgnoreReturn>(jsCode);
        }
        
        public static Task<T> Invoke<T>(string jsCode)
        {
            if (!Config.CommandLineOptions?.StartFromElectron ?? true)
            {
                Console.WriteLine($"[nodejs] {jsCode}");
                return default;
            }
            
            var pack = new Pack
            {
                Id = s_idGenerator.Next(),
                Type = PackType.InvokeCode,
                Content = jsCode
            };

            using var bw = new BinaryWriter(Console.OpenStandardOutput());
            bw.Write(pack.Encode());
            bw.Flush();

            return s_taskManager.Invoke<T>(pack);
        }

        public static void Loop()
        {
            var inputStream = Console.OpenStandardInput();
            using var br = new BinaryReader(inputStream);

            while (true)
            {
                var length = br.ReadInt32();
                var data = br.ReadBytes(length);
                var pack = Pack.Decode(data);
                try
                {
                    switch (pack.Type)
                    {
                        case PackType.ConsoleOutput:
                            /* DO NOTHING */
                            break;
                        case PackType.InvokeCode:
                            InvokeCode(pack);
                            break;
                        case PackType.InvokeResult:
                            s_taskManager.Result(pack);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine($"NodeJs.Loop Error: {err}");
                }
            }
        }

        public static void InvokeCode(Pack pack)
        {
            var result = s_taskManager.DoInvoke(pack);
            var retPack = new Pack
            {
                Id = pack.Id,
                Type = PackType.InvokeResult,
                Content = JsonConvert.SerializeObject(result)
            };
            
            using var bw = new BinaryWriter(Console.OpenStandardOutput());
            bw.Write(retPack.Encode());
            bw.Flush();
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

            using var bw = new BinaryWriter(Console.OpenStandardOutput());
            bw.Write(pack.Encode());
            bw.Flush();
        }
    }
}