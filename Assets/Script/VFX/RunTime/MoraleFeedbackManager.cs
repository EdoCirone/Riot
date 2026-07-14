using System.Collections.Generic;
using UnityEngine;

public class MoraleFeedbackManager : MonoBehaviour
{
    [Header("Event References")]
    [SerializeField] private MoraleEventSO moraleEvent;

    [Header("Feedback Settings")]
    [SerializeField] private MoraleFeedbackVFX feedbackPrefab;
    [SerializeField] private Transform feedbackParent;

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 10;

    private Queue<MoraleFeedbackVFX> _feedbackPool;
    private int _poolIndex;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        _feedbackPool = new Queue<MoraleFeedbackVFX>();

        for (int i = 0; i < poolSize; i++)
        {
            MoraleFeedbackVFX feedback = Instantiate(feedbackPrefab, feedbackParent);
            feedback.gameObject.SetActive(false);
            _feedbackPool.Enqueue(feedback);
        }
    }

    private MoraleFeedbackVFX GetFeedbackFromPool()
    {
        if (_feedbackPool.Count == 0)
        {
            MoraleFeedbackVFX newFeedback = Instantiate(feedbackPrefab, feedbackParent);
            return newFeedback;
        }

        return _feedbackPool.Dequeue();
    }

    private void ReturnFeedbackToPool(MoraleFeedbackVFX feedback)
    {
        feedback.gameObject.SetActive(false);
        _feedbackPool.Enqueue(feedback);
    }

    private void OnEnable()
    {
        if (moraleEvent != null)
            moraleEvent.Subscribe(OnMoraleChanged);
    }

    private void OnDisable()
    {
        if (moraleEvent != null)
            moraleEvent.Unsubscribe(OnMoraleChanged);
    }

    private void OnMoraleChanged(MoraleChangeData data)
    {
        MoraleFeedbackVFX feedback = GetFeedbackFromPool();
        feedback.gameObject.SetActive(true);

        feedback.ShowMoraleFeedback(data, () => ReturnFeedbackToPool(feedback));
    }
}