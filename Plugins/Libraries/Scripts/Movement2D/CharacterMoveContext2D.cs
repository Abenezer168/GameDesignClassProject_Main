// Created By: Isaac Bustad
// Date Created: 1/25/2026


using System.Collections;
using System.Collections.Generic;
using BugFreeProductions.Tools;
using UnityEngine;

namespace BugFreeProductions.Tools2D
{
    public enum CharacterMoveStateType2D
    { Idle,
        Walk,
        Run,
        Jump,
        Attack
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterMoveContext2D : MonoBehaviour
    {
        #region Variables
        // initial movement state
        protected CharacterMoveState2D currentState = null;

        // possible movement states
        protected IdleMoveState2D idleState = null;
        protected WalkMoveState2D walkState = null;
        protected RunMoveState2D runState = null;
        protected JumpMoveState2D jumpState = null;
        protected AttackMoveState2D attackState = null;


        // movement state parameters
        [SerializeField] protected MoveStateParam2D idleStateParam2D = null;
        [SerializeField] protected MoveStateParam2D walkStateParam2D = null;
        [SerializeField] protected MoveStateParam2D runStateParam2D = null;
        [SerializeField] protected MoveStateParam2D jumpStateParam2D = null;
        [SerializeField] protected MoveStateParam2D attackStateParam2D = null;
        
        // enum for state checking
        // current move state type 
        protected CharacterMoveStateType2D currentMoveStateType2D = CharacterMoveStateType2D.Idle;
        
        // last move state type
        protected CharacterMoveStateType2D lastMoveStateType2D = CharacterMoveStateType2D.Idle;

        // reference for playerInputNode2D
        protected GamePlayerNode playerInputNode = null;

        // reference for Rigidbody2D
        protected Rigidbody2D rb2D = null;


        #endregion

        #region Methods
        // set defaults for class
        protected virtual void SetDefault()
        {
            // create states
            idleState = new IdleMoveState2D(this);
            walkState = new WalkMoveState2D(this);
            runState = new RunMoveState2D(this);
            jumpState = new JumpMoveState2D(this);
            attackState = new AttackMoveState2D(this);

            // assign initial state
            currentState = idleState;

            // get reference to playerInputNode2D
            playerInputNode = GetComponent<GamePlayerNode>();

            // get reference to Rigidbody2D
            rb2D = GetComponent<Rigidbody2D>();
        }

        // Unity OnEnable
        protected virtual void OnEnable()
        {
            // set the defaults
            SetDefault();
        }

        // Physics Update
        protected virtual void FixedUpdate()
        {
            currentState.OnFixedUpdate();
        }

        // Frame Update
        protected virtual void Update()
        {
            currentState.OnUpdate();
        }


        #endregion


        #region Accessors
        
        #region State Accessors
        public CharacterMoveState2D CurrentMoveState
        {
            get { return currentState; } 
            set { currentState = value; }
        }
        public IdleMoveState2D IdleState
        {
            get { return idleState; }
        }
        public WalkMoveState2D WalkState
        {
            get { return walkState; }
        }
        public RunMoveState2D RunState
        {
            get { return runState; }
        }
        public JumpMoveState2D JumpState
        {
            get { return jumpState; }
        }
        public AttackMoveState2D AttackState
        {
            get { return attackState; }
        }
        #endregion

        #region State Param Accessors
        public MoveStateParam2D IdleStateParam2D
        {
            get { return idleStateParam2D; }
        }
        public MoveStateParam2D WalkStateParam2D
        {
            get { return walkStateParam2D; }
        }
        public MoveStateParam2D RunStateParam2D
        {
            get { return runStateParam2D; }
        }
        public MoveStateParam2D JumpStateParam2D
        {
            get { return jumpStateParam2D; }
        }
        public MoveStateParam2D AttackStateParam2D
        {
            get { return attackStateParam2D; }
        }
        #endregion

        // rb2D accessor
        public Rigidbody2D RB2D
        {
            get { return rb2D; }
        }
        #endregion
    }
    
}
