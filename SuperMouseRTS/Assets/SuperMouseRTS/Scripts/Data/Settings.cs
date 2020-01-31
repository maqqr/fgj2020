using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Create Settings ScriptableObject", order = 1)]
public class Settings : ScriptableObject
{
    public int TilesHorizontally = 15;
    public int TilesVertically = 10;

    public float PercentileOfTilesResources = 10;
    public float PercentileOfTilesRuins = 10;
    public float PercentileOfTilesObstacles = 5;

    public float TileSize = 1.5f;
}