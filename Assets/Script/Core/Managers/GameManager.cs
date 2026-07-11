using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public void ResetLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void PlayNewRun()
    {
        SceneManager.LoadScene("LVLTest");
    }

    public void OnApplicationQuit()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        PlayerPrefs.Save();
        Application.Quit();
    }


}
