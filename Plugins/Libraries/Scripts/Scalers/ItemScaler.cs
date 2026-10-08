// Created By   :   Isaac Bustad
// Created      :   7/14/2026




using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;


namespace BugFreeProductions.Tools
{
    public class ItemScaler : MonoBehaviour, ISubscriber
    {
        #region Vars
        [SerializeField] protected ItemScaler_SCO itemScaler_SCO;
        [SerializeField] protected bool isBounding = false;
        protected bool targetMax = true;

        // tell us to start from scaling start each time
        [SerializeField] protected bool startOver = true;

        // starting scale for an item
        protected Vector3 startingScale = Vector3.one;

        // clock reference to subscribe 
        [SerializeField] protected Clock_SCO clock;
        #endregion Vars


        #region Method

        #region Unity Methods
        protected virtual void Awake()
        {
            clock.Subscribe(this);
        }

        protected virtual void OnEnable()
        {
            if (startOver)
            {
                if (itemScaler_SCO != null)
                {
                    itemScaler_SCO.ScaleToStart(transform);
                }

                else
                {
                    Debug.LogWarning("itemScaler_SCO not set to a reference");
                }
            }
        }


        protected virtual void OnDisable()
        {

        }

        

        #endregion Unity Methods


        #region Implement ISubscriber

        public void OnNotify(ISubscriberNotification aSubMessage)
        {
            if (itemScaler_SCO != null)
            {
                if (isBounding)
                {
                    targetMax = itemScaler_SCO.ScaleObject(transform, targetMax);
                }

                else
                {
                    itemScaler_SCO.ScaleObject(transform, targetMax);
                }
                //itemScaler_SCO.ScaleObject(transform);
            }

            else
            {
                Debug.LogWarning("itemScaler_SCO not set to a reference");
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


        #endregion Method
    }
}