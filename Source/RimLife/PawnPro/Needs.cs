using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimLife
{
    public class NeedsInfo
    {
        // 所有的需求都放入这个列表，不再区分 Food/Rest
        public List<NeedEntry> AllNeeds = new List<NeedEntry>();
    }

    public struct NeedEntry
    {
        public string DefName;      // 需求ID (e.g. "Food", "Beauty")
        public string Label;        // 显示名
        public float CurLevel;      // 当前值 (0-1)
        public float ThresholdLow;  // 低于此值视为匮乏 (从 XML 读取)
        public bool IsCritical;     // 是否处于极低状态 (Extractor 预判)
    }
}
