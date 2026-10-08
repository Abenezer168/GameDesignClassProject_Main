// Created by   :   Isaac Bustad   
// Created      :   4/26/2026


using System.Collections.Generic;

using UnityEngine;
using BugFreeProductions.Spatial;
//using Vector3 = UnityEngine.Vector3;

namespace BugFreeProductions.Tools
{
    public enum TrailMethodType {standard,drift,boost}



    [CreateAssetMenu(fileName = "TrailMethod", menuName = "ScriptableObject/TrailMethod")]
    public class TrailMethod : ScriptableObject
    {
        #region Vars
        [SerializeField] protected int maxNumberOfPoints = 100;
        [SerializeField] protected float pointDelay = 0.1f;
        [SerializeField] protected float timeToLastPoint = 0f;



        #endregion Vars

        #region Methods

        #region TrailRendering
        public void RenderTrail(List<Vector3> positions, LineRenderer lineRenderer)
        {
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());
        }



        // Calculates for multiple trails
        public void RenderTrail(List<Vector3> aPos1, LineRenderer aLR1, List<Vector3> aPos2, LineRenderer aLR2)
        {
            aLR1.positionCount = aPos1.Count;
            aLR1.SetPositions(aPos1.ToArray());

            aLR2.positionCount = aPos2.Count;
            aLR2.SetPositions(aPos2.ToArray());
        }




        // take in a list of line renderers and lists of positions
        public void RenderTrail(List<List<Vector3>> aAllPos, List<LineRenderer> aAllLRs)
        {
            // store new lists of positions
            //List<List<Vector3>> nAllPos = CalculateNewListOfPositions(aAllPos);
            //Debug.Log("CallMe");
            // Draw the Lines nPos1.Add(aPos1[i] - (driftDir*i*aDrift/100));
            for (int l = 0; l < aAllPos.Count; l++)
            {
                // create available line renderer indexes
                aAllLRs[l].positionCount = aAllPos[l].Count;

                // assign positions to line renderer indexes
                aAllLRs[l].SetPositions(aAllPos[l].ToArray());

            }

        }


        // To do make new lines that push away from the center
        public void RenderTrail(List<List<Vector3>> aAllPos, List<LineRenderer> aAllLRs, List<Vector3> aCenterPos, float aDrift)
        {

            int minCount = CalculateMinCount(aAllPos);


            //Debug.Log("Called");
            // hold new all poses list
            List<List<Vector3>> nAllPos = new List<List<Vector3>>();
            //Debug.Log("new list made");

            //Debug.Log("Called" + aAllPos.Count());

            for(int l = 0; l < aAllPos.Count; l++)
            {
                //Debug.Log("Enter 4 loop = " + l);
                // hold the new pos list
                List<Vector3> nPos = new List<Vector3>();

                // 
                for (int i = 0; i < minCount; i++)
                {
                    // calc drift direction
                    Vector3 driftDir = aAllPos[l][i] - aCenterPos[i];
                    driftDir.Normalize();

                    driftDir *= aDrift / (i+1);

                    // calculate the new position
                    Vector3 pos = aAllPos[l][i] + driftDir;

                    // add the new position to the list

                    nPos.Add(pos);


                }
                //Debug.Log("tst adding to all nPos");
                nPos = nPos.GetRange(0,minCount-1);



                // add the new pos list to the list
                nAllPos.Add(nPos);

            }

            for (int l = 0; l < nAllPos.Count; l++)
            {
                //Debug.Log("Render");
                RenderTrail(nAllPos[l],aAllLRs[l]);
            }
        }

        // Render trail with offset
        public void RenderTrail(List<List<Vector3>> aAllPos, List<LineRenderer> aAllLRs, List<Vector3> aCenterPos, float aDrift, float aOffset)
        {

            int minCount = CalculateMinCount(aAllPos);


            //Debug.Log("Called");
            // hold new all poses list
            List<List<Vector3>> nAllPos = new List<List<Vector3>>();
            //Debug.Log("new list made");

            //Debug.Log("Called" + aAllPos.Count());

            for(int l = 0; l < aAllPos.Count; l++)
            {
                //Debug.Log("Enter 4 loop = " + l);
                // hold the new pos list
                List<Vector3> nPos = new List<Vector3>();

                // 
                for (int i = 0; i < minCount; i++)
                {
                    // calc drift direction
                    Vector3 driftDir = aAllPos[l][i] - aCenterPos[i];
                    driftDir.Normalize();

                    driftDir *= (aDrift / (i+1))+aOffset;

                    // calculate the new position
                    Vector3 pos = aAllPos[l][i] + driftDir;

                    // add the new position to the list

                    nPos.Add(pos);


                }
                //Debug.Log("tst adding to all nPos");
                nPos = nPos.GetRange(0,minCount-1);



                // add the new pos list to the list
                nAllPos.Add(nPos);

            }

            for (int l = 0; l < nAllPos.Count; l++)
            {
                //Debug.Log("Render");
                RenderTrail(nAllPos[l],aAllLRs[l]);
            }
        }

