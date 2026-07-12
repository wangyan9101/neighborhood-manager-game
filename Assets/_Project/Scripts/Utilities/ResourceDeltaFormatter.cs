using System.Collections.Generic;
using NeighborhoodManager.Models;

namespace NeighborhoodManager.Utilities
{
    public static class ResourceDeltaFormatter
    {
        public static string Format(ResourceDelta delta)
        {
            var parts = new List<string>(4);
            Add(parts, "预算", delta.Budget);
            Add(parts, "满意度", delta.Satisfaction);
            Add(parts, "投诉", delta.Complaint);
            Add(parts, "设备健康", delta.FacilityHealth);
            return parts.Count == 0 ? "无资源变化" : string.Join("，", parts);
        }

        private static void Add(List<string> parts, string label, int value)
        {
            if (value == 0)
            {
                return;
            }

            parts.Add(value > 0 ? $"{label} +{value}" : $"{label} {value}");
        }
    }
}
