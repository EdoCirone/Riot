using UnityEngine;

public class CorteoUnit : BaseUnit
{
    public int sectorID; // settore a cui appartiene (1-8)

    // Potrebbero esserci anche propriet� come:
    // public int potenza, velocit�, ruolo...

    // Eredita tutto da BaseUnit per ora, ma possiamo estenderlo con:
    public void ShoutSlogan()
    {
        Debug.Log($"[Settore {sectorID}] Slogan attivato!");
    }
}