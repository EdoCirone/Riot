using UnityEngine;


public class InputManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                GameManager.Instance.PauseGame();
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Paused)
                GameManager.Instance.ResumeGame();
        }

        if (GameManager.Instance.CurrentState == GameManager.GameState.Composition && Input.GetKeyDown(KeyCode.Return))
        {
            GameManager.Instance.ConfirmCorteoComposition();
        }

    }
}
