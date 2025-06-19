using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class HT2D : MonoBehaviour
{
    [Header("Object Physical Parameters")]
    public float thermalConductivity = 0.5f;  // k [W/m·K]
    public float density = 7800f;             // ρ [kg/m³]
    public float specificHeat = 500f;         // cp [J/kg·K]

    [Header("Simulation Settings")]
    public int widthPoints = 3;
    public int heightPoints = 3;
    
    private int numPoints; //How many points to be simulated

    public float deltaTime = 0.01f; //How quickly the sumulation goes
    public float deltaX = 1f;
    public float deltaY = 1f;
    public GameObject heatCell;


    [Header("Boundary Conditions")]
   
    [Tooltip("Units of Celsius")]
    public float T1 = 100;
    public float T2 = 0;


    [Tooltip("(0,0) is the bottom left")]
    public Vector2 cellToBeHot;

    public float tInfinity = 20;

    public bool isolatedSystem = true;
    public bool isothermalHeatSource = true;

    private float[] temperatures;
    private float[] newTemperatures;
    private GameObject[] visuals;
    private List<Vector2Int> heatCellPositions;

    [Header("Radiative Heat Transfer")]
    public float emissivity = 1;
    

    [Header("New Features")]
    public bool isPaused = true;

    public enum windSourcePosition
    {
        Top,
        Bottom, 
        Left, 
        Right
    }

    
    // Start is called before the first frame update
    void Start()
    {
        SetUp();
    }

   
    // Update is called once per frame
    void Update()
    {
        SimulateHeatFourier();
        UpdateVisuals();
    }


    public void SetUp()
    {
        numPoints = widthPoints * heightPoints;

        temperatures = new float[numPoints];
        newTemperatures = new float[numPoints];
        visuals = new GameObject[numPoints];

        heatCellPositions = new List<Vector2Int>();

        for (int j = 0; j < heightPoints; j++) //starts bottom left, then goes all the way right before going up one again
        {
            for (int i = 0; i < widthPoints; i++)
            {
                Vector3 pos = new Vector3(i - (widthPoints / 2), j - (heightPoints / 2), 0);
                visuals[getIndex(i, j)] = Instantiate(heatCell, pos, Quaternion.identity); //Generate the cells

                heatCellPositions.Add(new Vector2Int(i, j));//low key might not need this, we'll see

                temperatures[getIndex(i, j)] = T2; //make all the cells the cold temperature
            }
        }

        temperatures[getIndex(Mathf.FloorToInt(cellToBeHot.x), Mathf.FloorToInt(cellToBeHot.y))] = T1;
    }
    public void SimulateHeatFourier()
    {
        //Fouriers Law of Heat Conduction: q" = -k * deltaT/deltaX


        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++) //looping through each cell in the grid
            {

                if (isothermalHeatSource && getIndex(i,j) == getIndex(Mathf.FloorToInt(cellToBeHot.x), Mathf.FloorToInt(cellToBeHot.y)))
                {
                    newTemperatures[getIndex(i,j)] = T1;
                    continue; //if I am keeping an isothermal heat cell, I want the cell to stay at its current temperature then proceed to the next cell to be checked
                }

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
                        

                        //Radiative rate Equation: q" = E * stephan Boltzmann constant * (Ts^4-Tinf^4)
                        //orignally, the fucntionality was just more conductive transfer to the environment, so I swapped that for radiative
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

    public void SimulateHeatNewton()
    {
        //Convective HT Here
        //Newtons law of cooling q" = h * deltaT


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
        if ( x < 0 || x >= widthPoints || y < 0 || y >= heightPoints)
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
