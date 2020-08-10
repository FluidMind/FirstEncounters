using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;
public interface ISpawnSettings : IComponentData
{
    Entity prefab();
    int count();
    RigidTransform NextTransform(Random persistentRandomizer, ComponentDataFromEntity<Translation> targets); //{ get; }


}
