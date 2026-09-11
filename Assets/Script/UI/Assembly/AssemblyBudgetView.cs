using TMPro;
using UnityEngine;

public sealed class AssemblyBudgetView : MonoBehaviour
{
    [SerializeField] private TMP_Text _availableBudgetText;

    private void Awake()
    {
        if (_availableBudgetText == null)
        {
            Debug.LogError(
                "[ASSEMBLY UI] Available budget text not assigned",
                this);
        }
    }

    public void ShowUnavailable()
    {
        if (_availableBudgetText == null)
            return;

        _availableBudgetText.text =
            "Budget disponibile: -- PR";
    }
    public void ShowBudget(
        int availablePoints,
        int temporaryPoints)
    {
        if (_availableBudgetText == null)
            return;

        _availableBudgetText.text =
            $"Budget: {availablePoints} PR " +
            $"(+{temporaryPoints})";
    }
}
