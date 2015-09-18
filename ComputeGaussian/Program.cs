using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputeGaussian
{
    class Program
    {
        static void Main(string[] args)
        {
            bool again = true;

            while (again)
            {


                Console.Write("Gaussian radius (default 1): ");
                int radius = 1;
                if (!int.TryParse(Console.ReadLine(), out radius))
                {
                    radius = 1;
                    Console.WriteLine("using default radius of 1");
                }

                Console.Write("Blur amount (default 1.0): ");
                // Blur amount:
                // == 1= regular blur
                // > 1 = less blur
                // < 1 = more blur
                float blurAmount = 1f;

                if (!float.TryParse(Console.ReadLine(), out blurAmount))
                {
                    blurAmount = 1f;
                    Console.WriteLine("using default blur amount of 1.0");
                }

                var kernel = ComputeKernel(radius, blurAmount);

                Console.WriteLine();
                Console.WriteLine("Gaussian {0}-tap kernel:", kernel.Length);
                Console.Write("[");
                for (var i = 0; i < kernel.Length; i++)
                {
                    Console.Write(kernel[i]);
                    if (i < kernel.Length - 1)
                        Console.Write(", ");
                }
                Console.WriteLine("]");
                Console.Write("Calculate another? [Y/N]: ");
                string yesNo = Console.ReadLine();

                if (yesNo.ToLower() == "n")
                    again = false;
            }
        }

        /// <summary>
        /// Computes a Gaussian 1D kernel
        /// </summary>
        /// <param name="blurRadius">Radius excluding center (e.g. a 3-tap filter has radius of 1, a 5-tap has radius 2)</param>
        /// <param name="blurAmount">Blur amount (e.g. 1.0)</param>
        /// <returns></returns>
        public static float[] ComputeKernel(int blurRadius, float blurAmount)
        {
            var radius = blurRadius;
            var amount = blurAmount;

            var kernel = new float[radius * 2 + 1];
            var sigma = radius / amount;

            float twoSigmaSquare = 2.0f * sigma * sigma;
            float sigmaRoot = (float)Math.Sqrt(twoSigmaSquare * Math.PI);
            float total = 0.0f;
            float distance = 0.0f;
            int index = 0;

            for (int i = -radius; i <= radius; ++i)
            {
                distance = i * i;
                index = i + radius;
                kernel[index] = (float)Math.Exp(-distance / twoSigmaSquare) / sigmaRoot;
                total += kernel[index];
            }

            for (int i = 0; i < kernel.Length; ++i)
                kernel[i] /= total;

            return kernel;
        }


    }
}
