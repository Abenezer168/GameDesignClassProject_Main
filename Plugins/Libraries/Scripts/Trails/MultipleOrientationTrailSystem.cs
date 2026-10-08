// Created By   :   Isaac Bustad
// Created      :   6/22/2026

using System;
using System.Collections.Generic;
using UnityEngine;
using BugFreeProductions.Spatial;


namespace BugFreeProductions.Tools
{
        
    public class MultipleOrientationTrailSystem : MonoBehaviour, ISubscription, ISubscriber
    {
        #region Vars
        // class assigned data
        // notification delegate
        protected Action<ISubscriberNotification> onNotify;

        // notification data
        protected ISubscriberNotification subscriberNotification;
            


        [SerializeField] protected TrailMethod trailMethod;
        [Header("Parameters to draw line")]
        [SerializeField] protected List<LineRenderer> lineRenderers;
        [SerializeField] protected List<OrientationDirection> lineDirections = new List<OrientationDirection>();
        [SerializeField] protected int maxNumberOfPoints = 100;
        [SerializeField] protected float pointDelay = 0.1f;
        [SerializeField] protected float timeToLastPoint = 0f;

        [SerializeField] protected float lineDrift = 0f;
        [SerializeField] protected float trailOffset = 0f;

        [SerializeField] protected Clock_SCO trailClock;

        
        

        [Header("Orientation trail vars")]
        protected List<OrientationData> orientationDatas = new List<OrientationData>();
        [SerializeField] protected Vector3 vOffset = Vector3.zero; 

        #endregion Vars




        #region Methods
        #region Unity Methods
        protected virtual void OnEnable()
        {
            Setup();
        }

        protected virtual void Update()
        {
            // DelayPoint(Time.deltaTime);
            
            // if (trailMethod != null)
            // {
            //     RenderTrail();
            // }


        }

        #endregion Unity Methods

        protected virtual void Setup()
        {
            subscriberNotification = new TrailSystemNotification(
                                        new OrientationData(
                                            transform.position,
                                            transform.rotation),
                                            new OrientationData(
                                            transform.position,
                                            transform.rotation),
                                            10);


            // subscribe to assigned clock
            if (trailClock != null)
            {
                trailClock.Subscribe(this);
            }
        }

        protected virtual void AddPoint()
        {
            //TwoPointsTrail();

            OrientationData od = new OrientationData(transform.position + (transformHandle.rotation * vOffset),transform.rotation);
            orientationDatas.Add(od);

            while(orientationDatas.Count > maxNumberOfPoints)
            {
                orientationDatas.RemoveAt(0);
            }

            // update the orientation data of the notification
            if (subscriberNotification is TrailSystemNotification tsn)
            {
                // capture the current struct value
                // for the notification
                TrailSystemNotification aTSN = new TrailSystemNotification(orientationDatas[^1], orientationDatas[0], 10);

                // alter the copy
                // aTSN.orientationData = orientationDatas[^1];

                // reassign to update
                subscriberNotification = aTSN;

            }


        }

        protected virtual void RenderTrail()
        {
            // 
            trailMethod.RenderTrail(lineRenderers, orientationDatas, lineDirections, lineDrift, trailOffset, vOffset);
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

        

        #region ImplementISubscription
        public void Subscribe(ISubscriber subscriber)
        {
            onNotify += subscriber.OnNotify;
        }

        public void Notify()
        {
            onNotify.Invoke(subscriberNotification);
        }

        public void UnSubscribe(ISubscriber subscriber)
        {
            onNotify -= subscriber.OnNotify;
        }

        #endregion ImplementISubScription

        #region Implement ISubscribe
        public void OnNotify(ISubscriberNotification aSubMessage)
        {
            if (aSubMessage is ClockNotification clockNotification)
            {
                AddPoint();
                RenderTrail();
                Notify();
            }
        }

        // adds Subscriber to subscription
        public void OnSubscribe()
        {

        }

        // adds Subscriber to subscription
        public void OnSubscribe(ISubscription aSubscription)
        {

        }

        // removes Subscriber to subscription
        public void OnUnSubscribe()
        {

        }

        // removes Subscriber to subscription
        public void OnUnSubscribe(ISubscription aSubscription)
        {

        }

        #endregion Implement ISubscribe
        
        #endregion Methods
    }
}