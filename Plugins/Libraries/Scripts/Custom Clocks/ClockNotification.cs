// Created By   :   Isaac Bustad
// Created      :   7/5/2026


using UnityEngine;

namespace BugFreeProductions.Tools
{
    public class ClockNotification : ISubscriberNotification
    {
        #region Vars
        public float timeInterval;
        public bool isPaused;

        #endregion Vars


        #region Constructors
        public ClockNotification(float aTimeInterval, bool aPauseStatus)
        {
            timeInterval = aTimeInterval;

            isPaused = aPauseStatus;
        }
        #endregion Constructors
    }
}