using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class BootManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private CanvasGroup _fadeCanvas;
    [SerializeField] private CanvasGroup _loadingCanvas;
    [SerializeField] private TMP_Text _loadingText;

    [Header("Settings")]
    [SerializeField] private float _fadeDuration = 1f;
    [SerializeField] private string _sceneToLoad = "MainMenu";

    private Coroutine _loadingTextCoroutine;

    private void Start()
    {
        // Assicurati che il loading canvas sia invisibile
        _loadingCanvas.alpha = 0f;
        _loadingCanvas.gameObject.SetActive(false);

        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        Debug.Log("[BOOT] Avvio sequenza boot...");

        // --- fade bianco iniziale (da opaco a trasparente) ---
        yield return StartCoroutine(FadeCanvas(_fadeCanvas, 1f, 0f, _fadeDuration, Color.white));

        // --- carica la scena in background ---
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(_sceneToLoad);
        loadOp.allowSceneActivation = false;

        // --- prepara il video ---
        _videoPlayer.Prepare();
        while (!_videoPlayer.isPrepared)
        {
            Debug.Log("[BOOT] Attendo preparazione video...");
            yield return null;
        }

        // --- avvia il video ---
        _videoPlayer.Play();
        Debug.Log("[BOOT] Video avviato");

        // il video è già tagliato alla lunghezza corretta in After Effects
        yield return new WaitUntil(() => !_videoPlayer.isPlaying);

        Debug.Log("[BOOT] Video terminato → fade bianco");

        _videoPlayer.Pause();
        Debug.Log($"[BOOT] Video fermato a {_videoPlayer.time:F2}s → fade bianco");

        // --- fade bianco per transizione ---
        yield return StartCoroutine(FadeCanvas(_fadeCanvas, 0f, 1f, _fadeDuration, Color.white));

        // --- disattiva il video per evitare che rimanga visibile ---
        _videoPlayer.targetCameraAlpha = 0f;

        // --- mostra "loading" ---
        _loadingCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvas(_loadingCanvas, 0f, 1f, 0.5f, Color.clear));

        _loadingTextCoroutine = StartCoroutine(AnimateLoadingText());

        // --- attendi caricamento ---
        float timer = 0f;
        while (loadOp.progress < 0.9f && timer < 30f)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log($"[BOOT] Caricamento completato (progress={loadOp.progress:F2})");

        // --- stoppa animazione loading ---
        if (_loadingTextCoroutine != null)
            StopCoroutine(_loadingTextCoroutine);

        // --- fade out del loading canvas ---
        yield return StartCoroutine(FadeCanvas(_loadingCanvas, 1f, 0f, 0.3f, Color.clear));

        // --- fade nero e attiva scena ---
        _fadeCanvas.GetComponent<Image>().color = Color.black;
        yield return StartCoroutine(FadeCanvas(_fadeCanvas, 0f, 1f, _fadeDuration, Color.black));

        Debug.Log("[BOOT] Attivo MainMenu...");
        loadOp.allowSceneActivation = true;
    }

    private IEnumerator FadeCanvas(CanvasGroup canvas, float from, float to, float duration, Color? fadeColor = null)
    {
        float t = 0f;
        Image img = canvas.GetComponent<Image>();

        if (img != null && fadeColor.HasValue)
            img.color = fadeColor.Value;

        canvas.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvas.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        canvas.alpha = to;
    }

    private float _dotTimer = 0f;
    private int _dotCount = 0;

    private IEnumerator AnimateLoadingText()
    {
        while (true)
        {
            _dotTimer += Time.unscaledDeltaTime;
            if (_dotTimer > 0.5f)
            {
                _dotTimer = 0f;
                _dotCount = (_dotCount + 1) % 4;
                _loadingText.text = "Loading" + new string('.', _dotCount);
            }
            yield return null;
        }
    }
}
