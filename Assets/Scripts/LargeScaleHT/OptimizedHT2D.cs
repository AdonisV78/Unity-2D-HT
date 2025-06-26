using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;


public class OptimizedHT2D : MonoBehaviour
{
    #region Variables
    [Header("Object Physical Parameters")]
    [Tooltip("k [W/m·K]")] public float thermalConductivity = 0.5f;  // k [W/m·K]
    [Tooltip("p [kg/m^3]")] public float density = 7800f;             // p [kg/m^3]
    [Tooltip(" cp [J/kg·K]")] public float specificHeat = 500f;         // cp [J/kg·K]

    [Range(0f, 1f)]
    [Tooltip("For Radiative Transfer")]
    public float emissivity = 1;

    [Header("Environmental Fluid Physical Parameters")]
    [Tooltip("p [kg/m^3]")] public float fluidDensity = 1000; // kg/m^3
    [Tooltip("k [W/m·K]")] public float fluidThermalConductivity = 10; // k [W/m·K]
    [Tooltip("mu [Kg/m·s]")] public float fluidDynamicViscosity = 10; // mu [Kg/m·s]
    [Tooltip("J/kg*k")] public float fluidSpecificHeat = 4200f; // J/kg*k



    [Header("Simulation Settings")]
    [Tooltip("Number of Cells horizontally")] public int widthPoints = 3;
    [Tooltip("Number of Cells Vertically")] public int heightPoints = 3;

    private int numPoints; //How many points to be simulated

   
    [Tooltip("RawImage (UI) that will show the heatmap")]
    public RawImage displayPlane;

    private Texture2D heatmapTexture; //one pixel per cell
    private Color[] colorMap;

    public bool haveHeatSource = true;

    [Tooltip("(0,0) is the bottom left, What cells should be at T1")]
    public Vector2Int[] cellsToBeHot;


    public bool isolatedSystem = true;
    public bool conductiveTransfer = true;
    [Tooltip("Forced Convection")] public bool convectiveTransfer = true;
    public bool radiativeTransfer = true;
    public bool isothermalHeatSource = true;


    [Range(0.01f, 20f)]
    [Tooltip("How quickly the sumulation goes, Time step between calculations")]
    public float deltaTime = 0.01f;

    [Range(0.01f, 1f)]
    [Tooltip("in meters")] public float deltaX = 1f;
    [Range(0.01f, 1f)]
    [Tooltip("in meters")] public float deltaY = 1f;



    //[Header("Boundary Conditions (Temp in Celsius)")]
    [Tooltip("Units of Celsius, For the Hot Cell")] public float T1 = 100;
    [Tooltip("Units of Celsius")] public float T2 = 0;
    [Tooltip("Units of Celsius")] public float tInfinity = 20;

    public float coldColorTemp = 0f;
    public float hotColorTemp = 100f;


    private float[] temperatures;
    private float[] newTemperatures;

    //[Header("New Features")]
    public bool isPaused = true;
    [Tooltip("Convective fluid speed (m/s)")] public float fluidSpeed = 5f;


    public enum fluidSourcePosition
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public fluidSourcePosition fluidSource = fluidSourcePosition.Top;

#endregion


    // Start is called before the first frame update
    void Start()
    {
        SetUp();
        UpdateVisuals();
        isPaused = false;
    }


    // Update is called once per frame
    void Update()
    {
        if (!isPaused)
        {
            HeatTransferCalculation();
        }

        UpdateVisuals();
    }


