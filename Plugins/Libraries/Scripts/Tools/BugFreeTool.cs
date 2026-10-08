// Isaac Bustad
// 8/9/2024


using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace BugFreeProductions.Extentions
{
    public static class BugFreeTool
    {
        #region Velocity Limiter World Speed
        public const float worldSpeed = 25f;

        public static void LimitToWorldVelocity(this Vector3 tV3)
        {
            if (tV3.magnitude > worldSpeed)
            {
                // limmit the velosity under word speed

                // store current Velocity


                // create new velocity

                tV3.x = Mathf.Clamp(tV3.x, 0, worldSpeed);
                tV3.y = Mathf.Clamp(tV3.y, 0, worldSpeed);
                tV3.z = Mathf.Clamp(tV3.z, 0, worldSpeed);
            }

        }

        public static void LimitVelocity(this Vector3 tV3, float aMag)
        {
            tV3.x = Mathf.Clamp(tV3.x, 0, aMag);
            tV3.y = Mathf.Clamp(tV3.y, 0, aMag);
            tV3.z = Mathf.Clamp(tV3.z, 0, aMag);
        }

        

        
        #endregion
    }
}
