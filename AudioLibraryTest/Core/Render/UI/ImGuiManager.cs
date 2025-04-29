using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using player.Core.Input;
using player.Core.Service;
using player.Core.Settings;
using player.Shaders;
using player.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Log = player.Core.Logging.Logger;

namespace player.Core.Render.UI
{
    class ImGuiManager : IService
    {
        public string ServiceName => "ImGUI";

        ImGuiController controller;
        VisGameWindow vgs;
        List<char> pressedChars = new List<char>();
        double timeLastMouseMoved;
        float currentFrameOpacity = 0;
        float mouseLastY = 50;
        ConsoleManager console;
        bool debugSubMenuOpen = false;
        Settings settings;
        ImGuiIOPtr io;

        public event EventHandler OnRenderingGui;
        public event EventHandler OnDisplayingPopupMenu;
        public event EventHandler OnDrawingDebugSubmenu;
        public event EventHandler<ImGuiRenderingEventArgs> OnDrawingTitlebar;

        FpsLimitOverrideContext overrideContext = null;

        public void Initialize()
        {
            controller = new ImGuiController();
            controller.Create();

            vgs = VisGameWindow.ThisForm;

            io = ImGui.GetIO();

            ServiceManager.GetService<InputManager>().MouseMoveEventRaw += ImGuiController_MouseMoveEventRaw;
            settings = new Settings(ServiceManager.GetService<SettingsService>());
            console = ServiceManager.GetService<ConsoleManager>();
        }

        public void Cleanup()
        {

        }

        private void ImGuiController_MouseMoveEventRaw(object sender, MouseMoveEventArgs e)
        {
            timeLastMouseMoved = TimeManager.TimeD;
            mouseLastY = e.Y;
        }

        public void CharPress(char c)
        {
            lock (pressedChars)
            {
                pressedChars.Add(c);
            }
        }

        public void OnInputsProcessed()
        {
            char[] charList;
            lock (pressedChars)
            {
                charList = pressedChars.ToArray();
                pressedChars.Clear();
            }

            foreach (var c in charList)
            {
                io.AddInputCharacter(c);
            }
        }

        public void NewFrame()
        {
            if (settings.AutoHideGui.Value)
            {
                currentFrameOpacity = console.Visible ? 1f : (float)UtilityMethods.Clamp(1.0 - ((TimeManager.TimeD - (timeLastMouseMoved + 5f)) / 2.5), 0, 1); //smooth opacity to 0 after 5 seconds of inactivity over a 2.5 second fade duration
            }
            else
            {
                currentFrameOpacity = 1;
            }
            if (currentFrameOpacity > 0)
            {
                controller.NewFrame(vgs.Width, vgs.Height);
            }
        }
        public void Render()
        {
            if (mouseLastY < 50 || debugSubMenuOpen)
            {
                debugSubMenuOpen = false;
                if (ImGui.BeginMainMenuBar())
                {
                    if (ImGui.BeginMenu("Debug"))
                    {
                        debugSubMenuOpen = true;
                        ImGui.Checkbox("AutoHide GUI", ref settings.AutoHideGui.Value);
                        OnDrawingDebugSubmenu?.Invoke(this, EventArgs.Empty);
                        ImGui.EndMenu();
                    }

                    if (OnDrawingTitlebar != null)
                    {
                        foreach (var del in OnDrawingTitlebar.GetInvocationList())
                        {
                            var args = new ImGuiRenderingEventArgs();
                            del.DynamicInvoke(this, args);
                            if (args.SomethingWasDrawn)
                            {
                                debugSubMenuOpen = true;
                            }
                        }
                    }

                    ImGui.EndMainMenuBar();
                }
            }
            bool popupDrawn = false;
            if (currentFrameOpacity > 0)
            {
                try
                {
                    OnRenderingGui?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    Log.Log($"ImGuiRender : {e}");
                }



                var hovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
                var right = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
                if (!hovered && right)
                {
                    ImGui.OpenPopup("mainPopup");
                }

                if (ImGui.BeginPopup("mainPopup"))
                {
                    popupDrawn = true;
                    ImGui.Text($"player v{Program.VersionNumber}");
                    ImGui.Separator();
                    if (OnDisplayingPopupMenu != null)
                    {
                        var invoList = OnDisplayingPopupMenu.GetInvocationList();
                        for (int i = 0; i < invoList.Length - 1; i++)
                        {
                            invoList[i].DynamicInvoke(this, EventArgs.Empty);
                            ImGui.Separator();
                        }

                        if (invoList.Length > 0)
                        {
                            invoList[invoList.Length - 1].DynamicInvoke(this, EventArgs.Empty);
                        }
                    }

                    ImGui.EndPopup();
                }
            }

            controller.Render(currentFrameOpacity);

            if (controller.DidRender || popupDrawn)
            {
                if (overrideContext == null)
                {
                    overrideContext = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("ImGuiRender", FpsLimitOverride.Maximum);
                }
            }
            else
            {
                if (overrideContext != null)
                {
                    overrideContext.Dispose();
                    overrideContext = null;
                }
            }
        }

