
[System.Flags]
public enum ActionType
{
    None = 0,
    Charge = 1 << 0,
    Throw = 1 << 1,
    Barricade = 1 << 2,
    Chant  = 1 << 3,
    SitStand = 1 << 4
}