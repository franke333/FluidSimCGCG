using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ComputeHelper 
{
    public static int GetStride<T>() where T : struct
    {
        return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
    }

    public static ComputeBuffer CreateBuffer<T>(int count) where T : struct
    {
        int stride = GetStride<T>();
        return new ComputeBuffer(count, stride);
    }

    // set all kernels that will use the buffer
    public static void SetBuffer(ComputeShader compute, string bufferName, ComputeBuffer buffer, params int[] kernels)
    {
        for (int i = 0; i < kernels.Length; i++)
            compute.SetBuffer(kernels[i], bufferName, buffer);
    }

    public static Vector3Int GetThreadGroupSizes(ComputeShader compute, int kernelIndex)
    {
        uint x, y, z;
        compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
        return new Vector3Int((int)x, (int)y, (int)z);
    }

    public static void Dispatch(ComputeShader cs, int numIterationsX, int kernelIndex)
    {
        Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
        int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
        cs.Dispatch(kernelIndex, numGroupsX, 1, 1);
    }
}
