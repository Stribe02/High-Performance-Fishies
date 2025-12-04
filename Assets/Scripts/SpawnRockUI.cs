using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;

public class SpawnRockUI : MonoBehaviour
{
    private EntityManager _entityManager;
    private Entity configEntity;

    private IEnumerator Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        // wait in case the ECB stuff hasn't loaded:
        yield return new WaitForSeconds(0.4f);
        // get config ref
        configEntity = _entityManager.CreateEntityQuery(typeof(Config)).GetSingletonEntity(); 
    }


    public void onRockClick()
    {
        var config = _entityManager.GetComponentData<Config>(configEntity);
        _entityManager.SetComponentData<RockSpawning>(configEntity, new RockSpawning {ShouldSpawnRock = true});
    }

}
