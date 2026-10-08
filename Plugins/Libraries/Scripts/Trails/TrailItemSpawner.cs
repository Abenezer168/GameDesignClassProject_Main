// Created By   :   Isaac Bustad
// Created      :   7/4/2026

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    public class TrailItemSpawner : MonoBehaviour, ISubscriber, ISubscription
    {
        #region Vars
        protected MultipleOrientationTrailSystem trailSystem = null;

        [Header("Trail Item Spawner")]
        [SerializeField] protected SubscribingFactory_SCO trailItemFactory = null;
        //[SerializeField] protected float itemPointDelay = 0.5f;
        

        [Header("Item Placement Parameters")]
        [SerializeField] protected float distanceBetweenItems = 1f;
        protected float timeToLastItem = 0f;




        // subscription management
        protected ISubscriberNotification notification;
        protected Queue<ISubscriber> subscribers = new Queue<ISubscriber>();


        // Notification storage
        protected TrailSystemNotification trailSystemNotification;




        


        #endregion Vars


        #region Methods

        #region Unity Methods
        protected virtual void OnEnable()
        {
            Setup();
            
        }

        protected virtual void Update()
        {
            Notify();
        }

        
        

        #endregion Unity Methods

        protected virtual void Setup()
        {
            // subscribe to trail system
            if (GetComponent<MultipleOrientationTrailSystem>() is MultipleOrientationTrailSystem aTrailSystem)
            {
                trailSystem = aTrailSystem;
                trailSystem.Subscribe(this);
            }

            // create struct
            notification = new TrailItemSpawnerNotification();
        }

        #region ISubscriber methods
        
        public void OnSubscribe()
        {

        }

        public void OnUnSubscribe()
        {

        }
        public void OnNotify(ISubscriberNotification aSubMessage)
        {
            if (aSubMessage is  TrailSystemNotification aTS)
            {
                trailSystemNotification = aTS;                
            }

            while (subscribers.Count > trailSystemNotification.trailLength)
            {
                // get and remove a Subscriber
                ISubscriber sub = subscribers.Dequeue();

                // call the Item to hide and pool itself
                if (sub is TrailItem trailItem)
                {
                    trailItem.OnUnSubscribe();
                }

            }
            
            // spawn the Item
            SpawnTrailItem();

        }

        // removes Subscriber to subscription
        public void OnUnSubscribe(ISubscription aSubscription)
        {

        }
        #endregion ISubscriber methods

        

        #region Implement ISubscription
        public void Subscribe(ISubscriber subscriber)
        {
            subscribers.Enqueue(subscriber);
        }
         // adds Subscriber to subscription
        public void OnSubscribe(ISubscription aSubscription)
        {
            
        }

        public void UnSubscribe(ISubscriber subscriber)
        {

        }
        public void Notify()
        {
            foreach(ISubscriber sub in subscribers)
            {
                sub.OnNotify(notification);
            }
        }

        #endregion Implement ISubscription
        
        #region Trail Item Spawning
        // protected virtual void DelayItemSpawn(OrientationData aOrientationData)
        // {
            
        // }
        protected virtual void SpawnTrailItem()
        {
            if(subscribers.Count > 0)
            {
                if (subscribers.ToList()[^1] is TrailItem trailItem)
                {
                    if (Vector3.Distance(trailItem.transform.position, trailSystemNotification.startOrientationData.positionData) > distanceBetweenItems)
                    {
                        trailItemFactory.CreateItem(trailSystemNotification.startOrientationData,this);
                    }
                }
            }
            else
            {
                trailItemFactory.CreateItem(trailSystemNotification.startOrientationData,this);
            }
        }

        #endregion Trail Item Spawning

        #endregion Methods

    }
}