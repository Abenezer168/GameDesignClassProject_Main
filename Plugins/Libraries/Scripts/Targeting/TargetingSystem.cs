// Created By   :   Isaac Bustad
// Created      :   7/24/2026



using BugFreeProductions.Spatial;
using System.Collections.Generic;
using UnityEngine;





namespace BugFreeProductions.Tools
{
    public class TargetingSystem : GenericSingleton<TargetingSystem>, ISubscription
    {
        #region Vars
        protected List<TargetNode> targetNodes = new List<TargetNode>();
        
        #endregion Vars



        #region Methods
        #region Unity Methods
        public virtual List<TargetData> FindTargets( TargetNode ignoreNode, 
                                                    TargetingSystemParams_SCO targetingSystemParams_SCO, 
                                                    OrientationData aOrientationData)
        {
            // construct a node 
            List<TargetData> targetingData = new List<TargetData>();

            // collect the targeting path Values
            List<Vector3> targetingPath = CalculateTargetingPath(targetingSystemParams_SCO.TargetingRange,
                                                                targetingSystemParams_SCO.TargetingInterval,
                                                                aOrientationData);
            
            // get refference to a filtered list
            List<TargetNode> validTargets = ValidTargetNodes(aOrientationData, ignoreNode, targetingSystemParams_SCO);
            
            // loop through each point for each possible target
            foreach (TargetNode tn in validTargets)
            {                
                targetingData.Add(CalculateClosestToPath(targetingPath, tn));
            }

            return targetingData;
        }

        #endregion Unity Methods

        #region Targeting Methods
        protected virtual List<Vector3> CalculateTargetingPath(float aDistance, 
                                                                float aInterval, 
                                                                OrientationData aOrientationData)
        {
            // hold the return Vector3 Positions
            List<Vector3> targetingPath = new List<Vector3>();

            // count the loops through
            // include in exit condition
            int intervalStep = 1;

            while (aInterval * intervalStep < aDistance)
            {
                // new ofset by interval
                Vector3 ofsetAtInterval = aOrientationData.positionData + 
                                            (aOrientationData.Direction(OrientationDirection.forward) * intervalStep);

                // add the calculated point to the path
                targetingPath.Add(ofsetAtInterval);


                // increment to calculation
                // increment for end condition
                intervalStep ++;
            }

            // return the constructed path
            return targetingPath;
        }


        // Only call if list is populated
        protected virtual TargetData CalculateClosestToPath(List<Vector3> aTargetPath, TargetNode aTargetNode)
        {
            // hold minimum distance
            float minDistance = Vector3.Distance(aTargetPath[0],aTargetNode.transform.position);

            // calculate distance to all path nodes
            foreach (Vector3 v3 in aTargetPath)
            {
                float aDistance = Vector3.Distance(v3, aTargetNode.transform.position);

                if (aDistance < minDistance)
                {
                    minDistance = aDistance;
                }
            }

            // construct response
            TargetData response = new TargetData(aTargetNode, minDistance);
            return response;
        }

        protected virtual List<TargetNode> ValidTargetNodes(OrientationData aOD, 
                                                                TargetNode ignoreNode, 
                                                                TargetingSystemParams_SCO targetingSystemParams_SCO)
        {
            // store list of valid targets
            List<TargetNode> validTargets = new List<TargetNode>();


            // loop through and add valid targets to a list
            foreach (TargetNode tn in targetNodes)
            {
                // ignore specified Node
                if (tn.GetInstanceID() != ignoreNode.GetInstanceID())
                {
                    // calculate the angle between the orientation and target
                    //float angleToTarget = OrientationExtension.CalculateAngleToTarget(aOD,tn.transform.position);
                    float angleToTarget = aOD.AngleToTarget(tn.transform.position);

                    if (angleToTarget < targetingSystemParams_SCO.TargetingAngle / 2 &&
                        Vector3.Distance(aOD.positionData,tn.transform.position) < targetingSystemParams_SCO.TargetingRange)
                    {
                        
                    }

                }

            }

            
            return validTargets;
        }

        #endregion Targeting Methods


        #region Implement ISubscription
        public void Subscribe(ISubscriber subscriber)
        {
            if(subscriber is TargetingNode aTargetingNode)
            {
                targetNodes.Add(aTargetingNode.TargetNode);
            }

        }

        public void UnSubscribe(ISubscriber subscriber)
        {
            if(subscriber is TargetingNode aTargetingNode)
            {
                targetNodes.Remove(aTargetingNode.TargetNode);
            }
        }
        public void Notify()
        {

        }
        #endregion Implement ISubscription

        #endregion Methods



        #region Constructors
        
        #endregion Constructors



        #region Accessors

        #endregion Accessors
    }
}