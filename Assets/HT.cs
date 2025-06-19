using UnityEngine;

public class HT : MonoBehaviour
{
    [Header("Physical Parameters")]
    public float thermalConductivity = 0.5f;  // k [W/m·K]
    public float density = 7800f;             // ρ [kg/m³]
    public float specificHeat = 500f;         // cp [J/kg·K]

    [Header("Simulation Settings")]
    public int numPoints = 9; //How many points to be simulated
    public float deltaTime = 0.01f; //How quickly the sumulation goes
    public float deltaX = 1f;
    public GameObject heatCell;


    [Header("Boundary Conditions")]
    public float T1 = 100;
    public float T2 = 0;

    public int cellToBeHot = 0;

    public float environmentTemperature = 20;

    public bool isolatedSystem = true;
    public bool isothermalHeatSource = true;

    private float[] temperatures;
    private float[] newTemperatures;
    private GameObject[] visuals;

    // Start is called before the first frame update
    void Start()
    {
        temperatures = new float[numPoints];
        newTemperatures = new float[numPoints];
        visuals = new GameObject[numPoints];

        for (int i = 0; i < numPoints; i++)
        {
            Vector3 pos = new Vector3(i - (numPoints / 2), 0, 0);
            visuals[i] = Instantiate(heatCell, pos, Quaternion.identity); //Generate the cells
            temperatures[i] = T2; //make all the cells the cold temperature
        }

        temperatures[cellToBeHot] = T1; //set the desired cell to be the hot temperature
    }

    // Update is called once per frame
    void Update()
    {
        SimulateHeatFourier();
        UpdateVisuals();
    }

    public void SimulateHeatFourier()
    {
        for (int i = 0; i < numPoints; i++)
        {
            if(isothermalHeatSource && i == cellToBeHot)
            {
                newTemperatures[i] = T1;
                continue;
            }

            float qLeft = 0;
            float qRight = 0;
            
            switch (isolatedSystem)
            {
                case true:

                    if ((i - 1) < 0)
                    {
                        qLeft = 0;
                        qRight = -thermalConductivity * (temperatures[i + 1] - temperatures[i]) / deltaX;

                    }
                    else if ((i + 1) == numPoints)
                    {
                        qLeft = -thermalConductivity * (temperatures[i] - temperatures[i - 1]) / deltaX;
                        qRight = 0;
                    }
                    else
                    {
                        qLeft = -thermalConductivity * (temperatures[i] - temperatures[i - 1]) / deltaX;
                        qRight = -thermalConductivity * (temperatures[i + 1] - temperatures[i]) / deltaX;
                    }
                    break;

                case false:

                    if ((i - 1) < 0)
                    {
                       qLeft = -thermalConductivity * (temperatures[i] - environmentTemperature) / deltaX;
                       qRight = -thermalConductivity * (temperatures[i + 1] - temperatures[i]) / deltaX;
                       
                    }
                    else if ((i + 1) == numPoints)
                    {
                       qLeft = -thermalConductivity * (temperatures[i] - temperatures[i - 1]) / deltaX;
                       qRight = -thermalConductivity * (environmentTemperature - temperatures[i]) / deltaX;
                    }
                    else
                    {
                        qLeft = -thermalConductivity * (temperatures[i] - temperatures[i - 1]) / deltaX;
                        qRight = -thermalConductivity * (temperatures[i + 1] - temperatures[i]) / deltaX;
                    }
                    break;
            }

            float dTdt = (qLeft - qRight) / deltaX;
            newTemperatures[i] = temperatures[i] + deltaTime * dTdt / (density * specificHeat);
        }

        for (int i = 0; i < numPoints; i++)
            temperatures[i] = newTemperatures[i];
    }




    void UpdateVisuals()
    {
        for (int i = 0; i < numPoints; i++)
        {
            float tNorm = Mathf.InverseLerp(T2, T1, temperatures[i]);
            Color color = Color.Lerp(Color.blue, Color.red, tNorm);
            visuals[i].GetComponent<SpriteRenderer>().color = color;
        }
    }
}
