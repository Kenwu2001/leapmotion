using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TcpSender))]
public class TcpSenderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TcpSender sender = (TcpSender)target;

        // Draw all default fields (except HideInInspector ones)
        DrawDefaultInspector();

        // If showDebugValues is off, stop here
        if (!sender.showDebugValues)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space();

        // --- Position debug (always shown) ---
        EditorGUILayout.LabelField("=== Debug: Position (offset from origin) ===", EditorStyles.boldLabel);
        EditorGUILayout.FloatField("pos_x", sender.debug_pos_x);
        EditorGUILayout.FloatField("pos_y", sender.debug_pos_y);
        EditorGUILayout.FloatField("pos_z", sender.debug_pos_z);

        EditorGUILayout.Space();

        // --- Rotation debug (only the selected mode) ---
        switch (sender.rotationMode)
        {
            case RotationType.Euler:
                EditorGUILayout.LabelField("=== Debug: Euler (degrees, offset from origin) ===", EditorStyles.boldLabel);
                EditorGUILayout.FloatField("euler_x", sender.debug_euler_x);
                EditorGUILayout.FloatField("euler_y", sender.debug_euler_y);
                EditorGUILayout.FloatField("euler_z", sender.debug_euler_z);
                break;

            case RotationType.Quaternion:
                EditorGUILayout.LabelField("=== Debug: Quaternion (relative rotation) ===", EditorStyles.boldLabel);
                EditorGUILayout.FloatField("quat_x", sender.debug_quat_x);
                EditorGUILayout.FloatField("quat_y", sender.debug_quat_y);
                EditorGUILayout.FloatField("quat_z", sender.debug_quat_z);
                EditorGUILayout.FloatField("quat_w", sender.debug_quat_w);
                break;

            case RotationType.RotationMatrix:
                EditorGUILayout.LabelField("=== Debug: Rotation Matrix (3x3, row-major) ===", EditorStyles.boldLabel);
                EditorGUILayout.Vector3Field("Row 0", sender.debug_matrix_row0);
                EditorGUILayout.Vector3Field("Row 1", sender.debug_matrix_row1);
                EditorGUILayout.Vector3Field("Row 2", sender.debug_matrix_row2);
                break;
        }

        // Force repaint while playing so values update live
        if (Application.isPlaying)
        {
            Repaint();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
