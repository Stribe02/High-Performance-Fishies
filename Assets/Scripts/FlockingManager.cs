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
            Vector3 randomPosition = Random.insideUnitSphere * 10;
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

            boid.velocity += cohesion + seperation + alignment;
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