    //Sets up the scene
    public void SetUp()
    {
        numPoints = widthPoints * heightPoints;

        temperatures = new float[numPoints];
        newTemperatures = new float[numPoints];
        colorMap = new Color[numPoints];

        // 1-pixel-per-cell texture
        heatmapTexture = new Texture2D(widthPoints, heightPoints, TextureFormat.RGBA32, mipChain: false, linear: true);
        heatmapTexture.filterMode = FilterMode.Point;      // keeps crisp edges
        heatmapTexture.wrapMode = TextureWrapMode.Clamp;
       
        
        //rescale to match dimensions desired
        RectTransform rt = displayPlane.rectTransform;
        
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500 * deltaX);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 500 * deltaY);
        

        if (displayPlane != null)
        {
            displayPlane.texture = heatmapTexture;
        }

        // initialise temperature field
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                temperatures[getIndex(i,j)] = T2;
            }
        }

        //makes all the desired cells hot
        if (cellsToBeHot.Length > 0 && haveHeatSource)
        {
            foreach (Vector2Int cell in cellsToBeHot)
            {
                temperatures[getIndex(cell.x, cell.y)] = T1;
            }
        }


    }

    //loops through the colorMap cells, updating its color based on the temperature
    public void UpdateVisuals()
    {
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                float tNorm = Mathf.InverseLerp(coldColorTemp, hotColorTemp, temperatures[getIndex(i, j)]);
                colorMap[getIndex(i,j)] = Color.Lerp(Color.blue, Color.red, tNorm);
               
            }
        }
        heatmapTexture.SetPixels(colorMap); // blast entire buffer at once
        heatmapTexture.Apply();             // upload to GPU
    }

    //sends calls to calculate each aspect of the HT and sums it up and calculates the new temperatures
    public void HeatTransferCalculation()
    {
        //main function that uses other functions to determine the heat loss for each cell
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++) //looping through each cell in the grid
            {

                //if I am keeping an isothermal heat cell, I want the cell to stay at its current temperature then proceed to the next cell to be checked
                if (haveHeatSource && isothermalHeatSource && isHotCell(i, j))
                {
                    newTemperatures[getIndex(i, j)] = T1;
                    continue;
                }


                float q = 0;
                if (conductiveTransfer)
                {
                    q += SimulateHeatFourier(i, j);
                }

                if (!isolatedSystem)
                {
                    if (convectiveTransfer)
                    {
                        q += SimulateHeatNewton(i, j);

                    }
                    if (radiativeTransfer)
                    {
                        q += SimulateHeatRadiation(i, j);
                    }
                }

                //add updated temperatures into a "dummy" temperature list
                newTemperatures[getIndex(i, j)] = temperatures[getIndex(i, j)] + deltaTime * (q) / (density * specificHeat);


            }
        }

        //update temperatures
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                temperatures[getIndex(i, j)] = newTemperatures[getIndex(i, j)];
            }

        }

    }

    //Conductive Heat Transfer
    float SimulateHeatFourier(int i, int j)
    {
        float qLeft = 0;
        float qRight = 0;
        float qUp = 0;
        float qDown = 0;

        int ThisBox = getIndex(i, j);
        int Rightbox = getIndex(i + 1, j);
        int LeftBox = getIndex(i - 1, j);
        int UpBox = getIndex(i, j + 1);
        int DownBox = getIndex(i, j - 1);
        // if any of the above cells "dont exist" I am retrieveing cell 0 in order to not get an error before clauclating the right value

        //calculate all the Q for all four sides, then if one of the sides is exposed to the outside, make the heat loss/gain 0 since it is insulated
        //There is a way more efficient way of doing this, for example I could store those cells then just repeat the calculations instead of checking the conditions for all
        //of the cells, but this works and is efficient enough for this. If i was to scale the project up I would change this.

        //This is in units of watts per meter squared
        qRight = -thermalConductivity * (temperatures[ThisBox] - temperatures[Rightbox]) / deltaX;
        qLeft = -thermalConductivity * (temperatures[ThisBox] - temperatures[LeftBox]) / deltaX;
        qDown = -thermalConductivity * (temperatures[ThisBox] - temperatures[DownBox]) / deltaY;
        qUp = -thermalConductivity * (temperatures[ThisBox] - temperatures[UpBox]) / deltaY;

        //if any of the cells were ones that didnt exist, recalculate the conductive transfer on that to 0
        if ((i - 1) < 0)
        {
            qLeft = 0;
        }

        if ((i + 1) == widthPoints)
        {
            qRight = 0;
        }

        if ((j - 1) < 0)
        {
            qDown = 0;
        }

        if ((j + 1) == heightPoints)
        {
            qUp = 0;
        }

        float dTdtx = (qLeft + qRight) / deltaX;
        float dTdty = (qUp + qDown) / deltaY;

        return (dTdtx + dTdty);
    }


    //Radiative Heat Transfer
    float SimulateHeatRadiation(int i, int j)
    {

        //if the system is not isolated on a side, I want to just calculate the radiative heat loss based on T infinity. Then if I want heat convection, I calculate that in a 
        //seperate function. I should technically have the radiative heat loss in its own function for readability, but this works.

        //Radiative rate Equation: q" = E * stephan Boltzmann constant * (Ts^4-Tinf^4)

        float qLeft = 0;
        float qRight = 0;
        float qUp = 0;
        float qDown = 0;

        int ThisBox = getIndex(i, j);
        int Rightbox = getIndex(i + 1, j);
        int LeftBox = getIndex(i - 1, j);
        int UpBox = getIndex(i, j + 1);
        int DownBox = getIndex(i, j - 1);

        double stephanBoltzmannConstant = (5.67 * Mathf.Pow(10, -8));

        if ((i - 1) < 0)
        {
            qLeft = -(float)(emissivity * stephanBoltzmannConstant * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));

        }

        if ((i + 1) == widthPoints)
        {
            qRight = -(float)(emissivity * stephanBoltzmannConstant * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
        }

        if ((j - 1) < 0)
        {
            qDown = -(float)(emissivity * stephanBoltzmannConstant * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
        }

        if ((j + 1) == heightPoints)
        {
            qUp = -(float)(emissivity * stephanBoltzmannConstant * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
        }

        float dTdtx = (qLeft + qRight) / deltaX;
        float dTdty = (qUp + qDown) / deltaY;

        return (dTdtx + dTdty);
    }

    //Forces convective heat Transfer
    float SimulateHeatNewton(int i, int j)
    {
        //Convective HT Here
        //Newtons law of cooling q" = h * deltaT
        //In order to calculatea H, non dimenional numbers have to be used. Re, Nu, and Pr
        //Assuming forced convection over each exposed surface

        //set the heat from each side to 0 to start

        float qLeft = 0;
        float qRight = 0;
        float qUp = 0;
        float qDown = 0;

        int ThisBox = getIndex(i, j);

        //calculate prandtl number 
        float Pr = (fluidSpecificHeat * fluidDynamicViscosity) / fluidThermalConductivity;
        //Debug.Log(Pr);

        switch (fluidSource)
        {
            case fluidSourcePosition.Top:

                if ((j + 1) == heightPoints)
                {
                    //calculate reynolds number and leave a placehodler nusselt number
                    float Re = (fluidDensity * fluidSpeed * (deltaX * widthPoints)) / fluidDynamicViscosity;
                    float Nu = 0;

                    //based on if its turbulent or not, use the appropriate correlation
                    if (Re < 2000)
                    {
                        Nu = 0.564f * Mathf.Pow(Re, 0.5f) * Mathf.Pow(Pr, 0.4f);
                    }
                    else
                    {
                        Nu = 0.13f * Mathf.Pow(Re, 0.8f) * Mathf.Pow(Pr, 0.4f);
                    }

                    float h = Nu * (fluidThermalConductivity / (deltaX * widthPoints));
                    qUp = -h * (temperatures[ThisBox] - tInfinity);

                }
                if ((i - 1) < 0 || (i + 1) == widthPoints)
                {

                    float scaleFactor = Mathf.Max(1 - ((float)(j) / (heightPoints - 1)), 0.1f);// need to do this otherwise it will end up dividing by 0

                    float Re = (fluidDensity * fluidSpeed * (deltaY * scaleFactor * heightPoints)) / fluidDynamicViscosity;
                    float Nu = 0;

                    if (Re < 5e5)
                    {
                        Nu = 0.664f * Mathf.Pow(Re, 0.5f) * Mathf.Pow(Pr, 1f / 3f);
                    }
                    else
                    {
                        Nu = 0.037f * Mathf.Pow(Re, 0.8f) * Mathf.Pow(Pr, 1f / 3f);
                    }
                    float h = Nu * (fluidThermalConductivity / (deltaY * scaleFactor * heightPoints));

                    if ((i - 1) < 0)
                        qLeft = -h * (temperatures[ThisBox] - tInfinity);
                    if ((i + 1) == widthPoints)
                        qRight = -h * (temperatures[ThisBox] - tInfinity);
                }
                break;

            case fluidSourcePosition.Bottom:

                if ((j - 1) < 0)
                {
                    //calculate reynolds number and leave a placehodler nusselt number
                    float Re = (fluidDensity * fluidSpeed * (deltaX * widthPoints)) / fluidDynamicViscosity;
                    float Nu = 0;

                    //based on if its turbulent or not, use the appropriate correlation
                    if (Re < 2000)
                    {
                        Nu = 0.564f * Mathf.Pow(Re, 0.5f) * Mathf.Pow(Pr, 0.4f);
                    }
                    else
                    {
                        Nu = 0.13f * Mathf.Pow(Re, 0.8f) * Mathf.Pow(Pr, 0.4f);
                    }

                    float h = Nu * (fluidThermalConductivity / (deltaX * widthPoints));
                    qDown = -h * (temperatures[ThisBox] - tInfinity);

                }

                if ((i - 1) < 0 || (i + 1) == widthPoints)
                {


                    float scaleFactor = Mathf.Max((((float)j) / (heightPoints - 1)), 0.1f);// need to do this otherwise it will end up dividing by 0

                    float Re = (fluidDensity * fluidSpeed * (deltaY * scaleFactor * heightPoints)) / fluidDynamicViscosity;
                    float Nu = 0;

                    if (Re < 5e5)
                    {
                        Nu = 0.664f * Mathf.Pow(Re, 0.5f) * Mathf.Pow(Pr, 1f / 3f);
                    }
                    else
                    {
                        Nu = 0.037f * Mathf.Pow(Re, 0.8f) * Mathf.Pow(Pr, 1f / 3f);
                    }
                    float h = Nu * (fluidThermalConductivity / (deltaY * scaleFactor * heightPoints));

                    if ((i - 1) < 0)
                        qLeft = -h * (temperatures[ThisBox] - tInfinity);
                    if ((i + 1) == widthPoints)
                        qRight = -h * (temperatures[ThisBox] - tInfinity);
                }
                break;

            case fluidSourcePosition.Right:

                if ((i + 1) == widthPoints)
                {
                    //calculate reynolds number and leave a placehodler nusselt number
                    float Re = (fluidDensity * fluidSpeed * (deltaY * heightPoints)) / fluidDynamicViscosity;
                    float Nu = 0;

                    //based on if its turbulent or not, use the appropriate correlation
                    if (Re < 2000)
                    {
                        Nu = 0.564f * Mathf.Pow(Re, 0.5f) * Mathf.Pow(Pr, 0.4f);
                    }
                    else
                    {
                        Nu = 0.13f * Mathf.Pow(Re, 0.8f) * Mathf.Pow(Pr, 0.4f);
                    }

                    float h = Nu * (fluidThermalConductivity / (deltaY * heightPoints));
                    qUp = -h * (temperatures[ThisBox] - tInfinity);

                }
                if ((j - 1) < 0 || (j + 1) == heightPoints)
                {

                    float scaleFactor = Mathf.Max(1 - ((float)(i) / (widthPoints - 1)), 0.1f);// need to do this otherwise it will end up dividing by 0

                    float Re = (fluidDensity * fluidSpeed * (deltaX * scaleFactor * widthPoints)) / fluidDynamicViscosity;
                    float Nu = 0;

                    if (Re < 5e5)
                    {
                        Nu = 0.664f * Mathf.Pow(Re, 0.5f) * Mathf.Pow(Pr, 1f / 3f);
                    }
                    else
                    {
                        Nu = 0.037f * Mathf.Pow(Re, 0.8f) * Mathf.Pow(Pr, 1f / 3f);
                    }
                    float h = Nu * (fluidThermalConductivity / (deltaX * scaleFactor * widthPoints));

                    if ((i - 1) < 0)
                        qLeft = -h * (temperatures[ThisBox] - tInfinity);
                    if ((i + 1) == widthPoints)
                        qRight = -h * (temperatures[ThisBox] - tInfinity);
                }
                break;

            case fluidSourcePosition.Left:

                if ((i - 1) < 0)
                {
                    //calculate reynolds number and leave a placehodler nusselt number
                    float Re = (fluidDensity * fluidSpeed * (deltaY * heightPoints)) / fluidDynamicViscosity;
                    float Nu = 0;

                    //based on if its turbulent or not, use the appropriate correlation
                    if (Re < 2000)
                    {
                        Nu = 0.564f * Mathf.Pow(Re, 0.5f) * Mathf.Pow(Pr, 0.4f);
                    }
                    else
                    {
                        Nu = 0.13f * Mathf.Pow(Re, 0.8f) * Mathf.Pow(Pr, 0.4f);
                    }

                    float h = Nu * (fluidThermalConductivity / (deltaY * heightPoints));
                    qUp = -h * (temperatures[ThisBox] - tInfinity);

                }
                if ((j - 1) < 0 || (j + 1) == heightPoints)
                {

                    float scaleFactor = Mathf.Max((((float)i) / (widthPoints - 1)), 0.1f);// need to do this otherwise it will end up dividing by 0

                    float Re = (fluidDensity * fluidSpeed * (deltaX * scaleFactor * widthPoints)) / fluidDynamicViscosity;
                    float Nu = 0;

                    if (Re < 5e5)
                    {
                        Nu = 0.664f * Mathf.Pow(Re, 0.5f) * Mathf.Pow(Pr, 1f / 3f);
                    }
                    else
                    {
                        Nu = 0.037f * Mathf.Pow(Re, 0.8f) * Mathf.Pow(Pr, 1f / 3f);
                    }
                    float h = Nu * (fluidThermalConductivity / (deltaX * scaleFactor * widthPoints));

                    if ((i - 1) < 0)
                        qLeft = -h * (temperatures[ThisBox] - tInfinity);
                    if ((i + 1) == widthPoints)
                        qRight = -h * (temperatures[ThisBox] - tInfinity);
                }
                break;

        }

        float dTdtx = (qLeft + qRight) / deltaX;
        float dTdty = (qUp + qDown) / deltaY;

        return (dTdtx + dTdty);

    }

    //way to make and recieve a unqiue index value based on the cells position.
    int getIndex(int x, int y) 
    {
        if (x < 0 || x >= widthPoints || y < 0 || y >= heightPoints)
        {
            return 0; //returning zero for cell overflows, this is sorted out in the heat transfer calculations
        }
        else
        {
            return (y * widthPoints) + x;
        }

    }


    //used for checking if a cell should be set to hot temperature
    public bool isHotCell(int i, int j)
    {
        Vector2Int thisCell = new Vector2Int(i, j);
        return cellsToBeHot.Contains(thisCell);
    }

    //This is for UI implementation of the simulation so it can all be controlled through the game screen
    public void PauseUnPause()
    {
        isPaused = (isPaused == true) ? false : true;
    }


    //Pause the simulation, destroy all the cells, clear all lists, then re-setup everything
    public void ResetSimulation()
    {
        isPaused = true;

        heatmapTexture = null;
        colorMap = null;
        temperatures = null;
        newTemperatures = null;

        SetUp();
    }
}
