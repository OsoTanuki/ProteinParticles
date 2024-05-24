using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeHelper : MonoBehaviour
{
	// Create buffers:
    // makes args buffer for instanced mesh drawing
    public static ComputeBuffer CreateArgsBuffer(Mesh mesh, int numInstances)
    {
        // Initialize the argument buffer:
        ComputeBuffer argsBuffer;
        
        // arguments array for the buffer
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        // number of indices
        args[0] = (uint) mesh.GetIndexCount(0);
        // number of particles
        args[1] = (uint) numInstances;
        // submesh start index
        args[2] = (uint) mesh.GetIndexStart(0);
        // base vertex
        args[3] = (uint) mesh.GetBaseVertex(0);
        // offset
        args[4] = 0;

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        return argsBuffer;
    }

	// creates a structured buffer from an array
    public static ComputeBuffer CreateStructuredBuffer<T>(T[] data)
    {
        var buffer = new ComputeBuffer(data.Length, GetStride<T>());
        buffer.SetData(data);
        return buffer;
    }
	// creates a structured buffer with specified size
    public static ComputeBuffer CreateStructuredBuffer<T>(int count)
    {
        return new ComputeBuffer(count, GetStride<T>());
    }

	// binds buffer to compute shader
	public static void SetBuffer(ComputeShader compute, string name, ComputeBuffer buffer, params int[] kernels)
	{
		foreach (var kernel in kernels)
    	{
        	compute.SetBuffer(kernel, name, buffer);
    	}
	}
    

	// Utility methods:
	// releases all buffers given
    public static void ReleaseAll(params ComputeBuffer[] buffers)
    {	
		foreach (var buffer in buffers)
        {
            if (buffer != null)
            {
                buffer.Release();
            }
        }
    }
    public static void Release(ComputeBuffer buffer)
	{	
        if (buffer != null)
        {
            buffer.Release();
        }
    }
	// dispatches the kernel of the compute shader
	public static void Dispatch(ComputeShader cs, int numIterationsX, int numIterationsY = 1, int numIterationsZ = 1, int kernelIndex = 0)
    {
        Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
        int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
        int numGroupsY = Mathf.CeilToInt(numIterationsY / (float)threadGroupSizes.y);
        int numGroupsZ = Mathf.CeilToInt(numIterationsZ / (float)threadGroupSizes.y);
        cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
    }

    // Helper methods:
    public static int GetStride<T>()
    {
        return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
    }
	public static Vector3Int GetThreadGroupSizes(ComputeShader compute, int kernelIndex = 0)
	{
        uint x, y, z;
        compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
        return new Vector3Int((int)x, (int)y, (int)z);
    }
}
