using System;
using System.Threading;
using DaisyDisk;

namespace ElectronFlex
{
    public static class Handler
    {
        private static DiskUsage _diskUsage = new();
        private static DiskItem _item;
        private static PieData _pieData;

        public static void Create(string path)
        {
            _item = _diskUsage.Create(path);
            _diskUsage.Fill(_item, async progress =>
            {
                await BrowserJs.Invoke($"window.vm.progress = {progress:F3}");
            });
            
            while (!_item.IsFilled) Thread.Sleep(100);
            Console.WriteLine($"process done: {path}");
            BrowserJs.Invoke($"window.vm.progress = 1");

            _pieData = PieData.FromDiskItem(_item);
            BrowserJs.Invoke($"window.vm.pie = {_pieData.ToJson()}");
        }
    }
}