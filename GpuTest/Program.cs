using GpuTest.CPU;
using GpuTest.GPU;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace GpuTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();


            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string imgDir = @"c:\TEMP\";
                string imgPath = Path.Combine(imgDir, "test.jpg");
                string lutPath = Path.Combine(imgDir, "CrispAutumn.cube");


                //CPUMethods.TestMiscAlgorithms(imgPath, "result_cpu.jpg");

                using GPUMethods device = new GPUMethods();
                //device.TestHistogram(imgPath).Wait();
                device.TestMiscAlgorithms(imgPath, "result.jpg").Wait();
                //device.TestStreamProcessing(imgPath, "result.jpg").Wait();
                //device.Test3Dlut(imgPath, lutPath, "result.jpg").Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
