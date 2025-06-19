using System.Collections.Generic;
using UnityEngine;

public class CorteoSelector : MonoBehaviour
{
    public static CorteoSelector Instance;

    [System.Serializable]
    public class CorteoSector
    {
        public int sectorNumber;
        public List<GameObject> units; // collegati da Inspector o dinamicamente
    }

    public List<CorteoSector> sectors = new List<CorteoSector>();

    private void Awake()
    {
        Instance = this;
    }

    public void SelectSector(int number)
    {
        foreach (var sector in sectors)
        {
            bool isActive = sector.sectorNumber == number;

            foreach (var unit in sector.units)
            {
                var sr = unit.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = isActive ? Color.yellow : Color.white;
                }
            }
        }

        Debug.Log($"Settore {number} selezionato.");
    }
}