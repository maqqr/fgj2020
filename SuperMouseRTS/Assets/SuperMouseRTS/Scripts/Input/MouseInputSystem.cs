using Assets.SuperMouseRTS.Scripts.GameWorld;
using Assets.SuperMouseRTS.Scripts.Players;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(RaycastSystem))]
public class MouseInputSystem : SystemBase
{
    private struct PreviousPosition
    {
        public PlayerID Id;
        public int2 Position;
        public int2 Target;
        public AIOperation Operation;

        public PreviousPosition(PlayerID id, int2 position, int2 target, AIOperation operation)
        {
            Id = id;
            Position = position;
            Target = target;
            Operation = operation;
        }
    }

    private readonly int2 DeselectTarget = new Unity.Mathematics.int2(-10000, -10000); 

    private RaycastSystem raycastSystem;
    private Dictionary<int, Entity> previouslySelectedEntity = new Dictionary<int, Entity>();

    public Entity GetPreviouslySelectedEntity(int pointerIndex)
    {
        if (previouslySelectedEntity.ContainsKey(pointerIndex))
        {
            return previouslySelectedEntity[pointerIndex];
        }
        return Entity.Null;
    }

    protected override void OnCreate()
    {
        raycastSystem = World.GetOrCreateSystem<RaycastSystem>();
    }

    protected override void OnUpdate()
    {
        var raycastSystem = this.raycastSystem;
        var previousPositions = new NativeList<PreviousPosition>(Allocator.Temp);
        Entities
            .WithAll<Player, PlayerID>()
            .WithNone<AIPlayer>()
            .ForEach((ref Player player, ref PlayerID playerId) =>
        {
            var pointerIndex = playerId.Value - 1;
            var pointer = MultiMouse.Instance.GetMouseByIndex(pointerIndex);
            if (pointer == null)
            {
                //UnityEngine.Debug.LogWarning($"Mouse pointer {pointerIndex} is null");
                return;
            }

            bool leftClicked = pointer.LeftButtonDown && !player.LeftDownOnLastFrame;
            bool rightClicked = pointer.RightButtonDown && !player.RightDownOnLastFrame;

            if (leftClicked)
            {
                var unityRay = UnityEngine.Camera.main.ScreenPointToRay(pointer.ScreenPosition);
                var ray = new RaycastInput() { Origin = unityRay.origin, Direction = unityRay.direction.normalized * 1000.0f };

                if (raycastSystem.Raycast(ray, out RaycastHit hit))
                {
                    BuildingClicked(playerId, hit.Entity, previousPositions);
                }
                else if (previouslySelectedEntity.ContainsKey(pointerIndex))
                {
                    // Cancel orders
                    var prevPos = EntityManager.GetComponentData<TilePosition>(previouslySelectedEntity[pointerIndex]).Value;
                    previousPositions.Add(new PreviousPosition(playerId, prevPos, DeselectTarget, AIOperation.Unassigned));
                    previouslySelectedEntity.Remove(pointerIndex);
                }
            }

            if (rightClicked)
            {
                var unityRay = UnityEngine.Camera.main.ScreenPointToRay(pointer.ScreenPosition);
                var ray = new RaycastInput() { Origin = unityRay.origin, Direction = unityRay.direction.normalized * 1000.0f };

                if (raycastSystem.Raycast(ray, out RaycastHit hit))
                {
                    UnityEngine.Debug.Log("Right click hit entity " + hit.Entity.Index);

                    BuyUnitsFromBuilding(playerId, hit.Entity);
                }
            }

            player.LeftDownOnLastFrame = pointer.LeftButtonDown;
            player.RightDownOnLastFrame = pointer.RightButtonDown;
        }).WithoutBurst().Run();

        Entities.ForEach((ref PlayerID id, ref OwnerBuilding owner, ref UnitTarget unitTarget) =>
        {
            foreach (var item in previousPositions)
            {
                if(id.Value != item.Id.Value)
                {
                    continue;
                }
                switch (item.Operation)
                {
                    case AIOperation.Unassigned:
                        unitTarget.Priority = Priorities.NotSet;
                        unitTarget.Operation = AIOperation.Unassigned;
                        break;
                    case AIOperation.Attack:
                        if (owner.owner.Value.x == item.Position.x && owner.owner.Value.y == item.Position.y)
                        {
                            unitTarget.Value = new TilePosition() { Value = item.Target };
                            unitTarget.Priority = Priorities.PlayerOrdered;
                            unitTarget.Operation = AIOperation.Attack;
                        }
                        break;
                    case AIOperation.Collect:
                        break;
                    case AIOperation.Repair:
                        if (owner.owner.Value.x == item.Position.x && owner.owner.Value.y == item.Position.y)
                        {
                            unitTarget.Value = new TilePosition() { Value = item.Target };
                            unitTarget.Priority = Priorities.PlayerOrdered;
                            unitTarget.Operation = AIOperation.Repair;
                        }
                        break;
                    default:
                        break;
                }               
            }
        }).WithoutBurst().Run();

        previousPositions.Dispose();
    }

