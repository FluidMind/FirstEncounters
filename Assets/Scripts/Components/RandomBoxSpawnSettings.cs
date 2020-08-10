using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[assembly: RegisterGenericComponentType(typeof(SpawnObjectSystem<RandomBoxSpawnSettings>))]
[GenerateAuthoringComponent]
public struct RandomBoxSpawnSettings : IComponentData, ISpawnSettings
{

    [SerializeField] public  Entity Prefab;
    [SerializeField] public  int Count;
    public Entity prefab() => Prefab;
    public int count() => Count;
    [SerializeField] public float3 position;
    [SerializeField] public float3 dimensions;
    [SerializeField] public Entity lookAt;

    public RigidTransform NextTransform(Random persistentRandomizer, ComponentDataFromEntity<Translation> targets)
    {
        Quaternion _quaternion = Quaternion.identity;

        if (lookAt != Entity.Null)
        {
            _quaternion = Quaternion.LookRotation(position - targets[lookAt].Value, math.up());
        }
        else
        {
            _quaternion = persistentRandomizer.NextQuaternionRotation();
        }
        return new RigidTransform()
        {
            pos = position + persistentRandomizer.NextFloat3(-dimensions, +dimensions),
            rot = _quaternion
        };
    }
}
