using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.ComponentModel;

public class HT2D : MonoBehaviour
{
    [Header("Object Physical Parameters")]
    public float thermalConductivity = 0.5f;  // k [W/m·K]
    public float density = 7800f;             // ρ [kg/m³]
    public float specificHeat = 500f;         // cp [J/kg·K]

    [Range(0f, 1f)]
    [Tooltip("For Radiative Transfer")]
    public float emissivity = 1;

    [Header("Environmental Fluid Physical Parameters")]
    public float fluidDensity = 1000; // kg/m^3
    public float fluidThermalConductivity = 10; // k [W/m·K]
    public float fluidDynamicViscosity = 10; // k [W/m·K]



    //[Header("Simulation Settings")]
    public int widthPoints = 3;
    public int heightPoints = 3;

    private int numPoints; //How many points to be simulated


    public GameObject heatCell;
    [Tooltip("(0,0) is the bottom left")]
    public Vector2 cellToBeHot;


    public bool isolatedSystem = true;
    public bool conductiveTransfer = true;
    public bool convectiveTransfer = true;
    public bool radiativeTransfer = true;
    public bool isothermalHeatSource = true;


    [Range(0.01f, 5f)]
    public float deltaTime = 0.01f; //How quickly the sumulation goes

    [Range(0.01f, 1f)]
    public float deltaX = 1f;
    [Range(0.01f, 1f)]
    public float deltaY = 1f;



    //[Header("Boundary Conditions (Temp in Celsius)")]
    [Tooltip("Units of Celsius")] public float T1 = 100;
    [Tooltip("Units of Celsius")] public float T2 = 0;
    [Tooltip("Units of Celsius")] public float tInfinity = 20;


    private float[] temperatures;
    private float[] newTemperatures;
    private GameObject[] visuals;
    private List<Vector2Int> heatCellPositions;

    [Header("New Features")]
    public bool isPaused = true;


    public float fluidSpeed = 5f;
    public enum fluidSourcePosition
    {
        Top,
        Bottom,
        Left,
        Right,
        everywhere
    }

    public fluidSourcePosition fluidSource = fluidSourcePosition.Top;

