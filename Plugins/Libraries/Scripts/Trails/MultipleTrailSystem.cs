// Created By   :   Isaac Bustad
// Created      :   4/1/2026



using System.Collections.Generic;
using System.Linq;
//using NUnit.Framework;
using UnityEngine;

namespace BugFreeProductions.Tools
{

    public class MultipleTrailSystem : FactoryItem//, ISubscriber
    {
        #region Vars
        [SerializeField] protected TrailType trailType;


        [SerializeField] protected TrailMethod trailMethod;
        [SerializeField] protected List<LineRenderer> lineRenderers;
        [SerializeField] protected int maxNumberOfPoints = 100;
        [SerializeField] protected float pointDelay = 0.1f;
        [SerializeField] protected float timeToLastPoint = 0f;

        [SerializeField] protected float lineDrift = 0f;
        [SerializeField] protected float trailOffset = 0f;

        // List of all the current trail points
        List<List<Vector3>> allPos = new List<List<Vector3>>();

        // tracked locations
        [SerializeField] protected List<Transform> locs = new List<Transform>();




        // location for center line
        [SerializeField] protected Transform centerLoc = null;

        // lists by location
        protected Dictionary<int, List<Vector3>> trailsByLocation = new Dictionary<int, List<Vector3>>();
        protected List<Vector3> centerLocs = new List<Vector3>();

        [Header("Orientation trail vars")]
        //protected List<OrientationData> orientationDatas = new List<OrientationData>();
        [SerializeField] protected Vector3 vOffset = Vector3.zero; 
        #endregion Vars

        #region Methods
        protected virtual void OnEnable()
        {
            for (int i = 0; i < locs.Count; i++)
            {
                trailsByLocation.Add(i,new List<Vector3>(){locs[i].position});

            }



        }


        protected virtual void Update()
        {
            DelayPoint(Time.deltaTime);
            
            if (allPos.Count > 0)
            {
                if (trailMethod != null)
                {
                    RenderTrail();
                }
            }
            
        }

        protected virtual void DelayPoint(float atime)
        {
            timeToLastPoint += atime;

            if (timeToLastPoint > pointDelay)
            {
                AddPoint();

                timeToLastPoint = 0;
            }
        }


        protected virtual void AddPoint()
        {
            TwoPointsTrail();

            
        }

        protected virtual void TwoPointsTrail()
        {
            // hold a list of all position list
            allPos = new List<List<Vector3>>();

            foreach (KeyValuePair<int, List<Vector3>> aKVP in trailsByLocation)
            {
                // add a single position
                aKVP.Value.Add(locs[aKVP.Key].position);

                while(aKVP.Value.Count > maxNumberOfPoints)
                {
                    aKVP.Value.RemoveAt(0);
                }

                // update all positions
                allPos.Add(aKVP.Value);
                //Debug.Log("All Pos: " + allPos.Count);

            }

            centerLocs.Add(centerLoc.position);

            while (centerLocs.Count > maxNumberOfPoints)
            {
                centerLocs.RemoveAt(0);
            }
        }




        protected virtual void RenderTrail()
        {
            
            //List<List(Vector3)>
            trailMethod.RenderTrail(allPos, lineRenderers,centerLocs,lineDrift,trailOffset);
            //trailMethod.RenderTrail(2, lineRenderers, orientationDatas, new List<OrientationDirection>{OrientationDirection.left,OrientationDirection.right}, lineDrift, trailOffset, vOffset);
        }


        #region ISubscriber
        // add this to subscriber delegate
        public void OnNotify(ISubscriberNotification aSubMessage)
        {

        }

        // adds Subscriber to subscription
        public void AddSubscription()
        {

        }

        // removes Subscriber to subscription
        public void RemoveSubscription()
        {

        }
        #endregion ISubscriber


        #endregion Methods
    }

    public enum TrailType
    {
        single,
        multiPoint,
        multiOrientation,
        multiOrientSpawn
    }
}