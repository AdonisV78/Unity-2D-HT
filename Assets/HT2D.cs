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

    [Range(0f, 1f)]
    [Tooltip("For Radiative Transfer")]
    public float emissivity = 1;

    public int widthPoints = 3;
    public int heightPoints = 3;

    private int numPoints;

    public GameObject heatCell;
    [Tooltip("(0,0) is the bottom left")]
    public Vector2 cellToBeHot;

    public bool isolatedSystem = true;
    public bool conductiveTransfer = true;
    public bool convectiveTransfer = true;
    public bool radiativeTransfer = true;
    public bool isothermalHeatSource = true;

    [Range(0.01f, 5f)]
    public float deltaTime = 0.01f;

    [Range(0.01f, 1f)]
    public float deltaX = 1f;

    [Range(0.01f, 1f)]
    public float deltaY = 1f;

    [Tooltip("Units of Celsius")] public float T1 = 100;
    [Tooltip("Units of Celsius")] public float T2 = 0;
    [Tooltip("Units of Celsius")] public float tInfinity = 20;

    private float[] temperatures;
    private float[] newTemperatures;
    private GameObject[] visuals;
    private List<Vector2Int> heatCellPositions;

    [Header("New Features")]
    public bool isPaused = true;

    public enum windSourcePosition
    {
        Top,
        Bottom,
        Left,
        Right
    }

    void Start()
    {
        SetUp();
    }

    void Update()
    {
        if (!isPaused)
        {
            SimulateHeatFourier();
        }

        UpdateVisuals();
    }

    public void SetUp()
    {
        numPoints = widthPoints * heightPoints;

        temperatures = new float[numPoints];
        newTemperatures = new float[numPoints];
        visuals = new GameObject[numPoints];
        heatCellPositions = new List<Vector2Int>();

        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                Vector3 pos = new Vector3((i - (widthPoints - 1) / 2f) * deltaX, (j - (heightPoints - 1) / 2f) * deltaY, 0);
                visuals[getIndex(i, j)] = Instantiate(heatCell, pos, Quaternion.identity);
                visuals[getIndex(i, j)].transform.localScale = new Vector3(deltaX, deltaY, 1f);

                heatCellPositions.Add(new Vector2Int(i, j));
                temperatures[getIndex(i, j)] = T2;
            }
        }

        temperatures[getIndex(Mathf.FloorToInt(cellToBeHot.x), Mathf.FloorToInt(cellToBeHot.y))] = T1;
    }

    public void SimulateHeatFourier()
    {
        for (int j = 0; j < heightPoints; j++)
        {
            for (int i = 0; i < widthPoints; i++)
            {
                if (isothermalHeatSource && getIndex(i, j) == getIndex(Mathf.FloorToInt(cellToBeHot.x), Mathf.FloorToInt(cellToBeHot.y)))
                {
                    newTemperatures[getIndex(i, j)] = T1;
                    continue;
                }

                float qLeft = 0, qRight = 0, qUp = 0, qDown = 0;

                int ThisBox = getIndex(i, j);
                int Rightbox = getIndex(i + 1, j);
                int LeftBox = getIndex(i - 1, j);
                int UpBox = getIndex(i, j + 1);
                int DownBox = getIndex(i, j - 1);

                switch (isolatedSystem)
                {
                    case true:
                        qRight = -thermalConductivity * (temperatures[Rightbox] - temperatures[ThisBox]) / deltaX;
                        qLeft = -thermalConductivity * (temperatures[ThisBox] - temperatures[LeftBox]) / deltaX;
                        qDown = -thermalConductivity * (temperatures[DownBox] - temperatures[ThisBox]) / deltaY;
                        qUp = -thermalConductivity * (temperatures[ThisBox] - temperatures[UpBox]) / deltaY;

                        if ((i - 1) < 0) qLeft = 0;
                        if ((i + 1) == widthPoints) qRight = 0;
                        if ((j - 1) < 0) qDown = 0;
                        if ((j + 1) == heightPoints) qUp = 0;
                        break;

                    case false:
                        qRight = -thermalConductivity * (temperatures[Rightbox] - temperatures[ThisBox]) / deltaX;
                        qLeft = -thermalConductivity * (temperatures[ThisBox] - temperatures[LeftBox]) / deltaX;
                        qDown = -thermalConductivity * (temperatures[DownBox] - temperatures[ThisBox]) / deltaY;
                        qUp = -thermalConductivity * (temperatures[ThisBox] - temperatures[UpBox]) / deltaY;

                        double sigma = 5.67e-8;

                        if ((i - 1) < 0)
                            qLeft = -(float)(emissivity * sigma * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));

                        if ((i + 1) == widthPoints)
                            qRight = -(float)(emissivity * sigma * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));

                        if ((j - 1) < 0)
                            qDown = -(float)(emissivity * sigma * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));

                        if ((j + 1) == heightPoints)
                            qUp = -(float)(emissivity * sigma * (Math.Pow(temperatures[ThisBox] + 273.15, 4) - Math.Pow(tInfinity + 273.15, 4)));
                        break;
                }

                float dTdtx = (qLeft - qRight) / deltaX;
                float dTdty = (qUp - qDown) / deltaY;
                newTemperatures[getIndex(i, j)] = temperatures[ThisBox] + deltaTime * (dTdtx + dTdty) / (density * specificHeat);
            }
        }

        for (int j = 0; j < heightPoints; j++)
            for (int i = 0; i < widthPoints; i++)
                temperatures[getIndex(i, j)] = newTemperatures[getIndex(i, j)];
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

    int getIndex(int x, int y)
    {
        if (x < 0 || x >= widthPoints || y < 0 || y >= heightPoints)
            return 0;
        else
            return (y * widthPoints) + x;
    }

    public void PauseUnPause()
    {
        isPaused = !isPaused;
    }
}