    // Start is called before the first frame update
    void Start()
    {
        SetUp();
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

    public void HeatTransferCalculation()
    {
        //main function that uses other functions to determine the heat loss for each cell
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++) //looping through each cell in the grid
            {

                //if I am keeping an isothermal heat cell, I want the cell to stay at its current temperature then proceed to the next cell to be checked
                if (isothermalHeatSource && getIndex(i, j) == getIndex(Mathf.FloorToInt(cellToBeHot.x), Mathf.FloorToInt(cellToBeHot.y)))
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

        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                //update temperatures to be displayed correctly
                temperatures[getIndex(i, j)] = newTemperatures[getIndex(i, j)];
            }

        }

    }
    public void SetUp()
    {
        numPoints = widthPoints * heightPoints;

        temperatures = new float[numPoints];
        newTemperatures = new float[numPoints];
        visuals = new GameObject[numPoints];

        

        for (int j = 0; j < heightPoints; j++) //starts bottom left, then goes all the way right before going up one again
        {
            for (int i = 0; i < widthPoints; i++)
            {
                Vector3 pos = new Vector3((i - (widthPoints - 1) / 2f) * deltaX, (j - (heightPoints - 1) / 2f) * deltaY, 0);
                visuals[getIndex(i, j)] = Instantiate(heatCell, pos, Quaternion.identity);
                visuals[getIndex(i, j)].transform.localScale = new Vector3(deltaX, deltaY, 1f);

                temperatures[getIndex(i, j)] = T2; //make all the cells the cold temperature
            }
        }

        temperatures[getIndex(Mathf.FloorToInt(cellToBeHot.x), Mathf.FloorToInt(cellToBeHot.y))] = T1;
    }

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
        qRight = -thermalConductivity * (temperatures[Rightbox] - temperatures[ThisBox]) / deltaX;
        qLeft = -thermalConductivity * (temperatures[ThisBox] - temperatures[LeftBox]) / deltaX;
        qDown = -thermalConductivity * (temperatures[DownBox] - temperatures[ThisBox]) / deltaY;
        qUp = -thermalConductivity * (temperatures[ThisBox] - temperatures[UpBox]) / deltaY;

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

        float dTdtx = (qLeft - qRight) / deltaX;
        float dTdty = (qUp - qDown) / deltaY;

        return (dTdtx + dTdty);
    }


    /*
    public void SimulateHeatFrier()
    {
        //Fouriers Law of Heat Conduction: q" = -k * deltaT/deltaX


        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++) //looping through each cell in the grid
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


                switch (isolatedSystem)
                {
                    case true:

                        //calculate all the Q for all four sides, then if one of the sides is exposed to the outside, make the heat loss/gain 0 since it is insulated
                        //There is a way more efficient way of doing this, for example I could store those cells then just repeat the calculations instead of checking the conditions for all
                        //of the cells, but this works and is efficient enough for this. If i was to scale the project up I would change this.

                        //This is in units of watts per meter squared
                        qRight = -thermalConductivity * (temperatures[Rightbox] - temperatures[ThisBox]) / deltaX;
                        qLeft = -thermalConductivity * (temperatures[ThisBox] - temperatures[LeftBox]) / deltaX;
                        qDown = -thermalConductivity * (temperatures[DownBox] - temperatures[ThisBox]) / deltaY;
                        qUp = -thermalConductivity * (temperatures[ThisBox] - temperatures[UpBox]) / deltaY;

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
                    break;

                      //if the system is not isolated on a side, I want to just calculate the radiative heat loss based on T infinity. Then if I want heat convection, I calculate that in a 
                      //seperate function. I should technically have the radiative heat loss in its own function for readability, but this works.
                    case false:
                        qRight = -thermalConductivity * (temperatures[Rightbox] - temperatures[ThisBox]) / deltaX;
                        qLeft = -thermalConductivity * (temperatures[ThisBox] - temperatures[LeftBox]) / deltaX;
                        qDown = -thermalConductivity * (temperatures[DownBox] - temperatures[ThisBox]) / deltaY;
                        qUp = -thermalConductivity * (temperatures[ThisBox] - temperatures[UpBox]) / deltaY;
                        double stephanBoltzmannConstant = (5.67 * Mathf.Pow(10, -8));
                        
                        if ((i - 1) < 0)
                        {
                            //qLeft = -thermalConductivity * (temperatures[ThisBox] - tInfinity) / deltaX;
                            qLeft = -(float)(emissivity * stephanBoltzmannConstant * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));

                        }

                        if ((i + 1) == widthPoints)
                        {
                            //qRight = -thermalConductivity * (tInfinity - temperatures[ThisBox]) / deltaX;
                            qRight = -(float)(emissivity * stephanBoltzmannConstant * -(Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
                        }

                        if ((j - 1) < 0)
                        {
                            // qDown = -thermalConductivity * (tInfinity - temperatures[ThisBox]) / deltaY;
                            qDown = -(float)(emissivity * stephanBoltzmannConstant * -(Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
                        }

                        if ((j + 1) == heightPoints)
                        {
                            //qUp = -thermalConductivity * (temperatures[ThisBox] - tInfinity) / deltaY;
                            qUp = -(float)(emissivity * stephanBoltzmannConstant * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
                        }

                    break;
                }

                float dTdtx = (qLeft - qRight) / deltaX;
                float dTdty = (qUp - qDown) / deltaY;
                newTemperatures[getIndex(i,j)] = temperatures[getIndex(i, j)] + deltaTime * (dTdtx+dTdty) / (density * specificHeat);

            }
        }

        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                temperatures[getIndex(i,j)] = newTemperatures[getIndex(i,j)];
            }
            
        }
    }
    */

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
            qRight = -(float)(emissivity * stephanBoltzmannConstant * -(Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
        }

        if ((j - 1) < 0)
        {
            qDown = -(float)(emissivity * stephanBoltzmannConstant * -(Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
        }

        if ((j + 1) == heightPoints)
        {
            qUp = -(float)(emissivity * stephanBoltzmannConstant * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
        }

        float dTdtx = (qLeft - qRight) / deltaX;
        float dTdty = (qUp - qDown) / deltaY;

        return (dTdtx + dTdty);
    }
    float SimulateHeatNewton(int i, int j)
    {
        //Convective HT Here
        //Newtons law of cooling q" = h * deltaT
        //In order to calculatea H, non dimenional numbers have to be used. Re and Nu

        //Re = density * speed * Length / (dynamic viscosity)
        float Re = fluidDensity;








        return 0;

    }



    void UpdateVisuals()
    {
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                float tNorm = Mathf.InverseLerp(0, 100, temperatures[getIndex(i, j)]);
                Color color = Color.Lerp(Color.blue, Color.red, tNorm);
                visuals[getIndex(i, j)].GetComponent<SpriteRenderer>().color = color;
            }
        }

    }

    int getIndex(int x, int y) //way to make and recieve a unqiue index value based on the cells position.
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


    //This is for UI implementation of the simulation so it can all be controlled through the game screen
    public void PauseUnPause()
    {
        isPaused = (isPaused == true) ? false : true;
    }
}