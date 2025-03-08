using System;
using System.Collections.Generic;
using Log = player.Core.Logging.Logger;

namespace player.Core.Service
{
    static class ServiceManager
    {
        public static event EventHandler OnServicesInitialized;
        static Dictionary<string, IService> serviceList = new Dictionary<string, IService>();

        static ServiceManager()
        {
            Log.Log("ServiceManager initialized!");
        }

        public static T RegisterService<T>(T service) where T : IService
        {
            string serviceName = TypeToKeyName(service.GetType());
            if (serviceList.ContainsKey(serviceName)) throw new InvalidOperationException($"The service {service.ServiceName} is already registered!");
            Log.Log($"Registered service '{serviceName}'");
            serviceList.Add(serviceName, service);
            return service;
        }

        public static T GetService<T>() where T : IService
        {
            string serviceName = TypeToKeyName(typeof(T));
            if (!serviceList.ContainsKey(serviceName))
            {
                Log.Log($"GetService type {serviceName} is null!");
                return default(T); //oh boy
            }
            return (T)serviceList[serviceName];
        }

        public static void InitializeAllServices()
        {
            Log.Log("Initializing services!");
            foreach (var service in serviceList)
            {
                try
                {
                    service.Value.Initialize();
                }
                catch (Exception e)
                {
                    Log.Log($"Error initializing service {service.Key}, name {service.Value.ServiceName}.\n{e.ToString()}");
                    throw new ServiceInitializationException($"Unable to initialize service {service.Value.ServiceName}", e); //if a service fails to initialize, it should be a catastrophic failure.
                }
            }

            OnServicesInitialized?.Invoke(null, EventArgs.Empty);
        }

        public static void CleanupAllServices()
        {
            Log.Log("Cleaning up!");
            foreach (var service in serviceList)
            {
                try
                {
                    service.Value.Cleanup();
                }
                catch (Exception e)
                {
                    Log.Log($"Error cleaning up service {service.Key}, name {service.Value.ServiceName}.\n{e.ToString()}");
                }
            }
        }

        private static string TypeToKeyName(Type type)
        {
            return string.Format("{0}.{1}", type.Namespace, type.Name);
        }
    }
}