        class Settings
        {
            public SettingsAccessor<bool> AutoHideGui { get; private set; }

            public Settings(SettingsService svc)
            {
                AutoHideGui = svc.GetAccessor("IMGUI.AutoHideGui", true);
            }
        }

        bool[] lastMouseButtons = new bool[3];

        public bool ShouldSwallowInputEvent(InputManager.InputEventContainer evc)
        {
            var io = ImGui.GetIO();

            if (evc.IsKeyEventArg)
            {
                var ka = evc.KeyboardKeyEventArg;
                io.AddKeyEvent(InputHelper.OpenTKKeyToImGuiKey(ka.Key), evc.KeyPressed);

                return io.WantCaptureKeyboard;
            }
            else if (evc.IsMouseEventArg)
            {
                var ma = evc.MouseEventArg;

                io.AddMousePosEvent(ma.X, ma.Y);

                return io.WantCaptureMouse;
            }
            else if (evc.IsMouseSnapshot)
            {
                var mb = evc.MouseSnapshot;
                io.AddMousePosEvent(mb.Args.X, mb.Args.Y);

                //ffs its lrm instead of lmr
                var mb0D = mb.MouseState.LeftButton == ButtonState.Pressed;
                var mb2D = mb.MouseState.MiddleButton == ButtonState.Pressed;
                var mb1D = mb.MouseState.RightButton == ButtonState.Pressed;
                if (lastMouseButtons[0] != mb0D)
                {
                    lastMouseButtons[0] = mb0D;
                    io.AddMouseButtonEvent(0, mb0D);
                }
                if (lastMouseButtons[1] != mb1D)
                {
                    lastMouseButtons[1] = mb1D;
                    io.AddMouseButtonEvent(1, mb1D);
                }
                if (lastMouseButtons[2] != mb2D)
                {
                    lastMouseButtons[2] = mb2D;
                    io.AddMouseButtonEvent(2, mb2D);
                }



                return io.WantCaptureMouse;
            }
            else if (evc.IsMouseMoveEventArg)
            {
                var ma = evc.MouseMoveEventArg;

                io.AddMousePosEvent(ma.X, ma.Y);

                return io.WantCaptureMouse;
            }
            else if (evc.IsMouseWheelEventArg)
            {
                var mw = evc.MouseWheelEventArg;

                io.MouseWheel = mw.DeltaPrecise;

                io.AddMouseWheelEvent(0, mw.DeltaPrecise);

                return io.WantCaptureMouse;
            }

            return false;
        }

        class ImGuiController
        {
            int g_FontTexture;
            Stopwatch sw = new Stopwatch();
            int width;
            int height;
            TexturedQuadShader shader = new TexturedQuadShader();
            Primitives primitives;
            ImGuiIOPtr io;


            public bool DidRender { get; private set; }

            /// <summary>
            /// Constructs a new ImGuiController.
            /// </summary>
            public ImGuiController()
            {
                ImGui.CreateContext();
                primitives = ServiceManager.GetService<Primitives>();
                io = ImGui.GetIO();
            }

            public void NewFrame(int width, int height)
            {
                this.width = width; this.height = height;
                io.DisplaySize = new Vector2(width, height);
                io.DisplayFramebufferScale = new Vector2(1);


                io.DeltaTime = (float)sw.Elapsed.TotalSeconds;
                sw.Restart();

                ImGui.NewFrame();
            }

            public unsafe void Render(float opacity)
            {
                ImGui.Render();
                RenderDrawData(ImGui.GetDrawData(), width, height, opacity);
            }

            public unsafe void Create()
            {
                // Build texture atlas
                IntPtr pixels;
                io.Fonts.GetTexDataAsRGBA32(out pixels, out var width, out var height);

                // Create OpenGL texture
                g_FontTexture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, g_FontTexture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    width,
                    height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pixels);

                // Store the texture identifier in the ImFontAtlas substructure.
                io.Fonts.SetTexID(new IntPtr(g_FontTexture));

