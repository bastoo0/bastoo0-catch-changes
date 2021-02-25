// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;

namespace osu.Game.Rulesets.Catch.Difficulty.Skills
{
    public class Movement : Skill
    {
        private const float absolute_player_positioning_error = 16f;
        private const float normalized_hitobject_radius = 41.0f;

        protected override double SkillMultiplier => 900;
        protected override double StrainDecayBase => 0.2;

        protected override double DecayWeight => 0.94;

        protected readonly float HalfCatcherWidth;

        private float? lastPlayerPosition;
        private float lastDistanceMoved;

        public double DirectionChangeCount;
        private bool previousWasDirectionChange = false;


        public Movement(float halfCatcherWidth)
        {
            HalfCatcherWidth = halfCatcherWidth;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var catchCurrent = (CatchDifficultyHitObject)current;

            lastPlayerPosition ??= catchCurrent.LastNormalizedPosition;

            float playerPosition = Math.Clamp(
                lastPlayerPosition.Value,
                catchCurrent.NormalizedPosition - (normalized_hitobject_radius - absolute_player_positioning_error),
                catchCurrent.NormalizedPosition + (normalized_hitobject_radius - absolute_player_positioning_error)
            );

            float distanceMoved = playerPosition - lastPlayerPosition.Value;

            double weightedStrainTime = catchCurrent.StrainTime;

            // We do the base scaling according to the distance moved
            double distanceAddition = Math.Pow(Math.Abs(distanceMoved), 0.50) / 140;

            double edgeDashBonus = 0;

            // Bonus for edge dashes.
            if (catchCurrent.LastObject.DistanceToHyperDash <= 20.0f)
            {
                // Bonus increased
                if (!catchCurrent.LastObject.HyperDash)
                    edgeDashBonus += 3.2;
                else
                {
                    // After a hyperdash we ARE in the correct position. Always!
                    playerPosition = catchCurrent.NormalizedPosition;
                }

                distanceAddition *= Math.Min(5, 1.0 + edgeDashBonus * ((20 - catchCurrent.LastObject.DistanceToHyperDash) / 20) * Math.Pow(Math.Min(1.5 * catchCurrent.StrainTime, 265) / 265, 1.5) / catchCurrent.ClockRate); // Edge Dashes are easier at lower ms values            }
            }
            double distanceRatioBonus;
            // Gives weight to non-hyperdashes
            if (!catchCurrent.LastObject.HyperDash)
            {
                // Speed is the ratio between "1/strain time" and the distance moved

                //Give value to long and fast movements
                distanceRatioBonus = 2.5 * Math.Abs(distanceMoved) / weightedStrainTime;

                if (Math.Sign(distanceMoved) != Math.Sign(lastDistanceMoved) && Math.Sign(lastDistanceMoved) != 0 && Math.Abs(distanceMoved) > 4)
                {
                    DirectionChangeCount += 1;
                    distanceRatioBonus *= 4.8;

                    // Give value to short movements if multiple direction changes (for wiggles)
                    if (Math.Abs(distanceMoved) < 120)
                    {
                        distanceRatioBonus *= 1.22;
                        if (previousWasDirectionChange)
                        {
                            distanceRatioBonus += (catchCurrent.BaseObject.HyperDash ? 0.7 : 1) * Math.Log(120 / Math.Abs(distanceMoved), 1.40) * 280 / weightedStrainTime;
                        }
                    }
                    previousWasDirectionChange = true;
                }
                else previousWasDirectionChange = false;

            }
            else // Hyperdashes calculation
            {
                double antiflowFactor = Math.Max(Math.Min(70, Math.Abs(lastDistanceMoved)) / 70, 0.38) * 2;
                bool directionChanged = (Math.Sign(distanceMoved) != Math.Sign(lastDistanceMoved));
                bool bonusFactor = previousWasDirectionChange && directionChanged;
                distanceRatioBonus = Math.Log(4.2 * Math.Abs(distanceMoved) / weightedStrainTime * antiflowFactor * (bonusFactor ? 1.2 : 1) * (directionChanged ? (catchCurrent.BaseObject.HyperDash ? 1.6 : 1) : 0.6) + 0.7, 1.75) + 0.7;
                //distance scaling (long distances nerf)
                double scaledDistance = Math.Abs(distanceMoved) / (CatchPlayfield.WIDTH / 2);
                distanceRatioBonus *= Math.Min(-0.22 * Math.Abs(scaledDistance) + 1, 1);
                previousWasDirectionChange = directionChanged;
            }
            double distanceRatioBonusFactor = 4.95;


            distanceAddition *= distanceRatioBonusFactor * distanceRatioBonus;

            lastPlayerPosition = playerPosition;
            lastDistanceMoved = distanceMoved;

            return distanceAddition / weightedStrainTime;
        }
}
