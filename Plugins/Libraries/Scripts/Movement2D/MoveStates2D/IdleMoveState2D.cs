// Created By: Isaac Bustad
// Date Created: 1/25/2026



using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BugFreeProductions.Tools2D
{
    public class IdleMoveState2D : CharacterMoveState2D
    {
        #region Variables

        #endregion

        #region Methods

        #endregion

        #region Constructors
        public IdleMoveState2D(CharacterMoveContext2D context) : base(context)
        {
        }
        #endregion

        #region Accessors
        public override CharacterMoveStateType2D MoveStateType2D
        {
            get { return CharacterMoveStateType2D.Idle; }
        }
        #endregion
    }
}