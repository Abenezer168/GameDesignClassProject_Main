// Created by   : Isaac Bustad
// Created      : 2/19/2026

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerAndRenderOrder : MonoBehaviour
{
    #region Variables
    // this variable selects a target layer for render and collision
    [SerializeField, Range(10,30)] protected int targetLayer = 0;

    // array of all objects that need their layers changed
    [SerializeField] protected List<GameObject> targetObjects = new List<GameObject>();

    #endregion Variables


    #region Methods

    #region Unity Methods
    // Unity Event Methods
    protected virtual void OnEnable() 
    {
        // change to the specified layers and ordering
        ChangeToTargetLayers();
    }

    #endregion Unity Methods

    // change layers
    protected virtual void ChangeToTargetLayers()
    {
        ChangeLayer();
        ChangeRenderLayer();
    }


    // method that changes the objects layer and renderlayer 
    protected virtual void ChangeLayer()
    {
        if (targetObjects.Count > 0)
        {
            foreach ( GameObject target in targetObjects)
            {
                // change the game objects collision layer
                gameObject.layer = targetLayer;
            }
        }

        else
        {
            // change the game objects collision layer
            gameObject.layer = targetLayer;
        }
        

    }

    protected virtual void ChangeRenderLayer()
    {
        if (targetObjects.Count > 0)
        {
            foreach ( GameObject target in targetObjects)
            {
                SpriteRenderer aSPR = target.GetComponent<SpriteRenderer>();
                // change the game objects collision layer
                aSPR.sortingOrder  = targetLayer;
            }
        }

        else
        {
            // find refference for local sprite renderer
            SpriteRenderer aSPR = GetComponent<SpriteRenderer>();
            aSPR.sortingOrder  = targetLayer;
        }
        
    }

    #endregion Methods

    #region Accessors

    #endregion Accessors
}
