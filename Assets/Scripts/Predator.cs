using UnityEngine;

public class Predator : MonoBehaviour
{
    public GameObject manager;
    private FlockingManager flockingManager;

    public float moveSpeed = 15f;
    public Vector3 targetPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        flockingManager = manager.GetComponent<FlockingManager>();
    }

    void Update()
    {
        MovePredator();
    }

    private void MovePredator()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPosition) < 5f)
        {
            targetPosition = new Vector3(Random.Range(-100, 100), Random.Range(-50, 50), Random.Range(-100, 100));
        }
    }

    void OnTriggerStay(Collider other)
    {
        Boid boid = other.GetComponent<Boid>();
        if (boid != null) {
            flockingManager.SetSchoolWeights(boid.schoolIndex, -3f, -1f, flockingManager.defaultAlignmentWeight);
        }
        //flockingManager.cohesionWeight = -2f;
        //flockingManager.alignmentWeight = -0.5f;
    }

    void OnTriggerExit(Collider other)
    {
        Boid boid = other.GetComponent<Boid>();
        if (boid != null)
        {
            flockingManager.ResetSchoolWeights(boid.schoolIndex);
        }
        //flockingManager.cohesionWeight = 1f;
        //flockingManager.alignmentWeight = 1f;

    }

}
