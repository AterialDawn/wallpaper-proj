//#define SHOW_COPYRIGHT

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using player.Core.Audio;
using player.Core.Commands;
using player.Core.FFmpeg;
using player.Core.Input;
using player.Core.Render;
using player.Core.Render.UI;
using player.Core.Render.UI.Controls;
using player.Core.Service;
using player.Core.Settings;
using player.Shaders;
using player.Utility;
using player.Utility.DropTarget;
using player.Utility.Shader;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Log = player.Core.Logging.Logger;

#if DEBUG
using System.Linq;
using System.Runtime.InteropServices;
#endif

namespace player.Core
{
    public partial class VisGameWindow : GameWindow
    {
        internal static VisGameWindow ThisForm;
        internal static WallpaperMode FormWallpaperMode = WallpaperMode.None;
        internal static bool RTTEnabled = true;
        internal static bool WasapiMode = true;
        internal static event EventHandler OnBeforeThreadedRender;
        internal static event EventHandler OnAfterThreadedRender;
        internal static Vector2 RenderResolution { get; set; } = new Vector2();
        private uint MainFramebuffer;
        private uint MainRenderTexture;
        private uint MainDepthRenderBuffer;
        private Shader RTTQuadShader;

        private Thread renderingThread;
        private bool sizeUpdated = false;
        private object sizeLock = new object();
        private ManualResetEventSlim initCompleteEvent = new ManualResetEventSlim(false);
        internal FpsLimitHelper FpsLimiter = new FpsLimitHelper(5, 30, 60);

        private Primitives primitives;
        private VisRenderer visRenderer;
        private SoundDataProcessor soundDataProcessor;
        private FpsTracker fpsTracker;
        private InputManager inputManager;
        private UIManager uiManager;
        private ImGuiManager imGuiManager;
        private OLabel clockLabel;
        private DropTargetManager dropTarget;
        private FramebufferManager fbManager;

        public VisGameWindow(int newWidth, int newHeight)
            : base(newWidth, newHeight, GraphicsMode.Default)
        {
            ThisForm = this;
            Log.Log($"Player v{Program.VersionNumber} loading...");
            this.Title = "Aterial's Visualizer";

            dropTarget = new DropTargetManager(this.GetHandleOfGameWindow(true));

            if (FormWallpaperMode != WallpaperMode.None)
            {
                WallpaperUtils.EnableWallpaperMode();
                FpsLimiter.MaximumFps = 30; //win10 wallpaper reduces system responsiveness somewhat when set too high, cap it down to 30 fps
            }
            WindowsHotkeyUtil.Init();
            RenderResolution = new Vector2(newWidth, newHeight);
        }

        void RenderLoop(object unused)
        {
            ThreadedInit();

            Stopwatch renderWatch = Stopwatch.StartNew();

            if (ShadowplayUtil.AccessDenied)
            {
                ServiceManager.GetService<MessageCenterService>().ShowMessage("Attempted to kill shadowplay, but access was denied");
            }
            if (ShadowplayUtil.WasShadowplayKilled)
            {
                if (ShadowplayUtil.WasShadowplayRestarted)
                {
                    ServiceManager.GetService<MessageCenterService>().ShowMessage("Shadowplay was killed and restarted successfully!");
                }
                else
                {
                    ServiceManager.GetService<MessageCenterService>().ShowMessage("Shadowplay was killed but not restarted!");
                }
            }

            while (!IsExiting)
            {
                lock (sizeLock)
                {
                    if (sizeUpdated)
                    {
                        //FRAMEBUFFER
                        if (RTTEnabled)
                        {
                            GL.BindTexture(TextureTarget.Texture2D, MainRenderTexture);
                            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
                            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, MainDepthRenderBuffer);
                            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent32, Width, Height);
                            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                        }
                        //ENDFRAMEBUFFER
                        GL.Viewport(0, 0, Width, Height);

                        //TextRenderer.Instance.Initialize(Width, Height);
                        uiManager.Resize(Width, Height);
                        visRenderer.ResolutionChange(Width, Height);
                        RenderResolution = new Vector2(Width, Height);
                        sizeUpdated = false;
                    }
                }

                imGuiManager.NewFrame();
                inputManager.ProcessInputs();
                imGuiManager.OnInputsProcessed();

                ThreadedRendering(renderWatch.Elapsed.TotalSeconds);

                renderWatch.Restart();

