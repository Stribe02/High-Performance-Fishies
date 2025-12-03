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
        // wait so the ECB has loaded: "hacky"
        yield return new WaitForSeconds(0.4f);
        configEntity = _entityManager.CreateEntityQuery(typeof(Config)).GetSingletonEntity(); // get config ref
        
        
    }


    public void onRockClick(bool state)
    {
        state = !state;
        Debug.Log("I am being clicked!");
        var config = _entityManager.GetComponentData<Config>(configEntity);
        _entityManager.SetComponentData<RockSpawning>(configEntity, new RockSpawning {ShouldSpawnRock = state});
    }

}
