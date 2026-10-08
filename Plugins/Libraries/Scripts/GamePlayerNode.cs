// Isaac Bustad
// 5/20/2025



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BugFreeProductions.Tools
{
    // Input Bridge
    public class GamePlayerNode : MonoBehaviour
    {
        
        // Vars
        [SerializeField] protected Camera nodeCamera = null;

        // boolion for if node is player character
        protected bool isPlayerCharacter = false;

        // stored input value
        protected Vector2 lsWSADinput = Vector2.zero;
        protected Vector3 moveDir = Vector3.zero;

        // player context references
        protected Rigidbody rb = null;
        
        // store input collector
        protected SinglePlayerInputCollector singlePlayerInputCollector = null;

        // store game mode node
        protected GameModeNode gameModeNode = null;

        #region Methods
        protected virtual void OnEnable()
        {
            CollectVars();
        }

        protected virtual void CollectVars()
        {
            rb = GetComponent<Rigidbody>();
        }

        #region Camera Management

        #endregion

        #region Input Management
        #region ForPrimary gameplay Buttons
        // expect button callback
        public virtual void PrimaryButtonPress(InputAction.CallbackContext aCON)
        {

        }


        // expect button callback
        public virtual void SecondaryButtonPress(InputAction.CallbackContext aCON)
        {

        }

        // expext callback context
        public virtual void LeftStickAndWSAD(InputAction.CallbackContext aCON)
        {
            // read the value in
            Vector2 rVal = aCON.ReadValue<Vector2>();

            // store the value
            lsWSADinput = rVal;

            moveDir = new Vector3(rVal.x, 0, rVal.y);
        }
        #endregion

        #region AuxButtons for extra functionality
        // expect button callback
        public virtual void ButtonA(InputAction.CallbackContext aCON)
        {

        }
        public virtual void ButtonB(InputAction.CallbackContext aCON)
        {
            
        }
        public virtual void ButtonX(InputAction.CallbackContext aCON)
        {
            
        }
        public virtual void ButtonY(InputAction.CallbackContext aCON)
        {
            
        }

        // expect button callback
        public virtual void PositiveAuxButtonPress(InputAction.CallbackContext aCON)
        {

        }

        // expect button callback
        public virtual void NegativeAuxButtonPress(InputAction.CallbackContext aCON)
        {

        }
        #endregion



        #endregion

        #region Computer Player Toggle
        protected void ChangeIsPlayerCharacter(bool aIPA)
        {
            ComputerPlayerBridge cpb = GetComponent<ComputerPlayerBridge>();

            if (cpb != null)
            {
                cpb.IsPlayerCharacter = aIPA;
            }
        }
        #endregion

        #region Destroy
        protected virtual void OnDestroy()
        {

        }

        #endregion 

        #endregion Methods

        #region Accessors
        public virtual bool IsPlayerCharacter {  set { isPlayerCharacter = value; ChangeIsPlayerCharacter(value); } }
        public virtual Camera NodeCamera { get { return nodeCamera; } }
        public virtual Vector3 MovDir { get { return moveDir; } }

        public virtual Vector2 LSwsadInput {get{return lsWSADinput;}}

        public virtual SinglePlayerInputCollector SinglePlayerInputCollector { get { return singlePlayerInputCollector; } set { singlePlayerInputCollector = value; } }
        
        public virtual GameModeNode GameModeNode 
        { 
            get 
            { 
                if (gameModeNode == null) 
                { 
                    gameModeNode = GetComponentInParent<GameModeNode>(); 
                } 
                return gameModeNode; 
            } 
        }

        #endregion Accessors
    }

    
}