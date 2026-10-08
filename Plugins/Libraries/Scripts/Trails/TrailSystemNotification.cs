// Created By   :   Isaac Bustad
// Created      :   7/8/2026


using BugFreeProductions.Spatial;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    public struct TrailSystemNotification: ISubscriberNotification
    {
        public OrientationData startOrientationData;
        public OrientationData endOrientationData;
        public int trailLength;

        public TrailSystemNotification(OrientationData aStartOrientationData, OrientationData aEndOrientationData, int aTrailLength)
        {
            startOrientationData = aStartOrientationData;
            endOrientationData = aEndOrientationData;
            trailLength = aTrailLength;
        }

        
    }
}