using System.Collections.Generic;
using UnityEngine;

public class FlockingManager : MonoBehaviour
{
    public GameObject boidPrefab; //fish
    public int flockSize = 50;
    public int schools = 5;
    public Boid[] allBoids;
    public List<List<Boid>> allSchools = new List<List<Boid>>();
    public float defaultCohesionWeight = 1f;
    public float defaultSeparationWeight = 1f;
    public float defaultAlignmentWeight = 1f;
    public float defaultSeparationRadius = 2f;

    public List<float> cohesionWeights = new List<float>();
    public List<float> separationWeights = new List<float>();
    public List<float> alignmentWeights = new List<float>();

    private void Start()
    {
        //allBoids = new Boid[flockSize];
        for (int s = 0; s < schools; s++)
        {
            List<Boid> school = new List<Boid>();

            //per school weights
            cohesionWeights.Add(defaultCohesionWeight);
            separationWeights.Add(defaultSeparationWeight);
            alignmentWeights.Add(defaultAlignmentWeight);

            //gib random color to schools
            Color schoolColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

            for (int i = 0; i < flockSize; i++)
            {
                Vector3 randomPosition = Random.insideUnitSphere * 10;
                GameObject newBoidGO = Instantiate(boidPrefab, randomPosition, Quaternion.identity);
                Boid newBoid = newBoidGO.GetComponent<Boid>();
                newBoid.velocity = Random.insideUnitSphere.normalized * newBoid.maxSpeed;
                newBoid.schoolIndex = s;

                Renderer[] rends = newBoidGO.GetComponentsInChildren<Renderer>();
                foreach (Renderer rend in rends)
                {
                    rend.material = new Material(rend.material);
                    rend.material.color = schoolColor;
                }

                school.Add(newBoid);
            }

            allSchools.Add(school);
        }
    }

    private void Update()
    {
        for (int s = 0; s < allSchools.Count; s++)
        {
            List<Boid> school = allSchools[s];
            float cWeight = cohesionWeights[s];
            float sWeight = separationWeights[s];
            float aWeight = alignmentWeights[s];

            foreach(Boid boid in school)
            {
                Vector3 cohesion = boid.Cohesion(school) * cWeight;
                Vector3 seperation = boid.Separation(school, defaultSeparationRadius) * sWeight;
                Vector3 alignment = boid.Alignment(school) * aWeight;

                boid.velocity += cohesion + seperation + alignment;
                boid.velocity = Vector3.ClampMagnitude(boid.velocity, boid.maxSpeed);
                boid.transform.position += boid.velocity * Time.deltaTime;
                boid.transform.rotation = Quaternion.LookRotation(boid.velocity);
            }
        }
    }

    //utility for weight changes
    public void ResetSchoolWeights(int schoolIndex)
    {
        if (schoolIndex >= 0 && schoolIndex < allSchools.Count)
        {
            cohesionWeights[schoolIndex] = defaultCohesionWeight;
            separationWeights[schoolIndex] = defaultSeparationWeight;
            alignmentWeights[schoolIndex] = defaultAlignmentWeight;

        }
    }

    public void SetSchoolWeights(int schoolIndex, float c, float s, float a)
    {
        if (schoolIndex >= 0 && schoolIndex < allSchools.Count)
        {
            cohesionWeights[schoolIndex] = c;
            separationWeights[schoolIndex] = s;
            alignmentWeights[schoolIndex] = a;
        }
    }


    // OLD
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
