// Created By   :   Isaac Bustad
// Created      :   7/4/2026



using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Tools
{

    [CreateAssetMenu(fileName = "Clock_SCO", menuName = "ScriptableObject/Clock_SCO")]
    
    public class Clock_SCO : ScriptableObject, ISubscription
    {
        #region Vars
        [SerializeField] protected float timeInterval = 1f;
        [SerializeField] protected float timeElapsed = 0f;

        [SerializeField] protected List<ISubscriber> subscribers = new List<ISubscriber>();
        protected int curIdx = 0;

        protected ISubscriberNotification subMessage;


        #endregion Vars

        #region Methods
        protected virtual void OnEnable()
        {
            Setup();
        }
        public virtual void Setup()
        {
            subMessage = new ClockNotification(timeInterval,false);
        }

        public virtual void CheckTimeStep()
        {
            if (TimeInterval > 0)
            {
                while (timeElapsed >= TimeInterval)
                {
                    // if the index is out of bounds reset
                    if (curIdx >= subscribers.Count)
                    {
                        curIdx = 0;
                    }

                    // decrement the time elapsed
                    // serves as loop exit condition
                    timeElapsed -= TimeInterval;

                    // notify the current subscriber
                    subscribers[curIdx].OnNotify(subMessage);

                    // increment index
                    curIdx++;

                }
            }

        }

        public virtual void RunClock(float aDeltaTime)
        {
            timeElapsed += aDeltaTime;
            CheckTimeStep();
        }

        #region  Implement ISubscription
        public void Subscribe(ISubscriber subscriber)
        {
            subscribers.Add(subscriber);

        }

        public void UnSubscribe(ISubscriber subscriber)
        {
            subscribers.Remove(subscriber);
            //subscriber.OnUnSubscribe();
        }
        public void Notify()
        {
            // RunClock notifies all subscribers

        }

        #endregion Implement ISubscription


        #endregion Methods


        #region Accessors
        public virtual float TimeInterval
        {
            get
            {
                if (subscribers.Count > 0 && timeInterval > 0)
                {
                    return timeInterval / subscribers.Count;
                }

                else
                {
                    return 0;
                }
                
            }
        }

        #endregion Accessors

        

    }
}