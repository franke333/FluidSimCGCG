static const float PI = 3.1415926;

//TODO this is wrong.. its for 2d case. but we are in 3d
float SmoothingKernel(float dst, float radius)
{
    float volume = PI * pow(radius, 8) / 4;
    float value = max(0, radius * radius - dst * dst);
    return value;
}

// you can add more methods