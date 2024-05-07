using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class HeadTracking_V2 : MonoBehaviour
{
    [Tooltip("Densit� di pixel in DPI dello schermo usato")]
    public float screenDPI; //densit� di pixel in DPI dello schermo usato
    [Tooltip("Quanti pollici � lunga una unit� di lunghezza di Unity nella mondo reale")]
    public float UnitsToInchesScale = 0.35f;
    [Tooltip("Risoluzione della webcam ")]
    public Vector2 webcamResolution = new(640, 480);
    [Tooltip("Lunghezza focale della webcam")]
    public float webcamFocalLenght = 1;
    [Tooltip("Fattore per cui si moltiplica la dimensione del bonding box della testa rilevato per ottenere la distanza della testa dallo schermo")]
    public float headSizeFactor = 0.1f;
    
    // For setting the transform of the camera
    private Transform webcamTransform;
    public float webcamPitch = 0;
    public float webcamHeight = 1f;

    // Rapporto tra lunghezza di una unit� di unity e di un pixel: determina la dimensione dello schermo nel mondo virtuale
    [HideInInspector]
    public float pixelsPerUnit = 100;

    ScreenBorders screenBorders;
    Camera cam;
    UDPReceive uDPReceive; //script to access data from webcam
    //GameObject cameraObject; //gameobject della camera virtuale

    //Needed to update headDistance in gui
    private Label HeadDistanceLabel;

    List<float> xList = new();
    List<float> yList = new();
    List<float> zList = new();

    private void Start()
    {
        screenDPI = Screen.dpi;
        screenBorders = FindObjectOfType<ScreenBorders>();
        cam = GetComponent<Camera>();
        uDPReceive = GetComponent<UDPReceive>();


        SettingsSliders settingsUI = FindObjectOfType<SettingsSliders>();

        webcamTransform = transform.parent;

        RestoreSavedValues();

        if(screenDPI == 0)
        settingsUI.CreateFloatSlider(screenDPI, 0, 1000, "screenDPI").AddListener(call =>
        { screenDPI = call; });
        settingsUI.CreateFloatSlider(UnitsToInchesScale, 0.1f, 3, "UnitsToInchesScale").AddListener(call =>
        { UnitsToInchesScale = call; });
        settingsUI.CreateFloatSlider(webcamResolution.x, 0, 4000, "webcamResolution.x").AddListener(call => 
        { webcamResolution.x = call; });
        settingsUI.CreateFloatSlider(webcamResolution.y, 0, 4000, "webcamResolution.y").AddListener(call => 
        { webcamResolution.y = call; });
        settingsUI.CreateFloatSlider(webcamFocalLenght, 0, 5, "webcamFocalLenght").AddListener(call =>
        { webcamFocalLenght = call; });
        settingsUI.CreateFloatSlider(headSizeFactor, 0.1f, 5, "headSizeFactor").AddListener(call =>
        { headSizeFactor = call; });
        settingsUI.CreateFloatSlider(webcamPitch, -30f, 30f, "Webcam Pitch").AddListener(call =>
        { webcamTransform.rotation = Quaternion.Euler(new(call, 0f, 0f)); });
        settingsUI.CreateFloatSlider(webcamHeight, -6, 6, "Webcam Height").AddListener(call =>
        { webcamTransform.position = new(0f, call, 0f); });
        
        HeadDistanceLabel = settingsUI.rootVisualElement.Q<Label>("HeadDistance");

    }

    private void RestoreSavedValues()
    {
        Debug.Log("save not implemented");
    }

    public void SaveValues()
    {
        Debug.Log("Click!");
    }

    void Update()
    {
        string data = uDPReceive.data;

        if (data != "")
        {
            // Elabora dati da python
            float xBBPos, yBBpos, BBsize;
            ParseAndScaleBBData(data, out xBBPos, out yBBpos, out BBsize);
            float xPosAverage, yPosAverage, headSizeAverage;
            // Media mobile dei dati dalla camera per ammorbidire i movimenti
            AverageBBData(xBBPos, yBBpos, BBsize, out xPosAverage, out yPosAverage, out headSizeAverage);

            // Necessario per lo script di debug
            pixelsPerUnit = screenDPI / UnitsToInchesScale;
            
            // La posizione della testa nel mondo virtuale � ricostruita sfruttando il sistema di trasformate di Unity.
            // L'utente avr� cura di inserire nel programma i parametri del suo schermo, della sua webcam
            // e del posizionamento di quest'ultima rispetto al primo.
            //
            // In questo modo, muovendo a ogni frame la camera alla posizione della testa dell'utente nel mondo virtuale,
            // la prospettiva vista sullo schermo sar� analoga a quella di una finestra sul mondo 3D.

            // Calcolo della posizione della testa relativa alla webcam
            Vector3 headPositionRelativeToWebcam = BBDataToHeadPositionRelativeToWebcam(xPosAverage, yPosAverage, headSizeAverage, webcamFocalLenght, debug: true);

            // Posizioniamo la camera virtuale in corrispondenza della testa dell'utente nel mondo virtuale
            cam.transform.localPosition = headPositionRelativeToWebcam;

            // Orientiamo la camera parallelamente allo schermo virtuale
            cam.transform.rotation = screenBorders.transform.rotation;

            // Per calcolare le giusta distanza focale e ilngiusto lens shift per l'effetto finestra,
            // otteniamo la posizione del pinhole della camera rispetto al centro dello schermo virtuale
            // con il sistema di trasformate di unity
            Vector3 headPositionInWorld = transform.parent.TransformPoint(headPositionRelativeToWebcam); //da camera a mondo
            Vector3 headPositionRelativeToScreen = screenBorders.transform.worldToLocalMatrix.MultiplyPoint(headPositionInWorld);


            // TO DO: riscrivere
            // I parametri della camera di unity sono tutti in pollici (convenzione), misurati nel mondo reale.
            // Per comodit�, rendiamo le dimensioni del sensore virtuale uguali a quelle dello schermo.
            // Differentemente dalla realt�, questo non influir� sulla qualit� dell'immagine, ma ci semplifica i calcoli:
            // possiamo impostare il vettore del lens shift virtuale come uguale
            // alla posizione della testa nel mondo reale, se proiettata sul "piano dell'oggetto" della webcam,
            // e la distanza focale come uguale
            // alla componente parallela all'asse ottico della distanza della testa dell'utente dalla webcam 
            cam.sensorSize = new(Screen.width / screenDPI, Screen.height / screenDPI);

            //traduciamo questa posizione in lunghezza focale e lens shift 
            cam.lensShift = new Vector2((-headPositionRelativeToScreen.x / UnitsToInchesScale) / cam.sensorSize.x,
                                           (-headPositionRelativeToScreen.y / UnitsToInchesScale) / cam.sensorSize.y);
            cam.focalLength = -headPositionRelativeToScreen.z / UnitsToInchesScale;

            Vector2 virtualScreenSize = new(Screen.width / pixelsPerUnit, Screen.height / pixelsPerUnit);
            float distanceFromScreenInScreenWidths = -headPositionInWorld.z / virtualScreenSize.x;

            HeadDistanceLabel.text = distanceFromScreenInScreenWidths.ToString();
        }
        else
        {
            HeadDistanceLabel.text = "NO DATA";
        }

    }

    private Vector3 BBDataToHeadPositionRelativeToWebcam(float BBx, float BBy, float BBsize, float focalLenght, bool debug)
    {
        /*
        -rendere la camera cos� costruita facilmente spostabile e ruotabile
        -inserire funzionalit� per regolare scala e lunghezza focale durante l'uso
        - migliorare accuratezza del calcolo della distanza
        */

        // distanza ricostruita del volto dalla camera, lungo l'asse ottico
        float headDistance = headSizeFactor / BBsize;
        // calcolo del fattore di conversione delle coordinate della testa perpendicolari all'asse ottico,
        // per passare dalle coordinate dell'immagine alle coordinate del mondo 3D
        float p = headDistance / focalLenght;

        // disegna nell'editor di unity il frustum della webcam e il piano su cui si trova attualmente la testa dell'utente del mondo virtuale
        if (debug)
        {
            float h = webcamResolution.x / 200 * p, v = webcamResolution.y / 200 * p;

            Matrix4x4 childToParentMatrix = transform.parent.localToWorldMatrix;

            Vector3[] currPlane = {childToParentMatrix.MultiplyPoint(new( h,  v, -headDistance)),
                                   childToParentMatrix.MultiplyPoint(new( h, -v, -headDistance)),
                                   childToParentMatrix.MultiplyPoint(new(-h, -v, -headDistance)),
                                   childToParentMatrix.MultiplyPoint(new(-h,  v, -headDistance))};


            //piano dove si trova il volto
            Debug.DrawLine(currPlane[0], currPlane[1], Color.green);
            Debug.DrawLine(currPlane[1], currPlane[2], Color.green);
            Debug.DrawLine(currPlane[2], currPlane[3], Color.green);
            Debug.DrawLine(currPlane[3], currPlane[0], Color.green);

            //Disegno del frustum della camera
            //  calcoli preliminari, la distanza minima si ha quando il volto riempe l'inquadratura (size = 1),
            //  la massima quando � molto piccolo (size = 0.1).
            //  Questo disegno serve solo a visualizzare lo spazio visto dalla camera nel mondo 3D
            float p1 = -(headSizeFactor / 1f) / focalLenght; //smallest distance
            float pD1 = -(headSizeFactor / 1f); //smallest distance
            float p2 = -(headSizeFactor / 0.1f) / focalLenght; //biggest distance
            float pD2 = -(headSizeFactor / 0.1f); //biggest distance
            //  disegno del piano pi� vicino
            float h_c = webcamResolution.x / 200 * p1;
            float v_c = webcamResolution.y / 200 * p1;
            Vector3[] closePlane = {childToParentMatrix.MultiplyPoint(new( h_c,  v_c, pD1)),
                                    childToParentMatrix.MultiplyPoint(new( h_c, -v_c, pD1)),
                                    childToParentMatrix.MultiplyPoint(new(-h_c, -v_c, pD1)),
                                    childToParentMatrix.MultiplyPoint(new(-h_c,  v_c, pD1))};

            Debug.DrawLine(closePlane[0], closePlane[1], Color.red);
            Debug.DrawLine(closePlane[1], closePlane[2], Color.red);
            Debug.DrawLine(closePlane[2], closePlane[3], Color.red);
            Debug.DrawLine(closePlane[3], closePlane[0], Color.red);

            //  piano lontano
            float h_f = webcamResolution.x / 200 * p2;
            float v_f = webcamResolution.y / 200 * p2;
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

        // coordinate finali della posizione della testa dell'utente relative alla webcam nel mondo virtuale
        return new Vector3(-BBx * p, -BBy * p, -headDistance);
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
        xBBpos = (float.Parse(points[0]) - webcamResolution.x / 2f) / 100; // between -h_res/2 and h_res/2
        yBBpos = (float.Parse(points[1]) - webcamResolution.y / 2f) / 100; // between -v_res/2 and v_res/2

        // we normalize the bounding box size to 640, its maximum size
        BBsize = float.Parse(points[2]) / Mathf.Max(webcamResolution.x, webcamResolution.y);
    }
}
