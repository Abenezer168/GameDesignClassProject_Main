// Created By   :   Isaac Bustad
// Created      :   7/2/2026



using BugFreeProductions.Tools;
using UnityEngine;

public class AccelerationBoost_CMD_Service : CMD_Service
{
    #region Vars
    protected Rigidbody rb = null;


    protected Transform tf = null;

    #endregion Vars


    #region Methods
    // Override a Service to boost a body in an intended direction 
    protected override void Service(GameObject aGO)
        {
            // if we find an AccelerationBoost_Trig_CMD_Client in the request preform:
            if (aGO.GetComponent<AccelerationBoost_Trig_CMD_Client>() is AccelerationBoost_Trig_CMD_Client aAccelerationClient)
            {
                // allow boost based on boost orientation
                if (aAccelerationClient.BoostInDirection)
                {
                    rb.AddForce(aAccelerationClient.BoostDirection * aAccelerationClient.BoostValue, ForceMode.Force);
                }
                else
                {
                    if (tf != null)
                    {
                        Vector3 worldSpaceVector = transform.rotation * Vector3.forward;
                        worldSpaceVector *= aAccelerationClient.BoostValue;

                        // boost the object by the world space vector oriented with the body transform
                        rb.AddForce(worldSpaceVector, ForceMode.Force);
                    }
                }
            } 
            
        }
    #endregion Methods


}
