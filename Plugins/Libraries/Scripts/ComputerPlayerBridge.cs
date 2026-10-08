using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BugFreeProductions.Tools
{
    public class ComputerPlayerBridge : MonoBehaviour
    {
        #region Vars
        //protected GamePlayerNode playerNode = null;

        // is player or computer
        private bool isPlayerCharacter = false; 
        #endregion


        #region Methods

        #endregion


        #region Accessors
        public bool IsPlayerCharacter { set { isPlayerCharacter = value; Debug.LogError(isPlayerCharacter); } }
        //public GamePlayerNode PlayerNode { set { playerNode = value; Debug.LogError(isPlayerCharacter); } }
        #endregion
    }
}