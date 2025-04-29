using OpenTK.Graphics.OpenGL;
using player.Core.Service;
using System;
using System.Collections.Generic;

namespace player.Core.Render
{
    class FramebufferManager : IService
    {
        public string ServiceName => "Framebuffer Manager";

        private Dictionary<int, Stack<uint>> fbStackDict = new Dictionary<int, Stack<uint>>();

        public void Cleanup()
        {
        }

        public void Initialize()
        {
        }

        public void PushFramebuffer(FramebufferTarget target, uint frameBuffer)
        {
            switch (target)
            {
                case FramebufferTarget.Framebuffer:
                    {
                        var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                        if (!fbStackDict.TryGetValue(threadId, out var fbStack))
                        {
                            fbStack = new Stack<uint>();
                            fbStackDict[threadId] = fbStack;
                        }
                        fbStack.Push(frameBuffer);
                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
                        break;
                    }
                default: throw new NotImplementedException("Not Implemented");
            }
        }

        public uint PopFramebuffer(FramebufferTarget target)
        {
            switch (target)
            {
                case FramebufferTarget.Framebuffer:
                    {
                        var fbStack = fbStackDict[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        var result = fbStack.Pop();
                        uint toBind = 0;
                        if (fbStack.Count > 0)
                        {
                            toBind = fbStack.Peek();
                        }
                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, toBind);
                        return result;
                    }
                default: throw new NotImplementedException("Not Implemented");
            }
        }
    }
}
