// Created By   :   Isaac Bustad
// Created      :   6/30/2026

using UnityEngine;
using BugFreeProductions.Spatial;



namespace BugFreeProductions.Tools
{
    [CreateAssetMenu(fileName = "TrailFactory_SCO", menuName = "ScriptableObject/TrailFactory_SCO")]
    public class SubscribingFactory_SCO : GenericFactory_SCO
    {
        #region Vars
        protected ISubscription subscription = null;
        #endregion Vars



        #region Methods
        // orientation data, pool , the thing I want to subscribe to
        public virtual FactoryItem CreateItem(OrientationData aOD, ISubscription aSubscription)
        {
            // Create a factory Item
            FactoryItem fi = base.CreateItem(aOD);

            if (fi.GetComponent<SubscribingFactoryItem>() is SubscribingFactoryItem sfi)
            {
                sfi.OnSubscribe(aSubscription);
            }
            return fi;
        }


        #endregion Methods
    }
}
