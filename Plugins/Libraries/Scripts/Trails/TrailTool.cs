// Created By   :   Isaac Bustad
// Created      :   5/12/2026



using System.Collections.Generic;
using System.Numerics;

namespace BugFreeProductions.Tools
{
    public static class TrailTool
    {
        #region Methods
        public static void AddVectorToList(Dictionary<int, List<Vector3>> aTrailsByLocation, List<List<Vector3>> allPos, List<Vector3> aLocs, List<Vector3> aCenterLocs, Vector3 aCenterLoc, int aMaxNumberOfPoints)
        {
             // hold a list of all position list
            allPos = new List<List<Vector3>>();

            foreach (KeyValuePair<int, List<Vector3>> aKVP in aTrailsByLocation)
            {
                // add a single position
                aKVP.Value.Add(aLocs[aKVP.Key]);

                while(aKVP.Value.Count > aMaxNumberOfPoints)
                {
                    aKVP.Value.RemoveAt(0);
                }

                // update all positions
                allPos.Add(aKVP.Value);
                
            }

            aCenterLocs.Add(aCenterLoc);

            while (aCenterLocs.Count > aMaxNumberOfPoints)
            {
                aCenterLocs.RemoveAt(0);
            }

        }
        #endregion
    }
}