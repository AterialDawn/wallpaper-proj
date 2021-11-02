using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace player.Utility
{
    static class EasingMethods
    {
        //Borrowed from https://github.com/d3/d3-ease
        #region Helpers
        static double HalfPi = Math.PI / 2.0;
        static double Tau = 2.0 * Math.PI;
        static float b1 = 4f / 11f, b2 = 6f / 11f, b3 = 8f / 11f, b4 = 3f / 4f, b5 = 9f / 11f, b6 = 10f / 11f, b7 = 15f / 16f, b8 = 21f / 22f, b9 = 63f / 64f, b0 = 1f / b1 / b1; //Used in bounce
        #endregion

        #region Polynomial Easings
        public static float PolyIn(float time, float exponent = 3f)
        {
            return (float)Math.Pow(time, exponent);
        }

        public static float PolyOut(float time, float exponent = 3f)
        {
            return 1f - (float)Math.Pow(1.0 - time, exponent);
        }

        public static float PolyInOut(float time, float exponent = 3f)
        {
            return (float)((time *= 2f) <= 1 ? Math.Pow(time, exponent): 2.0 - Math.Pow(2.0 - time, exponent)) / 2f;
        }
        #endregion

        #region Quadriatic Easings
        public static float QuadIn(float time)
        {
            return time * time;
        }

        public static float QuadOut(float time)
        {
            return time * (2f - time);
        }

        public static float QuadInOut(float time)
        {
            return ((time *= 2f) <= 1f ? time * time : --time * (2f - time) + 1f) / 2f;
        }
        #endregion

        #region Cubic Easings
        public static float CubicIn(float time)
        {
            return time * time * time;
        }

        public static float CubicOut(float time)
        {
            return --time * time * time + 1f;
        }

        public static float CubicInOut(float time)
        {
            return ((time *= 2f) <= 1f ? time * time * time : (time -= 2f) * time * time + 2f) / 2f;
        }
        #endregion

        #region Sinusoidal Easings
        public static float SinIn(float time)
        {
            return (float)(1.0 - Math.Cos(time * HalfPi));
        }

        public static float SinOut(float time)
        {
            return (float)Math.Sin(time * HalfPi);
        }

        public static float SinInOut(float time)
        {
            return (float)((1.0 - Math.Cos(Math.PI * time)) / 2.0);
        }
        #endregion

        #region Exponential Easings
        public static float ExpIn(float time)
        {
            return (float)Math.Pow(2.0, 10.0 * time - 10.0);
        }

        public static float ExpOut(float time)
        {
            return 1f - (float)Math.Pow(2.0, -10.0 * time);
        }

        public static float ExpInOut(float time)
        {
            return ((time *= 2f) <= 1f ? (float)Math.Pow(2.0, 10.0 * time - 10.0) : 2f - (float)Math.Pow(2.0, 10.0 - 10.0 * time)) / 2f;
        }
        #endregion

        #region Circular Easings
        public static float CircleIn(float time)
        {
            return 1f - (float)Math.Sqrt(1.0 - time * time);
        }

        public static float CircleOut(float time)
        {
            return (float)Math.Sqrt(1.0 - --time * time);
        }

        public static float CircleInOut(float time)
        {
            return ((time *= 2f) <= 1f ? 1f - (float)Math.Sqrt(1.0 - time * time) : (float)Math.Sqrt(1.0 - (time -= 2f) * time) + 1f) / 2f;
        }
        #endregion

        #region Elastic Easings
        public static float ElasticIn(float time, float amplitude = 1f, float period = 0.3f)
        {
            double dPer = period / Tau;
            return amplitude * (float)Math.Pow(2.0, 10.0 * --time) * (float)Math.Sin((dPer * (float)Math.Asin(1.0 / amplitude) - time) / dPer);
        }

        public static float ElasticOut(float time, float amplitude = 1f, float period = 0.3f)
        {
            double dPer = period / Tau;
            return 1f - amplitude * (float)Math.Pow(2.0, -10.0 * time) * (float)Math.Sin((+time + dPer * Math.Asin(1.0 / amplitude)) / dPer);
        }

        public static float ElasticInOut(float time, float amplitude = 1f, float period = 0.3f)
        {
            double dPer = period / Tau;
            double s = dPer * Math.Asin(1.0 / amplitude);

            return ((time = time * 2f - 1f) < 0f
                  ? amplitude * (float)Math.Pow(2.0, 10.0 * time) * (float)Math.Sin((s - time) / dPer)
                  : 2f - amplitude * (float)Math.Pow(2.0, -10.0 * time) * (float)Math.Sin((s + time) / dPer)) / 2f;
        }
        #endregion

        #region Anticipatory Easings
        public static float BackIn(float time, float scale = 1.70158f)
        {
            return time * time * ((scale + 1f) * time - scale);
        }

        public static float BackOut(float time, float scale = 1.70158f)
        {
            return --time * time * ((scale + 1f) * time + scale) + 1f;
        }

        public static float BackInOut(float time, float scale = 1.70158f)
        {
            return ((time *= 2f) < 1f ? time * time * ((scale + 1f) * time - scale) : (time -= 2f) * time * ((scale + 1f) * time + scale) + 2f) / 2f;
        }
        #endregion

        #region Bounce Easings
        public static float BounceIn(float time)
        {
            return 1f - BounceOut(1f - time);
        }

        public static float BounceOut(float time)
        {
            return time < b1 ? b0 * time * time : time < b3 ? b0 * (time -= b2) * time + b4 : time < b6 ? b0 * (time -= b5) * time + b7 : b0 * (time -= b8) * time + b9;
        }

        public static float BounceInOut(float time)
        {
            return ((time *= 2f) <= 1f ? 1f - BounceOut(1f - time) : BounceOut(time - 1f) + 1f) / 2f;
        }
        #endregion
    }
}
