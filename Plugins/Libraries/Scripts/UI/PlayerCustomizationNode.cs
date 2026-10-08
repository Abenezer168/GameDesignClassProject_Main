// Isaac Bustad
// 8/24/2025



using BugFreeProductions.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Think about how

// makes the player customization more accessable by other classes and objects
namespace BugFreeProductions.Party
{
    public class PlayerCustomizationNode : MonoBehaviour
    {
        #region Vars
        // object representing a players custom selection
        [SerializeField] protected PlayerCustomization playerCustomization = new PlayerCustomization();

        // 
        #endregion

        #region Methods


        #endregion

        #region Accessors
        public PlayerCustomization PlayerCustomization { get { return playerCustomization; }  }

        #endregion


    }

}
