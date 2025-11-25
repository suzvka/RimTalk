using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RimLife
{
    public class PerspectiveInfo
    {
        // 统一环境信息
        public EnvironmentPro Environment { get; set; }

        // 可见事物快照列表 (不包含 Pawn)
        public List<ThingSnapshot> VisibleThings { get; set; } = new List<ThingSnapshot>();

        // 统一的可见 Pawn 快照列表：所有在视野与视线(LineOfSight)内的 Pawn（不再拆分）
        public List<PawnRelationSnapshot> VisiblePawnSnapshots { get; set; } = new List<PawnRelationSnapshot>();

        // 接口：获取“可见但不可识别的物种及数量” (距离 > RecognizableRange)
        public IEnumerable<string> GetUnrecognizableSpeciesCounts()
        {
            return VisiblePawnSnapshots
                .Where(p => p.Distance > PerspectiveExtractor.RecognizableRange)
                .GroupBy(p => p.DefName)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}:{g.Count()}");
        }

        // 接口：获取“可识别的 Pawn ID” (距离 ≤ RecognizableRange)
        public IEnumerable<string> GetRecognizablePawnIDs()
        {
            return VisiblePawnSnapshots
                .Where(p => p.Distance <= PerspectiveExtractor.RecognizableRange)
                .Select(p => p.ID);
        }
    }

    // 物体快照 (建筑、物品、污秽等)；不包含任何生物。生物统一在 VisiblePawnSnapshots 中表示。
    public struct ThingSnapshot
    {
        public string DefName;
        public string Label;      // e.g. "Grand sculpture"
        public string Category;   // e.g. "Building", "Item", "Filth", "Food"
        public float Distance;    // 离观察者的距离
        public string Quality;    // e.g. "Legendary" (如果有)
    }

    // Pawn 基础快照：仅保存物种与基础身份信息（不含社交关系/敌对状态等动态高层语义）。
    public struct PawnRelationSnapshot
    {
        public string ID;         // ThingID
        public string Name;       // 显示名（可能用于 UI 或上层进一步索引）
        public string DefName;    // 种族定义名
        public float Distance;    // 与观察者距离
    }

    public static class PerspectiveExtractor
    {
        // --- Constants (对外公开，供 PerspectiveInfo 的接口方法使用) ---
        public const float RecognizableRange = 13f; // 识别详细个体信息的有效距离阈值
        public const float VisualRange = 26f;       // 最大视野范围 (用于初步捕获)

        public static PerspectiveInfo Capture(Pawn pawn)
        {
            var snapshot = new PerspectiveInfo();

            // 1. 基础校验
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return snapshot;

            Map map = pawn.Map;

            // 2. 环境分析 (统一使用 EnvironmentPro)
            snapshot.Environment = new EnvironmentPro(pawn);

            // 3. 提取可见物体
            IEnumerable<Thing> candidateThings;
            if (snapshot.Environment.Type == EnvironmentType.Indoors)
            {
                // 室内环境，直接从房间获取物体列表
                // 注意：EnvironmentPro 内部已经处理了 room 的 null 判断
                Room room = pawn.GetRoom(); // 需要原始 Room 对象来获取物体
                if (room != null)
                {
                    candidateThings = room.ContainedAndAdjacentThings;
                }
                else
                {
                    // 理论上 Type 是 Indoors 时 room 不会为 null，作为安全保障
                    candidateThings = Enumerable.Empty<Thing>();
                }
            }
            else
            {
                // 室外环境，扫描 pawn 周围
                candidateThings = GenRadial.RadialDistinctThingsAround(pawn.Position, map, VisualRange, true);
            }

            foreach (var t in candidateThings)
            {
                if (t == pawn || t is Pawn || t.def.mote != null) continue;

                // 检查视线
                bool canSee = GenSight.LineOfSight(pawn.Position, t.Position, map, skipFirstCell: true);
                if (!canSee) continue;

                var ts = new ThingSnapshot
                {
                    DefName = t.def.defName,
                    Label = t.Label,
                    Category = GetCategoryLabel(t),
                    Distance = t.Position.DistanceTo(pawn.Position)
                };

                if (t.TryGetQuality(out QualityCategory qc))
                    ts.Quality = qc.ToString();

                snapshot.VisibleThings.Add(ts);
            }

            // 4. 捕获所有视野内可见 Pawn（统一放入 VisiblePawnSnapshots）
            var allPawns = map.mapPawns.AllPawnsSpawned;
            foreach (var target in allPawns)
            {
                if (target == pawn) continue;

                float dist = target.Position.DistanceTo(pawn.Position);
                if (dist > VisualRange) continue;
                if (!GenSight.LineOfSight(pawn.Position, target.Position, map, skipFirstCell: true)) continue;

                snapshot.VisiblePawnSnapshots.Add(CreatePawnSnapshot(target, dist));
            }

            return snapshot;
        }

        private static PawnRelationSnapshot CreatePawnSnapshot(Pawn target, float dist)
        {
            return new PawnRelationSnapshot
            {
                ID = target.ThingID,
                Name = target.Name.ToStringShort,
                DefName = target.def.defName,
                Distance = dist
            };
        }

        private static string GetCategoryLabel(Thing t)
        {
            if (t is Building) return "Building";
            if (t.def.IsFilth) return "Filth";
            if (t.def.IsIngestible) return "Food";
            return "Item";
        }
    }
}
