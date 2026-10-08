// Created By   :   Isaac Bustad
// Created      :   7/3/2026


using UnityEngine;

namespace BugFreeProductions.Tools
{
    public class SubscriberNode : MonoBehaviour, ISubscriber
    {
        #region Vars
        

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
            if (GetComponent<ISubscription>() is ISubscription subscription)
            {
                subscription.Subscribe(this);
            }
        }

        #region Implement Subscriber
        public void OnNotify(ISubscriberNotification aSubMessage)
        {

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


        #endregion Implement Subscriber
        
        #endregion Methods
    }
}