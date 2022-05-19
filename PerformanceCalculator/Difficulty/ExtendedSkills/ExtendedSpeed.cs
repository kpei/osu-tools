// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using System.Collections.Generic;

namespace PerformanceCalculator.Difficulty.ExtendedSkills
{
    public class ExtendedSpeed : Speed, IExtendedSkill {

        private List<ObjectWithStrain> objectsWithStrain;
        public ExtendedSpeed(Mod[] mods, double hitWindowGreat): base(mods, hitWindowGreat)
        {
            this.objectsWithStrain = new List<ObjectWithStrain>();
        }

        public ObjectWithStrain[] GetObjectsWithStrain()
        {
            return this.objectsWithStrain.ToArray();
        }

        public override string ToString(){
            return "speed";
        }  

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            var strainValue = base.StrainValueAt(current);
            this.objectsWithStrain.Add(new ObjectWithStrain(current, strainValue));

            return strainValue;
        }
    }
}
