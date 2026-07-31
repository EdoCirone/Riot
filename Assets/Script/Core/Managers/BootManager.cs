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
    [SerializeField] private float _minLoadingDisplayTime = 2f;
    [SerializeField] private string _sceneToLoad = "MainMenu";

    private Coroutine _loadingTextCoroutine;
    private bool _videoFinished;
    private void OnVideoFinished(VideoPlayer vp) => _videoFinished = true;

    private void Start()
    {
    #if UNITY_EDITOR
            string editorTargetPath = UnityEditor.EditorPrefs.GetString("PLAY_FROM_SCENE_PATH", "");
            if (!string.IsNullOrEmpty(editorTargetPath))
            {
                _sceneToLoad = editorTargetPath; // SceneManager accetta il path della scena
                Debug.Log($"[BOOT] Editor mode: target scena = '{_sceneToLoad}'");
                UnityEditor.EditorPrefs.DeleteKey("PLAY_FROM_SCENE_PATH");
            }
    #endif

        // Assicurati che il loading canvas sia invisibile
        _loadingCanvas.alpha = 0f;
        _loadingCanvas.gameObject.SetActive(false);

        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        Debug.Log("[BOOT] Avvio sequenza boot...");

        // --- avvia il video PRIMA del fade, così il bianco lo scopre ---
        // NON chiamare Prepare(): blocca la presentazione dei frame. Vedi CLAUDE.md.
        _videoFinished = false;
        _videoPlayer.loopPointReached += OnVideoFinished;
        _videoPlayer.Play();

        // frame vale -1 finché nulla è stato presentato: aspetta il primo frame reale
        yield return new WaitUntil(() => _videoPlayer.frame >= 0);

        // --- scopre il video sfumando via il bianco ---
        yield return StartCoroutine(FadeCanvas(_fadeCanvas, 1f, 0f, _fadeDuration, Color.white));

        // durata letta dall'ASSET: VideoPlayer.length vale 0 senza Prepare()
        float clipLength = _videoPlayer.clip != null ? (float)_videoPlayer.clip.length : 10f;
        float maxWait = clipLength + 2f;
        float videoTimer = 0f;

        // attesa fine video: evento (preciso) + fail-safe a tempo (non si blocca mai)
        yield return new WaitUntil(() =>
        {
            videoTimer += Time.unscaledDeltaTime;
            return _videoFinished || videoTimer > maxWait;
        });

        _videoPlayer.loopPointReached -= OnVideoFinished;
        Debug.Log($"[BOOT] Video: evento={_videoFinished} timer={videoTimer:F2}/{maxWait:F2} " +
                  $"frame={_videoPlayer.frame}/{_videoPlayer.frameCount}");
        _videoPlayer.Pause();

        // --- fade bianco per coprire il video ---
        yield return StartCoroutine(FadeCanvas(_fadeCanvas, 0f, 1f, _fadeDuration, Color.white));

        // --- nasconde il video (valido solo nei render mode a camera) ---
        _videoPlayer.targetCameraAlpha = 0f;

        // --- carica la scena in background ---
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(_sceneToLoad);
        loadOp.allowSceneActivation = false;

        // --- mostra "loading" sopra il bianco ---
        _loadingCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvas(_loadingCanvas, 0f, 1f, 0.5f));

        _loadingTextCoroutine = StartCoroutine(AnimateLoadingText());
        float loadingStartTime = Time.unscaledTime;

        // --- attendi caricamento (progress si ferma a 0.9 con allowSceneActivation=false) ---
        float timer = 0f;
        while (loadOp.progress < 0.9f && timer < 30f)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log($"[BOOT] Caricamento completato (progress={loadOp.progress:F2})");

        // --- durata minima visibile del loading ---
        float elapsed = Time.unscaledTime - loadingStartTime;
        if (elapsed < _minLoadingDisplayTime)
            yield return new WaitForSecondsRealtime(_minLoadingDisplayTime - elapsed);

        // --- stoppa animazione loading ---
        if (_loadingTextCoroutine != null)
        {
            StopCoroutine(_loadingTextCoroutine);
            _loadingTextCoroutine = null;
        }

        // --- fade out del loading canvas ---
        yield return StartCoroutine(FadeCanvas(_loadingCanvas, 1f, 0f, 0.3f));
        _loadingCanvas.gameObject.SetActive(false);

        // --- transizione bianco → nero animando il COLORE, non l'alpha ---
        yield return StartCoroutine(FadeImageColor(_fadeCanvas, Color.white, Color.black, 0.5f));

        Debug.Log("[BOOT] Attivo MainMenu...");
        loadOp.allowSceneActivation = true;
    }

    /// Sfuma il colore dell'Image mantenendo il canvas opaco.
    /// Serve quando lo schermo è GIÀ coperto e devi cambiare tinta:
    /// animare l'alpha in quel caso scoprirebbe ciò che sta sotto.
    private IEnumerator FadeImageColor(CanvasGroup canvas, Color from, Color to, float duration)
    {
        Image img = canvas.GetComponent<Image>();
        if (img == null) yield break;

        canvas.alpha = 1f;
        img.color = from;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            img.color = Color.Lerp(from, to, t / duration);
            yield return null;
        }
        img.color = to;
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

    private void OnDestroy()
    {
        if (_videoPlayer != null)
            _videoPlayer.loopPointReached -= OnVideoFinished;
    }
}
