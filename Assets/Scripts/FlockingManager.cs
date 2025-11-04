using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class FlockingManager : MonoBehaviour
{
    public GameObject boidPrefab; //fish
    public int flockSize = 50;
    public Boid[] allBoids;
    public float cohesionWeight = 1f;
    public float separationWeight = 1f;
    public float alignmentWeight = 1f;
    public float separationRadius = 2f;

    private void Start()
    {
        allBoids = new Boid[flockSize];
        for (int i = 0; i < flockSize; i++)
        {
            //Vector3 randomPosition = Random.insideUnitSphere * 10;
            //Viewport space is normalized and relative to the camera. The bottom-left of the viewport is (0,0); the top-right is (1,1). The z position is in world units from the camera.
            // so spawning location needs to a bit outside of this. at least one of the coords needs to be higher than 1 or below 0.
            float randomSide = Random.value;
            float sideCoord = 0f;
            if (randomSide > 0.5)
            {
                sideCoord = 1.3f;
            }
            else sideCoord = -0.3f;

            Vector3 randomPosition = Camera.main.ViewportToWorldPoint(new Vector3(sideCoord, 1, Camera.main.nearClipPlane));
            GameObject newBoid = Instantiate(boidPrefab, randomPosition, Quaternion.identity);
            allBoids[i] = newBoid.GetComponent<Boid>();
            allBoids[i].velocity = Random.insideUnitSphere.normalized * allBoids[i].maxSpeed;
        }
    }

    private void Update()
    {
        foreach (Boid boid in allBoids)
        {
            // Find them neighbors
            Boid[] neighbors = FindNeighbors(boid, 5f);

            Vector3 cohesion = boid.Cohesion(neighbors) * cohesionWeight;
            Vector3 seperation = boid.Separation(neighbors, separationRadius) * separationWeight;
            Vector3 alignment = boid.Alignment(neighbors) * alignmentWeight;
            Vector3 keepInBounds = boid.BoundBoidsToScreen(boid);

            boid.velocity += cohesion + seperation + alignment+ keepInBounds;
            boid.velocity = Vector3.ClampMagnitude(boid.velocity, boid.maxSpeed);
            boid.transform.position += boid.velocity * Time.deltaTime;
            boid.transform.rotation = Quaternion.LookRotation(boid.velocity);
        }
    }

    Boid[] FindNeighbors(Boid boid, float radius)
    {
        List<Boid> neighbors = new List<Boid>();
        foreach (Boid otherBoid in allBoids)
        {
            if (otherBoid != boid && Vector3.Distance(boid.transform.position, otherBoid.transform.position) < radius)
            {
                neighbors.Add(otherBoid);
            }
        }

        return neighbors.ToArray();
    }
}
