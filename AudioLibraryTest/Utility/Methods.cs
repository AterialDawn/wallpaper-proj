using System;
using System.Diagnostics;
using System.IO;

namespace player.Utility
{
    static class UtilityMethods
    {
        public const float TargetFrameTime = 1000f / 60f;

        public static float LinearInterpolate(float AbsMin, float AbsMax, float Value)
        {
            return (Value / (1f / (AbsMax - AbsMin))) + AbsMin;
        }

        public static double LinearInterpolate(double AbsMin, double AbsMax, double Value)
        {
            return (Value / (1.0 / (AbsMax - AbsMin))) + AbsMin;
        }

        public static float Clamp(float Value, float Min, float Max)
        {
            return Math.Min(Math.Max(Value, Min), Max);
        }

        public static double Clamp(double Value, double Min, double Max)
        {
            return Math.Min(Math.Max(Value, Min), Max);
        }

        public static int Clamp(int Value, int Min, int Max)
        {
            return Math.Min(Math.Max(Value, Min), Max);
        }

        //APPROXIMATION! NOT 100% ACCURATE NOR WILL IT EVER BE A ONESIZEFIXALL SOLUTION
        public static float FixedToVariableTimestep(float value, float targetTimestep, float realTimestep)
        {
            return value * (targetTimestep / realTimestep);
        }

        //Borrowed from MonoXNA project
        public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
        {
            // All transformed to double not to lose precission
            // Otherwise, for high numbers of param:amount the result is NaN instead of Infinity
            double v1 = value1, v2 = value2, t1 = tangent1, t2 = tangent2, s = amount, result;
            double sCubed = s * s * s;
            double sSquared = s * s;

            if (amount == 0f)
                result = value1;
            else if (amount == 1f)
                result = value2;
            else
                result = (2.0f * v1 - 2.0f * v2 + t2 + t1) * sCubed +
                    (3.0f * v2 - 3.0f * v1 - 2.0f * t1 - t2) * sSquared +
                    t1 * s +
                    v1;
            return (float)result;
        }

        //Borrowed from MonoXNA project
        public static float SmoothStep(float value1, float value2, float amount)
        {
            // It is expected that 0 < amount < 1
            // If amount < 0, return value1
            // If amount > 1, return value2
            float result = Clamp(amount, 0f, 1f);
            result = Hermite(value1, 0f, value2, 0f, result);
            return result;
        }

        public static bool DoesDirectoryExist(string path)
        {
            DirectoryInfo Info;
            try
            {
                Info = new DirectoryInfo(path);
            }
            catch
            {
                return false;
            }
            return Info.Exists;
        }

        public static bool DoesFileExist(string file)
        {
            FileInfo Info;
            try
            {
                Info = new FileInfo(file);
            }
            catch
            {
                return false;
            }
            return Info.Exists;
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        /// <summary>
        /// Times how long it takes to execute methodToTime
        /// </summary>
        /// <param name="methodToTime">The method to time</param>
        /// <returns>Milliseconds elapsed executing methodToTime</returns>
        public static double TimeMethod(Action methodToTime)
        {
            Stopwatch sw = Stopwatch.StartNew();
            methodToTime();
            return sw.Elapsed.Ticks;
        }
    }
}
