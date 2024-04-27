using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using DataStructures.ViliWonka.KDTree;
using UnityEditor.Search;
using static UnityEditor.PlayerSettings;


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

    [Range(0.1f, 10)]
    public float density_radius;
    public float target_density;
    public float pressure_multiplier;

    [Range(0.05f, 3)]
    public float display_radius;

    public GameObject particle;

    int num_particles;
    GameObject[] particles;
    Vector3[] positions;
    Vector3[] velocities;
    float[] densities;

    HashSet<int>[] spatialGrid;
    int grid_width;
    int grid_depth;
    int grid_height;

    int GetGridIndex(Vector3 pos)
    {
        int x = (int)Math.Floor((pos.x + (float) bounding_width / 2) / density_radius);
        int y = (int)Math.Floor((pos.y + (float) bounding_height / 2) / density_radius);
        int z = (int)Math.Floor((pos.z + +(float) bounding_depth / 2) / density_radius);

        return y * grid_depth * grid_width + z * grid_width + x;
    }

    void AddGridPoint(int p_index)
    {
        int gridIndex = GetGridIndex(positions[p_index]);
        HashSet<int> points = spatialGrid[gridIndex];
        points.Add(p_index);
    }

    void RemoveGridPoint(int p_index)
    {
        int gridIndex = GetGridIndex(positions[p_index]);
        HashSet<int> points = spatialGrid[gridIndex];
        points.Remove(p_index);
    }

    List<int> GetGridPoints(int p_index)
    {
        Vector3 pos = positions[p_index];
        int x = (int)Math.Floor((pos.x + (float)bounding_width / 2) / density_radius);
        int y = (int)Math.Floor((pos.y + (float)bounding_height / 2) / density_radius);
        int z = (int)Math.Floor((pos.z + +(float)bounding_depth / 2) / density_radius);

        List<int> output = new();
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                for (int k = -1; k < 2; k++)
                {
                    int index = (y + i) * grid_width * grid_depth + (z + j) * grid_width + (x + k);
                    if (0 <= index && index < spatialGrid.Length)
                    {
                        HashSet<int> points = spatialGrid[index];

                        foreach (int p in points)
                        {
                            if ((positions[p] - pos).magnitude < density_radius)
                            {
                                output.Add(p);
                            }
                        }
                    }
                }
            }
        }
        return output;
    }

    // Start is called before the first frame update
    void Start()
    {
        num_particles = num_p_width * num_p_depth * num_p_height;
        particles = new GameObject[num_particles];
        positions = new Vector3[num_particles];
        velocities = new Vector3[num_particles];
        densities = new float[num_particles];
        grid_width = (int) Math.Ceiling(bounding_width / density_radius);
        grid_depth = (int)Math.Ceiling(bounding_depth / density_radius);
        grid_height = (int)Math.Ceiling(bounding_height / density_radius);

        spatialGrid = new HashSet<int>[grid_width * grid_depth * grid_height];

        for (int i = 0; i < spatialGrid.Length; i++)
        {
            spatialGrid[i] = new HashSet<int>();
        }


        float width_space = (float)particles_width / (num_p_width - 1);
        float depth_space = (float)particles_depth / (num_p_depth - 1);
        float height_space = (float)particles_height / (num_p_height - 1);
        int x = 0;

        System.Random rnd = new System.Random();
        for (int i = 0; i < num_p_height; i++)
        {
            for (int j = 0; j < num_p_width; j++)
            {
                for (int k = 0; k < num_p_depth; k++)
                {
                    Vector3 pos = new(width_space * j - (float)particles_width / 2, height_space * i - (float) particles_height / 2, depth_space * k - (float)particles_depth / 2);
                    particles[x] = Instantiate(particle, pos, transform.rotation);
                    particles[x].transform.localScale = Vector3.one * 2 * display_radius;
                    positions[x] = pos;

                    AddGridPoint(x);
                    velocities[x] = new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble());
                    x++;
                }
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        RunSimStep(Time.deltaTime);
    }

    void RunSimStep(float deltaTime)
    {
        // Calculate densities
        Parallel.For(0, num_particles, i =>
        {
            densities[i] = CalculateDensity(i);
        });


        // Add pressure force and gravity
        Parallel.For(0, num_particles, i =>
        {
            velocities[i] += new Vector3(0, -gravity, 0) * deltaTime;
            velocities[i] += CalculatePressureGradient(i) / densities[i] * deltaTime;
        });

        for (int i = 0; i < num_particles; i++)
        {
            RemoveGridPoint(i);
        }

        

        // Handle collisions with box
        Parallel.For(0, num_particles, i =>
        {
            positions[i] += velocities[i] * deltaTime;
            
            if ((bounding_height / 2) - Mathf.Abs(positions[i].y) < display_radius)
            {
                velocities[i].y *= -1 * damping;
                float over_amt = display_radius - ((bounding_height / 2) - Mathf.Abs(positions[i].y));
                if (positions[i].y > 0)
                {
                    positions[i] -= new Vector3(0, 2 * over_amt, 0);
                }
                else
                {
                    positions[i] += new Vector3(0, 2 * over_amt, 0);
                }

            }

            if ((bounding_width / 2) - Mathf.Abs(positions[i].x) < display_radius)
            {
                velocities[i].x *= -1 * damping;
                float over_amt = display_radius - ((bounding_width / 2) - Mathf.Abs(positions[i].x));
                if (positions[i].x > 0)
                {
                    positions[i] -= new Vector3(2 * over_amt, 0, 0);
                }
                else
                {
                    positions[i] += new Vector3(2 * over_amt, 0, 0);
                }

            }

            if ((bounding_width / 2) - Mathf.Abs(positions[i].z) < display_radius)
            {
                velocities[i].z *= -1 * damping;
                float over_amt = display_radius - ((bounding_width / 2) - Mathf.Abs(positions[i].z));
                if (positions[i].z > 0)
                {
                    positions[i] -= new Vector3(0, 0, 2 * over_amt);
                }
                else
                {
                    positions[i] += new Vector3(0, 0, 2 * over_amt);
                }

            }
       
        });

        // Update particles position
        // TODO: replace this with shader that takes in positions[i]
        for (int i = 0; i < num_particles; i++)
        {
            AddGridPoint(i);
            particles[i].transform.position = positions[i];
        }
    }

    float CalculateDensity(int p_index)
    {
        float count = 0;
        float volume = 2 * Mathf.PI * Mathf.Pow(density_radius, 5) / 15;
        //float volume = 4 * Mathf.PI * Mathf.Pow(density_radius, 3) / 3;

        List<int> particles_in_range = GetGridPoints(p_index);

        foreach (int i in particles_in_range)
        {
            float dist = (positions[i] - positions[p_index]).magnitude;
            count += (density_radius - dist) * (density_radius - dist);
        }
        //for (int i = 0; i < num_particles; i++)
        //{
        //    float dist = (positions[i] - positions[p_index]).magnitude;

        //    if (dist > density_radius)
        //    {
        //        continue;
        //    }
        //    count += (density_radius - dist) * (density_radius - dist);
        //    //count += 1;
        //}
        return count / volume;
    }


    float CalculatePressure(float d1, float d2)
    {
        float p1 = (d1 - target_density) * pressure_multiplier;
        float p2 = (d2 - target_density) * pressure_multiplier;
        return (p1 + p2) / 2;
    }

    Vector3 CalculatePressureGradient(int p_index)
    {
        Vector3 gradient = new();

        List<int> particles_in_range = GetGridPoints(p_index);

        foreach (int i in particles_in_range)
        {
            if (i == p_index)
            {
                continue;
            }

            Vector3 dir = (positions[i] - positions[p_index]);
            float dist = dir.magnitude;

            if (dist == 0)
            {
                dir = new Vector3(1, 0, 0);
            }
            else
            {
                dir /= dist;
            }

            float strength = (dist - density_radius) * 15 / (Mathf.PI * Mathf.Pow(density_radius, 5));
            gradient += CalculatePressure(densities[i], densities[p_index]) * strength * dir / densities[i];
        }

        //for (int i = 0; i < num_particles; i++)
        //{
        //    if (i == p_index)
        //    {
        //        continue;
        //    }

        //    Vector3 dir = (positions[i] - positions[p_index]);
        //    float dist = dir.magnitude;

        //    if (dist > density_radius)
        //    {
        //        continue;
        //    }

        //    if (dist == 0)
        //    {
        //        dir = new Vector3(1, 0, 0);
        //    }
        //    else
        //    {
        //        dir /= dist;
        //    }

        //    float strength = (dist - density_radius) * 15 / (Mathf.PI * Mathf.Pow(density_radius, 5));
        //    gradient += CalculatePressure(densities[i], densities[p_index]) * strength * dir / densities[i];
        //}

        return gradient;
    }

    void OnDrawGizmos()
    {
        // Draw a yellow cube at the transform position
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(new Vector3(0, 0, 0), new Vector3(bounding_width, bounding_height, bounding_depth));
    }
}
