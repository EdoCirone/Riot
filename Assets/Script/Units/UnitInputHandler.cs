using UnityEngine;

public class UnitInputHandler : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Pathfinder pathfinder;

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Selezione settori con tasti 1-8
        for (int i = 1; i <= 8; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                UnitSelector.Instance.SelectSector(i);
            }
        }

        // Movimento con click destro
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int targetCoords = gridManager.WorldToGridCoordinates(worldPos);
            Debug.Log($"📍 Click rilevato a worldPos: {worldPos}, grid: {targetCoords}");

            var selectedUnits = UnitSelector.Instance.GetSelectedUnits();
            Debug.Log($"👥 Unità selezionate: {selectedUnits.Count}");

            if (selectedUnits.Count == 0)
            {
                Debug.LogWarning("⚠️ Nessuna unità selezionata.");
                return;
            }

            UnitController.Instance.CommandMove(selectedUnits, targetCoords);
        }
    }
}
