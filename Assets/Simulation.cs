using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Simulation : MonoBehaviour {
    enum Type {
        NEUT, //Neutral
        CONS, //Substrate (Constructive)
        RIBO, //Ribosome
        DPOL, //DNA Polymerase
        LATC, //Potential Latch
        COND, //Conductor
        EXTR, //Extender
        WALL, //Wall
        CHLO, //Chlorophyll
        POST, //+ Terminal
        CHAN, //Channel
        DSTR, //Substrate (Destructive)
        NEGT, //- Terminal
        MOLE  //Molecule
    }
    private static Vector3[] typeColors = {
        new Vector3(0.500f, 0.500f, 0.500f), // neut
        new Vector3(0.784f, 0.568f, 0.105f), // subst +
        new Vector3(0.086f, 0.352f, 0.345f), // ribo
        new Vector3(0.074f, 0.682f, 0.662f), // dna pol
        new Vector3(0.411f, 0.000f, 0.411f), // pot
        new Vector3(0.831f, 0.784f, 0.643f), // con
        new Vector3(0.490f, 0.070f, 0.039f), // ext
        new Vector3(0.788f, 0.788f, 0.788f), // wall
        new Vector3(0.066f, 0.564f, 0.066f), // chlo
        new Vector3(0.666f, 0.125f, 0.125f), // +term
        new Vector3(0.858f, 0.694f, 0.360f), // chan
        new Vector3(0.262f, 0.188f, 0.031f), // subs -
        new Vector3(0.125f, 0.125f, 0.666f), // -term
        new Vector3(0, 0, 0)  // mole
    };

    //FIELDS
    [Header("Simulation settings")]
    [Range(0, 1)] public float collisionDamping;
    public float gravity;
    public float initialVelocityVariance;
    public int numParticles = 2;
    public Vector2 boundsSize;

    //DEBUG
    [Header("Debug")]
    public bool ShowReflectLines = true;

    [Header("References")] 
    public static ParticleDisplay display;

    private Bounds bounds;

    public Vector2[] positions;
    public Vector2[] velocities;
    public int[] types;
    // public ComputeBuffer positionBuffer { get; private set; }
    // public ComputeBuffer velocityBuffer { get; private set; }
    // public ComputeBuffer typeBuffer { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
        bounds = new Bounds(new Vector3(-boundsSize.x, -boundsSize.y, 0),
                            new Vector3( boundsSize.x,  boundsSize.y, 0));
        
        InitializeParticles();

        // InitializeBuffers();
    }
    void InitializeParticles() 
    {
        positions = new Vector2[numParticles];
        velocities = new Vector2[numParticles];
        types = new int[numParticles];

//        positions[0] = Vector2.zero;

        for(int i = 0; i < numParticles; i++) {
            // randomize positions
            positions[i] = new Vector2(UnityEngine.Random.Range(bounds.min.x, bounds.max.x), UnityEngine.Random.Range(bounds.min.y, bounds.max.y));

            // randomize velocities
            velocities[i] = new Vector2(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1));
            velocities[i].Normalize();
            velocities[i] *= UnityEngine.Random.Range(1-initialVelocityVariance, 1+initialVelocityVariance);

            // randomize types (only using type 0 right now)
            types[i] = 0;
        }
    }

    // void InitializeBuffers()
    // {
    //     positionBuffer = new ComputeBuffer(numParticles, GetStride<float2>());
    //     velocityBuffer = new ComputeBuffer(numParticles, GetStride<float2>());
    //     typeBuffer = new ComputeBuffer(numParticles, sizeof(int));
    // }


    // Update is called once per frame
    void Update() {
        // float2[] positions = positionBuffer.GetData();
        // float2[] velocities = positionBuffer.GetData();
        
        // UpdatePositions(positions, velocities);
        //
        // ResolveCollisions(positions, velocities);

        // DrawMeshes();
    }
