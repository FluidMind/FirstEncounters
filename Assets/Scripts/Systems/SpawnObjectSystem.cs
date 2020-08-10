using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Random = Unity.Mathematics.Random;

//[UpdateInGroup(typeof(LateSimulationSystemGroup))]
//[UpdateAfter(typeof(ForceApplier))]
//[AlwaysUpdateSystem]
public abstract class SpawnObjectSystem<T> : JobComponentSystem where T: struct, ISpawnSettings
{
    public delegate void OnBeforeSpawn<ISpawnSettings>();
    public delegate void OnAfterSpawn<ISpawnSettings>();

    private JobHandle spawnJobHandle;
    private SpawnJob<T> spawnJob;
    private ComponentDataFromEntity<Translation> TempTargets;

    protected Random persistentRandom;

    protected EntityCommandBufferSystem ECBS;
    protected override void OnCreate()
    {
        ECBS = World.GetExistingSystem<EntityCommandBufferSystem>();
        persistentRandom = new Random();
        persistentRandom.InitState();
        TempTargets = GetComponentDataFromEntity<Translation>(true);
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        TempTargets = GetComponentDataFromEntity<Translation>(true);
        EntityCommandBuffer _ecb = ECBS.CreateCommandBuffer();
        Random _random = persistentRandom;
        //the interface requires a .run and withoutburst?

        //var spawnTypes = Assembly.GetAssembly(typeof(ISpawnSettings)).GetTypes().Where<Type>(myType =>
        //    myType.IsValueType &&
        //    !myType.IsAbstract &&
        //    myType.GetInterfaces().Contains<Type>(typeof(ISpawnSettings)) &&
        //    myType.GetInterfaces().Contains<Type>(typeof(IComponentData)) //&&
        //    //myType.IsSubclassOf(typeof(ComponentType))
        //    );
        //List<Type> spawnTypeList = spawnTypes.ToList<Type>();
        //List<ComponentType> spawnComponentTypeList = new List<ComponentType>(spawnTypeList.Count);
        //
        //EntityCommandBuffer _ecb2 = ECBS.CreateCommandBuffer();
        //List<EntityQuery> spawnerQueries = new List<EntityQuery>(spawnTypeList.Count);
        ////populate description
        //Debug.Log("spawnTypes:" + spawnTypeList.Count.ToString());
        //for (int i = 0; i < spawnTypeList.Count; i++)
        //{
        //    spawnerQueries.Add(GetEntityQuery(typeof(T)));
        //    Type e = spawnTypeList[i];
        //    int j = System.Runtime.InteropServices.Marshal.SizeOf(e);
            
        spawnJob = new SpawnJob<T>()
        {
            entityType = GetArchetypeChunkEntityType(),
            spawnSettingsType = GetArchetypeChunkComponentType<T>(),
            persistentRandom = persistentRandom,
            targetData = GetComponentDataFromEntity<Translation>(),
            _ecb = _ecb.ToConcurrent()
        };
        spawnJobHandle = spawnJob.Schedule<SpawnJob<T>>(GetEntityQuery(typeof(T)), inputDeps);
        ECBS.AddJobHandleForProducer(spawnJobHandle);

        //}


        ECBS.AddJobHandleForProducer(spawnJobHandle);
        inputDeps = JobHandle.CombineDependencies(inputDeps, spawnJobHandle);
        return inputDeps;
    }
#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
    protected struct SpawnJob<T> : IJobChunk where T : struct, ISpawnSettings
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
    {
        [ReadOnly] public ArchetypeChunkComponentType<T> spawnSettingsType;
        [ReadOnly] public ArchetypeChunkEntityType entityType;
        [ReadOnly] public Random persistentRandom;
        [ReadOnly] public ComponentDataFromEntity<Translation> targetData;
        public EntityCommandBuffer.Concurrent _ecb;
        //needs to 
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<Entity> entityData = chunk.GetNativeArray(entityType);
            NativeArray<T> spawnerData = chunk.GetNativeArray<T>(spawnSettingsType);
            for (int i = 0; i < chunk.Count; i++)
            {
                T thisSpawnerSettings = spawnerData[i];
            
                RigidTransform _transformCache = new RigidTransform();
                Entity e = Entity.Null;
                for (int spawnCount = 0; spawnCount < thisSpawnerSettings.count(); spawnCount++)
                {
                    _transformCache = thisSpawnerSettings.NextTransform(persistentRandom, targetData);
                    e =_ecb.Instantiate(chunkIndex, thisSpawnerSettings.prefab());
                    _ecb.SetComponent<Translation>(chunkIndex, e, new Translation() { Value = _transformCache.pos });
                    _ecb.SetComponent<Rotation>(chunkIndex, e, new Rotation() { Value = _transformCache.rot });
                }
                _ecb.RemoveComponent(chunkIndex, entityData[i], typeof(T));
            }
        }
    }

    public virtual EntityCommandBuffer spawn(Entity OnEntity, SpawnSettings spawnSettings)
    {
        EntityCommandBuffer _ecb = ECBS.CreateCommandBuffer();
        _ecb.AddComponent<SpawnSettings>(OnEntity);
        return _ecb;
    }
}
