using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HT2D : MonoBehaviour
{
    [Header("Physical Parameters")]
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
    public float T1 = 100;
    public float T2 = 0;


    [Tooltip("(0,0) is the bottom left")]
    public Vector2 cellToBeHot;

    public float environmentTemperature = 20;

    public bool isolatedSystem = true;
    public bool isothermalHeatSource = true;

    private float[] temperatures;
    private float[] newTemperatures;
    private GameObject[] visuals;
    private List<Vector2Int> heatCellPositions;



    [Header("New Features")]
    public float isPaused;

    
    // Start is called before the first frame update
    void Start()
    {
        numPoints = widthPoints * heightPoints;

        temperatures = new float[numPoints];
        newTemperatures = new float[numPoints];
        visuals = new GameObject[numPoints];

        heatCellPositions = new List<Vector2Int>();

        for (int j = 0; j < heightPoints; j++) //starts bottom left, then goes all the way right before going up one again
        {
            for(int i = 0; i < widthPoints; i++)
            {
                Vector3 pos = new Vector3(i - (widthPoints / 2), j - (heightPoints/2), 0);
                visuals[getIndex(i,j)] = Instantiate(heatCell, pos, Quaternion.identity); //Generate the cells
                
                heatCellPositions.Add(new Vector2Int(i, j));//low key might not need this, we'll see

                temperatures[getIndex(i,j)] = T2; //make all the cells the cold temperature
            }
        }

        temperatures[getIndex(Mathf.FloorToInt(cellToBeHot.x), Mathf.FloorToInt(cellToBeHot.y))] = T1;
       
    }

   
    // Update is called once per frame
    void Update()
    {
        SimulateHeatFourier();
        UpdateVisuals();
    }

    public void SimulateHeatFourier()
    {
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {

                if (isothermalHeatSource && getIndex(i,j) == getIndex(Mathf.FloorToInt(cellToBeHot.x), Mathf.FloorToInt(cellToBeHot.y)))
                {
                    newTemperatures[getIndex(i,j)] = T1;
                    continue;
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

                switch (isolatedSystem)
                {
                    case true:

                        //calculate all the Q for all four sides, then if one of the sides is exposed to the outside, make the heat loss/gain 0 since it is insulated
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

                    case false:
                        qRight = -thermalConductivity * (temperatures[Rightbox] - temperatures[ThisBox]) / deltaX;
                        qLeft = -thermalConductivity * (temperatures[ThisBox] - temperatures[LeftBox]) / deltaX;
                        qDown = -thermalConductivity * (temperatures[DownBox] - temperatures[ThisBox]) / deltaY;
                        qUp = -thermalConductivity * (temperatures[ThisBox] - temperatures[UpBox]) / deltaY;

                        if ((i - 1) < 0)
                        {
                            qLeft = -thermalConductivity * (temperatures[ThisBox] - environmentTemperature) / deltaX; ;
                        }

                        if ((i + 1) == widthPoints)
                        {
                            qRight = -thermalConductivity * (environmentTemperature - temperatures[ThisBox]) / deltaX;
                        }

                        if ((j - 1) < 0)
                        {
                            qDown = -thermalConductivity * (environmentTemperature - temperatures[ThisBox]) / deltaY;
                        }

                        if ((j + 1) == heightPoints)
                        {
                            qUp = -thermalConductivity * (temperatures[ThisBox] - environmentTemperature) / deltaY;
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


    

    void UpdateVisuals()
    {
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                float tNorm = Mathf.InverseLerp(Mathf.Min(T2, T1, environmentTemperature), Mathf.Max(T2, T1, environmentTemperature), temperatures[getIndex(i, j)]);
                Color color = Color.Lerp(Color.blue, Color.red, tNorm);
                visuals[getIndex(i, j)].GetComponent<SpriteRenderer>().color = color;
            }
        }
           
    }

    int getIndex(int x, int y) //way to make a unqiue index value based on position
    {
        if ( x < 0 || x >= widthPoints || y < 0 || y >= heightPoints)
        {
            return 0;
        }
        else
        {
            return (y * widthPoints) + x;
        }
            
    }
}