//
//     void UpdatePositions(float2[] positions, float2[] velocities) {
// //        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
// //        positions[1] = mousePos;
//         
//         for(int i = 0; i < numParticles; i++) {
//             positions[i] += velocities[i] * Time.deltaTime;
//         }
//
//         UpdateMeshBuffer();
//     }
//     void ResolveCollisions(float2[] positions, float2[] velocities) {
//         for(int i = 0; i < numParticles; i++) {
//             Vector2 position = positions[i];
//             Vector2 velocity = velocities[i];
//
//             // if out of bounds on the left or right
//             if(Mathf.Abs(position.x) > boundsSize.x) {
//                 // move the ball within the bounds
//                 position.x = boundsSize.x * Mathf.Sign(position.x);
//                 // reflect the velocity; bounce it off the bound
//                 velocity.x *= -1 * collisionDamping;
//             }
//             // if out of bounds on the top or bottom
//             if(Mathf.Abs(position.y) > boundsSize.y) {
//                 // move the ball within the bounds
//                 position.y = boundsSize.y * Mathf.Sign(position.y);
//                 // reflect the velocity; bounce it off the bound
//                 velocity.y *= -1 * collisionDamping;
//             }
//
//             for(int j = 0; j < numParticles; j++) {
//                 if(j == i) continue;
//                 if(IsColliding(i, j)) Collide(i, j);
//             }
//         }
//     }
//
//     bool IsColliding(int a, int b, float2[] positions) {
//         float dist = (positions[a] - positions[b]).sqrMagnitude;
//         return dist <= particleRadius * particleRadius * 4;
//     }
//     void Collide(int a, int b)
//     {
//         int[] types = typeBuffer.GetData();
//         if(HasCollisionInteraction(types[a], types[b])) {
//             // do collision interactions
//         }
//         // reflect
//         Reflect(a, b);
//     }
//     bool HasCollisionInteraction(Type a, Type b) {
//         switch(a) {
//             case Type.WALL:
//                 return b == Type.WALL;
//             case Type.DSTR:
//             case Type.CONS:
//             case Type.CHAN:
//             case Type.DPOL:
//             case Type.RIBO:
//             case Type.CHLO:
//                 if(b == Type.MOLE) return HasMoleculeInteraction(a, b);
//                 return false;
//             default: return false;
//         }
//     }
//     bool HasMoleculeInteraction(Type a, Type b) { return true; }
     void Reflect(int a, int b)
     {
		Vector2 A = positions[a];
		Vector2 B = positions[b];		

        // raw velocities
        Vector2 VA = velocities[a];
        Vector2 V = velocities[b];
        // make relative to a
        VA -= VA; // (0,0)
        V += VA; // relative velocity of B
		
		Vector2 norm = A + B;
		Vector2 tan = new Vector2(norm.y, -norm.x);
		if(Vector2.Angle(V, Vector2.right) > Vector2.Angle(norm, Vector2.right)) tan = -tan;

        if(ShowReflectLines) {
			Gizmos.color = new Color(0.1f, 0.5f, 0.75f, 1.0f);
			Gizmos.DrawLine(B, V - B);
			Gizmos.DrawLine(A, norm);
        }


         // pA.x + pB.x = pA'.x + pB'.x
         // since mA = mB = 1
         // vA.x + vB.x = vA'.x + vB'.x
         // looking from the perspective of A so it becomes "at rest"
         // vB.x = vA'.x + vB'.x - vA.x
         // if we get the coordinates so B is only moving along the "x" axis
     }

    // render the meshes
    // public void DrawMeshes() {
    //     if(mesh == null) print("mesh null");
    //     Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    // }

    int GetStride<T>()
    {
        return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
    }

    public static float3 GetColorFromType(int type)
    {
        return typeColors[type];
    }

    public Vector3 GetColorFromTypeArray(int index)
    {
        return typeColors[(int)types[index]];
    }
	public static Vector3 Color(int index) {
		return typeColors[index];
	}
}
