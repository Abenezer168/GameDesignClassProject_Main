// Isaac Bustad
// 9/6/2025

using BugFreeProductions.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Party
{
    public class CoinBehavoiur : FactoryItem
    {
        #region Methods
        public void HideCoin()
        {
            gameObject.SetActive(false);
        }
        #endregion 
    }
}