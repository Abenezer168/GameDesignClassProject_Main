// Created By   :   Isaac Bustad   
// Created      :   4/17/2026  


using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Tools{
    public class SinglePlayerCustomConfig : MonoBehaviour
    {
        #region Vars
        protected List<PlayerCustomConfig> playerCustomConfigs = null;

        #endregion Vars

        public List<PlayerCustomConfig> PlayerCustomConfig
        {
            get{return playerCustomConfigs;}
        }



    }
}
