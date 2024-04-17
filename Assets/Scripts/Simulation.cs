using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    public float gravity;
    [Range(0f, 1f)]
    public float damping;
    public int num_p_width;
    public int num_p_height;
    public int particles_width;
    public int particles_height;
    public int bounding_width;
    public int bounding_height;
    [Range(0.2f, 5)]
    public float radius;

    public GameObject particle;

    int num_particles;
    GameObject[] particles;
    Vector3[] velocities;

    // Start is called before the first frame update
    void Start()
    {
        num_particles = num_p_width * num_p_height;
        particles = new GameObject[num_particles];
        velocities = new Vector3[num_particles];
        float width_space = (float)particles_width / (num_p_width - 1);
        float height_space = (float)particles_height / (num_p_height - 1);
        for (int i = 0; i < num_p_height; i++) {
            for (int j = 0; j < num_p_width; j++)
            {
                Vector3 pos = new(width_space * j - (float)particles_width / 2, 5 + height_space * i, 0);
                particles[i * num_p_width + j] = Instantiate(particle, pos, transform.rotation);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < num_particles; i++)
        {
            particles[i].transform.localScale = Vector3.one * 2 * radius;
            particles[i].transform.position += velocities[i] * Time.deltaTime;
            velocities[i] += new Vector3(0, -gravity, 0) * Time.deltaTime;


            if (particles[i].transform.position.y < radius)
            {
                velocities[i].y *= -1 * damping;
                float under_amt = radius - particles[i].transform.position.y;
                particles[i].transform.position += new Vector3(0, 2 * under_amt, 0);
            }
        }
    }

    void OnDrawGizmos()
    {
        // Draw a yellow cube at the transform position
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(new Vector3(0, (float) bounding_height / 2, 1), new Vector3(bounding_width, bounding_height, 1));
    }
}
