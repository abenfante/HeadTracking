using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class HeadTracking : MonoBehaviour
{
    public UDPReceive uDPReceive; //script to access data from webcam
    public GameObject cameraObject; //gameobject della camera virtuale
    public Transform cameraTarget; //oggetto relativo al quale si muove la camera virtuale
    public float focalDistance = 1, scalingFactor = 5; //lunghezza focale della webcam e fattore di scala tra spazio 3D reale e virtuale
    public Vector2 cameraResolution = new(640,480); //risoluzione della webcam
    public float DistanceToVCameraFocalLenghtFactor = 1f; //fattore di conversione tra distanza della testa e lunghezza focale della camera virtuale 
    public float lensShiftFactor = 1;
    List<float> xList = new();
    List<float> yList = new();
    List<float> zList = new();

    
    void Update()
    {
        string data = uDPReceive.data;
        
        if (data != "")
        {
            float xBBPos, yBBpos, BBsize;
            ParseAndScaleBBData(data, out xBBPos, out yBBpos, out BBsize);
            float xAverage, yAverage, sizeAverage;
            AverageBBData(xBBPos, yBBpos, BBsize, out xAverage, out yAverage, out sizeAverage);


            float headDistance;

            Vector3 headPosition = BBDataToHeadPosition(xAverage, yAverage, sizeAverage, focalDistance, scalingFactor, out headDistance, debug: true);
            cameraObject.transform.localPosition = headPosition;
            Camera cam = GetComponent<Camera>();
            cam.focalLength = DistanceToVCameraFocalLenghtFactor * headDistance / scalingFactor;
            cam.lensShift = - headPosition * lensShiftFactor;

            //Debug.Log(cam.scaledPixelHeight);
            //Debug.Log(cam.pixelRect);
            
            // assumiamo che l'utente guardi verso un determinato oggetto
            //cameraObject.transform.LookAt(cameraTarget, Vector3.up);

        }
        
    }

    private Vector3 BBDataToHeadPosition(float x, float y, float size, float focalDistance, float scalingFactor, out float headDistance, bool debug) 
    {
        /*
        -rendere la camera così costruita facilmente spostabile e ruotabile
        -inserire funzionalità per regolare scala e lunghezza focale durante l'uso
        - migliorare accuratezza del calcolo della distanza
        */

        // distanza ricostruita del volto dalla camera, lungo l'asse ottico
        float pointDistance = scalingFactor / size;
        // fattore di trasformazione dalle coordinate dell'immagine alle coordinate 3D perpendicolari all'asse ottico
        float p = pointDistance / focalDistance;

        headDistance = pointDistance;

        if (debug)
        {
            float h = cameraResolution.x / 200 * p, v = cameraResolution.y / 200 * p;

            Matrix4x4 childToParentMatrix = transform.parent.localToWorldMatrix;

            Vector3[] currPlane = {childToParentMatrix.MultiplyPoint(new( h,  v, -pointDistance)),
                                   childToParentMatrix.MultiplyPoint(new( h, -v, -pointDistance)),
                                   childToParentMatrix.MultiplyPoint(new(-h, -v, -pointDistance)),
                                   childToParentMatrix.MultiplyPoint(new(-h,  v, -pointDistance))};
            

            //piano dove si trova il volto
            Debug.DrawLine(currPlane[0], currPlane[1], Color.green);
            Debug.DrawLine(currPlane[1], currPlane[2], Color.green);
            Debug.DrawLine(currPlane[2], currPlane[3], Color.green);
            Debug.DrawLine(currPlane[3], currPlane[0], Color.green);
            
            //Disegno del frustum della camera
            //  calcoli preliminari, la distanza minima si ha quando il volto riempe l'inquadratura (size = 1),
            //  la massima quando è molto piccolo (size = 0.1).
            //  Questo disegno serve solo a visualizzare lo spazio visto dalla camera nel mondo 3D
            float p1 = -(scalingFactor / 1f) / focalDistance; //smallest distance
            float pD1 = -(scalingFactor / 1f); //smallest distance
            float p2 = -(scalingFactor / 0.1f) / focalDistance; //biggest distance
            float pD2 = -(scalingFactor / 0.1f); //biggest distance
            //  disegno del piano più vicino
            float h_c = cameraResolution.x / 200 * p1;
            float v_c = cameraResolution.y / 200 * p1;
            Vector3[] closePlane = {childToParentMatrix.MultiplyPoint(new( h_c,  v_c, pD1)),
                                    childToParentMatrix.MultiplyPoint(new( h_c, -v_c, pD1)),
                                    childToParentMatrix.MultiplyPoint(new(-h_c, -v_c, pD1)),
                                    childToParentMatrix.MultiplyPoint(new(-h_c,  v_c, pD1))};

            Debug.DrawLine(closePlane[0], closePlane[1], Color.red);
            Debug.DrawLine(closePlane[1], closePlane[2], Color.red);
            Debug.DrawLine(closePlane[2], closePlane[3], Color.red);
            Debug.DrawLine(closePlane[3], closePlane[0], Color.red);

            //  piano lontano
            float h_f = cameraResolution.x / 200 * p2;
            float v_f = cameraResolution.y / 200 * p2;
            Vector3[] farPlane = {childToParentMatrix.MultiplyPoint(new( h_f,  v_f, pD2)),
                                  childToParentMatrix.MultiplyPoint(new( h_f, -v_f, pD2)),
                                  childToParentMatrix.MultiplyPoint(new(-h_f, -v_f, pD2)),
                                  childToParentMatrix.MultiplyPoint(new(-h_f,  v_f, pD2))};

            Debug.DrawLine(farPlane[0], farPlane[1], Color.red);
            Debug.DrawLine(farPlane[1], farPlane[2], Color.red);
            Debug.DrawLine(farPlane[2], farPlane[3], Color.red);
            Debug.DrawLine(farPlane[3], farPlane[0], Color.red);

            //  spigoli

            Debug.DrawLine(closePlane[0], farPlane[0], Color.red);
            Debug.DrawLine(closePlane[1], farPlane[1], Color.red);
            Debug.DrawLine(closePlane[2], farPlane[2], Color.red);
            Debug.DrawLine(closePlane[3], farPlane[3], Color.red);

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
        if (zList.Count > 150) { zList.RemoveAt(0); }

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
