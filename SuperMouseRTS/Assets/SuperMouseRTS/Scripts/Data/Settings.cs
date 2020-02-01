using UnityEngine;

[System.Serializable]
public struct TilePrefabPair
{
    public Assets.SuperMouseRTS.Scripts.GameWorld.TileContent TileContent;
    public GameObject TilePrefab;
}

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Create Settings ScriptableObject", order = 1)]
public class Settings : ScriptableObject
{
    public int TilesHorizontally = 15;
    public int TilesVertically = 10;

    public TilePrefabPair[] TilePrefabs;

    public GameObject UnitPrefab;

    public float PercentileOfTilesResources = 10;
    public float PercentileOfTilesRuins = 10;
    public float PercentileOfTilesObstacles = 5;


    public int Players = 2;
    public int StartingResources = 100;

    public int UnitCost = 50;
    public float UnitSpawnTime = 2f;

    public Health UnitHealth = new Health(50, 50);
    public int UnitAttackStrength = 5;
    public int ResourceDeposits = 1000;
}