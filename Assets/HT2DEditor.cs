using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(HT2D))]
public class HT2DEditor : Editor
{

    #region SerializedProperty
    SerializedProperty thermalConductivity; // k [W/m·K]
    SerializedProperty density;             // ρ [kg/m³]
    SerializedProperty specificHeat;    // cp [J/kg·K]

    SerializedProperty emissivity;

    SerializedProperty widthPoints;
    SerializedProperty heightPoints;


    SerializedProperty heatCell;
    SerializedProperty cellToBeHot;

    //bunch of bools
    SerializedProperty isolatedSystem;
    SerializedProperty conductiveTransfer;
    SerializedProperty convectiveTransfer;
    SerializedProperty radiativeTransfer;
    SerializedProperty isothermalHeatSource;


    SerializedProperty deltaTime; //How quickly the sumulation goes
    SerializedProperty deltaX;
    SerializedProperty deltaY;



    SerializedProperty T1;
    SerializedProperty T2;
    SerializedProperty tInfinity;


    SerializedProperty isPaused;

    bool simulationSettingsGroup;
    #endregion

    private void OnEnable()
    {
        thermalConductivity = serializedObject.FindProperty("thermalConductivity"); // k [W/m·K]
        density = serializedObject.FindProperty("density");             // ρ [kg/m³]
        specificHeat = serializedObject.FindProperty("specificHeat");    // cp [J/kg·K]


        emissivity = serializedObject.FindProperty("emissivity");

        widthPoints = serializedObject.FindProperty("widthPoints");
        heightPoints = serializedObject.FindProperty("heightPoints");


        heatCell = serializedObject.FindProperty("heatCell");
        cellToBeHot = serializedObject.FindProperty("cellToBeHot");

        //bunch of bools
        isolatedSystem = serializedObject.FindProperty("isolatedSystem");
        conductiveTransfer = serializedObject.FindProperty("conductiveTransfer");
        convectiveTransfer = serializedObject.FindProperty("convectiveTransfer");
        radiativeTransfer = serializedObject.FindProperty("radiativeTransfer");
        isothermalHeatSource = serializedObject.FindProperty("isothermalHeatSource");


        deltaTime = serializedObject.FindProperty("deltaTime"); //How quickly the sumulation goes
        deltaX = serializedObject.FindProperty("deltaX");
        deltaY = serializedObject.FindProperty("deltaY");



        T1 = serializedObject.FindProperty("T1");
        T2 = serializedObject.FindProperty("T2");
        tInfinity = serializedObject.FindProperty("tInfinity");


        isPaused = serializedObject.FindProperty("isPaused");
    }
    public override void OnInspectorGUI()
    {
        HT2D _HT2D = (HT2D)target;

        serializedObject.Update();

        EditorGUILayout.PropertyField(thermalConductivity);
        EditorGUILayout.PropertyField(density);
        EditorGUILayout.PropertyField(specificHeat);
        EditorGUILayout.PropertyField(emissivity);

        simulationSettingsGroup = EditorGUILayout.BeginFoldoutHeaderGroup(simulationSettingsGroup, "Simulation Settings");
        if (simulationSettingsGroup)
        {
            EditorGUILayout.PropertyField(widthPoints);
            EditorGUILayout.PropertyField(heightPoints);

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(deltaX);
            EditorGUILayout.PropertyField(deltaY);

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(deltaTime);

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(heatCell);
            EditorGUILayout.PropertyField(cellToBeHot);

            EditorGUILayout.PropertyField(isothermalHeatSource);

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(T1);
            EditorGUILayout.PropertyField(T2);
            EditorGUILayout.PropertyField(tInfinity);

            EditorGUILayout.Space(5);


            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(conductiveTransfer);
            EditorGUILayout.PropertyField(isolatedSystem);
            if (!_HT2D.isolatedSystem)
            {
                EditorGUILayout.PropertyField(convectiveTransfer);
                EditorGUILayout.PropertyField(radiativeTransfer);
            }
            EditorGUILayout.Space(10);
        }

        EditorGUILayout.PropertyField(isPaused);








        serializedObject.ApplyModifiedProperties();
    }
}
