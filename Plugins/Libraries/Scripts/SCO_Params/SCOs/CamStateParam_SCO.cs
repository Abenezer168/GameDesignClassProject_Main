// Isaac Bustad
// 9/12/2024


using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BugFreeProductions.ArcadeRacer
{
    [CreateAssetMenu(fileName = "RacerCamState_SCO",menuName = "ScriptableObject/CamStateParam_SCO")]
    public class CamStateParam_SCO : ScriptableObject
    {
        // Vars
        [SerializeField] protected Vector3 camToSet = Vector3.zero;
        [SerializeField] protected Vector3 camFromSet = Vector3.zero;

        [SerializeField,Range(0,1)] protected float tweenTime = .5f;
        [SerializeField, Range(0, 1)] protected float changeRate = .9f;
        [SerializeField, Range(0, 1)] protected float buffer = .2f;
        [SerializeField, Range(0, 10)] protected float toDistMult = .2f;
        [SerializeField, Range(0, 10)] protected float fromDistMult = .2f;



        // Methods



        // Accessors
        public Vector3 CamToSet { get { return camToSet; } }
        public Vector3 CamFromSet { get {  return camFromSet; } }
        public float ChangeRate { get { return changeRate; } }
        public float Buffer { get { return buffer; } }
        public float TweenTime { get { return tweenTime; } }
        public float ToDistMult { get { return toDistMult; } }
        public float FromDistMult { get { return fromDistMult; } }

        
    
    
    }

}