using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimLife
{
	/// <summary>
	/// Represents a snapshot of a Pawn's skills, including level and passion.
	/// Note: This data is a snapshot and its temporal consistency is not guaranteed.
	/// </summary>
	public class SkillsInfo
	{
		/// <summary>
		/// All skills of the pawn with level and passion information.
		/// Empty when the pawn has no skills (e.g., non-humanlike).
		/// </summary>
		public IReadOnlyList<SkillEntry> AllSkills { get; }

		private SkillsInfo()
		{
			AllSkills = new List<SkillEntry>();
		}

		private SkillsInfo(IReadOnlyList<SkillEntry> allSkills)
		{
			AllSkills = allSkills;
		}

		/// <summary>
		/// Creates a skills snapshot from a Pawn. Must be called on the main thread.
		/// </summary>
		public static SkillsInfo CreateFrom(Pawn p)
		{
			if (p?.skills == null) return new SkillsInfo();

			var list = new List<SkillEntry>();
			var skills = p.skills.skills; // RimWorld supplies a list of SkillRecord
			if (skills != null)
			{
				foreach (var sr in skills)
				{
					if (sr == null || sr.def == null) continue;
					Passion passion;
					try { passion = sr.passion; }
					catch { passion = Passion.None; }

					bool hasPassion = passion != Passion.None;

					string label = sr.def.label ?? sr.def.defName; // label is fine for display; defName as fallback

					list.Add(new SkillEntry
					{
						DefName = sr.def.defName,
						Label = label,
						Level = sr.Level,
						Passion = passion.ToString(),
						HasPassion = hasPassion,
						TotallyDisabled = sr.TotallyDisabled
					});
				}
			}

			return new SkillsInfo(list);
		}

		/// <summary>
		/// Asynchronously creates a SkillsInfo snapshot by dispatching the work to the main thread.
		/// </summary>
		public static Task<SkillsInfo> CreateFromAsync(Pawn p)
		{
			if (p == null) return Task.FromResult(new SkillsInfo());
			return MainThreadDispatcher.EnqueueAsync(() => CreateFrom(p));
		}
	}

	public struct SkillEntry
	{
		public string DefName; // 技能ID (e.g. "Shooting")
		public string Label; // 显示名
		public int Level; // 数值等级 (0-20)
		public string Passion; // 热情：None / Minor / Major
		public bool HasPassion; // 是否有热情
		public bool TotallyDisabled; // 是否彻底禁用
	}
}
