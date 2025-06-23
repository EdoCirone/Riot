using UnityEngine;

public static class GridUtility
{
    public static Vector2Int WorldToGridCoordinates(this GridManager gridManager, Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / gridManager.CellSize);
        int y = Mathf.FloorToInt(worldPosition.y / gridManager.CellSize);
        return new Vector2Int(x, y);
    }

    public static Vector3 GridToWorldCoordinates(this GridManager gridManager, Vector2Int gridPosition)
    {
        float x = gridPosition.x * gridManager.CellSize + gridManager.CellSize / 2f;
        float y = gridPosition.y * gridManager.CellSize + gridManager.CellSize / 2f;
        return new Vector3(x, y, 0);
    }
}
