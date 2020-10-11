using System;
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
    public Material[] UnitMaterials;
    public Material[] BuildingMaterials;

    public float PercentileOfTilesResources = 10;
    public float PercentileOfTilesRuins = 10;
    public float PercentileOfTilesObstacles = 5;


    public int Players = 2;
    public int HumanPlayers = 0;
    public int StartingResources = 100;

    public int UnitCost = 50;
    public float UnitSpawnTime = 2f;

    public Health UnitHealth = new Health(50, 50);
    public int UnitAttackStrength = 5;
    public int ResourceDeposits = 1000;

    public int UnitCapacity = 25;
    public int OreHaulSpeedFromDeposit = 1;
    public float UnitRangeOfOperation = 0.3f;

    public int PassiveOreGenerationAmount = 5;
    public float PassiveOreGenerationInterval = 4;

    public Color[] PlayerColors;

    [Header(" -- Common health bar settings --")]
    public bool ShowHpAlways = true;
    public Mesh HealthBarMesh;
    public Material[] HealthBarMaterials;
    public Vector3 HealthBarRotation = Vector3.zero;

    [Header(" -- Unit health bar settings --")]
    public float UnitHealthBarY = 1.0f;
    public float UnitHealthBarWidth = 0.5f;

    [Header(" -- Building health bar settings --")]
    public float BuildingHealthBarY = 1.0f;
    public float BuildingHealthBarWidth = 1.5f;
    public float BuildingHealthBarHeight = 0.3f;

    [Header(" -- Selection GUI stuff --")]
    public Material circleMaterial;
    public float CircleMaxRadius = 2.2f;
    public float CircleGrowSpeed = 1.0f;
    public Material LineMaterial;
    public float ArrowHeadLength = 0.5f;

    [Header(" -- Bullets --")]
    public Material BulletMaterial;

    [Header(" -- Effect prefabs --")]
    public GameObject UnitDeathEffectPrefab;
    public float ExplosionSize = 0.05f;

    [Header(" -- Camera control --")]
    public float CameraMoveSpeed = 1.0f;
    public float CameraMaxMoveSpeed = 5.0f;
    public float CameraDamping = 0.8f;

    public float CameraZoomSpeed = 1.0f;
}