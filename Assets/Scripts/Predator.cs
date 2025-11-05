using UnityEngine;

public class Predator : MonoBehaviour
{
    public GameObject manager;
    private FlockingManager flockingManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        flockingManager = manager.GetComponent<FlockingManager>();
    }

    void OnTriggerStay(Collider other)
    {
        flockingManager.cohesionWeight = -2f;
        flockingManager.alignmentWeight = -0.5f;
    }

    void OnTriggerExit(Collider other)
    {
        flockingManager.cohesionWeight = 1f;
        flockingManager.alignmentWeight = 1f;

    }

}
