using UnityEngine;

public class Config
{
    //test modes
    public static bool offlineMode = true;
    public static bool sameSpawnPlaceTestMode = true;
    public static bool moreMoneyTestMode = true;
    public static bool debugTestMode = false;

    //time factors
    public static float troopMovementTime = 0.5f;

    //age factors
    public static int numAges = 6;
    public static int ageIncomeOffset = 5;
    public static int ageCostFactor = 2;
    public static int ageUnitFactor = 2;

    //gold
    public static int basicGoldUnit = 10;
    public static int goldFactor = 5; //each land give this many gold each turn
    public static float goldPercent = 0.375f; //1 / (4 * goldPercent) gives percent of land player can conquer before gold goes down

    //default map setting
    public static string defaultMode = "Water";
    public static int defaultStartingTime = 20;
    public static int defaultTimeInc = 10;

    public static Vector2Int mapSize = new (27, 8);

    //colors
    public static float territoryColorOpacity = 0.3f;

    public static Color spawnImageColor = new Color(1, 1, 1, 0.5f);

    //for the end turn icons
    public static Color readyColor = new (33f / 255f, 128f / 255f, 68f / 255f);
    public static Color notReadyColor = new(113f / 255f, 115f / 255f, 125f / 255f);
}
