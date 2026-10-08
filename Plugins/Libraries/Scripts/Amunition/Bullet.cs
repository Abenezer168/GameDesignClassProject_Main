// Isaac Bustad
// 10/15/2024


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BugFreeProductions.Extentions;

namespace BugFreeProductions.Tools {
    public class Bullet : Trig_CMD_Client
    {
        // Vars
        #region Bullet Refferences
        protected Rigidbody rb = null;

        protected Amunition amo = null;

        // trigger effect
        [SerializeField] protected GenericFactory_SCO hitEffectFact_SCO = null;

        #endregion
        #region Delay stuff
        [SerializeField, Header("delay to hide bullet w/o col")] 
        protected float delayDeActive = 3;

        protected WaitForSeconds waitForDeActive;
        #endregion


        // all settings including deviation and max speed


        // Methods
        protected virtual void OnEnable()
        {
            CollectVars();
        }

        
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            // create hit effect
            hitEffectFact_SCO.CreateItem(transform);


            // deactivate bullet
            DeActivateBullet();
            // Debug.Log("Why oh why");
        }

        protected virtual void Update()
        {
            rb.linearVelocity.LimitToWorldVelocity();
        }

        protected virtual void CollectVars()
        {
            if (rb == null)
            {
                // find Rigidbody
                rb = GetComponent<Rigidbody>();

                // Set WaitForSeconds
                waitForDeActive = new WaitForSeconds(delayDeActive);
            }
        }

        public virtual void FireBullet(Amunition anAmo)
        {
            amo = anAmo;
            //Debug.Log("fire 1");
            ReadyBullet();
            //Debug.Log("appDev 1");
            AplyDeviation();
            //Debug.Log("initial 1");
            AplyInitialForce();
        }

        protected virtual void AplyDeviation()
        {
            Vector3 devRot = Vector3.zero;

            // rand rot in bounds
            devRot.x = Mathf.Clamp(Random.Range(-Mathf.Abs(amo.BulDevMax.x), Mathf.Abs(amo.BulDevMax.x)), -45f, 45f);
            devRot.y = Mathf.Clamp(Random.Range(-Mathf.Abs(amo.BulDevMax.y), Mathf.Abs(amo.BulDevMax.y)), -45f, 45f);
            devRot.z = Mathf.Clamp(Random.Range(-Mathf.Abs(amo.BulDevMax.z), Mathf.Abs(amo.BulDevMax.z)), -45f, 45f);

            transform.Rotate(devRot);
        }

        protected virtual void AplyInitialForce()
        {
            //Debug.Log("initial 1");

            rb.AddForce(transform.forward * amo.Powder, ForceMode.Impulse);
            //Debug.Log("initial 2");

        }

        protected virtual void ReadyBullet()
        {
            // turn on game object
            //Debug.Log("why me 1");
            gameObject.SetActive(true);
            rb.linearVelocity = Vector3.zero;

            //Debug.Log("why me 2");
            // position and align
            transform.position = amo.transform.position;
            transform.rotation = amo.transform.rotation;

            //Debug.Log("why me 3");
            // start our coroutine
            StartCoroutine(DelayDeActive());
        }

        protected virtual void DeActivateBullet()
        {
            StopAllCoroutines();
            amo.PoolAmo(this);
            Debug.Log("why me 7");
            gameObject.SetActive(false);
            Debug.Log("why me 8");
        }

        // Coroutines
        protected virtual IEnumerator DelayDeActive()
        {
            // at call
            //Debug.Log("why me 4");
            yield return waitForDeActive;
            //Debug.Log("why me 5");
            // after delay
            DeActivateBullet();
            //Debug.Log("why me 6");
        }


        // Accessors




    }
}