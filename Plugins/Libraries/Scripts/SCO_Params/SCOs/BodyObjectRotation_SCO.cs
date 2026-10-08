// Isaac Bustad
// 8/15/2024

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    [CreateAssetMenu(fileName = "BodyObjectRotation_SCO", menuName = "ScriptableObject/BodyObjectRotation_SCO")]
    public class BodyObjectRotation_SCO : ScriptableObject
    {
        // Vars
        [SerializeField, Header("Multiplier For rotation Speed")]
        protected float rotMult = 1;

        // Methods


        // Accessors
        public float RotMult {  get { return rotMult; }  }
    }
}
