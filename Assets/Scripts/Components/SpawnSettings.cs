using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(SpawnObjectSystem<SpawnSettings>))]
[GenerateAuthoringComponent]
public struct SpawnSettings : ISpawnSettings, IComponentData
{
    [SerializeField] public Entity Prefab;
    [SerializeField] public int Count;
    public Entity prefab() => Prefab;
    public int count() => Count;
    public RigidTransform NextTransform(Unity.Mathematics.Random persistentRandomizer, ComponentDataFromEntity<Translation> targets)
    {
        return new RigidTransform() { pos = float3.zero, rot = quaternion.identity };
    }
}