                SwapBuffers();
                FpsLimiter.Sleep(renderWatch.Elapsed.TotalMilliseconds); //pass in how much time we spent swapping this frame (fps limiter subtracts this from actual work done
            }
        }

        void ThreadedRendering(double time)
        {
            //Main Render Loop
            TimeManager.Update(time);

            if (RTTEnabled)
            {
                fbManager.PushFramebuffer(FramebufferTarget.Framebuffer, MainFramebuffer);
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            MainThreadDelegator.ExecuteDelegates(InvocationTarget.BeforeRender);
            try
            {
                OnBeforeThreadedRender?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exc) { Log.Log($"OnBeforeThreadedRendering : {exc}"); }

            clockLabel.Text = $"{DateTime.Now.ToString("hh:mm:ss tt")}";
            visRenderer.Render(time);
            fpsTracker.Update();

            uiManager.Render(time);
            imGuiManager.Render();


            if (RTTEnabled)
            {
                fbManager.PopFramebuffer(FramebufferTarget.Framebuffer);
                GL.BindTexture(TextureTarget.Texture2D, MainRenderTexture);
                RTTQuadShader.Activate();

                primitives.QuadBuffer.Draw();
            }

            MainThreadDelegator.ExecuteDelegates(InvocationTarget.AfterRender);
            try
            {
                OnAfterThreadedRender?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exc2) { Log.Log($"OnAfterThreadedRender : {exc2}"); }
        }

        void ThreadedInit()
        {
            using (new ProfilingHelper("Threaded Renderer Initialization"))
            {
                MakeCurrent(); //We now own this context

                AddInitializableClasses();
                RegisterHotkeys();
                Log.Log($"Player v{Program.VersionNumber} loaded!");
                initCompleteEvent.Set();

                GL.ClearColor(0f, 0f, 0f, 0f);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0f, 1f, 0f, 1f, -1f, 1f);
                GL.Viewport(0, 0, Width, Height);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                GL.Disable(EnableCap.DepthTest);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                if (RTTEnabled)
                {
                    GL.GenFramebuffers(1, out MainFramebuffer);
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, MainFramebuffer);
                    GL.GenTextures(1, out MainRenderTexture);
                    GL.BindTexture(TextureTarget.Texture2D, MainRenderTexture);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
                    GL.GenRenderbuffers(1, out MainDepthRenderBuffer);
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, MainDepthRenderBuffer);
                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent32, Width, Height);
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, MainDepthRenderBuffer);
                    GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, MainRenderTexture, 0);
                    DrawBuffersEnum[] outVal = { DrawBuffersEnum.ColorAttachment0 };
                    GL.DrawBuffers(1, outVal);
                    Log.Log("FrameBuffer status = {0}", GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
                }

                LoadShader();
                uiManager.Initialize(Width, Height);

                ServiceManager.InitializeAllServices();

                if (WasapiMode)
                {
                    soundDataProcessor.SetSoundProviderSource(ServiceManager.GetService<WasapiSoundManager>());
                }

                if (FormWallpaperMode == WallpaperMode.None)
                {
                    BringToFront();
                }
                else
                {
                    Bounds = WallpaperUtils.WallpaperBounds;
                }

                //Everything is initialized, print registered commands via ConsoleManager
                ServiceManager.GetService<ConsoleManager>().PostInit();

                clockLabel = new OLabel("ClockLabel", "12:00:00 PM", QuickFont.QFontAlignment.Left, true);
                clockLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
                clockLabel.AutoSize = true;
                clockLabel.Enabled = ServiceManager.GetService<SettingsService>().GetSettingAs("Core.ClockEnabled", false);

                ServiceManager.GetService<TrayIconManager>().BeforeTrayIconShown += (s, e) =>
                {
                    e.ContextMenu.MenuItems.Add(toggleClockMenuItem);
                };
                toggleClockMenuItem.Click += (s, e) =>
                {
                    clockLabel.Enabled = !clockLabel.Enabled;
                    ServiceManager.GetService<SettingsService>().SetSetting("Core.ClockEnabled", clockLabel.Enabled);
                };

#if DEBUG
                if (Program.CLIParser.ActiveOptions.Where(opt => opt.Item1 == "GLDebug").Any())
                {
                    //add opengl debug logging on debug builds if -GLDebug is specified
                    _debugHandle = GCHandle.Alloc(_debugProcCallback);
                    GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
                    GL.Enable(EnableCap.DebugOutput);
                    GL.Enable(EnableCap.DebugOutputSynchronous);
                }
