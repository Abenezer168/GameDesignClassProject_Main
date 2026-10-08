// Created By   :   Isaac Bustad
// Created      :   6/30/2026


using UnityEngine;


// LifetimeTimingValue
namespace BugFreeProductions.Tools
{
    public struct LifetimeTimingValue
    {
        #region Vars
        public float scaleIncrease;
        public float timeToLive;
        #endregion Vars

        #region Constructor
        public LifetimeTimingValue(float aScaleIncrease, float aTime)
        {
            scaleIncrease = aScaleIncrease;
            timeToLive = aTime;

        }
        #endregion Constructor
    }
}