// Created By   :   Isaac Bustad
// Created      :   7/2/2026



using UnityEngine;
using BugFreeProductions.Tools;
using Unity.VisualScripting;

namespace BugFreeProductions.Tools
{
    // Add Force
    public class AccelerationBoost_Trig_CMD_Client : Trig_CMD_Client
    {
        #region Vars
        // boostInDirection represents if the boost is applied to the object based on booster orientation
        [SerializeField] protected bool boostInDirection = true;
        
        [SerializeField] protected float boostValue = 50f;

        #endregion Vars



        #region Methods
        
        #endregion Methods     


        #region Accessors
        public virtual Vector3 BoostDirection
        {
            get
            {
                return transform.forward;
            }
        }

        public virtual bool BoostInDirection
        {
            get
            {
                return boostInDirection;
            }
        }

        public virtual float BoostValue
        {
            get
            {
                return boostValue;
            }
            
        }
        #endregion Accessors   
    }
}