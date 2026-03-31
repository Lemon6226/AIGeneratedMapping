#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RoomMaterialSetup
{
    const string MAT_PATH = "Assets/Resources/RoomMaterial.mat";

    [MenuItem("Tools/Setup Room Material")]
    static void Setup()
    {
        // Ensure Resources folder exists
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Error",
                "URP Lit shader not found — is URP installed?", "OK");
            return;
        }

        var mat = new Material(shader);
        mat.color = new Color(0.55f, 0.55f, 0.55f); // mid-grey walls

        AssetDatabase.CreateAsset(mat, MAT_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RoomMaterialSetup] Created " + MAT_PATH);
        EditorUtility.DisplayDialog("Done",
            "RoomMaterial created at " + MAT_PATH + "\n\nHit Play — walls will now be lit correctly.", "OK");
    }
}
#endif
