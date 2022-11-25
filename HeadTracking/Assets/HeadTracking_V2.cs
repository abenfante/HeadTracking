using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeadTracking_V2 : MonoBehaviour
{

    [Tooltip("Densità di pixel in DPI dello schermo usato")]
    public float screenDPI; //densità di pixel in DPI dello schermo usato
    [Tooltip("Quanti pollici è lunga una unità di lunghezza di Unity")]
    public float UnitsToInchesScale; //
    [Tooltip("Risoluzione della webcam ")]
    public Vector2 cameraResolution = new(640, 480); //risoluzione della webcam
    [Tooltip("Lunghezza focale della webcam")]
    public float focalLenght = 1; //lunghezza focale della webcam
    [Tooltip("Fattore di conversione da distanza della testa a dimensione del bounding box rilevato")]
    public float headSizeFactor = 0.1f; //lunghezza focale della webcam

    ScreenBorders screenBorders;
    Camera camera;
    UDPReceive uDPReceive; //script to access data from webcam
    GameObject cameraObject; //gameobject della camera virtuale


    List<float> xList = new();
    List<float> yList = new();
    List<float> zList = new();

    private void Start()
    {
        screenDPI = Screen.dpi;
        screenBorders = FindObjectOfType<ScreenBorders>();
        camera = GetComponent<Camera>();
        uDPReceive = GetComponent<UDPReceive>();
    }



    void Update()
    {
        string data = uDPReceive.data;

        if (data != "")
        {
            
            float xBBPos, yBBpos, BBsize;
            ParseAndScaleBBData(data, out xBBPos, out yBBpos, out BBsize);
            float xAverage, yAverage, sizeAverage;
            AverageBBData(xBBPos, yBBpos, BBsize, out xAverage, out yAverage, out sizeAverage);
            
            Vector3 headPosition = BBDataToHeadPositionRelativeToCamera(
                xAverage, yAverage, sizeAverage, focalLenght, debug: true);

            //settiamo qui la posizione della camera
            //perché questo script è sul gameobject figlio della webcam, che contiene la camera
            camera.transform.localPosition = headPosition;
            //la camera è rivolta verso lo schermo
            camera.transform.rotation = screenBorders.transform.rotation;

            
            //per calcolare la distanza focale e il lens shift,
            //calcoliamo la posizione della camera rispetto al centro dello schermo
            Vector3 headPositionInWorld = transform.localToWorldMatrix.MultiplyPoint(headPosition); //da camera a mondo
            Vector3 headPositionRelativeToScreen = screenBorders.transform.worldToLocalMatrix.MultiplyPoint(headPositionInWorld);
            
            //I parametri della camera sono tutti in pollici misurati nel mondo reale
            //dimensioniamo il sensore in base all'aspect ratio dello schermo e alla dimensione.
            camera.sensorSize = new(Screen.width / screenDPI, Screen.height / screenDPI);

            //traduciamo questa posizione in lunghezza focale e lens shift 
            camera.lensShift =new Vector2(-headPositionRelativeToScreen.x * UnitsToInchesScale / camera.sensorSize.x,
                                          -headPositionRelativeToScreen.y * UnitsToInchesScale / camera.sensorSize.y);
            camera.focalLength = -headPositionRelativeToScreen.z * UnitsToInchesScale;
        }

    }

    private Vector3 BBDataToHeadPositionRelativeToCamera(float BBx, float BBy, float BBsize, float focalLenght, bool debug)
    {
        /*
        -rendere la camera così costruita facilmente spostabile e ruotabile
        -inserire funzionalità per regolare scala e lunghezza focale durante l'uso
        - migliorare accuratezza del calcolo della distanza
        */

        // distanza ricostruita del volto dalla camera, lungo l'asse ottico
        float pointDistance = headSizeFactor / BBsize;
        // fattore di trasformazione dalle coordinate dell'immagine alle coordinate 3D perpendicolari all'asse ottico
        float p = pointDistance / focalLenght;

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
            float p1 = -(headSizeFactor / 1f) / focalLenght; //smallest distance
            float pD1 = -(headSizeFactor / 1f); //smallest distance
            float p2 = -(headSizeFactor / 0.1f) / focalLenght; //biggest distance
            float pD2 = -(headSizeFactor / 0.1f); //biggest distance
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

        return new Vector3(-BBx * p, -BBy * p, -pointDistance);
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
