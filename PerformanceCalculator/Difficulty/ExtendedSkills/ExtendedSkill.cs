// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using System.Collections.Generic;

namespace PerformanceCalculator.Difficulty.ExtendedSkills
{
    public interface IExtendedSkill
    {
        ObjectWithStrain[] GetObjectsWithStrain();
    }
}
