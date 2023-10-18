using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace player.Core.Render
{
    class FpsLimitHelper
    {
        class SleepDurationsContainer
        {
            public double WorkingTime { get; set; }
            public double SleepingTime { get; set; }
        }
        public double IdleFps { get; set; }
        public double MinimumFps { get; set; }
        public double MaximumFps { get; set; }
        public bool Enabled { get { return _enabled; } set { _enabled = value; if (!Enabled) EstimatedCPUUsage = 1f; } }
        private bool _enabled = true;
        public bool AllowOverrides { get; private set; }
        object timelock = new object();

        public float EstimatedCPUUsage { get; private set; }

        private object overrideLock = new object();
        double clockDrift = 0;
        Stopwatch stopwatch;
        Stopwatch swSleepLength;
        List<ConcreteOverrideContext> fpsLimitOverrides = new List<ConcreteOverrideContext>();
        Queue<SleepDurationsContainer> sleepDurations = new Queue<SleepDurationsContainer>();

        public FpsLimitHelper(double idleFps, double lowestFps, double highestFps, bool allowOverride = true)
        {
            stopwatch = Stopwatch.StartNew();
            IdleFps = idleFps;
            MinimumFps = lowestFps;
            MaximumFps = highestFps;
            AllowOverrides = allowOverride;
        }

        public void RenderNow()
        {
            lock (timelock) Monitor.PulseAll(timelock);
        }

        /// <summary>
        /// Sleeps the calling thread in order to limit it to Fps
        /// </summary>
        public void Sleep(double timeSpentSwapping = 0)
        {
            if (!Enabled) return;
            double msToSleep = CalculateInstantaneousSleepMs();
            double elapsedMillis = stopwatch.Elapsed.TotalMilliseconds;
            double nextUpdate = msToSleep - elapsedMillis + clockDrift;
            clockDrift = 0;
            if (nextUpdate < 0.0)
            {
                clockDrift = nextUpdate;
            }
            else
            {
                if (nextUpdate > msToSleep * 2) nextUpdate = msToSleep * 2; //cap delay, RenderNow can cause nextUpdate to go into extreme values.

                swSleepLength = Stopwatch.StartNew();
                //Thread.Sleep((int)nextUpdate);
                lock (timelock) { Monitor.Wait(timelock, (int)nextUpdate); }
                double actualSleep = swSleepLength.Elapsed.TotalMilliseconds;
                sleepDurations.Enqueue(new SleepDurationsContainer { WorkingTime = elapsedMillis - timeSpentSwapping, SleepingTime = actualSleep + timeSpentSwapping });
                UpdateCpuEstimate();
                clockDrift = nextUpdate - actualSleep;
            }
            stopwatch.Restart();
        }

        void UpdateCpuEstimate()
        {
            double targetHistoryLengthMs = 1000;
            double totalWorkTime = 0;
            double totalSleepTime = 0;
            foreach (var val in sleepDurations)
            {
                totalWorkTime += val.WorkingTime;
                totalSleepTime += val.SleepingTime;
            }

            while (totalWorkTime + totalSleepTime > targetHistoryLengthMs && sleepDurations.Count > 0)
            {
                var dq = sleepDurations.Dequeue();
                totalWorkTime -= dq.WorkingTime;
                totalSleepTime -= dq.SleepingTime;
            }
            if (sleepDurations.Count == 0)
            {
                EstimatedCPUUsage = 0;
                return;
            }

            EstimatedCPUUsage = (float)Math.Min(1f, Math.Max(0, totalWorkTime / totalSleepTime));
        }

        public FpsLimitOverrideContext OverrideFps(string overrideName, FpsLimitOverride fpsOverride, double customFps = 0.0)
        {
            if (!AllowOverrides) return new DummyContext();
            ConcreteOverrideContext overrideContext = new ConcreteOverrideContext(this, fpsOverride, overrideName, customFps);
            fpsLimitOverrides.Add(overrideContext);
            return overrideContext;
        }

        //Attempts to maintain fps as low as possible, unless an override well, overrides it.
        private double CalculateInstantaneousSleepMs()
        {
            double highestFpsLimit = IdleFps;
            bool unlimitedFps = false;
            if (fpsLimitOverrides.Count > 0)
            {
                lock (overrideLock)
                {
                    foreach (var overrider in fpsLimitOverrides)
                    {
                        switch (overrider.FpsLimitOverride)
                        {
                            case FpsLimitOverride.Minimum:
                                {
                                    if (MinimumFps > highestFpsLimit) highestFpsLimit = MinimumFps;
                                    break;
                                }
                            case FpsLimitOverride.Maximum:
                                {
                                    if (MaximumFps > highestFpsLimit) highestFpsLimit = MaximumFps;
                                    break;
                                }
                            case FpsLimitOverride.Custom:
                                {
                                    if (overrider.CustomFps > highestFpsLimit)
                                    {
                                        highestFpsLimit = overrider.CustomFps;
                                    }
                                    break;
                                }
                            case FpsLimitOverride.Unlimited:
                                {
                                    unlimitedFps = true;
                                    break;
                                }
                        }
                        if (unlimitedFps) break;
                    }
                }
            }
            if (unlimitedFps) return 0; //no sleep
            return 1000.0 / highestFpsLimit;
        }

        private void RemoveOverride(ConcreteOverrideContext context)
        {
            lock (overrideLock)
            {
                if (fpsLimitOverrides.Contains(context))
                {
                    fpsLimitOverrides.Remove(context);
                }
            }
        }

        private class ConcreteOverrideContext : FpsLimitOverrideContext
        {
            public string Name { get; private set; }
            public double CustomFps { get; private set; }
            public FpsLimitOverride FpsLimitOverride { get; private set; }

            private FpsLimitHelper parentHelper;

            internal ConcreteOverrideContext(FpsLimitHelper parent, FpsLimitOverride fpsOverride, string name, double customFps)
            {
                parentHelper = parent;
                FpsLimitOverride = fpsOverride;
                CustomFps = customFps;
                Name = name;
            }

            public void Dispose()
            {
                parentHelper.RemoveOverride(this);
            }
        }

        private class DummyContext : FpsLimitOverrideContext
        {
            public double CustomFps { get { return 0; } }
            public string Name { get { return "Dummy"; } }

            public FpsLimitOverride FpsLimitOverride { get { return FpsLimitOverride.Minimum; } }

            internal DummyContext() { }

            public void Dispose()
            {

            }
        }
    }

    interface FpsLimitOverrideContext : IDisposable
    {
        string Name { get; }
        FpsLimitOverride FpsLimitOverride { get; }
        double CustomFps { get; }
    }

    enum FpsLimitOverride
    {
        Minimum = 0, //wots the point LOL
        Maximum,
        Unlimited,
        Custom
    }
}
