// Created By   :   Isaac Bustad
// Created      :   2/21/2026
// Assisted     :   Qwen 2.5 7b





using UnityEngine;
using Vector3 = UnityEngine.Vector3;


namespace BugFreeProductions.Spatial
{
    public enum OrientationDirection
    {
        forward,
        backward,
        left,
        right,
        up,
        down
    }

    /// <summary>
    /// Represents orientation data including position, rotation, and direction vectors.
    /// </summary>
    public struct OrientationData
    {
        /// <summary>
        /// The position data.
        /// </summary>
        public Vector3 positionData { get; private set; }

        /// <summary>
        /// The rotation data.
        /// </summary>
        public Quaternion rotationData { get; private set; }

        /// <summary>
        /// Gets the direction vector based on the given orientation direction.
        /// </summary>
        /// <param name="aDirection">The orientation direction.</param>
        /// <returns>The direction vector.</returns>
        public Vector3 Direction(OrientationDirection aDirection)
        {
            // hold the calculated direction result
            Vector3 direction = Vector3.zero;

            switch (aDirection)
            {
                case OrientationDirection.forward:
                    direction = rotationData * Vector3.forward;
                    break;

                case OrientationDirection.backward:
                    direction = rotationData * -Vector3.forward;
                    break;

                case OrientationDirection.left:
                    direction = rotationData * -Vector3.right;
                    break;

                case OrientationDirection.right:
                    direction = rotationData * Vector3.right;
                    break;

                case OrientationDirection.up:
                    direction = rotationData * Vector3.up;
                    break;

                case OrientationDirection.down:
                    direction = rotationData * -Vector3.up;
                    break;

            }

            return direction;

        }

        /// <summary>
        /// Initializes a new instance of the OrientationData struct.
        /// </summary>
        /// <param name="aPos">The position data.</param>
        /// <param name="aRot">The rotation data.</param>
        public OrientationData(Vector3 aPos, Quaternion aRot)
        {
            positionData = aPos;
            rotationData = aRot;
        }
    }
}