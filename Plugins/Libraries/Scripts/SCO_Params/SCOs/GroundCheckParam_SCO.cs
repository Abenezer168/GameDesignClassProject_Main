// Isaac Bustad
// 8/14/2024

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;


namespace BugFreeProductions.ArcadeRacer
{
    [CreateAssetMenu(fileName = "GroundCheck_SCO", menuName = "ScriptableObject/GroundCheck_SCO")]
    public class GroundCheck_SCO : ScriptableObject
    {
        // Vars
        [SerializeField] protected int[] groundLayers;
        [SerializeField] protected float[] frictionLVLs;

        // Methods
        public virtual int FilterLayer(int aLayerInt)
        {
            // hold value the layer is worth, what layer it is
            int layerValue = 1;
            foreach (int i in groundLayers)
            {
                if (aLayerInt == i) { layerValue = i; }
            }
            return layerValue;
        }

        /*public virtual int FilterLayers(int[] aLayerInts)
        {
            // hold value the layer is worth, what layer it is
            int layerValue = 0;
            foreach (int al in aLayerInts)
            {
                foreach (int i in groundLayers)
                {
                    if (al == i) { layerValue = i; }
                }
            }
            
            return layerValue;
        }*/

        public virtual float CalcFriction(int layersTriggered)
        {
            float friction = 1f;

            for (int idx = 0; idx < groundLayers.Length; idx++)
            {
                if (layersTriggered % groundLayers[idx] == 0)
                {
                    friction = frictionLVLs[idx];
                }
            }

            

            return friction;
        }
    }
}
