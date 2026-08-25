#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayFromAnyScene
{
    private const string BootScenePath = "Assets/Scenes/Boot.unity";
    private const string TargetScenePathKey = "PLAY_FROM_SCENE_PATH";

    static PlayFromAnyScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            Scene activeScene = SceneManager.GetActiveScene();

            // Se sei già nella BootScene, non salvare target
            if (activeScene.path == BootScenePath)
            {
                EditorPrefs.DeleteKey(TargetScenePathKey);
            }
            else
            {
                EditorPrefs.SetString(TargetScenePathKey, activeScene.path);
                Debug.Log($"[PlayFromAnyScene] Target salvata: {activeScene.path}");
            }

            // Forza l'avvio dalla BootScene
            SceneAsset bootSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootSceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = bootSceneAsset;
            }
            else
            {
                Debug.LogError($"[PlayFromAnyScene] BootScene non trovata al path: {BootScenePath}");
            }
        }

        // Opzionale: reset quando torni in edit mode
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Se vuoi lasciare il forcing attivo sempre, commenta questa riga
            // EditorSceneManager.playModeStartScene = null;
        }
    }
}
#endif
