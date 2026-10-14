using PlayerState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace MagicMirror
{
    // Casting's item-ID switch is inside a Burst job; Harmony cannot intercept it.
    // Handle our own cast before that job and use its existing replicated "in use"
    // flag to prevent the vanilla switch running. All player data is vanilla NetCode
    // state: no custom player components, inventory writes, or RPCs are needed.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PlayerStateSystemGroup))]
    [UpdateBefore(typeof(UpdatePlayerStateSystem))]
    [UpdateBefore(typeof(ChangePlayerStateSystem))]
    public partial class MirrorCastSystem : PugSimulationSystemBase
    {
        private EntityQuery players, time, rate;
        protected override void OnCreate()
        {
            base.OnCreate();
            players = GetEntityQuery(ComponentType.ReadOnly<Simulate>(), ComponentType.ReadWrite<CastingStateCD>(),
                ComponentType.ReadWrite<PlayerStateCD>(), ComponentType.ReadOnly<EquippedObjectCD>(),
                ComponentType.ReadOnly<HealthCD>(), ComponentType.ReadOnly<PlayerMovementCD>(),
                ComponentType.ReadWrite<TeleportingStateCD>(), ComponentType.ReadOnly<SummarizedConditionEffectsBuffer>(),
                ComponentType.ReadWrite<AnimationBuffer>(), ComponentType.ReadWrite<AnimationBufferPointer>());
            time = GetEntityQuery(ComponentType.ReadOnly<NetworkTime>());
            rate = GetEntityQuery(ComponentType.ReadOnly<ClientServerTickRate>());
            RequireForUpdate(time);
            RequireForUpdate(rate);
        }
        protected override void OnUpdate()
        {
            var mirror = MagicMirrorMod.Id;
            if (mirror == ObjectID.None) return;
            var tick = time.GetSingleton<NetworkTime>().ServerTick;
            uint tickRate = (uint)rate.GetSingleton<ClientServerTickRate>().SimulationTickRate;
            if (!tick.IsValid || tickRate == 0) return;
            using (var entities = players.ToEntityArray(Allocator.Temp))
            foreach (var entity in entities)
            {
                var state = EntityManager.GetComponentData<PlayerStateCD>(entity);
                if (!state.HasAnyState(PlayerStateEnum.Casting)) continue;
                var cast = EntityManager.GetComponentData<CastingStateCD>(entity);
                if (cast.objectData.objectID != mirror) continue;
                // Leave death, stun and already-requested transitions to the game.
                if (state.nextState != PlayerStateEnum.Null && state.nextState != PlayerStateEnum.Casting) continue;
                if (!cast.itemIsInProcessOfBeingUsed) Animate(entity, -1518581387, tick);
                cast.itemIsInProcessOfBeingUsed = true;
                var equipped = EntityManager.GetComponentData<EquippedObjectCD>(entity);
                var health = EntityManager.GetComponentData<HealthCD>(entity);
                int maxHealth = health.GetMaxHealthWithConditions(EntityManager.GetBuffer<SummarizedConditionEffectsBuffer>(entity));
                // Same damage pushback as vanilla Casting.UpdateState.
                if (cast.previousHealth > health.health && cast.previousMaxHealth == maxHealth && maxHealth > 0)
                {
                    uint pushback = (uint)math.round((float)(cast.previousHealth - health.health) / maxHealth * 3f / tickRate);
                    uint elapsed = (uint)cast.castTimer.GetElapsedTicks(tick);
                    cast.castTimer.startTick.Add(math.min(pushback, elapsed));
                }
                cast.previousHealth = health.health;
                cast.previousMaxHealth = maxHealth;
                // Check identity even on completion: a last-tick slot switch must not
                // recall using an item that is no longer held.
                bool changed = !equipped.containedObject.objectData.Equals(cast.objectData)
                    || equipped.equippedSlotIndex != cast.inventoryIndexOnCast;
                bool moving = math.length(EntityManager.GetComponentData<PlayerMovementCD>(entity).targetMovementVelocity) > 0.1f;
                if (!MagicMirrorMod.Active || changed || moving)
                {
                    state.SetNextState(PlayerStateEnum.Walk);
                    Animate(entity, -1065991089, tick);
                }
                else if (cast.castTimer.IsTimerElapsed(tick))
                {
                    var teleport = EntityManager.GetComponentData<TeleportingStateCD>(entity);
                    teleport.targetPosition = PlayerControllerBurstableStatics.PLAYER_SPAWN_POSITION;
                    EntityManager.SetComponentData(entity, teleport);
                    state.SetNextState(PlayerStateEnum.Teleporting, nextStateLocked: true);
                    // Deliberately enqueue no ConsumeEntityAt inventory operation.
                }
                EntityManager.SetComponentData(entity, cast);
                EntityManager.SetComponentData(entity, state);
            }
        }
        private void Animate(Entity entity, int trigger, NetworkTick tick)
        {
            var pointer = EntityManager.GetComponentData<AnimationBufferPointer>(entity);
            PlayerController.PlayAnimationTrigger(trigger, tick, EntityManager.GetBuffer<AnimationBuffer>(entity), ref pointer);
            EntityManager.SetComponentData(entity, pointer);
        }
    }
}
