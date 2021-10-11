using UnityEngine;
using UnityEditor;
using Invertex.Unity.URP;

namespace Invertex.Unity.Editors.URP
{
    [CustomEditor(typeof(TilemapShadowCaster2D))]
    public class TilemapShadowCaster2DEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            TilemapShadowCaster2D tilemapCaster = (TilemapShadowCaster2D)target;
            EditorGUILayout.Space(5f);

            if (GUILayout.Button("Regenerate"))
            {
                tilemapCaster.Regenerate();
            }
        }
    }
}