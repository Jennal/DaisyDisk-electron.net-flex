using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ElectronFlex
{
    public class InvokeData
    {
        public string Class;
        public string Method;
        public object[] Arguments;
    }

    public class InvokeTaskManager
    {
        public ConcurrentDictionary<byte, object> _dict = new ConcurrentDictionary<byte, object>();

        public Task<T> Invoke<T>(Pack pack)
        {
            var task = new TaskCompletionSource<T>();
            _dict[pack.Id] = task;
            return task.Task;
        }

        public void Result(Pack pack)
        {
            if (pack.Type != PackType.InvokeResult) return;
            if (!_dict.TryRemove(pack.Id, out var obj)) return;
            if (obj.GetType().GenericTypeArguments.Length <= 0) return;

            var resultType = obj.GetType().GenericTypeArguments[0];
            var setResultMethod = typeof(TaskCompletionSource<>).MakeGenericType(resultType)
                .GetMethod(nameof(TaskCompletionSource.SetResult));

            if (resultType != typeof(IgnoreReturn))
            {
                var jsonConvertMethod = typeof(JsonConvert).GetGenericMethod(nameof(JsonConvert.DeserializeObject),
                    new[] {resultType}, typeof(string));
                var result = jsonConvertMethod.Invoke(null, new object?[] {pack.Content});
                setResultMethod!.Invoke(obj, new[] {result});
            }
            else
            {
                setResultMethod!.Invoke(obj, new object[] {null});
            }
        }

        public object DoInvoke(Pack pack)
        {
            var invoke = JsonConvert.DeserializeObject<InvokeData>(pack.Content);
            if (invoke == null)
            {
                return new InvokeError(null, "wrong invoke structure");
            }

            var (ns, _) = SplitNsClass(invoke?.Class);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (!string.IsNullOrEmpty(ns))
                assemblies = assemblies.Where(o => o.GetName().Name?.StartsWith(ns) ?? false).ToArray();
            // else assemblies = new[] {typeof(BrowserInvoke).Assembly};

            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(invoke.Class);
                if (type == null) continue;

                var method = type.GetMethod(invoke.Method, BindingFlags.Static | BindingFlags.Public, invoke.Arguments);
                var result = method.Invoke(null, invoke.Arguments);
                result = UnPackResult(result);
                return result;
            }

            return new InvokeError(invoke, "can't find method");
        }

        public static object? UnPackResult(object? result)
        {
            if (result == null) return result;
            var type = result.GetType();
            if (type.Name != "Task" && !type.Name.StartsWith("Task`")) return result;

            var method = type.GetMethod("Wait", BindingFlags.Instance | BindingFlags.Public, new object[0]);
            method.Invoke(result, null);

            var propertyInfo = type.GetProperty("Result");
            if (propertyInfo == null) return null;

            result = propertyInfo.GetValue(result);
            return UnPackResult(result);
        }

        private static Tuple<string, string> SplitNsClass(string invokeClass)
        {
            if (string.IsNullOrEmpty(invokeClass)) return null;

            var idx = invokeClass.LastIndexOf(".");
            return new Tuple<string, string>(
                invokeClass.Substring(0, idx),
                invokeClass.Substring(idx + 1, invokeClass.Length - idx - 1)
            );
        }
    }
}