    private void BuyUnitsFromBuilding(PlayerID playerId, Entity selectedBuilding)
    {
        if (!EntityManager.HasComponent<PlayerID>(selectedBuilding))
        {
            return;
        }

        var buildingOwnership = EntityManager.GetComponentData<PlayerID>(selectedBuilding);
        bool isOwned = playerId.Value == buildingOwnership.Value;

        if (isOwned)
        {
            var resources = EntityManager.GetComponentData<OreResources>(selectedBuilding);
            var timer = EntityManager.GetComponentData<SpawnScheduler>(selectedBuilding);

            if (resources.Value >= GameManager.Instance.LoadedSettings.UnitCost)
            {
                resources.Value -= GameManager.Instance.LoadedSettings.UnitCost;
                timer.TimeLeftToSpawn = GameManager.Instance.LoadedSettings.UnitSpawnTime;
                timer.SpawnsOrdered += 1;

                EntityManager.SetComponentData(selectedBuilding, resources);
                EntityManager.SetComponentData(selectedBuilding, timer);
            }
        }
        else
        {
            UnityEngine.Debug.Log("Clicked building is not owned!");
        }
    }

    private void BuildingClicked(PlayerID playerId, Entity selectedBuilding, NativeList<PreviousPosition> cache)
    {
        var pointerIndex = playerId.Value - 1;

        var tile = EntityManager.GetComponentData<Tile>(selectedBuilding);
        var tilePosition = EntityManager.GetComponentData<TilePosition>(selectedBuilding);

        if (tile.tile == TileContent.Ruins)
        {
            if (previouslySelectedEntity.ContainsKey(pointerIndex))
            {
                var selectedPosition = EntityManager.GetComponentData<TilePosition>(selectedBuilding).Value;
                var prevSelectedPosition = EntityManager.GetComponentData<TilePosition>(previouslySelectedEntity[pointerIndex]).Value;
                cache.Add(new PreviousPosition(playerId, prevSelectedPosition, selectedPosition, AIOperation.Repair));
            }
        }
        else if (tile.tile == TileContent.Building)
        {
            var buildingOwnership = EntityManager.GetComponentData<PlayerID>(selectedBuilding);
            bool isOwned = playerId.Value == buildingOwnership.Value;

            if (isOwned)
            {
                previouslySelectedEntity[pointerIndex] = selectedBuilding;
            }
            else
            {
                if (previouslySelectedEntity.ContainsKey(pointerIndex))
                {
                    var selectedPosition = EntityManager.GetComponentData<TilePosition>(selectedBuilding).Value;
                    var prevSelectedPosition = EntityManager.GetComponentData<TilePosition>(previouslySelectedEntity[pointerIndex]).Value;

                    cache.Add(new PreviousPosition(playerId, prevSelectedPosition, selectedPosition, AIOperation.Attack));
                    previouslySelectedEntity.Remove(pointerIndex);
                }
                else
                {
                    // Clicking enemy building first does nothing
                }
            }
        }
    }
}