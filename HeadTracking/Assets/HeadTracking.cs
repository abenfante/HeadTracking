using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class HeadTracking : MonoBehaviour
{
    public UDPReceive uDPReceive; //script to access data from webcam
    public GameObject cameraObject; //gameobject of camera
    public Transform cameraTarget;
    public float focalDistance = 1, scalingFactor = 5;
    public Vector2 cameraResolution = new(640,480);


    List<float> xList = new();
    List<float> yList = new();
    List<float> zList = new();

    
    void Update()
    {
        string data = uDPReceive.data;
        //since it arrives (x,y) removes the ()
        if (data != "")
        {
            float xBBPos, yBBpos, BBsize;
            ParseAndScaleBBData(data, out xBBPos, out yBBpos, out BBsize);
            float xAverage, yAverage, sizeAverage;
            AverageBBData(xBBPos, yBBpos, BBsize, out xAverage, out yAverage, out sizeAverage);



            cameraObject.transform.localPosition = BBDataToCameraPosition(xAverage, yAverage, sizeAverage, focalDistance, scalingFactor, debug:true);


            // we must assume the direction of the gaze towards a target, as we dont really know it
            cameraObject.transform.LookAt(cameraTarget, Vector3.up);
        }
        
    }

    private Vector3 BBDataToCameraPosition(float x, float y, float size, float focalDistance, float scalingFactor, bool debug) 
    {
        //TO DO: update this caluculation with a more sofisticated one to map the point distance correctly
        float pointDistance = scalingFactor / size;
        float p = pointDistance / focalDistance;

        if (debug)
        {
            
            float h = cameraResolution.x / 200 * p, v = cameraResolution.y / 200 * p;

            //draw current plane
            Debug.DrawLine(new(h, v, -pointDistance), new(h, -v, -pointDistance), Color.green);
            Debug.DrawLine(new(h, -v, -pointDistance), new(-h, -v, -pointDistance), Color.green);
            Debug.DrawLine(new(-h, -v, -pointDistance), new(-h, v, -pointDistance), Color.green);
            Debug.DrawLine(new(-h, v, -pointDistance), new(h, v, -pointDistance), Color.green);
            
            //draw camera movement frustrum
            float p1 = -(scalingFactor / 1f) / focalDistance; //smallest distance
            float pD1 = -(scalingFactor / 1f); //smallest distance
            float p2 = -(scalingFactor / 0.1f) / focalDistance; //biggest distance
            float pD2 = -(scalingFactor / 0.1f); //biggest distance
            //closest plane
            float h_c = cameraResolution.x / 200 * p1;
            float v_c = cameraResolution.y / 200 * p1;
            Debug.DrawLine(new(h_c, v_c, pD1), new(h_c, -v_c, pD1), Color.red);
            Debug.DrawLine(new(h_c, -v_c, pD1), new(-h_c, -v_c, pD1), Color.red);
            Debug.DrawLine(new(-h_c, -v_c, pD1), new(-h_c, v_c, pD1), Color.red);
            Debug.DrawLine(new(-h_c, v_c, pD1), new(h_c, v_c, pD1), Color.red);
            //far plane
            float h_f = cameraResolution.x / 200 * p2;
            float v_f = cameraResolution.y / 200 * p2;
            Debug.DrawLine(new(h_f, v_f, pD2), new(h_f, -v_f, pD2), Color.red);
            Debug.DrawLine(new(h_f, -v_f, pD2), new(-h_f, -v_f, pD2), Color.red);
            Debug.DrawLine(new(-h_f, -v_f, pD2), new(-h_f, v_f, pD2), Color.red);
            Debug.DrawLine(new(-h_f, v_f, pD2), new(h_f, v_f, pD2), Color.red);
            //edges
            Debug.DrawLine(new(h_c, v_c, pD1), new(h_f, v_f, pD2), Color.red);
            Debug.DrawLine(new(h_c, -v_c, pD1), new(h_f, -v_f, pD2), Color.red);
            Debug.DrawLine(new(-h_c, v_c, pD1), new(-h_f, v_f, pD2), Color.red);
            Debug.DrawLine(new(-h_c, -v_c, pD1), new(-h_f, -v_f, pD2), Color.red);

        }


        return new Vector3(-x * p, -y * p, -pointDistance);
    }

    private void AverageBBData(float x, float y, float z, out float xAverage, out float yAverage, out float zAverage)
    {
        xList.Add(x);
        yList.Add(y);
        zList.Add(z);

        //adds a bunch of this coordinates to obtain an average to move the camera smoothly
        if (xList.Count > 50) { xList.RemoveAt(0); }
        if (yList.Count > 50) { yList.RemoveAt(0); }
        if (zList.Count > 50) { zList.RemoveAt(0); }

        xAverage = Queryable.Average(xList.AsQueryable());
        yAverage = Queryable.Average(yList.AsQueryable());
        zAverage = Queryable.Average(zList.AsQueryable());
    }

    private void ParseAndScaleBBData(string data, out float xBBpos, out float yBBpos, out float BBsize)
    {
        data = data.Substring(1, data.Length - 2); //discard first and last character

        string[] points = data.Split(','); //since data arrives in x,y it splits

        // we transform the bounding box position so that it is 0 in the center and is a small number at the edges
        xBBpos = (float.Parse(points[0]) - cameraResolution.x / 2f) / 100; // between -h_res/2 and h_res/2
        yBBpos = (float.Parse(points[1]) - cameraResolution.y / 2f) / 100; // between -v_res/2 and v_res/2

        // we normalize the bounding box size to 640, its maximum size
        BBsize = float.Parse(points[2]) / Mathf.Max(cameraResolution.x, cameraResolution.y);
    }
}
