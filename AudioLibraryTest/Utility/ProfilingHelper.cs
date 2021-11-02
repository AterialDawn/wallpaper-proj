using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Log = player.Core.Logging.Logger;

namespace player.Utility
{
    internal class ProfilingHelper : IDisposable
    {
        Stopwatch sw = new Stopwatch();
        string name;

        public ProfilingHelper(string profileName)
        {
            sw.Start();
            name = profileName;
        }

        public void Dispose()
        {
            sw.Stop();
            Log.Log($"Profile : {name} {sw.Elapsed.TotalMilliseconds:0.00}ms");
        }
    }
}
