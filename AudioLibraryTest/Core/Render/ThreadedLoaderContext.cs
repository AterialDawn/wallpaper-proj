using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using player.Core.Input;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Concurrent;
using Log = player.Core.Logging.Logger;
using System.Collections.Generic;

namespace player.Core.Render
{
    //This class provides a mean to load resources asynchrously from the main rendering thread into opengl
    //This is not an IInitializable because we need explicit initialization.
    class ThreadedLoaderContext
    {
        private static ThreadedLoaderContext _instance = new ThreadedLoaderContext();
        public static ThreadedLoaderContext Instance { get { return _instance; } }
        private ThreadedLoaderContext() { }

        private int ContextCount = 3;

        public delegate void ThreadedResourceLoaderCallback();
        
        private BlockingCollection<ThreadedResourceLoaderCallback> callbackCollection = new BlockingCollection<ThreadedResourceLoaderCallback>();
        private List<GLLoaderContext> contextList = new List<GLLoaderContext>();

        //Creates a context on an internal thread
        public void Initialize()
        {
            for (int i = 0; i < ContextCount; i++)
            {
                var curContext = new GLLoaderContext($"GLLoaderContext #{i + 1}", callbackCollection);
                curContext.Initialize();
                contextList.Add(curContext);
            }
        }

        public void WaitUntilContextIsReady()
        {
            foreach (var context in contextList) context.ContextReadyEvent.Wait();
        }

        public void ExecuteOnLoaderThread(ThreadedResourceLoaderCallback callback)
        {
            callbackCollection.Add(callback);
        }

        private class GLLoaderContext
        {
            public ManualResetEventSlim ContextReadyEvent = new ManualResetEventSlim(false);

            private INativeWindow window;
            private IGraphicsContext context;
            private Thread thread;
            private string contextName;
            private BlockingCollection<ThreadedResourceLoaderCallback> callbackCollection;

            public GLLoaderContext(string name, BlockingCollection<ThreadedResourceLoaderCallback> collection)
            {
                contextName = name;
                callbackCollection = collection;
            }

            public void Initialize()
            {
                thread = new Thread(PrivInitialize);
                thread.Name = contextName;
                thread.IsBackground = true;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            private void PrivInitialize()
            {
                window = new NativeWindow();
                context = new GraphicsContext(GraphicsMode.Default, window.WindowInfo);
                Log.Log($"{contextName} context created");
                context.MakeCurrent(window.WindowInfo);
                ContextReadyEvent.Set();
                Log.Log($"{contextName} context ready");

                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                ProcessCallbacks();
            }

            public void ProcessCallbacks()
            {
                foreach (var callback in callbackCollection.GetConsumingEnumerable())
                {
                    try
                    {
                        callback();
                    }
                    catch (Exception e)
                    {
                        Log.Log($"{contextName} exc!\n{e.ToString()}");
                    }
                }
            }
        }
    }
}