        // Render trail with offset
        public void RenderTrail(List<List<Vector3>> aAllPos, List<LineRenderer> aAllLRs, List<Vector3> aCenterPos, float aDrift, float aOffset, GameObject aSpawnable, float aDistBetweenSpawns)
        {

            int minCount = CalculateMinCount(aAllPos);


            //Debug.Log("Called");
            // hold new all poses list
            List<List<Vector3>> nAllPos = new List<List<Vector3>>();
            //Debug.Log("new list made");

            //Debug.Log("Called" + aAllPos.Count());

            for(int l = 0; l < aAllPos.Count; l++)
            {
                //Debug.Log("Enter 4 loop = " + l);
                // hold the new pos list
                List<Vector3> nPos = new List<Vector3>();

                // 
                for (int i = 0; i < minCount; i++)
                {
                    // calc drift direction
                    Vector3 driftDir = aAllPos[l][i] - aCenterPos[i];
                    driftDir.Normalize();

                    driftDir *= (aDrift / (i+1))+aOffset;

                    // calculate the new position
                    Vector3 pos = aAllPos[l][i] + driftDir;

                    // add the new position to the list

                    nPos.Add(pos);


                }
                //Debug.Log("tst adding to all nPos");
                nPos = nPos.GetRange(0,minCount-1);



                // add the new pos list to the list
                nAllPos.Add(nPos);

            }

            for (int l = 0; l < nAllPos.Count; l++)
            {
                //Debug.Log("Render");
                RenderTrail(nAllPos[l],aAllLRs[l]);
            }
        }

        #endregion TrailRendering

        public void RenderTrail(int aTrailCount, List<LineRenderer> aAllLRs, List<OrientationData> aOrientations, List<OrientationDirection> aDirections, float aDrift, float aOffset)
        {
            //Dictionary<OrientationDirection, List<Vector3>> trailsByData = new Dictionary<OrientationDirection, List<Vector3>>();
            List<List<Vector3>> allTrailList = new List<List<Vector3>>();
            foreach (OrientationDirection dir in aDirections)
            {
                List<Vector3> posLst = new List<Vector3>();

                for (int i = 0; i < aOrientations.Count; i++)
                {
                    // calc drift direction
                    Vector3 driftDir = aOrientations[i].Direction(dir);
                    driftDir.Normalize();

                    driftDir *= (aDrift / (i+1)) + aOffset;

                    // calculate the new position
                    Vector3 pos = aOrientations[i].positionData + driftDir;

                    // add the new position to the list

                    posLst.Add(pos);


                }

                //trailsByData.Add(dir, posLst);
                allTrailList.Add(posLst);
            }
            RenderTrail(allTrailList, aAllLRs);
            Debug.Log("New Render Ran");
            
        }

        public void RenderTrail(List<LineRenderer> aAllLRs, List<OrientationData> aOrientations, List<OrientationDirection> aDirections, float aDrift, float aTrailOffset, Vector3 aOffset)
        {
            //Dictionary<OrientationDirection, List<Vector3>> trailsByData = new Dictionary<OrientationDirection, List<Vector3>>();
            List<List<Vector3>> allTrailList = new List<List<Vector3>>();
            foreach (OrientationDirection dir in aDirections)
            {
                List<Vector3> posLst = new List<Vector3>();

                for (int i = 0; i < aOrientations.Count; i++)
                {
                    // calc drift direction
                    Vector3 driftDir = aOrientations[i].Direction(dir);
                    driftDir.Normalize();

                    driftDir *= (aDrift / (i+1)) + aTrailOffset ;
                    driftDir += aOrientations[i].rotationData * aOffset;

                    // calculate the new position
                    Vector3 pos = aOrientations[i].positionData + driftDir;

                    // add the new position to the list

                    posLst.Add(pos);


                }

                //trailsByData.Add(dir, posLst);
                allTrailList.Add(posLst);
            }
            RenderTrail(allTrailList, aAllLRs);
            //Debug.Log("New Render Ran");
            
        }



        #region Trail Calculating
        protected virtual int CalculateMinCount(List<List<Vector3>> aAllPos)
        {
            // store the minimum
            int minCount = aAllPos[0].Count;

            // loop and find the minimum

            foreach (List<Vector3> aPos in aAllPos)
            {
                if (aPos.Count < minCount)
                {
                    minCount = aPos.Count;

                }
            }

            // return the minimum
            return minCount - 1;
        }


        protected virtual List<List<Vector3>> CalculateNewListOfPositions(List<List<Vector3>> aAllPos)
        {
            // store the minimum
            int minCount = CalculateMinCount(aAllPos);
            Debug.Log("Minimum count = " + minCount );

            // store new lists of positions
            List<List<Vector3>> nAllPos = new List<List<Vector3>>();


            // loop and clamp the V3 list leangths
            foreach (List<Vector3> aPos in aAllPos)
            {
                // construct new clamped list
                List<Vector3> nPos = aPos.GetRange(0, minCount);

                // add clamped list to new all list
                nAllPos.Add(nPos);
            }


            // return new list of positions
            return nAllPos;
        }




        #endregion Trail Calculating




        #endregion Methods


        #region Accessors

        #endregion Accessors
    }
}
