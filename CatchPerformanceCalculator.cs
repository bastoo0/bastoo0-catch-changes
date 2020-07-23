// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace osu.Game.Rulesets.Catch.Difficulty
{
    public class CatchPerformanceCalculator : PerformanceCalculator
    {
        protected new CatchDifficultyAttributes Attributes => (CatchDifficultyAttributes)base.Attributes;

        private Mod[] mods;

        private int fruitsHit;
        private int ticksHit;
        private int tinyTicksHit;
        private int tinyTicksMissed;
        private int misses;

        public CatchPerformanceCalculator(Ruleset ruleset, WorkingBeatmap beatmap, ScoreInfo score)
            : base(ruleset, beatmap, score)
        {
        }

        public override double Calculate(Dictionary<string, double> categoryDifficulty = null)
        {
            mods = Score.Mods;

            fruitsHit = Score?.GetCount300() ?? Score.Statistics[HitResult.Perfect];
            ticksHit = Score?.GetCount100() ?? 0;
            tinyTicksHit = Score?.GetCount50() ?? 0;
            tinyTicksMissed = Score?.GetCountKatu() ?? 0;
            misses = Score.Statistics[HitResult.Miss];

            // Don't count scores made with supposedly unranked mods
            if (mods.Any(m => !m.Ranked))
                return 0;

            // We are heavily relying on aim in catch the beat
            double value = Math.Pow(5.0 * Math.Max(1.0, Attributes.StarRating / 0.0049) - 4.0, 2.0) / 100000.0;

            // Longer maps are worth more. "Longer" means how many hits there are which can contribute to combo
            int numTotalHits = totalComboHits();

            // Longer maps with more movements are worth more
            double lengthBonusFactor = numTotalHits * 0.75 + (float)Attributes.DirectionChangeCount / 1.8;

            double lengthBonus = Math.Log10(lengthBonusFactor + 100) * 0.75 - 1.3;

            // Longer maps are worth more
            value *= lengthBonus;

            // Penalize misses exponentially. This mainly fixes tag4 maps and the likes until a per-hitobject solution is available
            value *= Math.Pow(0.97, misses);

            // Combo scaling
            if (Attributes.MaxCombo > 0)
                value *= Math.Min(Math.Pow(Score.MaxCombo, 0.8) / Math.Pow(Attributes.MaxCombo, 0.8), 1.0);

            float approachRate = (float)Attributes.ApproachRate;
            float approachRateFactor = 1.0f;
            if (approachRate > 9.0f)
                approachRateFactor += 0.1f * (approachRate - 9.0f); // 10% for each AR above 9
            if (approachRate > 10.0f)
                approachRateFactor += 0.1f * (approachRate - 10.0f); // Additional 10% at AR 11, 30% total
            else if (approachRate < 8.0f)
                approachRateFactor += 0.04f * (8.0f - approachRate); // 4% for each AR below 8

            value *= approachRateFactor;

            if (mods.Any(m => m is ModHidden))
            {
                value *= 1 + 0.2 * (9.8 - Math.Min(9.8, Attributes.ApproachRate)); // 20% for each AR below 10
            }

            if (mods.Any(m => m is ModFlashlight))
            {
                // Apply length bonus again if flashlight is on simply because it becomes a lot harder on longer maps.
                value *= 1.35 * lengthBonus;

                if (approachRate > 8.0f)
                    approachRateFactor += 0.1f * (approachRate - 8.0f); // 10% for each AR above 8
                if (approachRate < 8.0f)
                    approachRateFactor -= 0.07f * (approachRate - 8.0f); // -7% for each AR below 8
            }


            // Scale the aim value with accuracy _slightly_
            value *= Math.Pow(accuracy(), 7.5);

            // Custom multiplier for HalfTime -> slower catcher = easier to control
            if (mods.Any(m => m is ModHalfTime))
                value *= 0.85;

            // Custom multiplier for HalfTime -> faster catcher = harder to control
            if (mods.Any(m => m is ModDoubleTime))
                value *= 1.10;

            // Custom multipliers for NoFail. SpunOut is not applicable.
            if (mods.Any(m => m is ModNoFail))
                value *= 0.90;

            return value;
        }

        private float accuracy() => totalHits() == 0 ? 0 : Math.Clamp((float)totalSuccessfulHits() / totalHits(), 0, 1);
        private int totalHits() => tinyTicksHit + ticksHit + fruitsHit + misses + tinyTicksMissed;
        private int totalSuccessfulHits() => tinyTicksHit + ticksHit + fruitsHit;
        private int totalComboHits() => misses + ticksHit + fruitsHit;
    }
}
