using System;

namespace DiceMaterials
{
    public static class DieMaterialMath
    {
        public static int ComputeFinalScore(int score, float multiplier)
            => (int)Math.Round(score * (double)multiplier, MidpointRounding.AwayFromZero);
    }
}
