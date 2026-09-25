using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace SkillXPMultiplier.Systems
{
    /// <summary>
    /// Runs right before the game's AddSkillValueSystem and multiplies the pending XP amount on
    /// every AddSkillValueCD entity by that skill's configured multiplier. The game then adds the
    /// scaled amount to the player's skill progress and destroys the entity (via the
    /// BeginSimulation command buffer, i.e. at the start of the next frame).
    ///
    /// A two-frame "already scaled" set guards against touching the same entity twice in the
    /// unlikely case the game's system skips a frame while the entity is still alive.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(AddSkillValueSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class ScaleSkillXPSystem : PugSimulationSystemBase
    {
        private EntityQuery _grants;
        private HashSet<Entity> _scaledLastFrame = new HashSet<Entity>();
        private HashSet<Entity> _scaledThisFrame = new HashSet<Entity>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _grants = GetEntityQuery(ComponentType.ReadWrite<AddSkillValueCD>());
            RequireForUpdate(_grants);
        }

        protected override void OnUpdate()
        {
            var entities = _grants.ToEntityArray(Allocator.Temp);
            var grants = _grants.ToComponentDataArray<AddSkillValueCD>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (_scaledLastFrame.Contains(entity))
                {
                    _scaledThisFrame.Add(entity);
                    continue;
                }
                _scaledThisFrame.Add(entity);

                var grant = grants[i];
                float mult = SkillXPTable.Get((int)grant.skillID);
                if (mult == 1f || grant.amount <= 0f) continue;

                grant.amount *= mult;
                EntityManager.SetComponentData(entity, grant);
            }

            entities.Dispose();
            grants.Dispose();

            var swap = _scaledLastFrame;
            _scaledLastFrame = _scaledThisFrame;
            _scaledThisFrame = swap;
            _scaledThisFrame.Clear();
        }
    }
}
