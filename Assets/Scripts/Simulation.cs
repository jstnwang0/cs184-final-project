using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class Simulation : MonoBehaviour
{
    public float gravity;
    [Range(0f, 1f)]
    public float damping;
    public int num_p_width;
    public int num_p_depth;
    public int num_p_height;
    
    public int particles_width;
    public int particles_depth;
    public int particles_height;

    public int bounding_width;
    public int bounding_depth;
    public int bounding_height;

    public float density_radius;

    [Range(0.2f, 5)]
    public float radius;

    public GameObject particle;

    int num_particles;
    GameObject[] particles;
    Vector3[] positions;
    Vector3[] velocities;
    Vector3[] forces;
    float[] densities;

    // Start is called before the first frame update
    void Start()
    {
        num_particles = num_p_width * num_p_depth * num_p_height;
        particles = new GameObject[num_particles];
        positions = new Vector3[num_particles];
        velocities = new Vector3[num_particles];
        densities = new float[num_particles];
        float width_space = (float)particles_width / (num_p_width - 1);
        float depth_space = (float)particles_depth / (num_p_depth - 1);
        float height_space = (float)particles_height / (num_p_height - 1);
        forces = new Vector3[num_particles];
        int x = 0;
        for (int i = 0; i < num_p_height; i++)
        {
            for (int j = 0; j < num_p_width; j++)
            {
                for (int k = 0; k < num_p_depth; k++)
                {
                    Vector3 pos = new(width_space * j - (float)particles_width / 2, 5 + height_space * i, depth_space * k - (float)particles_depth / 2);
                    particles[x] = Instantiate(particle, pos, transform.rotation);
                    positions[x] = pos;
                    forces[x] = new Vector3(0, -gravity, 0);
                    x++;
                }
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        runSimStep(Time.deltaTime);
    }

    void runSimStep(float deltaTime)
    {
        Parallel.For(0, num_particles, i =>
        {
            densities[i] = CalculateDensity(i);
        });

        Parallel.For(0, num_particles, i =>
        {
            //particles[i].transform.localScale = Vector3.one * 2 * radius;
            velocities[i] += forces[i] * deltaTime;
        });

        for (int i = 0; i < num_particles; i++)
        {
            positions[i] += velocities[i] * deltaTime;
            
            if (positions[i].y < radius)
            {
                velocities[i].y *= -1 * damping;
                float under_amt = radius - positions[i].y;
                positions[i] += new Vector3(0, 2 * under_amt, 0);
            }

            particles[i].transform.position = positions[i];
        }
    }

    float CalculateStrength(float dist, float radius)
    {
        if (dist > radius)
        {
            return 0;
        }

        float volume = 2 * Mathf.PI * Mathf.Pow(radius, 5) / 15;

        return (radius - dist) * (radius - dist) / volume;
    }

    float CalculateDensity(int p_index)
    {
        float density = 0;
        for (int i = 0; i < num_particles; i++)
        {
            float dist = (positions[i] - positions[p_index]).magnitude;
            density += CalculateStrength(dist, density_radius);
        }
        return density;
    }

    //float CalculatePressure(float d1, float d2)
    //{

    //}

    //Vector3 CalculateDensityGradient(int p_index)
    //{
    //    Vector3 gradient = new Vector3();
    //    for (int i = 0; i < num_particles; i++)
    //    {
    //        if (i == p_index)
    //        {
    //            continue;
    //        }

    //        Vector3 dir = (positions[i] - positions[p_index]);
    //        float dist = dir.magnitude;
    //        dir /= dist;
    //        float strength = CalculateStrength(dist, density_radius);

    //    }
    //    return density;
    //}

    void OnDrawGizmos()
    {
        // Draw a yellow cube at the transform position
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(new Vector3(0, (float) bounding_height / 2, 0), new Vector3(bounding_width, bounding_height, bounding_depth));
    }
}
