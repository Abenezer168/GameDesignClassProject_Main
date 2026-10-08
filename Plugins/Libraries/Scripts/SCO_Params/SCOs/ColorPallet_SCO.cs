// Isaac Bustad
//10/8/2024


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    [CreateAssetMenu(fileName = "ColorPallet_SCO", menuName = "ScriptableObject/ColorPallet_SCO")]
    public class ColorPallet_SCO : ScriptableObject
    {
        // Vars
        [SerializeField] protected ColorSwatch_SCO[] colorSwatches;

        // Methods
        public virtual ColorSwatch_SCO ColorSwatchReturn(int aColorSwatchIdx)
        {
            if(aColorSwatchIdx < colorSwatches.Length)
            {
                return colorSwatches[aColorSwatchIdx];
                
            }
            else
            {
                return colorSwatches[0];
            }
        }
        public virtual Color ColorReturn(int aColorIdx,int aColorSwatchIdx)
        {
            return ColorSwatchReturn(aColorSwatchIdx).ColorReturn(aColorIdx);
        }

        // Accessors


    }
}