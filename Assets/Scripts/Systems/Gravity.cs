using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Entities.UniversalDelegates;
using UnityEngine.UIElements;
using System.Text;

[AlwaysUpdateSystem]
[UpdateAfter(typeof(ForceApplier))]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class Gravity : JobComponentSystem
{
    public static float GravitationalConstant = 6.674e-11f;
    public static float MinimumMassForGravity = 1e+17f;
    public static float MinimumGravityForce = 0.05f;
    public static NativeString32 systemLabel = "Gravity";
    public EntityCommandBufferSystem ECBS;
    EntityQuery emitters;
    EntityQuery subjects;
    protected override void OnCreate()
    {
        ComponentType[] emitterDescription = new ComponentType[1];
        emitterDescription[0] = typeof(PhysicsMass);
        emitters = GetEntityQuery(emitterDescription);
        ComponentType[] subjectDescription = new ComponentType[2];
        subjectDescription[0] = typeof(PhysicsVelocity);
        subjectDescription[1] = typeof(AppliedForces);
        subjects = GetEntityQuery(subjectDescription);
        ECBS = World.GetExistingSystem<EntityCommandBufferSystem>();
    }
    ///////////the native queue seems to work well, must remember the .dispose
    GravityJob gravityJob;
    JobHandle gravityJobHandle;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //collect massive objects - add gravity emitter tag
        //ecb
        NativeString32 _systemLabel = systemLabel;
        EntityCommandBuffer _ecb = ECBS.CreateCommandBuffer();
        //spacial query for proximity to massive objects
        //add force towards gravity emitter

        // get subjects
        NativeArray<ArchetypeChunk> subjectData = subjects.CreateArchetypeChunkArray(Allocator.TempJob);
        gravityJob = new GravityJob()
        {
            SystemLabel = _systemLabel,
            entityType = GetArchetypeChunkEntityType(),
            massType = GetArchetypeChunkComponentType<PhysicsMass>(),
            positionType = GetArchetypeChunkComponentType<Translation>(),
            forceType = GetArchetypeChunkBufferType<AppliedForces>(),
            potentialSubjects = subjectData,
            GravitationalConstant = GravitationalConstant,
            MinimumMassForGravity = MinimumMassForGravity,
            MinimumGravityForce = MinimumGravityForce,
            dt = Time.DeltaTime
        };
        gravityJobHandle = gravityJob.Schedule<GravityJob>(emitters, inputDeps);
        inputDeps = JobHandle.CombineDependencies(inputDeps, gravityJobHandle);

        subjectData.Dispose(gravityJobHandle);
        

        return inputDeps;
    }

    protected override void OnDestroy()
    {
        
    }

    private struct GravityJob : IJobChunk
    {
        [ReadOnly] public NativeString32 SystemLabel;
        [ReadOnly] public ArchetypeChunkEntityType entityType;
        [ReadOnly] public ArchetypeChunkComponentType<PhysicsMass> massType;
        [ReadOnly] public ArchetypeChunkComponentType<Translation> positionType;
        public ArchetypeChunkBufferType<AppliedForces> forceType;
        [ReadOnly] public NativeArray<ArchetypeChunk> potentialSubjects;
        [ReadOnly] public float GravitationalConstant;
        [ReadOnly] public float MinimumMassForGravity;
        [ReadOnly] public float MinimumGravityForce;
        [ReadOnly] public float dt;

        [BurstCompile]
        public void Execute(ArchetypeChunk massCarriers, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<PhysicsMass> massData = massCarriers.GetNativeArray<PhysicsMass>(massType);
            NativeArray<Translation> positionData = massCarriers.GetNativeArray<Translation>(positionType);

            //StringBuilder debugTimes = new StringBuilder("======== Begin Gravity debug frame =======" + System.Environment.NewLine);
            //chunk is only for mass carriers
            //debugTimes.Append("  massCarriers.Count: " + massCarriers.Count.ToString() + System.Environment.NewLine);
            //Debug.Log(debugTimes);
            for (int i = 0; i < massCarriers.Count; i++)
            {
                //debugTimes.Append("Inside massCarriers loop, i: " + i.ToString() + System.Environment.NewLine);
                //debugTimes.Append("  massData[i].InverseMass: " + massData[i].InverseMass.ToString() + System.Environment.NewLine);
                //debugTimes.Append("  math.rcp(massData[i].InverseMass): " + math.rcp(massData[i].InverseMass).ToString() + System.Environment.NewLine);
                //debugTimes.Append("  MinimumMassForGravity: " + MinimumMassForGravity.ToString() + System.Environment.NewLine);
                //debugTimes.Append("  math.rcp(massData[i].InverseMass) < MinimumMassForGravity: " + (math.rcp(massData[i].InverseMass) < MinimumMassForGravity).ToString() + System.Environment.NewLine);
                //Debug.Log(debugTimes);
                if (math.rcp(massData[i].InverseMass) < MinimumMassForGravity)
                {
                    //debugTimes.Append("--math.rcp(massData[i].InverseMass) < MinimumMassForGravity Tested true: " + (math.rcp(massData[i].InverseMass) < MinimumMassForGravity).ToString() + System.Environment.NewLine);
                    //Debug.Log(debugTimes);
                    continue;
                }
                else
                {
                    //debugTimes.Append("--math.rcp(massData[i].InverseMass) < MinimumMassForGravity Tested false: " + (math.rcp(massData[i].InverseMass) < MinimumMassForGravity).ToString() + System.Environment.NewLine);
                    //Debug.Log(debugTimes);
                    //debugTimes.Append("potentialSubjects.Length: " + potentialSubjects.Length.ToString() + System.Environment.NewLine);

                    float minDistance = (math.sqrt((GravitationalConstant * math.rcp(massData[i].InverseMass)) / MinimumGravityForce));
                    for (int s = 0; s < potentialSubjects.Length; s++)
                    {
                        //debugTimes.Append("Inside potentialSubjects iterator, s:" + s.ToString() + System.Environment.NewLine);
                        //NativeArray<Entity> subject = potentialSubjects[s].GetNativeArray(entityType);
                        NativeArray<Translation> subjectPosition = potentialSubjects[s].GetNativeArray<Translation>(positionType);
                        BufferAccessor<AppliedForces> subjectForces = potentialSubjects[s].GetBufferAccessor<AppliedForces>(forceType);
                        //debugTimes.Append("  potentialSubjects[s].Count: " + potentialSubjects[s].Count.ToString() + System.Environment.NewLine);
                        //Debug.Log(debugTimes);

                        for (int t = 0; t < potentialSubjects[s].Count; t++)
                        {
                            //debugTimes.Append("Inside potentialSubjects[s] iterator, t:" + t.ToString() + System.Environment.NewLine);

                            //minDistanceCalculaton;
                            //minGravity = GM/d^2 = 0.05
                            //GM = 0.05d^2
                            //GM/0.05 = d^2
                            //sqrt(GM/0.05) = d

                            float distanceBetween = math.length(positionData[i].Value - subjectPosition[t].Value);
                            //debugTimes.Append("  DistanceBetween: " + distanceBetween.ToString() + System.Environment.NewLine);
                            //debugTimes.Append("  minDistance: " + minDistance.ToString() + System.Environment.NewLine);
                            //debugTimes.Append("  DistanceBetween < minDistance: " + (distanceBetween < minDistance).ToString() + System.Environment.NewLine);
                            //debugTimes.Append("  !(DistanceBetween < minDistance): " + (!(distanceBetween < minDistance)).ToString() + System.Environment.NewLine);
                            //debugTimes.Append("  distanceBetween == 0: " + (distanceBetween == 0).ToString() + System.Environment.NewLine);
                            //debugTimes.Append("--Final test (!(distanceBetween < minDistance) || distanceBetween == 0): " + (!(distanceBetween < minDistance) || distanceBetween == 0).ToString() + System.Environment.NewLine);
                            //Debug.Log(debugTimes);

                            if (!(distanceBetween < minDistance) || distanceBetween == 0)
                            {
                                //debugTimes.Append("Final test (!(distanceBetween < minDistance) || distanceBetween == 0) returned true, returning.");
                                //Debug.Log(debugTimes);
                                continue;
                            }
                            else
                            {
                                //debugTimes.Append("--Final test (!(distanceBetween < minDistance) || distanceBetween == 0) returned false, processing GravityForce.");
                                //debugTimes.Append("  GravitationalConsant: " + Gravity.GravitationalConstant.ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  Target's InverseMass: " + massData[i].InverseMass.ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  Reciprocal of Target's mass: " + math.rcp(massData[i].InverseMass).ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  DistanceBetween: " + distanceBetween.ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  SquareDistanceBetween: " + math.pow(distanceBetween, 2).ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  GravitationalConstant times Reciprocal of Targer's InverseMass: " + (Gravity.GravitationalConstant * math.rcp(massData[i].InverseMass)).ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  (G * M) / (D ^ 2) GravityForce: " + ((Gravity.GravitationalConstant * math.rcp(massData[i].InverseMass)) / math.pow(distanceBetween, 2)).ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  EmitterPosition: " + (positionData[i].Value.ToString() + System.Environment.NewLine));
                                //debugTimes.Append("  SubjectPosition: " + (subjectPosition[t].Value.ToString() + System.Environment.NewLine));
                                //debugTimes.Append("  VectorBetween: " + ((positionData[i].Value - subjectPosition[t].Value).ToString() + System.Environment.NewLine));
                                //debugTimes.Append("  UnitVector: " + (((positionData[i].Value - subjectPosition[t].Value) / distanceBetween).ToString() + System.Environment.NewLine));
                                //debugTimes.Append("  DeltaTime: " + (dt.ToString() + System.Environment.NewLine));
                                //debugTimes.Append("  GravityForce times UnitVector: " + (((Gravity.GravitationalConstant * math.rcp(massData[i].InverseMass)) / math.pow(distanceBetween, 2)) * (positionData[i].Value - subjectPosition[t].Value)).ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  GravityForce times UnitVector divided by SqareDistance: " + (((Gravity.GravitationalConstant * math.rcp(massData[i].InverseMass)) / math.pow(distanceBetween, 2)) * (positionData[i].Value - subjectPosition[t].Value) / math.pow(distanceBetween,2)).ToString() + System.Environment.NewLine);
                                //debugTimes.Append("  GravityForce times UnitVector divided by SqareDistance times DeltaTime: " + (((Gravity.GravitationalConstant * math.rcp(massData[i].InverseMass)) / math.pow(distanceBetween, 2)) * ((positionData[i].Value - subjectPosition[t].Value) / math.pow(distanceBetween, 2)) * dt).ToString() + System.Environment.NewLine);
                                //Debug.Log(debugTimes);
                                subjectForces[t].Add(new AppliedForces()
                                {
                                    SourceTitle = systemLabel,
                                    Vector = (((Gravity.GravitationalConstant * math.rcp(massData[i].InverseMass)) / math.pow(distanceBetween, 2)) * ((positionData[i].Value - subjectPosition[t].Value) / math.pow(distanceBetween, 2)) * dt)
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    protected bool collapseTruth(bool3 collapse)
    {
        return collapse.x && collapse.y && collapse.z;
    }

}
