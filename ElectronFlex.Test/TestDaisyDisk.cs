using System;
using DaisyDisk;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ElectronFlex.Test
{
    public class TestDaisyDisk
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test()
        {
            var diskUsage = new DiskUsage();
            var diskItem = diskUsage.Create(@"E:\bin\ExcelCompare");
            diskUsage.Fill(diskItem, progress =>
            {
                Console.WriteLine($"{progress}");
            }).Wait();

            var pieData = PieData.FromDiskItem(diskItem);
            var json = JsonConvert.SerializeObject(pieData, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
}