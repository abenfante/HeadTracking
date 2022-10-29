
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
 
public class HeadTracking : MonoBehaviour
{
    public UDPReceive uDPReceive; //script to access data from webcam
    public GameObject cameraObject; //gameobject of camera
    List<float> xList = new List<float>();
    List<float> yList = new List<float>();
    List<float> zList = new List<float>();
 
    // Start is called before the first frame update
    void Start()
    {
        
    }
 
    // Update is called once per frame
    void Update()
    {
        string data = uDPReceive.data;
        //since it arrives (x,y) removes the ()
        if(data != ""){
            data = data.Substring(1, data.Length - 2);
    
            string[] points = data.Split(','); //since data arrives in x,y it splits
    
            // takes the size of 320 x 240 (640x480 / 2) and takes percentage of width and height
            float x = (float.Parse(points[0])-320) / 100; 
            float y = (float.Parse(points[1])-240) / 100;

            // since the maximum size of the bounding box is 640, we take the percentage of its size
            float z = float.Parse(points[2]) / 640;
            xList.Add(x);
            yList.Add(y);
            zList.Add(z);

            //adds a bunch of this coordinates to obtain an average to move the camera smoothly
            if (xList.Count > 50) { xList.RemoveAt(0); }
            if (yList.Count > 50) { yList.RemoveAt(0); }
            if (zList.Count > 50) { zList.RemoveAt(0); }
    
            float xAverage = Queryable.Average(xList.AsQueryable());
            float yAverage = Queryable.Average(yList.AsQueryable());
            float zAverage = Queryable.Average(zList.AsQueryable());
    
            //instances of camera position and rotation
            Vector3 cameraPosition = cameraObject.transform.localPosition;
            Vector3 cameraRotation = cameraObject.transform.localPosition;

            //changes position and rotation of camera
            //-26.11 is the x position of the camera, 2.49 is the y, used to avoid out of bounds
            //for the z we start from the initial position and we add 5 (we can try to tune it) multiplied by the percentage of the size of the bounding box
            cameraObject.transform.localPosition = new Vector3(-26.11f - xAverage, 2.49f- yAverage, -5.35f+ zAverage * 5); 
            //18 is the x rotation of the camera
            cameraObject.transform.localEulerAngles = new Vector3(18f - yAverage * 10, xAverage * 10 , 0);
        }
    }
}
