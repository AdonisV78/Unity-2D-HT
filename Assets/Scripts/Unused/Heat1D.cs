using UnityEngine;

public class Heat1D : MonoBehaviour
{
    [Header("Physical Parameters")]
    public float thermalConductivity = 0.5f;  // k [W/m·K]
    public float density = 7800f;             // ρ [kg/m³]
    public float specificHeat = 500f;         // cp [J/kg·K]

    [Header("Simulation Settings")]
    public int numPoints = 50;
    public float deltaTime = 0.01f;
    public float deltaX = 1f;
    public GameObject heatCell;

    [Header("Initial Conditions")]
    public float backgroundTemperature = 20f;    // cool spot temp
    public float hotspotTemperature = 100f;      // hot spot temp
    public bool isothermalHotspot = true;        // constant temp hotspot toggle

    [Header("Boundary Conditions")]
    public bool isolateSystem = true;             // toggle boundary insulation

    private float[] temperatures;
    private float[] newTemperatures;
    private GameObject[] visuals;

    void Start()
    {
        temperatures = new float[numPoints];
        newTemperatures = new float[numPoints];
        visuals = new GameObject[numPoints];

        for (int i = 0; i < numPoints; i++)
        {
            Vector3 pos = new Vector3(i - (numPoints / 2), 0, 0);
            visuals[i] = Instantiate(heatCell, pos, Quaternion.identity);
            temperatures[i] = backgroundTemperature;
        }

        temperatures[numPoints / 2] = hotspotTemperature;
    }

    void Update()
    {
        SimulateHeatFourier();
        UpdateVisuals();
    }

    void SimulateHeatFourier()
    {
        for (int i = 1; i < numPoints - 1; i++)
        {
            if (isothermalHotspot && i == numPoints / 2)
            {
                newTemperatures[i] = hotspotTemperature;
                continue;
            }

            float qLeft = -thermalConductivity * (temperatures[i] - temperatures[i - 1]) / deltaX;
            float qRight = -thermalConductivity * (temperatures[i + 1] - temperatures[i]) / deltaX;

            float dTdt = (qLeft - qRight) / deltaX;
            newTemperatures[i] = temperatures[i] + deltaTime * dTdt / (density * specificHeat);
        }

        // Boundary conditions
        if (isolateSystem)
        {
            // Insulated boundaries (zero heat flux)
            newTemperatures[0] = newTemperatures[1];
            newTemperatures[numPoints - 1] = newTemperatures[numPoints - 2];
        }
        else
        {
            // Fixed boundary temperatures equal to background temp
            newTemperatures[0] = backgroundTemperature;
            newTemperatures[numPoints - 1] = backgroundTemperature;
        }

        for (int i = 0; i < numPoints; i++)
            temperatures[i] = newTemperatures[i];
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < numPoints; i++)
        {
            float tNorm = Mathf.InverseLerp(backgroundTemperature, hotspotTemperature, temperatures[i]);
            Color color = Color.Lerp(Color.blue, Color.red, tNorm);
            visuals[i].GetComponent<SpriteRenderer>().color = color;
        }
    }
}
