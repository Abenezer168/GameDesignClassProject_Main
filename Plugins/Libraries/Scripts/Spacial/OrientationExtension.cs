// Created By   :   Isaac Bustad
// Created      :   7/25/2026


using UnityEngine;



namespace BugFreeProductions.Spatial
{
    public static class OrientationExtension
    {
        

        public static float CalculateAngleToTarget(OrientationData current, Vector3 targetPos)
        {
            Quaternion targetRotation = RotationTool.CalculateLookAt(current.positionData, targetPos);
            // Measure the minimal angle between where we are facing vs where we NEED to face
            return Quaternion.Angle(current.rotationData, targetRotation);
        }

        // extend the OrientationData CalculateAngleToTarget
        public static float AngleToTarget(this OrientationData current, Vector3 targetPos)
        {
            Quaternion targetRotation = RotationTool.CalculateLookAt(current.positionData, targetPos);
            // Measure the minimal angle between where we are facing vs where we NEED to face
            return Quaternion.Angle(current.rotationData, targetRotation);
        }

        public static Quaternion CalcNewRotation(   this OrientationData od, 
                                                    Vector3 targetPosition, 
                                                    float progressStep)
        {
        
            Quaternion targetRotation = od.CalculateLookAt(targetPosition);
            {
                // 1. Calculate how far apart the two rotations are in degrees
                float remainingAngleInDegrees = Quaternion.Angle(od.rotationData, targetRotation);
        
                return Quaternion.Slerp(od.rotationData, targetRotation, progressStep);
            }

        }

        public static bool CalcNewRotation( this OrientationData od, 
                                            Vector3 targetPosition,
                                            float progressStep, 
                                            float bufferAngleInDegrees, 
                                            out Quaternion resultRotation)
        {
            Quaternion targetRotation = od.CalculateLookAt(targetPosition);

            // 1. Check if we are already close enough to snap
            if (Quaternion.Angle(od.rotationData, targetRotation) <= bufferAngleInDegrees)
            {
                resultRotation = targetRotation; // Snap to target
                return true;                     // Rotation is finished
            }
            
            // 2. Otherwise, step toward the target
            resultRotation = Quaternion.Slerp(od.rotationData, targetRotation, progressStep);
            return false;
        }

        public static Quaternion CalculateLookAt(this OrientationData od, Vector3 targetPos)
        {
            Vector3 direction = targetPos - od.positionData;
            if (direction.sqrMagnitude < 0.0001f) return Quaternion.identity;
            return Quaternion.LookRotation(direction);
        }
            
    }
}