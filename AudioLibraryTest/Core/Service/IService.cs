using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Core.Service
{
    interface IService
    {
        /// <summary>
        /// Information-only name of the service
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Called for all registered IServices when ServiceManager.Initialize is called
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called for all registered IServices when ServiceManager.Cleanup is called
        /// </summary>
        void Cleanup();
    }
}