#endif
            }
        }

        MenuItem toggleClockMenuItem = new MenuItem("Toggle Clock");

        void AddInitializableClasses()
        {
            using (new ProfilingHelper("Service Registration"))
            {
                primitives = ServiceManager.RegisterService(new Primitives());
                ServiceManager.RegisterService(new SettingsService());
                ServiceManager.RegisterService(new WallpaperImageSettingsService());
                ServiceManager.RegisterService(new ConsoleManager());
                ServiceManager.RegisterService(new ShaderManager());
                ServiceManager.RegisterService(new FFMpegManager());
                ServiceManager.RegisterService(new MessageCenterService());
                ServiceManager.RegisterService(new TrayIconManager());
                if (WasapiMode)
                {
                    ServiceManager.RegisterService(new WasapiSoundManager());
                }
                fpsTracker = ServiceManager.RegisterService(new FpsTracker());
                visRenderer = ServiceManager.RegisterService(new VisRenderer());
                soundDataProcessor = ServiceManager.RegisterService(new SoundDataProcessor());
                ServiceManager.RegisterService(new WindowCommands());
                ServiceManager.RegisterService(new PlayerCommands());
                inputManager = ServiceManager.RegisterService(new InputManager());
                uiManager = ServiceManager.RegisterService(new UIManager());
                imGuiManager = ServiceManager.RegisterService(new ImGuiManager());
                fbManager = ServiceManager.RegisterService(new FramebufferManager());

                ServiceManager.RegisterService(dropTarget);
            }
        }

        void RegisterHotkeys()
        {
            inputManager.RegisterKeyHandler(Key.Escape, HotkeyHandler, false);
            inputManager.RegisterKeyHandler(Key.Right, HotkeyHandler, false);
            inputManager.RegisterKeyHandler(Key.Left, HotkeyHandler, false);
            inputManager.RegisterKeyHandler(Key.F, HotkeyHandler, false);
        }

        void HotkeyHandler(object sender, Key key, bool keyDown)
        {
            if (!keyDown) return; //We only care about key presses
            if (key == Key.Escape)
            {
                if (FormWallpaperMode != WallpaperMode.None) return; //Only allow escape on normal mode

                if (WindowState == WindowState.Fullscreen)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    this.Close();
                }
            }
            else if (key == Key.Right)
            {
                visRenderer.NextRenderer();
            }
            else if (key == Key.Left)
            {
                visRenderer.PreviousRenderer();
            }
            else if (key == Key.F)
            {
                if (FormWallpaperMode != WallpaperMode.None) return; //Only allow F toggle on normal mode

                WindowState = (WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            ShadowplayUtil.StartIfNeeded();

            //Initialize threaded loader context
            Context.MakeCurrent(null);
            using (new ProfilingHelper("Threaded Context Initialization"))
            {
                ThreadedLoaderContext.Instance.Initialize();
                ThreadedLoaderContext.Instance.WaitUntilContextIsReady();
            }

            renderingThread = new Thread(RenderLoop);
            renderingThread.IsBackground = true;
            renderingThread.Name = "Rendering Thread";
            renderingThread.Start();

            initCompleteEvent.Wait();
        }

        private void BringToFront()
        {
            Win32.SetWindowPos(this.GetHandleOfGameWindow(true), Win32.SetWindowPosLocationFlags.HWND_TOP, 0, 0, 0, 0, Win32.SetWindowPosFlags.IgnoreResize | Win32.SetWindowPosFlags.IgnoreMove);
        }

        private void LoadShader()
        {
            RTTQuadShader = new TexturedQuadShader();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            //After the first OnRenderFrame call, we take over the event loop so we can avoid GameWindow's "Catchup" system
            while (!IsExiting)
            {
                Win32.WaitMessage();
                ProcessEvents();
            }
        }

        #region InputManager overrides
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            //base.OnKeyDown(e);
            inputManager.KeyDown(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            //base.OnKeyUp(e);
            inputManager.KeyUp(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            //base.OnMouseDown(e);
            inputManager.MouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            //base.OnMouseUp(e);
            inputManager.MouseUp(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            //base.OnMouseWheel(e);
            inputManager.MouseWheel(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            //base.OnMouseMove(e);
            inputManager.MouseMove(e);
        }

        protected override void OnKeyPress(OpenTK.KeyPressEventArgs e)
        {
            //base.OnKeyPress(e);
            imGuiManager.CharPress(e.KeyChar);
        }
        #endregion

        protected override void OnResize(EventArgs e)
        {
            lock (sizeLock)
            {
                sizeUpdated = true;
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            WallpaperUtils.BeforeClose();
            base.OnClosed(e);
            ServiceManager.CleanupAllServices();
            Log.Log($"Player v{Program.VersionNumber} Shutting down!");
        }

#if DEBUG
        static DebugProc _debugProcCallback = DebugCallback;
        static GCHandle _debugHandle;
        private static void DebugCallback(DebugSource source, DebugType type, int id,
    DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            string messageString = Marshal.PtrToStringAnsi(message, length);
            Log.Log($"[{DateTime.Now.Ticks}] {severity} {type} | {messageString}");
        }
#endif
    }
}
