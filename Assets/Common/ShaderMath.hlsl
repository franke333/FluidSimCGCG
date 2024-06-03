static const float PI = 3.1415926;

float SpikyKernelPow3(float dst, float radius)
{
    if (dst < radius)
    {
        float scale = 15 / (PI * pow(radius, 6));
        float v = radius - dst;
        return v * v * v * scale;
    }
    return 0;
}

float SpikyKernelPow2(float dst, float radius)
{
    if (dst < radius)
    {
        //float volume = PI * pow(radius, 8) / 4;
        
        float scale = 15.0 / (2.0 * PI * pow(radius, 5));
        float v = radius  - dst;
        return v * v * scale;
    }
    return 0;
}

//TODO this lso for 2d
float DerivativeSpikyPow2(float dst, float radius)
{
    if (dst <= radius)
    {
        float scale = 15 / (pow(radius, 5) * PI);
        float v = radius - dst;
        return -v * scale;
    }
    return 0;
}

float DensityDerivative(float dst, float radius)
{
    return DerivativeSpikyPow2(dst, radius);
}

float SmoothingKernel(float dst, float radius)
{
    return SpikyKernelPow2(dst, radius);
}


//for 3d
float DerivativeSpikyPow3(float dst, float radius)
{
    if (dst <= radius)
    {
        float scale = 45 / (pow(radius, 6) * PI);
        float v = radius - dst;
        return -v * v * scale;
    }
    return 0;
}

float NearDensityDerivative(float dst, float radius)
{
    return DerivativeSpikyPow3(dst, radius);
}

// you can add more methods