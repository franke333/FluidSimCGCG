static const float PI = 3.1415926;

//TODO this is wrong.. its for 2d case. but we are in 3d


float SpikyKernelPow2(float dst, float radius)
{
    if (dst < radius)
    {
        float scale = 15 / (2 * PI * pow(radius, 5));
        float v = radius - dst;
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

// you can add more methods