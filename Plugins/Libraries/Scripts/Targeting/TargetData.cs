// Created By   :   Isaac Bustad
// Created      :   7/24/2026




using UnityEngine;



namespace BugFreeProductions.Tools
{
    
    public struct TargetData
    {
        #region Vars
        public TargetNode targetNode;
        public float targetDeviation;
    
        #endregion Vars


        #region Constructors
        public TargetData(TargetNode aTargetNode, float aTargetDeviation)
        {
            targetDeviation = aTargetDeviation;
            targetNode = aTargetNode;

        }

        #endregion Constructors
    }
}