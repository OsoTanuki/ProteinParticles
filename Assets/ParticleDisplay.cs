using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class ParticleDisplay : MonoBehaviour
{
	// Type enums
	private static Vector3[] typeColors = {
		new Vector3(0.500f, 0.500f, 0.500f) * 0.9f,       // 0  neut		(middle gray)
		new Vector3(0.784f, 0.568f, 0.105f) * 0.9f,       // 1  synt		(yellow)
		new Vector3(0.086f, 0.352f, 0.345f) * 0.9f,       // 2  ribo		(dark turquoise)
		new Vector3(0.074f, 0.682f, 0.662f) * 0.9f,       // 3  dpol		(turquoise)
		new Vector3(0.411f, 0.000f, 0.411f) * 0.9f,       // 4  latc		(purple)
		new Vector3(0.831f, 0.784f, 0.542f) * 0.9f,       // 5  cond		(beige)
		new Vector3(0.490f, 0.070f, 0.039f) * 0.9f,       // 6  extr		(dark red)
		new Vector3(0.788f, 0.788f, 0.788f) * 0.9f,       // 7  wall		(light gray)
		new Vector3(0.066f, 0.564f, 0.066f) * 0.9f,       // 8  chlo		(green)
		new Vector3(1.000f, 0.000f, 0.000f) * 0.9f,		// 9  pion		(red)
		new Vector3(0.858f, 0.694f, 0.360f) * 0.9f,		// 10 chan		(light yellow)
		new Vector3(0.262f, 0.188f, 0.031f) * 0.9f,		// 11 lyss		(dark yellow)
		new Vector3(0.000f, 0.000f, 1.000f) * 0.9f,		// 12 nion		(blue)
		new Vector3(0.667f, 0.125f, 0.125f) * 0.9f,		// 13 post		(medium red)
		new Vector3(0.125f, 0.125f, 0.667f) * 0.9f,		// 14 negt		(medium blue)
		new Vector3(0.431f, 0.800f, 0.792f) * 0.9f		// 15 DNA		(light turquoise)
	};

	private static int[] defaultMolecules =
	{
		0b000000_00000_00010_0000_0000_0000_0000, // Nitrogen
		0b000000_00010_00000_0000_0000_0000_0000, // Oxygen
		0b000000_00001_00000_0000_0000_0000_0000, // Water
		0b000000_00000_00001_0000_0000_0000_0000, // Ammonia
		0b000000_00100_00000_0001_0000_0000_0000, // Phosphate
		0b000000_00000_00000_0000_0001_0000_0000, // Hydrogen sulfide
		0b000001_00010_00000_0000_0000_0000_0000, // Carbon dioxide
		0b000000_00000_00000_0000_0000_0010_0000, // Fluorine
		0b000000_00000_00000_0000_0000_0000_0010  // Chlorine
	};
	
	// Unity Editor variables
	[Header("References")]
    public Mesh mesh;
    public Shader shader;
    public ComputeShader compute;

    [Header("Frame settings")] 
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    
    [Header("Simulation settings")]
    public Vector2 boundsSize;
    [FormerlySerializedAs("numParticles")] public int maxParticles = 1000;
    public int numMoleculesPerGridspace = 10;
    public float scale = 1;
	public float maxSpeed = 10;
    [Range(0,1)] public float elasticity = 1;

    [Header("Particle spawn controls")]
    public bool randomizeMolecules;
    public bool serializePositions;
    public bool serializeVelocities;
    public bool serializeTypes;
	public int[] uniformTypes;
    public float spacingSize = 1.1f;
    public float verticalOffset;
    [Range(0, 5)] public float initialVelocityRange = 1;
    
    [Header("Debug options")]
    public bool drawCollisionGizmos = false;
    public bool totalVelocities = false;
	public bool centerOneParticle = false;
	
	// Private variables
    private Material material;
    private Bounds bounds;
    private float totalSpeed = 0;
    private int numMolecules;
    
    // Buffers
    private ComputeBuffer argsBuffer;
    public ComputeBuffer posBuffer { get; private set; }
    public ComputeBuffer velBuffer { get; private set; }
    public ComputeBuffer newVelBuffer { get; private set; }
    public ComputeBuffer typBuffer { get; private set; }
    public ComputeBuffer bnd1Buffer { get; private set; }
    public ComputeBuffer bnd2Buffer { get; private set; }
    public ComputeBuffer molBuffer { get; private set;  }
    public ComputeBuffer molBnd1Buffer { get; private set;  }
    public ComputeBuffer molBnd2Buffer { get; private set;  }
    
    // Kernel IDs
	const int checkCollisionsKernel = 0;
	const int handleOverlapKernel = 1;
	const int calculateRepulsionKernel = 2;
	const int updatePositionsKernel = 3;
	const int runMoleculeInteractionsKernel = 4;

    // Start is called before the first frame update
    void Start()
    {
	    // set fixed timestep
	    float deltaTime = 1 / 60f;
	    Time.fixedDeltaTime = deltaTime;
	    
        // create material
        material = new Material(shader);
        material.enableInstancing = true;

        numMolecules = (int)boundsSize.x * (int)boundsSize.y * numMoleculesPerGridspace;
        
        compute.SetInt("numParticles", maxParticles);
        
        // create and bind buffers
        argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, maxParticles);
        CreateParticleBuffers();
        SetParticleBufferData();
        BindParticleBuffers();
        
        Debug.Log("Startup complete. Begin simulation.");
    }

    void FixedUpdate()
    {
	    if (fixedTimeStep)
	    {
		    RunSimulationFrame(Time.fixedDeltaTime);
	    }
	    
	    Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }
    void Update()
    {
	    if (!fixedTimeStep && Time.frameCount > 10) 
	    {
	        RunSimulationFrame(Time.deltaTime);
        }
	        
	    Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
	}

    void RunSimulationFrame(float frameTime)
    {
	    float timeStep = frameTime / iterationsPerFrame;
	    if (!fixedTimeStep)
	    {
		    compute.SetFloat("dt", timeStep);
	    }

	    UpdateParameters(frameTime);

	    for (int i = 0; i < iterationsPerFrame; i++)
	    {
		    RunSimulationStep();
	    }
    }
    void RunSimulationStep()
    {
	    ComputeHelper.Dispatch(compute, maxParticles, kernelIndex: checkCollisionsKernel);
	    ComputeHelper.Dispatch(compute, maxParticles, kernelIndex: handleOverlapKernel);
	    ComputeHelper.Dispatch(compute, maxParticles, kernelIndex: calculateRepulsionKernel);
	    ComputeHelper.Dispatch(compute, maxParticles, kernelIndex: updatePositionsKernel);
	    // ComputeHelper.Dispatch(compute, maxParticles, kernelIndex: runMoleculeInteractionsKernel);

	    if (totalVelocities)
	    {
		    CalculateTotalSpeed();
	    }
    }

    void CalculateTotalSpeed()
    {
	    Vector2[] velocities = new Vector2[maxParticles];
	    velBuffer.GetData(velocities, 0, 0, maxParticles);
	    float newSpeed = 0;
	    for (int i = 0; i < maxParticles; i++)
	    {
		    newSpeed += velocities[i].magnitude;
	    }

	    if (Mathf.Abs(newSpeed - totalSpeed) > 0.001f * maxParticles)
	    {
		    Debug.Log("Speed changed by " + (newSpeed - totalSpeed) + ", now " + newSpeed);
		    totalSpeed = newSpeed;
	    }
    }

    // updates simulation parameters
    void UpdateParameters(float deltaTime)
    {
	    bounds = new Bounds(Vector2.zero, new Vector2(boundsSize.x, boundsSize.y));

	    material.SetFloat("scale", scale);
    
	    compute.SetInt("numParticles", maxParticles);
	    compute.SetInt("NumMoleculesPerGridspace", numMoleculesPerGridspace);
	    compute.SetFloat("dt", deltaTime);
	    compute.SetFloat("diameter", scale);
	    compute.SetFloat("elasticity", elasticity);
		compute.SetFloat("maxSpeed", maxSpeed);
	    compute.SetVector("boundsSize", boundsSize);
	    
	    // mouse input:
	    Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
	    var mouseDown = Input.GetMouseButton(0);
	    
	    compute.SetVector("mousePos", mousePos);
	    compute.SetBool("mouseDown", mouseDown);
    }
    
	void CreateParticleBuffers()
	{
		Debug.Log("Creating Particle Buffers");
		ComputeHelper.ReleaseAll(molBnd2Buffer, molBnd1Buffer, molBuffer, bnd2Buffer, bnd1Buffer, typBuffer, newVelBuffer, 
            velBuffer, 
            posBuffer);

        posBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(maxParticles);
        velBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(maxParticles);
		newVelBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(maxParticles);
        typBuffer = ComputeHelper.CreateStructuredBuffer<int>(maxParticles);
		bnd1Buffer = ComputeHelper.CreateStructuredBuffer<int>(maxParticles);
		bnd2Buffer = ComputeHelper.CreateStructuredBuffer<int>(maxParticles);
		molBuffer = ComputeHelper.CreateStructuredBuffer<int>(numMolecules);
		molBnd1Buffer = ComputeHelper.CreateStructuredBuffer<int>(maxParticles);
		molBnd2Buffer = ComputeHelper.CreateStructuredBuffer<int>(maxParticles);
		//renBuffer = ComputeHelper.CreateStructuredBuffer<byte>(numParticles);
	}

    void SetParticleBufferData()
    {
	    Vector2[] positions = new Vector2[maxParticles];
	    Vector2[] velocities = new Vector2[maxParticles];
	    int[] types = new int[maxParticles];
		byte[] render = new byte[maxParticles];
		int[] bonds = new int[maxParticles];
		int[] molecules = new int[numMolecules];
		int[] molBind1 = new int[maxParticles];
		int[] molBind2 = new int[maxParticles];
        
        Debug.Log("Spawning Particles & populating molecules");

	    SpawnParticles(positions, velocities, types, render, bonds, molBind1, molBind2);
	    PopulateMolecules(molecules);

        Debug.Log("Setting Buffer Data");
        posBuffer.SetData(positions);
        velBuffer.SetData(velocities);
        typBuffer.SetData(types);
        molBuffer.SetData(molecules);
		//renBuffer.SetData(render);
    }
    void SpawnParticles(Vector2[] pos, Vector2[] vel, int[] typ, byte[] ren, int[] bonds, int[] molBnd1, int[] molBnd2)
    {
	    var sqrt = (int)Mathf.Sqrt(maxParticles);
	    var isEven = (maxParticles / sqrt) % 2 == 0;
	    
	    for (var i = 0; i < maxParticles; i++)
	    {
		    if (serializePositions) {
			    pos[i] = PositionInSpawnBox(sqrt, i);
		    } else {
			    pos[i] = new Vector2(UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
									 UnityEngine.Random.Range(bounds.min.y, bounds.max.y));
		    }

		    if (serializeVelocities) {
			    if (isEven) {
				    vel[i] = new Vector2(-1 * (((i % 2) * 2) - 1), 0) * initialVelocityRange;
			    } else {
				    vel[i] = new Vector2(-1 * ((i % 3) - 1), 0) * initialVelocityRange;
			    }
		    } else {
			    vel[i] = new Vector2(UnityEngine.Random.Range(-1.0f, 1.0f),
									 UnityEngine.Random.Range(-1.0f, 1.0f)) * initialVelocityRange;
		    }
			
			if (uniformTypes != null && uniformTypes.Length > 0) {
				typ[i] = uniformTypes[i % uniformTypes.Length];
		    } else if (serializeTypes) {
		    	typ[i] = i % typeColors.Length;
		    } else {
		    	typ[i] = UnityEngine.Random.Range(0, typeColors.Length);
		    }

		    if (typ[i] == 1 || typ[i] == 10 || typ[i] == 11)
		    {
			    var synt = defaultMolecules[i % defaultMolecules.Length];
			    if (randomizeMolecules) synt = RandomValidMolecule();
			    
			    molBnd1[i] = RandomChildMolecule(synt);
			    molBnd2[i] = synt - molBnd1[i];
		    }

			ren[i] = 1;

			bonds[i] = maxParticles;
	    }
		if(centerOneParticle) pos[0] = Vector2.zero;

		// for (int i = numParticles; i < maxParticles; i++)
		// {
		// 	pos[i] = 1000 * Vector2.one;
		// 	vel[i] = Vector2.zero;
		// 	typ[i] = 16;
		// 	bonds[i] = maxParticles;
		// 	ren[i] = 1;
		// }
    }
    Vector2 PositionInSpawnBox(int sqrt, int i)
    {
	    int height = sqrt, width = maxParticles / height;
	    Vector2 downLeft = 0.5f * new Vector2
	    (
		    -((width - 1) * spacingSize),
		    (height * verticalOffset) - ((height - 1) * spacingSize)
		);
	    return downLeft + new Vector2( (i % width) * spacingSize, (i / width) * spacingSize - (i * verticalOffset) );
    }

    void PopulateMolecules(int[] molecules)
    {
	    for (int i = 0; i < numMolecules; i++)
	    {
		    if(randomizeMolecules) molecules[i] = RandomValidMolecule();
		    else molecules[i] = defaultMolecules[i % defaultMolecules.Length];
	    }
    }

    int RandomValidMolecule()
    {
	    var mol = (int)UnityEngine.Random.Range(0, 0xFFFFFFFF);

	    mol = ValidateMolecule(mol);
	    
	    if (mol == 0) return RandomValidMolecule();
	    return mol;
    }
    int RandomChildMolecule(int mol)
    {
	    if(NumBits(mol) <= 2) return 0;
	    
	    var childMol = 0;
	    for (int i = 0; i < 32; i++)
	    {
		    var n = UnityEngine.Random.Range(0, 2);
		    childMol |= mol & n;
	    }
	    childMol = ValidateMolecule(childMol);

	    if (childMol == 0) return RandomChildMolecule(mol);
	    return childMol;
    }
    int ValidateMolecule(int mol)
    {
	    var availableSites = AvailableSites(mol);
	    
	    if (availableSites < 0)
	    {
		    var Cl = mol & 0x000000F;
		    var F = mol >> 4 & 0x0000000F;
		    
		    var lessF = Mathf.Min(availableSites / 2, F);
		    var lessCl = Mathf.Min(availableSites - lessF, Cl);
		    
		    mol -= lessF * 0x00000010;
		    mol -= lessCl * 0x00000001;
	    }
	    
		if(AvailableSites(mol) < 0) return 0;
	    return mol;
    }
    int AvailableSites(int mol)
    {
	    int availableSites = 0;
	    
	    int lmnt = mol & 0x0000000F;
	    availableSites -= lmnt;
	    
	    lmnt = mol >> 4 & 0x0000000F;
	    availableSites -= lmnt;
	    
	    lmnt = mol >> 12 & 0x0000000F;
	    availableSites += lmnt;
	    
	    lmnt = mol >> 16 & 0x0000001F;
	    availableSites += lmnt;

	    lmnt = mol >> 26;
	    availableSites += 2 * lmnt;

	    return availableSites;
    }

    int NumBits(int i)
    {
	    var bits = 0;
	    
	    while (i > 0)
	    {
		    bits += i & 1;
		    i >>= 1;
	    }

	    return bits;
    }

    void BindParticleBuffers()
    {
	    material.SetBuffer("_Positions", posBuffer);
	    material.SetBuffer("_Types", typBuffer);
		//material.SetBuffer("_Render", renBuffer);
	    
	    Debug.Log("Binding Buffers for Compute Shader");
	    ComputeHelper.SetBuffer(compute, "_Positions", posBuffer, checkCollisionsKernel, handleOverlapKernel, 
            calculateRepulsionKernel, updatePositionsKernel);
	    ComputeHelper.SetBuffer(compute, "_Velocities", velBuffer, checkCollisionsKernel, handleOverlapKernel, calculateRepulsionKernel, updatePositionsKernel);
		ComputeHelper.SetBuffer(compute, "_NewVelocities", newVelBuffer, checkCollisionsKernel, handleOverlapKernel, calculateRepulsionKernel, updatePositionsKernel);
	    ComputeHelper.SetBuffer(compute, "_Types", typBuffer, checkCollisionsKernel, handleOverlapKernel, calculateRepulsionKernel);
		ComputeHelper.SetBuffer(compute, "_Bond1", bnd1Buffer, handleOverlapKernel);
		ComputeHelper.SetBuffer(compute, "_Bond2", bnd2Buffer, handleOverlapKernel);
		
		// ComputeHelper.SetBuffer(compute, "_MoleculeGrid", molBuffer, runMoleculeInteractionsKernel);
		// ComputeHelper.SetBuffer(compute, "_MoleculeBind1", molBnd1Buffer, runMoleculeInteractionsKernel);
		// ComputeHelper.SetBuffer(compute, "_MoleculeBind2", molBnd2Buffer, runMoleculeInteractionsKernel);
		
		//ComputeHelper.SetBuffer(compute, "_Render", renBuffer, checkCollisionsKernel, updatePositionsKernel);
    }

    void OnDestroy()
    {
		Debug.Log("Releasing Buffers");
		ComputeHelper.ReleaseAll(molBnd1Buffer, molBnd2Buffer, molBuffer, bnd2Buffer, bnd1Buffer, typBuffer, velBuffer, 
            posBuffer, 
            argsBuffer);
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0.5f, 0.4f);
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);

        if (drawCollisionGizmos && argsBuffer != null)
        {
	        DrawCollisionGizmos();
        }
    }
    
    void DrawCollisionGizmos()
    {
	    Vector2[] positions = new Vector2[maxParticles];
	    Vector2[] velocities = new Vector2[maxParticles];
	    
	    posBuffer.GetData(positions, 0, 0, maxParticles);
	    velBuffer.GetData(velocities, 0, 0, maxParticles);

	    Color velColor = new Color(1, 0, 0.5f, 0.4f);
	    Color colColor = new Color(0.8f, 0.8f, 0, 0.4f);
	    
	    for (int i = 0; i < maxParticles; i++)
	    {
		    Vector2 pos1 = positions[i];
		    Vector2 vel = velocities[i];
		    
		    Gizmos.color = velColor;
		    Gizmos.DrawLine(pos1, pos1 + (vel * scale));
		    
		    for (int j = i; j < maxParticles; j++)
		    {
			    Vector2 pos2 = positions[j];
			    Vector2 norm = pos2 - pos1;

			    if (norm.sqrMagnitude <= scale * scale)
			    {
				    Vector2 midpoint = pos1 + (norm * 0.5f);
				    Vector2 tngt = 0.5f * new Vector2(-norm.y, norm.x);
			        
				    Gizmos.color = colColor;
				    Gizmos.DrawLine(pos1, pos2);
				    Gizmos.DrawLine(midpoint - tngt, midpoint + tngt);
			    }
		    }
	    }
    }
}
