using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DaisyDisk
{
    public static class PieDataItemId
    {
        public const int ROOT_ID = 0;
        public const int OUTTER_OTHER_ID = -1;
        public const int INNER_OTHER_ID = -2;
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class PieDataItem
    {
        public int baseId;
        public int id;
        public string name;

        [System.Text.Json.Serialization.JsonIgnore]
        public long size;

        public int y;
        public List<PieDataItem> children;

        public PieDataItem Find(int id)
        {
            if (this.id == id) return this;
            if (children == null) return null;

            foreach (var child in children)
            {
                var result = child.Find(id);
                if (result != null) return result;
            }

            return null;
        }
    }

    [JsonObject]
    public class PieData
    {
        public int baseId;
        public string title;
        public string totalSize;
        public List<PieDataItem> data;

        public PieData Find(int id)
        {
            foreach (var item in data)
            {
                var result = item.Find(id);
                if (result == null) continue;

                return new PieData
                {
                    baseId = result.children.First().baseId,
                    title = $"{title}\\{result.name}",
                    totalSize = result.size.ToHumanReadable(),
                    data = result.children
                };
            }

            return null;
        }
        
        public static PieData FromDiskItem(DiskItem data)
        {
            var _pieData = new PieData
            {
                baseId = PieDataItemId.ROOT_ID,
                title = data.Path,
                totalSize = data.Size.ToHumanReadable(),
                data = new List<PieDataItem>()
            };

            var other = new PieDataItem
            {
                baseId = data.Id,
                id = PieDataItemId.OUTTER_OTHER_ID,
                name = "Others",
                children = new List<PieDataItem>(),
                y = 1,
            };

            var percent = 0;
            foreach (var child in data.Children.OrderByDescending(o => o.Size)
                .ThenBy(o => o.Type)
                .ThenBy(o => o.Path))
            {
                if (child.Size <= 0) continue;

                var name = Path.GetFileName(child.Path);
                var item = new PieDataItem
                {
                    baseId = data.Id,
                    id = child.Id,
                    name = $"{name} ({child.Size.ToHumanReadable()})",
                    size = child.Size,
                    y = (int) Math.Round((double) child.Size / data.Size * 10000, 0)
                };

                if (percent >= 9500)
                {
                    // if (item.y <= float.Epsilon) item.y = 10;
                    other.size += child.Size;
                    other.children.Add(item);
                    other.name = $"Others ({other.size.ToHumanReadable()})";
                    continue;
                }

                percent += item.y;

                if (child.Type == FileType.Directory)
                {
                    item.children = new List<PieDataItem>();
                    var subPercent = 0f;
                    var subOther = new PieDataItem
                    {
                        baseId = child.Id,
                        id = PieDataItemId.INNER_OTHER_ID,
                        name = $"{name}/Others",
                        y = 1,
                        children = new List<PieDataItem>()
                    };
                    foreach (var subChild in child.Children.OrderByDescending(o => o.Size)
                        .ThenBy(o => o.Type)
                        .ThenBy(o => o.Path))
                    {
                        if (subChild.Size <= 0) continue;

                        var subName = $"{name}/{Path.GetFileName(subChild.Path)}";
                        subName = subName.Replace("\r", "")
                            .Replace("\n", "");


                        var subItem = new PieDataItem
                        {
                            baseId = child.Id,
                            id = subChild.Id,
                            name = $"{subName} ({subChild.Size.ToHumanReadable()})",
                            size = subChild.Size,
                            y = (int) Math.Round((double) subChild.Size / data.Size * 10000, 0)
                        };

                        if (subPercent >= item.y * 0.95f) // || item.children.Count >= 10)
                        {
                            subOther.size += subChild.Size;
                            subOther.name = $"{name}/Others ({subOther.size.ToHumanReadable()})";
                            subOther.children.Add(subItem);
                            continue;
                        }

                        subPercent += subItem.y;

                        item.children.Add(subItem);
                    }

                    if (subOther.size > 0)
                    {
                        subOther.y = item.y - item.children.Sum(o => o.y);
                        item.children.Add(subOther);
                    }
                }

                _pieData.data.Add(item);
            }

            if (other.children.Any())
            {
                other.y = 10000 - _pieData.data.Sum(o => o.y);
                _pieData.data.Add(other);
            }

            return _pieData;
        }
    }
}