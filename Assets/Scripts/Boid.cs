using System;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    public Vector3 velocity;
    public float maxSpeed = 5f;
    public int schoolIndex;
    
    // more vars to come

    // Rule 1: Stick together
    // we normalise to ensure each behavior contributes equally to the final movement of the flock
    public Vector3 Cohesion(List<Boid> school)
    {
        Vector3 centerOfMass = Vector3.zero;
        int count = 0;

        foreach (Boid neighbor in school)
        {
            if (neighbor != this)
            {
                centerOfMass += neighbor.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            centerOfMass /= count;
            return (centerOfMass - transform.position).normalized;
        }

        return Vector3.zero;
    }

    // Rule 2 
    public Vector3 Separation(List<Boid> school, float separationRadius)
    {
        Vector3 moveAway = Vector3.zero;
        int count = 0;

        foreach (Boid neighbor in school)
        {
            if (neighbor != this &&
                Vector3.Distance(transform.position, neighbor.transform.position) < separationRadius)
            {
                Vector3 difference = transform.position - neighbor.transform.position;
                moveAway += difference.normalized / difference.magnitude; // something with scaling 
                count++;
            }
        }

        if (count > 0)
        {
            moveAway /= count;
        }

        return moveAway.normalized;
    }
    
    // Rule 3 
    public Vector3 Alignment(List<Boid> school)
    {
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;

        foreach (Boid neighbor in school)
        {
            if (neighbor != this)
            {
                averageVelocity += neighbor.velocity;
                count++;
            }
        }

        if (count > 0)
        {
            averageVelocity /= count;
            return averageVelocity.normalized;
        }

        return Vector3.zero;
    }
}
