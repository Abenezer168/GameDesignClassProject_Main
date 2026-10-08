// Created By   :   Isaac Bustad
// Created      :   7/13/2026

using System.Collections.Generic;
//using System.Linq;
using UnityEngine;

namespace BugFreeProductions.Tools
{
    public class MaterialApplier : MonoBehaviour
    {
        #region Vars
        [SerializeField] protected MaterialReference materialReference;
        #endregion Vars


        #region Methods
        protected virtual void OnEnable()
        {
            AssignMaterials();
        }

        protected virtual void AssignMaterials()
        {
            if (GetComponent<MeshRenderer>() is MeshRenderer meshRenderer )
            {
                meshRenderer.materials = materialReference.Materials.ToArray();
            }
        }

        #endregion Methods


    }
}