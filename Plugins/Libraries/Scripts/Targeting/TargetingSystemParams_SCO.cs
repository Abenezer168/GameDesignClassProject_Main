// Created By   :   Isaac Bustad
// Created      :   7/25/2026



using UnityEngine;



namespace BugFreeProductions
{
    [CreateAssetMenu(fileName = "TargetingSystemParams_SCO", menuName = "ScriptableObject/TargetingSystemParams_SCO")]
    public class TargetingSystemParams_SCO : ScriptableObject
    {
        #region Vars
        protected float targetingRange = 3f;
        protected float targetingInterval = 1;
        protected float targetingAngle = 45f;

        #endregion Vars



        #region Methods

        #endregion Methods



        #region Accessors
        public float TargetingRange
        {
            get
            {
                return targetingRange;
            }
        }

        public float TargetingInterval
        {
            get
            {
                return targetingInterval;
            }
        }

        public float TargetingAngle
        {
            get
            {
                return targetingAngle;
            }
        }

        #endregion Accessors
        
    }
}