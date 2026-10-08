// Isaac Bustad
// 10/8/2024


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BugFreeProductions.Spatial;


namespace BugFreeProductions.Tools
{
    public class FactoryItem : Poolable
    {
        // Vars
        // id standing for particular prefab
        [SerializeField] protected string id = "NA";


        // Methods
        public virtual void UseFactoryItem(Transform aTF, GenericPool aGP)
        {
            gameObject.SetActive(true);
            pool = aGP;
            // position and align
            transform.position = aTF.position;
            transform.rotation = aTF.rotation;
        }

        public virtual void UseFactoryItem(OrientationData aOD, GenericPool aGP)
        {
            gameObject.SetActive(true);
            pool = aGP;
            // position and align
            transform.position = aOD.positionData;
            transform.rotation = aOD.rotationData;
        }

        public virtual void UseFactoryItem(Vector3 aPosition, Vector3 aRotation, GenericPool aGP)
        {
            gameObject.SetActive(true);
            pool = aGP;
            // position and align
            transform.position = aPosition;
            transform.rotation = Quaternion.Euler(aRotation);
        }

        #region Stuff For Object Placement
        public virtual void UseFactoryItem(ObjectPlacement aPlacement, GenericPool aGP)
        {
            gameObject.SetActive(true);
            pool = aGP;
            // position and align
            transform.position = new Vector3(aPlacement.tpX, aPlacement.tpY, aPlacement.tpZ);
            transform.rotation = Quaternion.Euler(aPlacement.trX, aPlacement.trY, aPlacement.trZ);

            // pool objects created with placement data
            pool.PoolObj(this);
        }
        #endregion

        protected virtual void OnDestroy()
        {
            if (pool != null)
            {
                pool.RevFromPool(this);
            }            
        }


        public virtual ObjectPlacement ObjectPlacement() 
        {
            ObjectPlacement nObjPlace = new ObjectPlacement();

            nObjPlace.id = id;

            Vector3 objPos = transform.position;

            nObjPlace.tpX = objPos.x;
            nObjPlace.tpY = objPos.y;
            nObjPlace.tpZ = objPos.z;

            Vector3 objRot = transform.eulerAngles;

            nObjPlace.trX = objRot.x;
            nObjPlace.trY = objRot.y;
            nObjPlace.trZ = objRot.z;

            return nObjPlace;
        }

        // Accessors
        public virtual string ID { get { return id; } }



    }
}