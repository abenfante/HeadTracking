using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenBorders : MonoBehaviour //Disegna la cornice dello schermo reale in unity
{
     // screen resolution / pixels per unità di unity = dimensione dello schermo in unit
    public HeadTracking_V2 headTracking;

    public bool debug = true;
    void Start()
    {
        headTracking = FindObjectOfType<HeadTracking_V2>();
    }

    // Update is called once per frame
    void Update()
    {
        
        Matrix4x4 childToParentMatrix = transform.localToWorldMatrix;
        Vector2 virtualScreenSize = new(Screen.width / headTracking.pixelsPerUnit, Screen.height / headTracking.pixelsPerUnit);

        if (debug)
        {
            Vector3[] screenCorners = 
                {childToParentMatrix.MultiplyPoint(new Vector2( virtualScreenSize.x,  virtualScreenSize.y) * 0.5f),
                 childToParentMatrix.MultiplyPoint(new Vector2( virtualScreenSize.x, -virtualScreenSize.y) * 0.5f),
                 childToParentMatrix.MultiplyPoint(new Vector2(-virtualScreenSize.x, -virtualScreenSize.y) * 0.5f),
                 childToParentMatrix.MultiplyPoint(new Vector2(-virtualScreenSize.x,  virtualScreenSize.y) * 0.5f)};

            Debug.DrawLine(screenCorners[0], screenCorners[1], Color.cyan);
            Debug.DrawLine(screenCorners[1], screenCorners[2], Color.cyan);
            Debug.DrawLine(screenCorners[2], screenCorners[3], Color.cyan);
            Debug.DrawLine(screenCorners[3], screenCorners[0], Color.cyan);
        }



    }
}
