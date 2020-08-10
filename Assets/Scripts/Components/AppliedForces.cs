using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using JetBrains.Annotations;

public struct AppliedForces : IBufferElementData
{
    public NativeString32 SourceTitle;
    public float3 Vector;

    public static implicit operator float3(AppliedForces e) { return e.Vector; }
    public static implicit operator NativeString32(AppliedForces e) { return e.SourceTitle; }
    public static implicit operator AppliedForces(float3 e) { return new AppliedForces() { SourceTitle = "Unknown", Vector = e }; }
    public static implicit operator AppliedForces(NativeString32 e) { return new AppliedForces() { SourceTitle = e, Vector = float3.zero }; }
    public static float3 operator*(AppliedForces af, float3 f)
    {
        return af.Vector * f;
    }

    public override string ToString()
    {
        return SourceTitle.ToString() + ": " + Vector.ToString();
    }
}
