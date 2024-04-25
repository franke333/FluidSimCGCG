using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Simulation))]
public class SimulationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Simulation simulation = (Simulation)target;

        GUILayout.Space(10);
        GUILayout.Label("To apply changes, click the button below");
        // Add a button to the inspector
        if (GUILayout.Button("Set Compute Variables"))
        {
            // Call the SetComputeVariables method when the button is clicked
            simulation.SetComputeVariables();
        }
    }
}
