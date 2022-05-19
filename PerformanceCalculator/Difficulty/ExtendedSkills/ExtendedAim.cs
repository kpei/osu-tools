// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using System.Collections.Generic;

namespace PerformanceCalculator.Difficulty.ExtendedSkills
{
    public class ExtendedAim : Aim, IExtendedSkill {

        public List<ObjectWithStrain> objectsWithStrain;

        private bool withSliders = false;
        public ExtendedAim(Mod[] mods, bool withSliders): base(mods, withSliders)
        {
            this.withSliders = withSliders;
            this.objectsWithStrain = new List<ObjectWithStrain>();
        }

        public ObjectWithStrain[] GetObjectsWithStrain()
        {
            return this.objectsWithStrain.ToArray();
        }

        public override string ToString(){
            var withSliders = this.withSliders ? "WithSliders" : "";
            return $"aim{withSliders}";
        }  

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            var strainValue = base.StrainValueAt(current);
            this.objectsWithStrain.Add(new ObjectWithStrain(current, strainValue));

            return strainValue;
        }
    }
}
