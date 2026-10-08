// Isaac Bustad 
// 10/15/2024


using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;



namespace BugFreeProductions.Tools
{
    public class Amunition : FactoryItem
    {
        // Vars
        // core components of amo
        [SerializeField] protected Bullet[] bullets;
        protected List<Bullet> bulletsList = new List<Bullet>();
        [SerializeField] protected GameObject[] casings;
        [SerializeField] protected ParticleSystem muzzFlash;
        [SerializeField, Range(10,1000)] protected float powder = 10;
        [SerializeField] protected Vector3 bulDevMax = Vector3.zero;

        
    

        // Methods
        public override void UseFactoryItem(Transform aTF, GenericPool aGP)
        {
            // aline using base class method
            base.UseFactoryItem(aTF, aGP);

            // do some extra crap
            FireAmunition();
        }


        protected virtual void FireAmunition()
        {
            //Debug.Log("amo 1");
            bulletsList = bullets.ToList();
            muzzFlash.Play();
            //Debug.Log("amo 2");
            foreach (Bullet b in bullets)
            {
                b.FireBullet(this);
            }
            //Debug.Log("amo 3");
        }

        public virtual void PoolAmo(Bullet aBullet)
        {
            bulletsList.Remove(aBullet);
            if (bulletsList.Count < 1 )
            {
                PoolSelf();
            }
        }

        protected override void PoolSelf()
        {
            gameObject.SetActive(false);
            base.PoolSelf();
        }

        // Accessors
        public float Powder { get { return powder; } }
        public Vector3 BulDevMax { get { return bulDevMax; } }


    }
}