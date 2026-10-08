// Created By   :   Isaac Bustad
// Created      :   7/24/2026


using System.Collections.Generic;
using BugFreeProductions.Spatial;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    public class TargetingNode : MonoBehaviour, ISubscriber
    {
        #region Vars
        [SerializeField] protected TargetNode targetNode = null;
        [SerializeField] protected TargetingSystemParams_SCO targetingSystemParams_SCO = null;
        [SerializeField] protected Clock_SCO clock_SCO;
        

        #endregion Vars


        #region Methods

        #region Unity Methods
        protected virtual void OnEnable()
        {
            Setup();
        }

        #endregion Unity Methods
        
        protected virtual void Setup()
        {
            TargetingSystem.Instance.Subscribe(this);

            // setup the clock
            if (clock_SCO != null )
            {
                clock_SCO.Subscribe(this);
            }
        }

        #region Implement ISubscriber
        public void OnNotify(ISubscriberNotification aSubMessage)
        {
            if (aSubMessage is ClockNotification clockNotification)
            {
                TargetingSystem.Instance.FindTargets(   
                                                        targetNode,
                                                        targetingSystemParams_SCO,
                                                        new OrientationData(transform.position,transform.rotation)
                                                    );

                                                    
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
        #endregion Implement ISubscriber
        
        

        #endregion Methods


        #region Accessors
        public TargetNode TargetNode
        {
            get
            {
                // if the field is empty add the component and assign
                if (targetNode == null)
                {
                    targetNode = gameObject.AddComponent<TargetNode>();
                }

                return targetNode;
            }
        }

        #endregion Accessors
    }


    public enum TargetingType
    {
        hold,
        toggle,
        auto
    }
}