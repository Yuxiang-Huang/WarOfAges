using UnityEngine;

public class Config
{
    public static bool offlineMode = false;

    public static int ageIncomeOffset = 5;
    public static int ageCostFactor = 2;
    public static int ageUnitFactor = 2;

    public static string defaultMode = "Water";
    public static int defaultStartingTime = 20;
    public static int defaultTimeInc = 10;

    public static Vector2Int mapSize = new Vector2Int(27, 8);

    public static float territoryColorOpacity = 0.3f;
}
