// Isaac Bustad
// 5/13/2025


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BugFreeProductions.Tools;
using Unity.VisualScripting;
using UnityEngine;


namespace BugFreeProductions.Tools

{
    public class GameModeNode : MonoBehaviour, ISubscription
    {
        // Vars
        // game player nodes
        [SerializeField] protected List<GamePlayerNode> playerNodes = null;

        // camera viewport management system
        protected CameraViewportManager cameraViewportManager = new CameraViewportManager();

        // store SinglePlayerInputCollector node ranking
        protected Dictionary<SinglePlayerInputCollector,int> playerRanks = new Dictionary<SinglePlayerInputCollector, int>();

        // has the game mode been initialized
        protected bool gameModeInit = false;

        // hold instance statically for easy reference
        protected static GameModeNode instance = null;

        #region Methods
        protected virtual void OnEnable()
        {
            // Make this object a singleton
            MakeSingleton();

            //GameManager_Singleton gms = GameManager_Singleton.Instance;
            if (GameManager_Singleton.Instance != null)
            {
                //gms.GameModeNode = this;
                GameManager_Singleton.Instance.RefreshGameModeNode();
            }

        }

        // Set Defaults
        protected virtual void SetDefaults()
        {

        }

        #region Player Rank Management
        // read initial rank for the player 
        // based on Single Player Input
        protected virtual void ReadInitRank()
        {
            playerRanks = GameManager_Singleton.Instance.PlayerRanks;
        }

        // update local player ranks on node
        protected virtual void UpdatePlayerRank(SinglePlayerInputCollector aSPIC, int aRank)
        {
            playerRanks[aSPIC] += aRank;
        }

        // rank players
        // to be overwritten by child classes
        protected virtual void RankPlayers()
        {


        }

        // update player rank in mannager
        // assumes upatting of all players in game 
        protected virtual void UpdateManagerRanks()
        {
            GameManager_Singleton.Instance.UpdatePlayerRanks(playerRanks);
        }
        #endregion

        #region Player and Camera management


        public virtual void RefreshGameModeInfo()
        {

            //Debug.LogError("Refreshing info: " + this.name);
            // refresh Camera Viewport info
            RefreshCameraViewportReference();

            // refresh Inputs 
            RefreshInputReference();

            // initialized if not initialized
            InitGameMode();


        }

        // initialize the game mode
        protected virtual void InitGameMode()
        {
                gameModeInit = true;
        }

        // method to refresh input assignments
        protected virtual void RefreshInputReference()
        {
            GameManager_Singleton gms = GameManager_Singleton.Instance;

            // set all gameManagerSingalton game node to this node
            //gms.GameModeNode = this;

            // get input collectors from
            List<SinglePlayerInputCollector> spics = gms.PICollectors;


            // assign player node reference to input collectors
            if (spics.Count > 0 && playerNodes.Count >= spics.Count)
            {
                for (int i = 0; i < spics.Count; i++)
                {
                    // assign all input collectors a player node
                    spics[i].PlayerNode = playerNodes[i];

                    // assign all player nodes a input collector
                    playerNodes[i].SinglePlayerInputCollector = spics[i];

                    // look for player node camera
                    Camera camera = playerNodes[i].NodeCamera;

                    // add player cameras if there are any
                    if (camera != null)
                    {
                        cameraViewportManager.ChangeCamViewport(camera, true);
                    }

                }
                Debug.LogError("Player found: " + this.name);
            }
            else if(playerNodes.Count < spics.Count)
            {
                Debug.LogError("Not enough Player Nodes are not added to Player Nodes Array on: " + this.name);
            }

        }

        protected virtual void RefreshCameraViewportReference()
        {
            cameraViewportManager = new CameraViewportManager();

        }

        protected virtual void EndGame()
        {

        }
        #endregion

        protected void MakeSingleton()
        {
            // if there is no instance assign this object
            if (instance == null)
            {
                // assign this object
                instance = this;
            }

            else
            {
                Destroy(this);
            }
        }

        #region ISubscription
        public void Subscribe(ISubscriber subscriber) 
        {
            
        }

        public void Notify()
        {
            
        }

        public void UnSubscribe(ISubscriber subscriber)
        {
            
        }
        #endregion ISubscription

        #endregion Methods

        #region Accessors
        public static GameModeNode Instance
        {
            get
            {
                // return the instance
                return instance;
            }
        }

        #endregion Accessors



    }
}