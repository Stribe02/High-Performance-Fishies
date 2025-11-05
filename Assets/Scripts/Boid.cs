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
    
    // Rule 4: Keep being on screen/ camera you little shits
    // We need a method to find the bounds of the screen and then call it in the other method.

    public Vector3 BoundBoidsToScreen(Boid boid)
    {
        Camera camera = Camera.main;
        
        Vector3 screenBounds = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.fieldOfView));// Converts the center of the screen to World Units
         
        Vector3 vector = new Vector3(0,0,0); // vector to be returned
        //Viewport space is normalized and relative to the camera. The bottom-left of the viewport is (0,0); the top-right is (1,1). The z position is in world units from the camera.

        // boid.x is < xmin -> set vector.x to 10 or similar
        // if boid.x is > xmax -> vector.x to -10 or similar 
        if (boid.transform.position.x < -screenBounds.x)
        {
            vector.x = 10;
        } else if (boid.transform.position.x > screenBounds.x)
        {
            vector.x = -10;
        }

          // if boid.y < ymin -> set vector.
        if (boid.transform.position.y < -screenBounds.y)
        {
            vector.y = 10;
        } else if (boid.transform.position.y > screenBounds.y)
        {
            vector.y = -10;
        }

        if (boid.transform.position.z < -2.0f)
        {
            vector.z = 10;
        } else if (boid.transform.position.z < camera.fieldOfView)
        {
            vector.z = -5;
        }

        return vector;
    }
}
