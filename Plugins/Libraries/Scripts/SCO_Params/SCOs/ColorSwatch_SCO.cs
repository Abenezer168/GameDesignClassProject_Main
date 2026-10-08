// Isaac Bustad
// 10/8/2024


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BugFreeProductions.Tools
{

    [CreateAssetMenu(fileName = "ColorSwatch_SCO", menuName = "ScriptableObject/ColorSwatch_SCO")]
    public class ColorSwatch_SCO : ScriptableObject
    {
        // Vars
        //[SerializeField, Header("Light colour")] protected Color hexRefL;
        //[SerializeField, Header("Medium colour")] protected Color hexRefM;
        //[SerializeField, Header("Dark colour")] protected Color hexRefD;
        [SerializeField, Header("Colour Refs")] protected Color[] rgbRefs;

        [SerializeField, Header("Dark colour")] protected Image iRefD;


        // Methods
        public Color ColorReturn(int aColorIdx)
        {
            if (aColorIdx < rgbRefs.Length)
            {
                return rgbRefs[aColorIdx];
                
            }
            else
            {
                return rgbRefs[0];
            }
            
        }

        // Accessors




    }
}