// Created by Isaac Bustad
// Date Created: 1/26/2026


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugFreeProductions.Extentions2D
{
    public static class BugFreeTool2D
    {
        #region Variables
        public static float worldSpeed = 25f;
        #endregion

        #region Methods
        public static void LimitToWorldVelocity(this Vector2 tV2)
        {
            if (tV2.magnitude > worldSpeed)
            {
                // limmit the velosity under word speed

                // store current Velocity


                // create new velocity

                tV2.x = Mathf.Clamp(tV2.x, 0, worldSpeed);
                tV2.y = Mathf.Clamp(tV2.y, 0, worldSpeed);
            }

        }

        public static void LimitVelocity(this Vector2 tV2, float aMag)
        {
            tV2.x = Mathf.Clamp(tV2.x, 0, aMag);
            tV2.y = Mathf.Clamp(tV2.y, 0, aMag);
        }
        #endregion

        #region Accessors

        #endregion
        
    }
}