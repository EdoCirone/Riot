using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Menu,
        PreGame,        // Caricamento iniziale, setup tecnico
        Composition,    // Fase di composizione del corteo
        Playing,        // Gioco attivo
        Paused,         // Pausa
        Ended           // Fine partita
    }

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentState = GameState.PreGame;
    }

    private void Start()
    {
        EnterCompositionPhase();
    }

    public void EnterCompositionPhase()
    {
        CurrentState = GameState.Composition;
        Debug.Log("Fase di composizione del corteo iniziata.");
    }


    public void ConfirmCorteoComposition()
    {
        StartGame();
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1;
        Debug.Log("Game Started");
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0;
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1;
        Debug.Log("Game Resumed");
    }

    public void EndGame()
    {
        CurrentState = GameState.Ended;
        Debug.Log("Game Ended");
    }
}
