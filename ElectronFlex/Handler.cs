using System;
using System.Threading;
using DaisyDisk;

namespace ElectronFlex
{
    public static class Handler
    {
        private static DiskUsage _diskUsage = new();
        private static DiskItem _item;
        private static DiskItem _currentItem;
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

        public static void SetById(long id)
        {
            if (_item == null) return;
            if (id == PieDataItemId.ROOT_ID && _currentItem == null) return;

            DiskItem newItem = null;
            switch (id)
            {
                case PieDataItemId.ROOT_ID:
                    newItem = _diskUsage.Parent(_item, _currentItem);
                    break;
                case PieDataItemId.OUTTER_OTHER_ID:
                case PieDataItemId.INNER_OTHER_ID:
                    var newData = _pieData.Find((int)id);
                    BrowserJs.Invoke($"window.vm.pie = {newData.ToJson()}");
                    return;
                    break;
                default:
                    newItem = _diskUsage.Find(_item, (int)id);
                    break;
            }
            if (newItem == null || newItem.Type == FileType.File) return;

            _currentItem = newItem;
            var pieData = PieData.FromDiskItem(_currentItem);
            BrowserJs.Invoke($"window.vm.pie = {pieData.ToJson()}");
        }
    }
}