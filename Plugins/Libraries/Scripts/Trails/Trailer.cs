// Created by   :   Isaac Bustad   
// Created      :   4/28/2026

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace BugFreeProductions.Tools{
    public class Trailer : MonoBehaviour
    {
        #region Vars
        [SerializeField] protected bool isLineTrail;


        [SerializeField] protected TrailMethod trailMethod;
        [SerializeField] protected LineRenderer lineRenderer;
        

        [SerializeField] protected int maxNumberOfPoints = 100;
        [SerializeField] protected float pointDelay = 0.1f;
        [SerializeField] protected float timeToLastPoint = 0f;
        protected List<Vector3> trailPoints = new List<Vector3>();
        #endregion Vars

        #region Methods
        protected virtual void OnEnable()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }
        }
        
        protected virtual void Update()
        {
            DelayPoint(Time.deltaTime);
            if (trailMethod != null)
            {
                RenderTrail();
            }
        }

        protected virtual void DelayPoint(float atime)
        {
            timeToLastPoint += pointDelay;

            if (timeToLastPoint > pointDelay)
            {
                AddPoint();

                timeToLastPoint = 0;
            }
        }

        protected virtual void AddPoint()
        {
            trailPoints.Add(transform.position);

            if (trailPoints.Count > maxNumberOfPoints)
            {
                for (int i = 0; i <= trailPoints.Count - maxNumberOfPoints; i++)
                {
                    trailPoints.RemoveAt(0);
                }
            }

            
        }

        protected virtual void RenderTrail()
        {
            trailMethod.RenderTrail(trailPoints, lineRenderer);
        }




        #endregion Methods




    }
}