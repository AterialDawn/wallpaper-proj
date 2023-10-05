namespace player.Core
{
    internal static class TimeManager
    {
        public static float Time { get { return (float)TimeD; } }
        public static double TimeD { get; private set; } = 0;

        public static float Delta { get { return (float)DeltaD; } }
        public static double DeltaD { get; private set; } = 0;

        public static long FrameNumber { get; private set; } = 0;

        internal static void Update(double time)
        {
            TimeD += time;
            DeltaD = time;
            FrameNumber++;
        }
    }
}