                // Cleanup (don't clear the input data if you want to append new fonts later)
                //io.Fonts->ClearInputData();
                io.Fonts.ClearTexData();
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            public unsafe void RenderDrawData(ImDrawDataPtr drawDataPtr, int displayW, int displayH, float opacity)
            {
                // We are using the OpenGL fixed pipeline to make the example code simpler to read!
                // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers.
                //System.Numerics.Vector4 clear_color = new System.Numerics.Vector4(114f / 255f, 144f / 255f, 154f / 255f, 1.0f);
                //GL.Viewport(0, 0, displayW, displayH);
                //GL.ClearColor(clear_color.X, clear_color.Y, clear_color.Z, clear_color.W);
                //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                if (drawDataPtr.CmdListsCount == 0 || opacity <= 0)
                {
                    DidRender = false;
                    return; //nothing to do :>
                }
                DidRender = true;

                int last_texture;
                GL.GetInteger(GetPName.TextureBinding2D, out last_texture);
                GL.PushAttrib(AttribMask.EnableBit | AttribMask.ColorBufferBit | AttribMask.TransformBit);
                //commented due to engine defaults 
                // GL.Enable(EnableCap.Blend);
                // GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                // GL.Disable(EnableCap.CullFace);
                // GL.Disable(EnableCap.DepthTest);
                GL.Enable(EnableCap.ScissorTest);
                GL.EnableClientState(ArrayCap.VertexArray);
                GL.EnableClientState(ArrayCap.TextureCoordArray);
                GL.EnableClientState(ArrayCap.ColorArray);

                shader.Activate();
                shader.Opacity = opacity;

                // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
                var io = ImGui.GetIO();

                // Setup orthographic projection matrix
                GL.MatrixMode(MatrixMode.Projection);
                GL.PushMatrix();
                GL.LoadIdentity();
                GL.Ortho(
                    0.0f,
                    io.DisplaySize.X / io.DisplayFramebufferScale.X,
                    io.DisplaySize.Y / io.DisplayFramebufferScale.Y,
                    0.0f,
                    -1.0f,
                    1.0f);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.PushMatrix();
                GL.LoadIdentity();

                // Render command lists
                const int VertexOffset = 0;
                const int TexOffset = 8;
                const int ColorOffset = 16;

                for (int n = 0; n < drawDataPtr.CmdListsCount; n++)
                {
                    var cmd_list = drawDataPtr.CmdLists[n];
                    byte* vtx_buffer = (byte*)cmd_list.VtxBuffer.Data;
                    ushort* idx_buffer = (ushort*)cmd_list.IdxBuffer.Data;
                    
                    GL.VertexPointer(2, VertexPointerType.Float, sizeof(ImDrawVert), new IntPtr(vtx_buffer + VertexOffset));
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(ImDrawVert), new IntPtr(vtx_buffer + TexOffset));
                    GL.ColorPointer(4, ColorPointerType.UnsignedByte, sizeof(ImDrawVert), new IntPtr(vtx_buffer + ColorOffset));

                    for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                    {
                        var pcmd = cmd_list.CmdBuffer[cmd_i].NativePtr;
                        if (pcmd->UserCallback != IntPtr.Zero)
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            GL.BindTexture(TextureTarget.Texture2D, pcmd->TextureId.ToInt32());
                            GL.Scissor(
                                (int)pcmd->ClipRect.X,
                                (int)(io.DisplaySize.Y - pcmd->ClipRect.W),
                                (int)(pcmd->ClipRect.Z - pcmd->ClipRect.X),
                                (int)(pcmd->ClipRect.W - pcmd->ClipRect.Y));
                            ushort[] indices = new ushort[pcmd->ElemCount];
                            for (int i = 0; i < indices.Length; i++) { indices[i] = idx_buffer[i]; }
                            GL.DrawElements(PrimitiveType.Triangles, (int)pcmd->ElemCount, DrawElementsType.UnsignedShort, new IntPtr(idx_buffer));
                        }
                        idx_buffer += pcmd->ElemCount;
                    }
                }

                // Restore modified state
                GL.DisableClientState(ArrayCap.ColorArray);
                GL.DisableClientState(ArrayCap.TextureCoordArray);
                GL.DisableClientState(ArrayCap.VertexArray);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.PopMatrix();
                GL.MatrixMode(MatrixMode.Projection);
                GL.PopMatrix();
                GL.PopAttrib();
                GL.Disable(EnableCap.ScissorTest);
                GL.BindTexture(TextureTarget.Texture2D, last_texture);
                GL.MatrixMode(MatrixMode.Modelview);
            }
        }
    }

    public class ImGuiRenderingEventArgs : EventArgs
    {
        public bool SomethingWasDrawn { get; private set; }

        public void DidRender()
        {
            SomethingWasDrawn = true;
        }
    }
}
