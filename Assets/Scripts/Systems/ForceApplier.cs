using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Physics;
using UnityEngine;
using Unity.Burst;

[AlwaysUpdateSystem]
//Empties AppliedForces into PhysicsVelocity
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class ForceApplier : JobComponentSystem
{
    EntityCommandBufferSystem ECBS;
    JobHandle SystemEnablerJobHandle;
    ComponentType[] velocityType = new ComponentType[1];

    EntityQuery velocityDescription;

    JobHandle ApplierJobHandle;


    protected override void OnCreate()
    {
        ECBS = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        velocityType[0] = typeof(PhysicsVelocity);
        velocityDescription = GetEntityQuery(velocityType);
    }

    private struct SystemEnabler : IJobChunk
    {
        [ReadOnly] public ArchetypeChunkEntityType entityType;
        [ReadOnly] public BufferFromEntity<AppliedForces> currentList;
        public EntityCommandBuffer.Concurrent ECB;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<Entity> e = chunk.GetNativeArray(entityType);
            for (int i = 0; i < chunk.Count; i++)
            {
                if (!currentList.Exists(e[i])) ECB.AddBuffer<AppliedForces>(chunkIndex,e[i]);
            }
        }
    }
    SystemEnabler SystemEnablerJob;

    private struct Applier : IJobChunk
    {
        [ReadOnly] public ArchetypeChunkEntityType entityType;
        public ArchetypeChunkComponentType<PhysicsVelocity> velocityType;
        public ArchetypeChunkBufferType<AppliedForces> forceType;

        [BurstCompile]
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<Entity> e = chunk.GetNativeArray(entityType);
            NativeArray<PhysicsVelocity> velocityData = chunk.GetNativeArray<PhysicsVelocity>(velocityType);
            BufferAccessor<AppliedForces> forceData = chunk.GetBufferAccessor<AppliedForces>(forceType);
            for (int i = 0; i < chunk.Count; i++)
            {
                if (!(forceData.Length == 0))
                {
                    for (int f = forceData[i].Length - 1; f >= 0; f--)
                    {
                        //Debug.Log(f);
                        velocityData[i] = new PhysicsVelocity()
                        {
                            Linear = velocityData[i].Linear + (forceData[i][f]),
                            Angular = velocityData[i].Angular
                        };
                        //Debug.Log(velocityData[i].Linear.ToString());
                        forceData[i].RemoveAt(f);
                    }
                }
            }
        }
    }
    Applier ApplierJob;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        BufferFromEntity<AppliedForces> currentList = GetBufferFromEntity<AppliedForces>(false);
        EntityCommandBuffer _ecb = ECBS.CreateCommandBuffer();

        SystemEnablerJob = new SystemEnabler()
        {
            entityType = GetArchetypeChunkEntityType(),
            currentList = GetBufferFromEntity<AppliedForces>(true),
            ECB = _ecb.ToConcurrent()
        };
        SystemEnablerJobHandle = SystemEnablerJob.ScheduleParallel<SystemEnabler>(velocityDescription, inputDeps);
        ECBS.AddJobHandleForProducer(SystemEnablerJobHandle);
        inputDeps = JobHandle.CombineDependencies(inputDeps, SystemEnablerJobHandle);
        //_ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
        ApplierJob = new Applier()
        {
            entityType = GetArchetypeChunkEntityType(),
            velocityType = GetArchetypeChunkComponentType<PhysicsVelocity>(),
            forceType = GetArchetypeChunkBufferType<AppliedForces>()
        };

        ApplierJobHandle = ApplierJob.ScheduleParallel<Applier>(velocityDescription, inputDeps);
        //ApplierJobHandle = Entities.ForEach((DynamicBuffer<AppliedForces> forces, PhysicsVelocity velocity) =>
        //{
        //    for (int i = forces.Length; i >= 0; i++)
        //    {
        //        velocity = new PhysicsVelocity()
        //        {
        //            Angular = velocity.Angular,
        //            Linear = velocity.Linear + forces[i].Vector
        //        };
        //        forces.RemoveAt(i);
        //    }
        //}).Schedule(inputDeps);
        inputDeps = JobHandle.CombineDependencies(inputDeps, ApplierJobHandle);
        return inputDeps;
    }

}
