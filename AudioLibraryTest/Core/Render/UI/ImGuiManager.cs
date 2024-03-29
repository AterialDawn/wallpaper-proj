﻿using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using player.Core.Input;
using player.Core.Service;
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
        //bool anyItemInMenuOpened = false; //maybe will use later maybe
        double timeLastMouseMoved;
        float currentFrameOpacity = 0;
        ConsoleManager console;

        public event EventHandler OnRenderingGui;
        public event EventHandler OnDisplayingPopupMenu;

        FpsLimitOverrideContext overrideContext = null;

        public void Initialize()
        {
            controller = new ImGuiController();
            controller.Create();

            vgs = VisGameWindow.ThisForm;

            var io = ImGui.GetIO();

            io.KeyMap[GuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Key.Home;
            io.KeyMap[GuiKey.End] = (int)Key.End;
            io.KeyMap[GuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[GuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[GuiKey.A] = (int)Key.A;
            io.KeyMap[GuiKey.C] = (int)Key.C;
            io.KeyMap[GuiKey.V] = (int)Key.V;
            io.KeyMap[GuiKey.X] = (int)Key.X;
            io.KeyMap[GuiKey.Y] = (int)Key.Y;
            io.KeyMap[GuiKey.Z] = (int)Key.Z;

            ServiceManager.GetService<InputManager>().MouseMoveEventRaw += ImGuiController_MouseMoveEventRaw;
            console = ServiceManager.GetService<ConsoleManager>();
        }

        public void Cleanup()
        {

        }

        private void ImGuiController_MouseMoveEventRaw(object sender, MouseMoveEventArgs e)
        {
            timeLastMouseMoved = TimeManager.TimeD;
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
                ImGui.AddInputCharacter(c);
            }
        }

        public void NewFrame()
        {
            currentFrameOpacity = console.Visible ? 1f : (float)UtilityMethods.Clamp(1.0 - ((TimeManager.TimeD - (timeLastMouseMoved + 5f)) / 2.5), 0, 1); //smooth opacity to 0 after 5 seconds of inactivity over a 2.5 second fade duration
            if (currentFrameOpacity > 0)
            {
                controller.NewFrame(vgs.Width, vgs.Height);
            }
        }
        public void Render()
        {
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



                if (!ImGui.IsAnyWindowHovered() && ImGui.IsMouseClicked(1))
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

        public bool ShouldSwallowInputEvent(InputManager.InputEventContainer evc)
        {
            var io = ImGui.GetIO();

            if (evc.IsKeyEventArg)
            {
                var ka = evc.KeyboardKeyEventArg;
                io.KeysDown[(int)ka.Key] = ka.Pressed;

                io.AltPressed = ka.Alt;
                io.CtrlPressed = ka.Ctrl;
                io.ShiftPressed = ka.Shift;

                return io.WantCaptureKeyboard;
            }
            else if (evc.IsMouseEventArg)
            {
                var ma = evc.MouseEventArg;

                io.MousePosition = new Vector2(ma.X, ma.Y);

                return io.WantCaptureMouse;
            }
            else if (evc.IsMouseSnapshot)
            {
                var mb = evc.MouseSnapshot;

                io.MouseDown[0] = mb.MouseState.LeftButton == ButtonState.Pressed;
                io.MouseDown[2] = mb.MouseState.MiddleButton == ButtonState.Pressed;
                io.MouseDown[1] = mb.MouseState.RightButton == ButtonState.Pressed;

                io.MousePosition = new Vector2(mb.Args.X, mb.Args.Y);

                return io.WantCaptureMouse;
            }
            else if (evc.IsMouseMoveEventArg)
            {
                var ma = evc.MouseMoveEventArg;

                io.MousePosition = new Vector2(ma.X, ma.Y);

                return io.WantCaptureMouse;
            }
            else if (evc.IsMouseWheelEventArg)
            {
                var mw = evc.MouseWheelEventArg;

                io.MouseWheel = mw.DeltaPrecise;

                io.MousePosition = new Vector2(mw.X, mw.Y);

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


            public bool DidRender { get; private set; }

            /// <summary>
            /// Constructs a new ImGuiController.
            /// </summary>
            public ImGuiController()
            {
                primitives = ServiceManager.GetService<Primitives>();
            }

            public void NewFrame(int width, int height)
            {
                this.width = width; this.height = height;
                IO io = ImGui.GetIO();
                io.DisplaySize = new System.Numerics.Vector2(width, height);
                io.DisplayFramebufferScale = new System.Numerics.Vector2(1);


                io.DeltaTime = (float)sw.Elapsed.TotalSeconds;
                sw.Restart();
                //SDL.SDL_ShowCursor(io.MouseDrawCursor ? 0 : 1);

                ImGui.NewFrame();
            }

            public unsafe void Render(float opacity)
            {
                ImGui.Render();
                if (ImGuiNative.igGetIO()->RenderDrawListsFn == IntPtr.Zero)
                    RenderDrawData(ImGuiNative.igGetDrawData(), width, height, opacity);
            }

            public unsafe void Create()
            {
                IO io = ImGui.GetIO();

                // Build texture atlas
                FontTextureData texData = io.FontAtlas.GetTexDataAsRGBA32();

                // Create OpenGL texture
                g_FontTexture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, g_FontTexture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    texData.Width,
                    texData.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    new IntPtr(texData.Pixels));

                // Store the texture identifier in the ImFontAtlas substructure.
                io.FontAtlas.SetTexID(g_FontTexture);

                // Cleanup (don't clear the input data if you want to append new fonts later)
                //io.Fonts->ClearInputData();
                io.FontAtlas.ClearTexData();
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            public unsafe void RenderDrawData(DrawData* drawData, int displayW, int displayH, float opacity)
            {
                // We are using the OpenGL fixed pipeline to make the example code simpler to read!
                // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers.
                //System.Numerics.Vector4 clear_color = new System.Numerics.Vector4(114f / 255f, 144f / 255f, 154f / 255f, 1.0f);
                //GL.Viewport(0, 0, displayW, displayH);
                //GL.ClearColor(clear_color.X, clear_color.Y, clear_color.Z, clear_color.W);
                //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                if (drawData->CmdListsCount == 0 || opacity <= 0)
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

                for (int n = 0; n < drawData->CmdListsCount; n++)
                {
                    NativeDrawList* cmd_list = drawData->CmdLists[n];
                    byte* vtx_buffer = (byte*)cmd_list->VtxBuffer.Data;
                    ushort* idx_buffer = (ushort*)cmd_list->IdxBuffer.Data;

                    GL.VertexPointer(2, VertexPointerType.Float, sizeof(DrawVert), new IntPtr(vtx_buffer + DrawVert.PosOffset));
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(DrawVert), new IntPtr(vtx_buffer + DrawVert.UVOffset));
                    GL.ColorPointer(4, ColorPointerType.UnsignedByte, sizeof(DrawVert), new IntPtr(vtx_buffer + DrawVert.ColOffset));

                    for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
                    {
                        DrawCmd* pcmd = &(((DrawCmd*)cmd_list->CmdBuffer.Data)[cmd_i]);
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
