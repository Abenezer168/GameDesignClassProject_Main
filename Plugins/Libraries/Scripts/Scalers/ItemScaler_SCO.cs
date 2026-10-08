// Created By   :   Isaac Bustad
// Created      :   7/14/2026


using UnityEngine;

namespace BugFreeProductions.Tools
{
    [CreateAssetMenu(fileName = "ItemScaler_SCO", menuName = "ScriptableObject/ItemScaler_SCO")]
    public class ItemScaler_SCO : ScriptableObject
    {
        #region Vars
        [SerializeField] protected Vector3 maxScale = Vector3.one; 
        [SerializeField] protected Vector3 minScale = Vector3.one;
        [SerializeField] protected Vector3 startingScale = Vector3.one;

        protected Vector3 targetScale = Vector3.one;

        // how far from the max or min whe can stop
        [Range(1,2), SerializeField] protected float bufferScale = 0.02f;

        // rate of change in seconds
        // to multiply this with vector to adjust speed of scaling
        [SerializeField] protected float rateOfChange;

        #endregion Vars


        #region Methods
        
        #region Unity Methods
        

        #endregion Unity Methods
        public virtual void ScaleToStart(Transform aScalable)
        {
            aScalable.localScale = startingScale;
        }

        public virtual void ScaleObject(Transform aScalable)
        {
            // Test line for scaler math
            targetScale = maxScale;
            
            // math
            // Scale towards a target scale

            Vector3 curScale = aScalable.localScale;

            // to - from for scaling direction
            // normalize for easy rat of change control
            Vector3 scaleDir = (targetScale - aScalable.localScale).normalized;


            // new val to represent scale direction and rate
            Vector3 rateScale = scaleDir * rateOfChange;

            // only change if we are outside the buffer            
            if (Vector3.Distance(targetScale, curScale) > ScaleBuffer)
            {
                aScalable.transform.localScale = curScale + rateScale;
            }
        }

        public virtual bool ScaleObject(Transform aScalable, bool aScaleMax)
        {            
            // math
            // Scale towards a target scale
            targetScale = TargetScale(aScaleMax);

            Vector3 curScale = aScalable.localScale;

            // to - from for scaling direction
            // normalize for easy rat of change control
            Vector3 scaleDir = (targetScale - aScalable.localScale).normalized;


            // new val to represent scale direction and rate
            Vector3 rateScale = scaleDir * rateOfChange;

            // only change if we are outside the buffer            
            if (Vector3.Distance(targetScale, curScale) > ScaleBuffer)
            {
                aScalable.transform.localScale = curScale + rateScale;
                return aScaleMax;
            }

            else
            {
                return !aScaleMax;
            }
        }

        protected virtual Vector3 TargetScale(bool aScaleMax)
        {
            if (aScaleMax)
            {
                return maxScale;
            }
            return minScale;
        }
        #endregion Methods

        #region Accessors
        protected virtual float ScaleBuffer
        {
            get
            {
                return bufferScale * rateOfChange;
            }
        }

        
        #endregion Accessors


        





    }
}