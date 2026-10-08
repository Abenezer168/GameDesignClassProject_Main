// Created By   :   Isaac Bustad
// Created      :   7/5/2026




using System.Collections.Generic;
using UnityEngine;


namespace BugFreeProductions.Tools
{
    public class ClockManager : Singleton<ClockManager>
    {
        #region Vars
        [SerializeField] protected List<Clock_SCO> clocks = new List<Clock_SCO>();

        #endregion Vars


        #region Methods
        protected override void Awake()
        {
            base.Awake();
            foreach (Clock_SCO clock in clocks)
            {
                clock.Setup();
            }
        }
        protected virtual void LateUpdate()
        {
            RunClocks();
        }
        protected virtual void RunClocks()
        {
            foreach (Clock_SCO clock  in clocks)
            {
                clock.RunClock(Time.deltaTime);
            }
        }

        #endregion Methods
    }
}