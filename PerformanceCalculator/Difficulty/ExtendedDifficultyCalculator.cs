// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using PerformanceCalculator.Difficulty.ExtendedSkills;

namespace PerformanceCalculator.Difficulty
{
    public interface IExtendedDifficultyCalculator
    {
        Skill[] GetSkills();
        DifficultyHitObject[] GetDifficultyHitObjects(IBeatmap beatmap, double clockRate);
    }

    public class SkillWithStrains {
        public Skill skill;
        public double[] strains;

        public SkillWithStrains(Skill skill, double[] strains) {

        }
    }

    public class ExtendedOsuDifficultyCalculator : OsuDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills;

        public ExtendedOsuDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap) : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;
        public DifficultyHitObject[] GetDifficultyHitObjects(IBeatmap beatmap, double clockRate) => CreateDifficultyHitObjects(Beatmap, clockRate).ToArray();

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            this.skills = skills;
            return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            base.CreateSkills(beatmap, mods, clockRate);
            HitWindows hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            var hitWindowGreat = hitWindows.WindowFor(HitResult.Great) / clockRate;

            return new Skill[]
            {
                new ExtendedAim(mods, true),
                new ExtendedAim(mods, false),
                new ExtendedSpeed(mods, hitWindowGreat),
                new ExtendedFlashlight(mods)
            };
        }
    }

    public class ExtendedTaikoDifficultyCalculator : TaikoDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills;

        public ExtendedTaikoDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap) : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;
        public DifficultyHitObject[] GetDifficultyHitObjects(IBeatmap beatmap, double clockRate) => CreateDifficultyHitObjects(beatmap, clockRate).ToArray();

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            this.skills = skills;
            return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
        }
    }

    public class ExtendedCatchDifficultyCalculator : CatchDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills;

        public ExtendedCatchDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap) : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;
        public DifficultyHitObject[] GetDifficultyHitObjects(IBeatmap beatmap, double clockRate) => CreateDifficultyHitObjects(beatmap, clockRate).ToArray();

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            this.skills = skills;
            return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
        }
    }

    public class ExtendedManiaDifficultyCalculator : ManiaDifficultyCalculator, IExtendedDifficultyCalculator
    {
        private Skill[] skills;

        public ExtendedManiaDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap) : base(ruleset, beatmap)
        {
        }

        public Skill[] GetSkills() => skills;
        public DifficultyHitObject[] GetDifficultyHitObjects(IBeatmap beatmap, double clockRate) => CreateDifficultyHitObjects(beatmap, clockRate).ToArray();

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            this.skills = skills;
            return base.CreateDifficultyAttributes(beatmap, mods, skills, clockRate);
        }
    }
}
