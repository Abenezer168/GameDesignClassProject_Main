// Created By   :   Isaac Bustad
// Created      :   7/4/2026

using System.Collections;
using UnityEngine;

namespace BugFreeProductions.Tools
{

    public class TrailItem : SubscribingFactoryItem
    {
        #region Vars        
        //[SerializeField] protected ItemScaleMode itemScaleSetting = ItemScaleMode.none;

        [Header("Scaler Settings")]
        [SerializeField] protected float maxScale = 2f;
        [SerializeField] protected float minScale = .5f;

        [SerializeField] protected float scaleRate = 1.01f;
        

        // clock
        //[SerializeField] protected Clock_SCO clock = null;

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
            // if (clock != null)
            // {
            //     clock.Subscribe(this);
            // }
            
        }



        

        protected override void ItemNotify(ISubscriberNotification aSubMessage)
        {
            
        }

        protected override void ItemOnSubscribe()
        {
            //clock.Subscribe(this);
        }

        protected override void ItemOnUnSubscribe()
        {
            // Pool Ourself
            PoolSelf();
        }
        protected override void ItemOnUnSubscribe(ISubscription aSubscription)
        {
            // Pool Ourself
            PoolSelf();

        }

        protected override void PoolSelf()
        {
            base.PoolSelf();

            gameObject.SetActive(false);
        }

        #endregion Methods


        #region Accessors

        #endregion Accessors
    }


}