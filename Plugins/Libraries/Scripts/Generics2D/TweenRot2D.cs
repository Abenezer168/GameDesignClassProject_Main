// Created by Isaac Bustad
// Created on 1/29/2026


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TweenRot2D : MonoBehaviour
{
    #region Variables
    [SerializeField] protected float rotationRate;

    #endregion

    #region Methods
    protected void RotateOverTime()
    {
        Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, rotationRate), Time.fixedDeltaTime);
    }
    #endregion

    #region Accessors

    #endregion
}
