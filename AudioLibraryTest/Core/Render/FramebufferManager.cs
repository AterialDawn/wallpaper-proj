using player.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace player.Core.Render
{
    class FramebufferManager : IService
    {
        public string ServiceName => "Framebuffer Manager";

        private Stack<uint> fbStack = new Stack<uint>();

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
