// Created By   :   Isaac Bustad
// Created      :   7/13/2026


using System.Collections.Generic;
using UnityEngine;


namespace BugFreeProductions.Tools
{
    [CreateAssetMenu(fileName = "MaterialReference", menuName = "ScriptableObject/MaterialReference_SCO")]
    public class MaterialReference : ScriptableObject
    {
        #region Vars
            [SerializeField] protected List<Material> materials = new List<Material>();
        #endregion Vars


        #region Methods

        #endregion Methods


        #region Accessors
        public List<Material> Materials
        {
            get
            {
                return materials;
            }
        }
        #endregion Accessors
    }
}