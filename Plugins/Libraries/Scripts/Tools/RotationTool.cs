// Created By   :   Isaac Bustad
// Created      :   7/25/2026


using UnityEngine;

namespace BugFreeProductions.Spatial
{
    public static class RotationTool
    {
        public static Quaternion CalcNewRotation( Vector3 startForward, Vector3 endForward, Vector3 startUp, Vector3 endUp, float rateOfDecay)
        {
            Vector3 targetForward = (1f - rateOfDecay) * startForward + rateOfDecay * endForward;
            Vector3 targetUp = (1f - rateOfDecay) * startUp + rateOfDecay * endUp;

            return Quaternion.LookRotation(targetForward,targetUp);
        }

        

        
        public static Quaternion StepRotateWithBuffer(Quaternion current, Quaternion target, float maxDegreesDelta, float bufferAngle, out bool isFinished)
        {
            // 1. Find the remaining angle in degrees between rotations
            float remainingAngle = Quaternion.Angle(current, target);

            // 2. Check if we are within the snap threshold
            if (remainingAngle <= bufferAngle)
            {
                isFinished = true;
                return target;
            }

            // 3. Step forward by maxDegreesDelta degrees without any lerp/slerp smoothing
            isFinished = false;
            return Quaternion.RotateTowards(current, target, maxDegreesDelta);
        }

        public static Quaternion CalcNewRotation(   Quaternion currentRotation, 
                                                    Quaternion targetRotation, 
                                                    float progressStep)
        {
            // 1. Calculate how far apart the two rotations are in degrees
            float remainingAngleInDegrees = Quaternion.Angle(currentRotation, targetRotation);
    
            return Quaternion.Slerp(currentRotation, targetRotation, progressStep);
        }

        public static bool CalcNewRotation( Quaternion currentRotation, 
                                            Quaternion targetRotation, 
                                            float progressStep, 
                                            float bufferAngleInDegrees, 
                                            out Quaternion resultRotation)
        {
            // 1. Check if we are already close enough to snap
            if (Quaternion.Angle(currentRotation, targetRotation) <= bufferAngleInDegrees)
            {
                resultRotation = targetRotation; // Snap to target
                return true;                     // Rotation is finished
            }
            
            // 2. Otherwise, step toward the target
            resultRotation = Quaternion.Slerp(currentRotation, targetRotation, progressStep);
            return false;
        }

        public static Quaternion CalculateLookAt(Vector3 currentPos, Vector3 targetPos)
        {
            Vector3 direction = targetPos - currentPos;
            if (direction.sqrMagnitude < 0.0001f) return Quaternion.identity;
            return Quaternion.LookRotation(direction);
        }
    }
}
