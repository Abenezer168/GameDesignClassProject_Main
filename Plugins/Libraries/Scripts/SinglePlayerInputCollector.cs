// Isaac Bustad
// 5/13/2025


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace BugFreeProductions.Tools

{
    public class SinglePlayerInputCollector : MonoBehaviour
    {

        // Vars
        //protected GameModeNode gameModeNode = null;
        [SerializeField] protected GamePlayerNode playerNode = null;


        // Methods
        public virtual void OnGameModeNodeChange(GameModeNode aGMN)
        {
            //gameModeNode = aGMN;
        }



        // done on Enable to save resources
        //Setup my vars and set external references
        protected virtual void OnEnable()
        {
            CollectVariables();
        }

        protected virtual void CollectVariables()
        {
            //pib
            //GameMannager_Singleton.Instance.onGameNodeChangeDel += OnGameModeNodeChange;
            transform.parent = GameManager_Singleton.Instance.transform;
        }

        #region Input Management
        #region ForPrimary gameplay Buttons
        // expect button callback
        public virtual void PrimaryButtonPress(InputAction.CallbackContext aCON)
        {
            if (playerNode != null)
            {
                playerNode.PrimaryButtonPress(aCON);
            }
        }


        // expect button callback
        public virtual void SecondaryButtonPress(InputAction.CallbackContext aCON)
        {
            if(playerNode != null)
            {
                playerNode.SecondaryButtonPress(aCON);
            }
        }

        // expext callback context
        // WSAD and left stick
        public virtual void LeftStickAndWSAD(InputAction.CallbackContext aCON)
        {
            //Debug.Log(aCON.ReadValue<Vector2>());
            if (playerNode != null)
            {
                playerNode.LeftStickAndWSAD(aCON);
            }
        }
        #endregion

        #region AuxButtons for extra functionality
        // expect button callback
        public virtual void ButtonA(InputAction.CallbackContext aCON)
        {
            if (playerNode != null)
            {
                playerNode.ButtonA(aCON);
            }
        }

        public virtual void ButtonB(InputAction.CallbackContext aCON)
        {
            if (playerNode != null)
            {
                playerNode.ButtonB(aCON);
            }
        }

        public virtual void ButtonX(InputAction.CallbackContext aCON)
        {
            if (playerNode != null)
            {
                playerNode.ButtonX(aCON);
            }
        }

        public virtual void ButtonY(InputAction.CallbackContext aCON)
        {
            if (playerNode != null)
            {
                playerNode.ButtonY(aCON);
            }
        }

        

        // expect button callback
        public virtual void PositiveAuxButtonPress(InputAction.CallbackContext aCON)
        {
            if (playerNode != null)
            {
                playerNode.PositiveAuxButtonPress(aCON);
            }
        }

        // expect button callback
        public virtual void NegativeAuxButtonPress(InputAction.CallbackContext aCON)
        {
            if ( playerNode != null)
            {
                playerNode.NegativeAuxButtonPress(aCON);
            }
        }
        #endregion

        #endregion

        #region Destroy Logic
        #endregion
        // Accessors
        public virtual GamePlayerNode PlayerNode { get { return playerNode; } set { playerNode = value; playerNode.IsPlayerCharacter = true; } }


    }
}