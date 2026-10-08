// Created By   :   Isaac Bustad
// Created      :   7/25/2026



using UnityEngine;

namespace BugFreeProductions.Spatial
{
    public static class SpatialGeometryTool
    {
        public static float CalcAngleViaSides(float oppSide, float oth1, float oth2)
        {
            float top = (oppSide * oppSide) - (oth1 * oth1) - (oth2 * oth2);
            float bottom = -2 * oth1 * oth2;
            float fract = top / bottom;

            return Mathf.Acos(fract) * Mathf.Rad2Deg;
        }
    }
}