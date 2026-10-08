// Isaac Bustad
// 8/6/24


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    public class ObjectBodyRotation : MonoBehaviour
    {

        // Vars
        [SerializeField, Header("Rotation Direction"), Tooltip("This Value will be normalized. redused to one")]
        protected Vector3 rotDir = Vector3.one;

        [SerializeField, Header("Body To Rotate")]
        protected Transform[] bodTF;

        //[SerializeField, Header("Speed of Rotation"), Range(0, 200)]
        //protected float rotSpeed = 0;

        [SerializeField, Header("Settings for Rotation")]
        protected BodyObjectRotation_SCO bodyObjectRotation_SCO;

        protected Rigidbody rb;
        // Methods
        private void OnEnable()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            RotateBodyObject();
        }

        // apply rotation based on Vector 3
        protected void RotateBodyObject()
        {
            foreach (Transform aTF in bodTF)
            {
                if (rb == null)
                {
                    aTF.Rotate(rotDir.normalized * bodyObjectRotation_SCO.RotMult * Time.deltaTime);
                }

                else
                {
                    aTF.Rotate(rotDir.normalized * bodyObjectRotation_SCO.RotMult * rb.linearVelocity.magnitude * Time.deltaTime);
                }
            }

           /* if (rb == null)
            {
                bodTF.Rotate(rotDir.normalized * bodyObjectRotation_SCO.RotMult * Time.deltaTime);
            }

            else
            {
                bodTF.Rotate(rotDir.normalized * bodyObjectRotation_SCO.RotMult * rb.velocity.magnitude * Time.deltaTime);
            }*/
        }



        // Accessors
        public Transform[] BodTF { get { return bodTF; } set { bodTF = value; } }

    }
}
