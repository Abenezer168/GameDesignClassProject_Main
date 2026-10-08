// Created By: Isaac Bustad
// Date Created: 1/25/2026


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BugFreeProductions.Extentions2D;



namespace BugFreeProductions.Tools2D
{
    public class CharacterMoveState2D
    {
        #region Variables
        // context of the move state
        protected CharacterMoveContext2D context = null;
        

        #endregion

        #region Methods
        // actions to perform on fixed update
        public virtual void OnFixedUpdate()
        {
            MoveCharacter();
        }

        // actions to perform on update
        public virtual void OnUpdate()
        {

        }

        // move the character
        protected virtual void MoveCharacter()
        {
            BugFreeTool2D.LimitToWorldVelocity(context.RB2D.linearVelocity);
        }

        #region Change State Methods
        // change to Idle State
        protected virtual void ToIdle()
        {
            context.CurrentMoveState = context.IdleState;
        }

        // change to Walk State
        protected virtual void ToWalk()
        {
            context.CurrentMoveState = context.WalkState;
        }

        // change to Run State
        protected virtual void ToRun()
        {
            context.CurrentMoveState = context.RunState;
        }

        // change to Jump State
        protected virtual void ToJump()
        {
            context.CurrentMoveState = context.JumpState;
        }

        // change to Attack State
        protected virtual void ToAttack()
        {
            context.CurrentMoveState = context.AttackState;
        }


        #endregion

        #endregion

        #region Constructors
        public CharacterMoveState2D(CharacterMoveContext2D aContext)
        {
            this.context = aContext;
        }
        #endregion

        #region Accessors
        public virtual CharacterMoveStateType2D MoveStateType2D
        {
            get { return CharacterMoveStateType2D.Idle; }
        }
        #endregion
    }
}