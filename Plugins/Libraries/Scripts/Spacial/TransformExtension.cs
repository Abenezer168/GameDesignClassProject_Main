// Created By   :   Isaac Busatd
// Created      :   7/28/2026



using BugFreeProductions.Spatial;
using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Spatial
{
    public static class TransformExtension
    {
        #region Vars

        #endregion Vars


        #region Methods
        public static List<Vector3> TransformsToVector3s(List<Transform> aTFs)
        {
            List<Vector3> v3s = new List<Vector3>();

            foreach (Transform tf in aTFs)
            {
                v3s.Add(tf.position);
            }
            
            return v3s;
        }

        public static OrientationData OrientationData(this Transform aTF)
        {
            return new OrientationData(aTF.position,aTF.rotation);
        }
        #endregion Methods
    }
}