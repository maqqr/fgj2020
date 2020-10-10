using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Settings))]
public class SettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Settings targetPlayer = (Settings)target;

        DrawDefaultInspector();

        //EditorGUILayout.LabelField("Some help", "Some other text");
        if (GUILayout.Button("Reload settings (works only in game)"))
        {
            if (Application.isPlaying)
            {
                GameManager.Instance.ReloadSettings();
            }
        }

        //targetPlayer.speed = EditorGUILayout.Slider("Speed", targetPlayer.speed, 0, 100);

        // Show default inspector property editor
    }
}
