using Unity.Entities;
using Unity.Mathematics;

public struct GravityEmitter : IComponentData
{
    public float3 position;
    public float gravityForce;
    public float maxDistance;
}
