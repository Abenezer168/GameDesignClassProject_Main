// Isaac Bustad
// 11/13/2024

using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;



namespace BugFreeProductions.Tools
{
    public class BulletHitEfect : FactoryItem
    {
        // Vars
        #region Basic Refs
        [SerializeField] protected ParticleSystem ps = null;
        //[SerializeField] protected AudioSource audioSource = null;
        [SerializeField] protected List<AudioClip> audioClips = new List<AudioClip>();
        #endregion

        #region 
        [SerializeField] protected float poolDelay = 1.0f;
        protected WaitForSeconds waitToPool = null;

        #endregion


        // Methods
        protected virtual void OnEnable()
        {
            CollectVars();
        }

        protected virtual void CollectVars()
        {
            if (waitToPool == null)
            {
                waitToPool = new WaitForSeconds(poolDelay);
            }
            
            StartCoroutine(DelayPool());
        }

        public override void UseFactoryItem(Transform aTF, GenericPool aGP)
        {
            // aline using base class method
            base.UseFactoryItem(aTF, aGP);

            // do some extra crap
            PlayHitEffect(aTF.position);

        }

        #region Play Effects
        protected virtual void PlayHitEffect(Vector3 aPos)
        {
            if (ps != null)
            {
                ps.Play();
            }
            
            //AudioSource.PlayClipAtPoint(audioClips,aPos);
            PlayRandAudioClip(aPos);
        }

        protected virtual void PlayRandAudioClip(Vector3 aPos)
        {
            if (audioClips.Count == 1)
            {
                AudioSource.PlayClipAtPoint(audioClips[0], aPos);
            }
            else if (audioClips.Count > 0)
            {
                int idx = Random.Range(0, audioClips.Count - 1);
                AudioSource.PlayClipAtPoint(audioClips[idx], aPos);
            }
        }

        #endregion
 

        protected override void PoolSelf()
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
            base.PoolSelf();
        }

        // Coroutines
        protected virtual IEnumerator DelayPool()
        {
            // on call

            yield return waitToPool;

            // post yield
            PoolSelf();

        }


        // Accessors



    }
}