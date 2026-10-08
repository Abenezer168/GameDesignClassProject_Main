// Created By: Isaac Bustad
// Date Created: 1/25/2026



using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BugFreeProductions.Tools2D
{
    public class RunMoveState2D : CharacterMoveState2D
    {
        #region Variables

        #endregion

        #region Methods
        protected override void MoveCharacter()
        {
            if (context.RB2D.linearVelocity.magnitude < context.RunStateParam2D.MaxSpeed)
            {
                //context.RB2D.AddForce(context)
            }
            base.MoveCharacter();
        }
        #endregion

        #region Constructors
        public RunMoveState2D(CharacterMoveContext2D context) : base(context)
        {
        }
        #endregion

        #region Accessors
        public override CharacterMoveStateType2D MoveStateType2D
        {
            get { return CharacterMoveStateType2D.Run; }
        }
        #endregion
    }
}