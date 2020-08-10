using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : SpawnObjectSystem<RandomBoxSpawnSettings>
{
    protected override void OnCreate()
    {
        base.OnCreate();
        //TODO: Implement seeds.
        persistentRandom.InitState();
    }
}

public class Spawner2 : SpawnObjectSystem<SpawnSettings>
{
    protected override void OnCreate()
    {
        base.OnCreate();
        //TODO: Implement seeds.
        persistentRandom.InitState();
    }
}
