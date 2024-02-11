namespace GpuTest.GPU
{
    public enum FilterType
    {
        GaussianBlur = 0,
        BoxBlur = 1,
        LaplacianOfGaussianBlur = 2,
        LaplacianBlur = 3,
        CreateSobelXKernel = 4,
        CreateSobelYKernel = 5,
        HighPass = 6,
        LowPass = 7,
        Median = 8,
        Emposs = 9,
        MotionBlur = 10,
        PrewittX = 11,
        PrewittY = 12,
        RobertsX = 13,
        RobertsY = 14,
        FreiChenX = 15,
        FreiChenY = 16,
        HorizontalBlur = 17,
        Sharpness = 18,
    }
}
