// Isaac Bustad
// 5/6/2024

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    public interface ISubscriber
    {
        // Methods
        // runs when Subscription notifies subscribers
        // add this to subscriber delegate
        public void OnNotify(ISubscriberNotification aSubMessage);

        // adds Subscriber to subscription
        public void OnSubscribe();

        // adds Subscriber to subscription
        public void OnSubscribe(ISubscription aSubscription);

        // removes Subscriber to subscription
        public void OnUnSubscribe();

        // removes Subscriber to subscription
        public void OnUnSubscribe(ISubscription aSubscription);

        


    }
}