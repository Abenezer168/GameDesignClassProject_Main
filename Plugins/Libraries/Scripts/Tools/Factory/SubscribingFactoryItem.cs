// Created By   :   Isaac Bustad
// Created      :   7/1/2026


using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    public class SubscribingFactoryItem : FactoryItem, ISubscriber
    {
        #region Vars
        //protected List<ISubscription> subscriptions = new List<ISubscription>();
        #endregion Vars


        #region Methods

        #region Implement ISubscriber
        public void OnNotify(ISubscriberNotification aSubMessage)
        {
            ItemNotify(aSubMessage);
        }

        // adds Subscriber to subscription
        public void OnSubscribe()
        {
            ItemOnSubscribe();
        }

        // adds Subscriber to subscription
        public void OnSubscribe(ISubscription aSubscription)
        {
            ItemOnSubscribe(aSubscription);
        }

        // removes Subscriber to subscription
        public void OnUnSubscribe()
        {
            ItemOnUnSubscribe();
        }

        // removes Subscriber to subscription
        public void OnUnSubscribe(ISubscription aSubscription)
        {
            
        }

        

        protected virtual void ItemOnSubscribe(ISubscription aSubscription)
        {
            aSubscription.Subscribe(this);
            //subscriptions.Add(aSubscription);
        }

        protected virtual void ItemOnSubscribe()
        {
            
        }

        protected virtual void ItemOnUnSubscribe()
        {
            // foreach (ISubscription aSubscription in subscriptions)
            // {
            //     aSubscription.UnSubscribe(this);
            //     subscriptions.Remove(aSubscription);
            // }
        }

        protected virtual void ItemNotify(ISubscriberNotification aSubMessage)
        {

        }

        protected virtual void ItemOnUnSubscribe(ISubscription aSubscription)
        {
            //subscriptions.Remove(aSubscription);
        }

        
        
        #endregion Implement ISubscriber

        #endregion Methods

    }
}