// Creator Isaac Bustad
// Created: 1/29/2026


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Tools2D{
    [CreateAssetMenu(fileName = "MoveStateParam2D", menuName = "ScriptableObject/MoveStateParam2D")]
    public class MoveStateParam2D : ScriptableObject
    {
        #region Variables
        // set the acceleration of the move state
        [SerializeField] protected float acceleration;

        // set the max speed of the move state
        [SerializeField] protected float maxSpeed;

        #endregion


        #region Methods

        #endregion

        #region Accessors
        public float Acceleration { get { return acceleration; } }
        public float MaxSpeed { get { return maxSpeed; } }
        #endregion
    }
}