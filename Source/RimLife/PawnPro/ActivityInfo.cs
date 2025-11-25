using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimLife
{

    public class ActivityInfo
    {
        public string Posture;             // 姿态
        public List<ActivityEntry> Activities = new List<ActivityEntry>();
    }

    public struct ActivityEntry
    {
        public string JobDefName;       // 工作ID
        public string JobReport;    // 工作描述文本 (e.g. "Hauling steel")
    }
}
