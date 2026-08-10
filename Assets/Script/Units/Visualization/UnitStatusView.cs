using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Mostra la CONDIZIONE dell'unità — panico e seduto — e niente altro.
/// Non sa di movimento, di azioni o di regole: riceve due booleani e allinea la grafica.
///
/// Sta sullo stesso GameObject di UnitMovement ma è responsabilità diversa:
/// UnitMovement anima gli spostamenti, questo dipinge uno stato.
///
/// ⚠ È l'UNICO proprietario di SpriteRenderer.color su quest'unità. Quando arriverà
/// il lampo rosso da danno dovrà stare qui dentro e tornare a _currentTint, non al
/// bianco: due componenti che scrivono lo stesso colore si cancellano a vicenda.
/// </summary>
public class UnitStatusView : MonoBehaviour
{
    [SerializeField] private Transform _graphicsTransform;

    [Header("Tint")]
    [Tooltip("Grigio freddo: legge come 'spento'. sr.color moltiplica, non desatura — " +
             "per una desaturazione vera servirebbe un parametro nello shader.")]
    [SerializeField] private Color _panicTint = new Color(0.62f, 0.66f, 0.74f);
    [SerializeField] private Color _seatedTint = new Color(0.55f, 0.72f, 1f);

    [Header("Panic wiggle")]
    [SerializeField] private float _wiggleDistance = 0.02f;
    [SerializeField] private float _wiggleDuration = 0.08f;

    [Header("Damage flash")]
    [SerializeField] private Color _damageFlash = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float _flashDuration = 0.15f;


    private SpriteRenderer[] _tintables;
    private Color[] _baseColors;
    private Color _currentTint = Color.white;

    private float _wiggleBaseX;
    
    private Tween _flashTween;
    private Tween _wiggleTween;

    /// <summary>
    /// Unico punto d'ingresso, chiamato da UnitsRenderer.UpdateView.
    /// Il panico vince sul seduto: oggi non possono coesistere (GetPanicWave salta i
    /// seduti) ma la funzione non deve dipendere da quella garanzia.
    /// </summary>
    public void Refresh(bool panicked, bool seated)
    {
        ApplyTint(panicked ? _panicTint : seated ? _seatedTint : Color.white);
        ApplyWiggle(panicked);
    }

    /// <summary>Da chiamare PRIMA di disattivare il GameObject.</summary>
    public void Clear()
    {
        if (_flashTween != null && _flashTween.IsActive()) _flashTween.Kill();
        _flashTween = null;

        ApplyWiggle(false);
        ApplyTint(Color.white);
    }

    /// <summary>
    /// Lampo rosso che sfuma verso la tinta CORRENTE, non verso il bianco: un'unità
    /// in panico deve tornare grigia dopo il colpo, non normale.
    /// </summary>
    public void Flash()
    {
        if (_tintables == null) CacheTintables();

        if (_flashTween != null && _flashTween.IsActive()) _flashTween.Kill();

        float t = 0f;
        _flashTween = DOTween.To(() => t, v =>
        {
            t = v;
            Color c = Color.Lerp(_damageFlash, _currentTint, t);
            for (int i = 0; i < _tintables.Length; i++)
                _tintables[i].color = _baseColors[i] * c;
        }, 1f, _flashDuration).SetEase(Ease.OutQuad);
    }

    private void ApplyTint(Color target)
    {
        if (target == _currentTint) return;   // UpdateView gira spessissimo
        _currentTint = target;

        if (_tintables == null) CacheTintables();

        // Se un lampo è in corso, lascialo finire: sta già sfumando verso _currentTint,
        // che abbiamo appena aggiornato. Scrivere adesso lo taglierebbe a metà.
        if (_flashTween != null && _flashTween.IsActive()) return;

        // Moltiplica invece di sostituire: conserva la tinta originale dello sprite.
        // Con Color.white la moltiplicazione è neutra, quindi lo stato normale torna esatto.
        for (int i = 0; i < _tintables.Length; i++)
            _tintables[i].color = _baseColors[i] * target;
    }

    private void ApplyWiggle(bool on)
    {
        if (_graphicsTransform == null) return;

        if (on)
        {
            // Già in corso: non ripartire, o il tremore scatta a ogni UpdateView.
            if (_wiggleTween != null && _wiggleTween.IsActive()) return;

            Vector3 p = _graphicsTransform.localPosition;
            _wiggleBaseX = p.x;

            // Si parte da -distance e si oscilla verso +distance, così il tremore
            // è simmetrico attorno alla posizione vera invece che spostato da un lato.
            _graphicsTransform.localPosition = new Vector3(_wiggleBaseX - _wiggleDistance, p.y, p.z);
            _wiggleTween = _graphicsTransform
                .DOLocalMoveX(_wiggleBaseX + _wiggleDistance, _wiggleDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.Linear);   // Linear, non InOutSine: un tremore è meccanico
        }
        else
        {
            if (_wiggleTween == null) return;

            if (_wiggleTween.IsActive()) _wiggleTween.Kill();
            _wiggleTween = null;

            // Kill lascia il transform dov'era: la X va riportata a mano.
            Vector3 p = _graphicsTransform.localPosition;
            _graphicsTransform.localPosition = new Vector3(_wiggleBaseX, p.y, p.z);
        }
    }

    private void CacheTintables()
    {
        List<SpriteRenderer> list = new();

        foreach (SpriteRenderer sr in _graphicsTransform.GetComponentsInChildren<SpriteRenderer>())
        {
            //  SelectionOutline crea SpriteRenderer duplicati sul layer "Outline", e il
            // suo Initialize gira PRIMA del nostro (vedi LVLManager.Start). Senza questo
            // filtro, tingendo l'unità tingeresti anche il suo contorno di selezione.
            if (sr.sortingLayerName == "Outline") continue;
            list.Add(sr);
        }

        _tintables = list.ToArray();
        _baseColors = new Color[_tintables.Length];
        for (int i = 0; i < _tintables.Length; i++)
            _baseColors[i] = _tintables[i].color;
    }

    // Rete di sicurezza: se il GameObject viene disattivato per una strada diversa da
    // UnitsRenderer.UpdateView (pooling, cambio scena), il tween non resta appeso.
    private void OnDisable() => ApplyWiggle(false);
}
