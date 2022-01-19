using OpenTK;
using OpenTK.Graphics.OpenGL;
using player.Core;
using player.Core.Audio;
using player.Core.Input;
using player.Core.Render;
using player.Core.Render.UI.Controls;
using player.Core.Service;
using player.Core.Settings;
using player.Renderers.BarHelpers;
using player.Shaders;
using player.Utility;
using player.Utility.DropTarget;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Log = player.Core.Logging.Logger;

namespace player.Renderers
{
    partial class BarRenderer : VisualizerRendererBase
    {
        private const double LOWACTIVITYCUTOFF = 5.0; //After this many seconds, render background in full brightness

        public float SmoothingFactor = 0.995f;
        private float fromCenterBoost = 1.25f;
        private double lowActivityTime = 0;
        private float width = 1f / SoundDataProcessor.BarCount;
        private float verticalOffset = 1f / 360f;
        private bool renderLines = true;
        private bool renderBars = true;
        private BarShader shader;
        private Primitives primitives;
        private DropTargetManager dropManager;
        private VertexFloatBuffer barBuffer;
        private VertexFloatBuffer barLineBuffer;
        private VertexFloatBuffer barBufferInv;
        private VertexFloatBuffer barLineBufferInv;
        private BackgroundController backgroundController;
        private FpsLimitOverrideContext fpsOverride = null;
        private BarDrawMode drawMode = BarDrawMode.FromCenter;
        private SettingsAccessor<InterpolationMode> interpolateSetting;
        private SettingsAccessor<bool> renderBarsSetting;
        private SettingsAccessor<bool> renderLinesSetting;
        private OLabel loadingLabel;
        private FpsLimitOverrideContext manualOverride = null;

        private TrayIconManager trayManager;
        private MenuItem nextWallpaper = new MenuItem("Next Wallpaper");
        private MenuItem toggleRotationMenuItem = new MenuItem("Toggle Rotation");
        private MenuItem loadWallpaper = new MenuItem("Load Wallpaper...");

        private ImGuiHandler imGuiHandler;

        public override SoundDataTypes RequiredDataType { get { return SoundDataTypes.BarData; } }

        public override string VisualizerName { get { return "Spectrum"; } }

        public override Vector2 Resolution { get; set; }

        public BackgroundScalingMode ScalingMode { get; set; }

        public BarRenderer()
        {
            ScalingMode = BackgroundScalingMode.Fit;
            primitives = ServiceManager.GetService<Primitives>();
            SettingsService settings = ServiceManager.GetService<SettingsService>();
            settings.OnSettingsReloaded += Settings_OnSettingsReloaded;
            trayManager = ServiceManager.GetService<TrayIconManager>();

            interpolateSetting = settings.GetAccessor("bar.interpolate", InterpolationMode.Automatic);
            renderBarsSetting = settings.GetAccessor("bar.renderbar", false);
            renderBars = renderBarsSetting.Get();
            renderLinesSetting = settings.GetAccessor("bar.renderlines", false);
            renderLines = renderLinesSetting.Get();
            shader = new BarShader();
            InitVBO();
            loadingLabel = new OLabel("Loading Label", "Loading Backgrounds...", QuickFont.QFontAlignment.Centre);
            loadingLabel.Enabled = true;
            backgroundController = new BackgroundController(shader);
            backgroundController.InitialBackgroundLoadComplete += (s, e) => { loadingLabel.Dispose(); loadingLabel = null; };
            backgroundController.UpdateWindowResolution(new Vector2(VisGameWindow.ThisForm.Width, VisGameWindow.ThisForm.Height));
            dropManager = ServiceManager.GetService<DropTargetManager>();
            ConsoleManager consoleManager = ServiceManager.GetService<ConsoleManager>();
            consoleManager.RegisterCommandHandler("bar:addsourcepath", AddSourcePathCommand);
            consoleManager.RegisterCommandHandler("bar:removesourcepath", RemoveSourcePathCommand);
            consoleManager.RegisterCommandHandler("bar:changebackgrounddelay", ChangeBackgroundDelayCommand);
            consoleManager.RegisterCommandHandler("bar:rescansourcepaths", RescanSourcePathsCommand);
            consoleManager.RegisterCommandHandler("bar:opencurrent", (_1,_2) =>
            {
                backgroundController.OpenCurrentWallpaper();
            });
            consoleManager.RegisterCommandHandler("bar:printcurrent", (_1,_2) =>
            {
                DisplayFadeoutMessage($"Path : {backgroundController.GetCurrentWallpaperPath()}");
            });

            nextWallpaper.Click += NextWallpaper_Click;
            toggleRotationMenuItem.Click += ToggleRotationMenuItem_Click;
            loadWallpaper.Click += LoadWallpaper_Click;

            if (renderBars || renderLines)
            {
                fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("BarRenderer", FpsLimitOverride.Minimum);
            }

            imGuiHandler = new ImGuiHandler(this);
        }

        private void Settings_OnSettingsReloaded(object sender, EventArgs e)
        {
            renderBars = renderBarsSetting.Get();
            renderLines = renderLinesSetting.Get();
            backgroundController.BackgroundFactory.Initialize();
            backgroundController.NewBackground();
        }

        public override void Activated()
        {
            SoundDataProcessor.SetDataPostProcessDelegate(SoundDataProcessorPostProcess);
            ServiceManager.GetService<InputManager>().KeyStateEvent += Instance_KeyStateEvent;
            fromCenterBoost = ServiceManager.GetService<SettingsService>().GetSettingAs<float>("FromCenterBoost", 1.25f);
            dropManager.OnDragEnter += BarRenderer_OnDragEnter;
            dropManager.OnDragOver += BarRenderer_OnDragOver;
            dropManager.OnDragDrop += BarRenderer_OnDragDrop;
            trayManager.BeforeTrayIconShown += TrayManager_BeforeTrayIconShown;
            backgroundController.OnActivate();
        }
        
        public override void Deactivated()
        {
            SoundDataProcessor.SetDataPostProcessDelegate(null); //Reset the delegate
            ServiceManager.GetService<InputManager>().KeyStateEvent -= Instance_KeyStateEvent;
            dropManager.OnDragEnter -= BarRenderer_OnDragEnter;
            dropManager.OnDragOver -= BarRenderer_OnDragOver;
            dropManager.OnDragDrop -= BarRenderer_OnDragDrop;
            trayManager.BeforeTrayIconShown -= TrayManager_BeforeTrayIconShown;
            backgroundController.OnDeactivate();
        }

        private void CheckFpsOverride()
        {
            if (!renderLines && !renderBars)
            {
                if (fpsOverride != null)
                {
                    fpsOverride.Dispose();
                    fpsOverride = null;
                }
            }
            else
            {
                if (fpsOverride == null)
                {
                    fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("BarRenderer", FpsLimitOverride.Minimum);
                }
            }
        }

        private void Instance_KeyStateEvent(object sender, KeyStateChangedEventArgs e)
        {
            if (!e.Pressed) return;
            switch (e.Key)
            {
                case OpenTK.Input.Key.L:
                    {
                        renderLines = !renderLines;
                        renderLinesSetting.Set(renderLines);
                        DisplayFadeoutMessage(renderLines ? "Rendering Lines" : "Not Rendering Lines");
                        CheckFpsOverride();
                        break;
                    }
                case OpenTK.Input.Key.B:
                    {
                        renderBars = !renderBars;
                        renderBarsSetting.Set(renderBars);
                        DisplayFadeoutMessage(renderBars ? "Rendering Bars" : "Not Rendering Bars");
                        CheckFpsOverride();
                        break;
                    }
                case OpenTK.Input.Key.K:
                    {
                        ToggleKeepRotation();
                        break;
                    }
                case OpenTK.Input.Key.I:
                    {
                        RotateInterpolations();
                        break;
                    }
                case OpenTK.Input.Key.X:
                    {
                        if (manualOverride == null)
                        {
                            manualOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("X Key Override", FpsLimitOverride.Maximum);
                            DisplayFadeoutMessage("Enabling maximum fps override");
                        }
                        else
                        {
                            manualOverride.Dispose();
                            manualOverride = null;
                            DisplayFadeoutMessage("Maximum fps override disabled!");
                        }
                        break;
                    }
                case OpenTK.Input.Key.N:
                    {
                        NewWallpaper(e.Ctrl);
                        break;
                    }

                case OpenTK.Input.Key.R:
                    {
                        switch (drawMode)
                        {
                            case BarDrawMode.FromCenter:
                                {
                                    drawMode = BarDrawMode.TopAndBottom;
                                    SoundDataProcessor.SetDataPostProcessDelegate(SoundDataProcessorPostProcess);
                                    break;
                                }
                            case BarDrawMode.TopAndBottom:
                                {
                                    drawMode = BarDrawMode.FromCenter;
                                    SoundDataProcessor.SetDataPostProcessDelegate(null);
                                    break;
                                }
                        }
                        DisplayFadeoutMessage($"Bar Mode set to {drawMode.ToString()}");
                        break;
                    }

                case OpenTK.Input.Key.O:
                    {
                        if (e.Ctrl)
                        {
                            backgroundController.OpenCurrentWallpaper();
                        }
                        break;
                    }
                case OpenTK.Input.Key.P:
                    {
                        DisplayFadeoutMessage($"Path : {backgroundController.GetCurrentWallpaperPath()}");
                        break;
                    }
            }
        }

        void NewWallpaper(bool previous, bool disableTransition = false)
        {
            if (backgroundController.IsTransitioning())
            {
                backgroundController.EndTransition();
                DisplayFadeoutMessage("Ending Transition");
                return;
            }
            if (previous)
            {
                if (!backgroundController.PreviousBackground(disableTransition)) return;
                DisplayFadeoutMessage("Last Background");
            }
            else
            {
                if (!backgroundController.NewBackground(disableTransition)) return;
                DisplayFadeoutMessage("Skipping Background");
            }
        }

        void ToggleKeepRotation()
        {
            backgroundController.KeepCurrentBackground = !backgroundController.KeepCurrentBackground;
            DisplayFadeoutMessage(backgroundController.KeepCurrentBackground ? "Keeping Wallpaper" : "Restoring Rotation");
        }

        private void TrayManager_BeforeTrayIconShown(object sender, TrayIconManager.TrayIconManagerEventArgs e)
        {
            e.ContextMenu.MenuItems.Add(nextWallpaper);
            e.ContextMenu.MenuItems.Add(toggleRotationMenuItem);
            e.ContextMenu.MenuItems.Add(loadWallpaper);
        }

        private void NextWallpaper_Click(object sender, EventArgs e)
        {
            NewWallpaper(false);
        }

        private void ToggleRotationMenuItem_Click(object sender, EventArgs e)
        {
            ToggleKeepRotation();
        }

        private void LoadWallpaper_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Title = "Select a loadable background" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (backgroundController.ImmediatelyLoadNewWallpaper(ofd.FileName))
                    {
                        DisplayFadeoutMessage($"{Path.GetFileName(ofd.FileName)} loaded!");
                    }
                    else
                    {
                        DisplayFadeoutMessage("Unable to load image");
                    }
                }
            }
        }

        #region CommandImplementation
        private void AddSourcePathCommand(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length < 1)
            {
                Log.Log("No directory specified!");
                return;
            }

            string pathToAdd = args.Arguments[0];
            if (backgroundController.BackgroundFactory.AddSourceFolder(pathToAdd))
            {
                Log.Log("Added directory '{0}'!", pathToAdd);
            }
            else
            {
                Log.Log("Unable to add directory '{0}'", pathToAdd);
            }
        }

        private void RemoveSourcePathCommand(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length < 1)
            {
                Log.Log("No directory specified!");
                return;
            }

            string pathToRemove = args.Arguments[0];
            if (backgroundController.BackgroundFactory.RemoveSourceFolder(pathToRemove))
            {
                Log.Log("Removed directory '{0}'!", pathToRemove);
            }
            else
            {
                Log.Log("Unable to remove directory '{0}'", pathToRemove);
            }
        }
        
        private void ChangeBackgroundDelayCommand(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length < 1)
            {
                Log.Log("No delay specified!");
                return;
            }

            float newDelay = 0f;
            if (!float.TryParse(args.Arguments[0], out newDelay))
            {
                Log.Log("Invalid delay specified! Only floating-point numbers are allowed");
                return;
            }

            backgroundController.ChangeBackgroundDuration(newDelay);

            Log.Log("Set delay to {0} seconds", newDelay);
        }

        private void RescanSourcePathsCommand(object sender, ConsoleLineReadEventArgs args)
        {
            Log.Log("Rescanning all sources");
            backgroundController.BackgroundFactory.RescanAllSources();
        }
        #endregion

        private void RotateInterpolations()
        {
            InterpolationMode currentInterpolation = interpolateSetting.Get();
            switch (currentInterpolation)
            {
                case InterpolationMode.Automatic:
                    {
                        currentInterpolation = InterpolationMode.Bicubic;
                        shader.SetPrimaryInterpolationType(BarShader.InterpolationType.BiCubic);
                        shader.SetSecondaryInterpolationType(BarShader.InterpolationType.BiCubic);
                        break;
                    }
                case InterpolationMode.Bicubic:
                    {
                        currentInterpolation = InterpolationMode.Bilinear;
                        shader.SetPrimaryInterpolationType(BarShader.InterpolationType.BiLinear);
                        shader.SetSecondaryInterpolationType(BarShader.InterpolationType.BiLinear);
                        break;
                    }
                case InterpolationMode.Bilinear:
                    {
                        currentInterpolation = InterpolationMode.BSpline;
                        shader.SetPrimaryInterpolationType(BarShader.InterpolationType.BSpline);
                        shader.SetSecondaryInterpolationType(BarShader.InterpolationType.BSpline);
                        break;
                    }
                case InterpolationMode.BSpline:
                    {
                        currentInterpolation = InterpolationMode.Catmull;
                        shader.SetPrimaryInterpolationType(BarShader.InterpolationType.CatmullRom);
                        shader.SetSecondaryInterpolationType(BarShader.InterpolationType.CatmullRom);
                        break;
                    }
                case InterpolationMode.Catmull:
                    {
                        currentInterpolation = InterpolationMode.None;
                        shader.SetPrimaryInterpolationType(BarShader.InterpolationType.None);
                        shader.SetSecondaryInterpolationType(BarShader.InterpolationType.None);
                        break;
                    }
                case InterpolationMode.None:
                    {
                        currentInterpolation = InterpolationMode.Automatic;
                        break;
                    }
            }

            DisplayFadeoutMessage($"Interpolation set to {currentInterpolation}");
            interpolateSetting.Set(currentInterpolation);
        }

        private void DisplayFadeoutMessage(string message)
        {
            ServiceManager.GetService<MessageCenterService>().ShowMessage(message);
        }

        //Makes sure bar values don't clip into the opposing bar's space
        private void SoundDataProcessorPostProcess(float[] barValues)
        {
            int indexSubtractend = SoundDataProcessor.BarCount - 1;
            for(int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                int opposingBarIndex = indexSubtractend - i;
                if (barValues[i] + barValues[opposingBarIndex] > 1f) barValues[i] = 1f - barValues[opposingBarIndex];
            }
        }

        public override void ResolutionUpdated()
        {
            verticalOffset = (1f / Resolution.Y) / 2f; //dividing the result by 2 makes the inverted line look a lot cleaner.

            if(loadingLabel != null) loadingLabel.Location = new System.Drawing.PointF(Resolution.X / 2f, Resolution.Y / 2f);

            backgroundController.UpdateWindowResolution(Resolution);
        }

        public override void Deinitialize()
        {
            barBuffer.Dispose();
            barBufferInv.Dispose();
            barLineBuffer.Dispose();
            barLineBufferInv.Dispose();

            backgroundController.Destroy();
        }

        private void InitVBO()
        {
            barBuffer = new VertexFloatBuffer(VertexFormat.XY, 6 * SoundDataProcessor.BarCount, BufferUsageHint.DynamicDraw);    //double triangle, 6 components per bar
            barBufferInv = new VertexFloatBuffer(VertexFormat.XY, 6 * SoundDataProcessor.BarCount, BufferUsageHint.DynamicDraw); //double triangle, 6 components per bar
            barLineBuffer = new VertexFloatBuffer(VertexFormat.XY, SoundDataProcessor.BarCount, BufferUsageHint.DynamicDraw, PrimitiveType.LineStrip);
            barLineBufferInv = new VertexFloatBuffer(VertexFormat.XY, SoundDataProcessor.BarCount, BufferUsageHint.DynamicDraw, PrimitiveType.LineStrip);
            
            barBuffer.Load();
            barBufferInv.Load();
            barLineBuffer.Load();
            barLineBufferInv.Load();
        }

        public override void Render(double time)
        {
            shader.Activate();
            backgroundController.Update(time);
            shader.Activate();
            if(interpolateSetting.Get() != InterpolationMode.None) UpdateShaderUniforms(); //update interpolation uniforms only if interpolation is enabled

            shader.SetTexturing(true);

            UpdateBackgroundSaturation(time);

            //Render background
            primitives.QuadBuffer.Draw();

            if (renderBars)
            {
                switch (drawMode)
                {
                    case BarDrawMode.TopAndBottom: DrawBars(); break;
                    case BarDrawMode.FromCenter: DrawBarsFromCenter(); break;
                }
            }
            shader.Activate();
            if (renderLines && lowActivityTime < LOWACTIVITYCUTOFF)
            {
                switch (drawMode)
                {
                    case BarDrawMode.TopAndBottom: RenderLinesOnBars(); break;
                    case BarDrawMode.FromCenter: RenderLinesOnBarsFromCenter(); break;
                }
            }
        }

        private void UpdateShaderUniforms()
        {
            shader.SetPrimarySize(backgroundController.PrimaryTextureResolution);
            shader.SetSecondarySize(backgroundController.SecondaryTextureResolution);

            if (interpolateSetting.Get() == InterpolationMode.Automatic) AutomaticInterpolation();
            
        }

        private void AutomaticInterpolation()
        {
            float primarySizeScale = (backgroundController.PrimaryTextureResolution.X * backgroundController.PrimaryTextureResolution.Y) / (Resolution.X * Resolution.Y);
            if (primarySizeScale < 0.95f) //5% too small, trigger upscaling filtering
            {
                shader.SetPrimaryInterpolationType(BarShader.InterpolationType.CatmullRom); //dont resample if we're the same res
            }
            else if (primarySizeScale > 1.05f) //5% too large, trigger downscaling
            {
                shader.SetPrimaryInterpolationType(BarShader.InterpolationType.BiCubic);
            }
            else
            {
                shader.SetPrimaryInterpolationType(BarShader.InterpolationType.None);
            }

            if (backgroundController.SecondaryTextureResolution.X == 0 && backgroundController.SecondaryTextureResolution.Y == 0) return; //dont bother if no texture to resample
            float secondarySizeScale = (backgroundController.SecondaryTextureResolution.X * backgroundController.SecondaryTextureResolution.Y) / (Resolution.X * Resolution.Y);

            if (secondarySizeScale < 0.95f) //5% too small, trigger upscaling filtering
            {
                shader.SetSecondaryInterpolationType(BarShader.InterpolationType.CatmullRom); //dont resample if we're the same res
            }
            else if (secondarySizeScale > 1.05f) //5% too large, trigger downscaling
            {
                shader.SetSecondaryInterpolationType(BarShader.InterpolationType.BiCubic);
            }
            else
            {
                shader.SetSecondaryInterpolationType(BarShader.InterpolationType.None);
            }
        }

        private void UpdateBackgroundSaturation(double time)
        {
            float Saturation = 0.7f + (SoundDataProcessor.AveragedVolume * .15f);
            if (!renderBars)
            {
                Saturation = 1f;
            }
            else if (SoundDataProcessor.AveragedVolume < 0.005) //CHANGE THIS TO BE ANIMATED INSTEAD OF SNAPPING TO FULL BRIGHTNESS
            {
                lowActivityTime += time;
                if (lowActivityTime > LOWACTIVITYCUTOFF)
                {
                    Saturation = 1f; //Full saturated background if low activity for 15s
                }
            }
            else
            {
                lowActivityTime = 0;
            }

            shader.SetSaturation(Saturation);
        }

        private void DrawBars()
        {
            shader.SetSaturation(1f);

            barBuffer.Clear();
            barBufferInv.Clear();

            //Render all bars from bottom-up
            float NextX;
            float CurrentX;
            float CurrentY;
            float LastY = SoundDataProcessor.BarValues[0];
            for (int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                NextX = ((float)i * width) + width;
                CurrentX = (float)i * width;
                CurrentY = SoundDataProcessor.BarValues[i];

                float xu1 = CurrentX;
                float yv1 = 0f;

                float xu2 = CurrentX;
                float yv2 = LastY;

                float xu3 = NextX;
                float yv3 = CurrentY;

                float xu4 = NextX;
                float yv4 = 0f;

                barBuffer.AddVertex(xu1, yv1);
                barBuffer.AddVertex(xu2, yv2);
                barBuffer.AddVertex(xu3, yv3);
                barBuffer.AddVertex(xu1, yv1);
                barBuffer.AddVertex(xu3, yv3);
                barBuffer.AddVertex(xu4, yv4);
                LastY = CurrentY;
            }
            barBuffer.Reload();

            //Render all bars from top-down
            LastY = SoundDataProcessor.BarValues[0];
            for (int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                NextX = 1f - ((float)i * width) + width;
                CurrentX = 1f - (i * width);
                CurrentY = SoundDataProcessor.BarValues[i];

                float xu1 = CurrentX;
                float yv1 = 1f;

                float xu2 = CurrentX;
                float yv2 = 1f - CurrentY;

                float xu3 = NextX;
                float yv3 = 1f - LastY;

                float xu4 = NextX;
                float yv4 = 1f;

                barBufferInv.AddVertex(xu1, yv1);
                barBufferInv.AddVertex(xu2, yv2);
                barBufferInv.AddVertex(xu3, yv3);
                barBufferInv.AddVertex(xu1, yv1);
                barBufferInv.AddVertex(xu3, yv3);
                barBufferInv.AddVertex(xu4, yv4);

                LastY = CurrentY;
            }
            barBufferInv.Reload();

            barBuffer.Draw();
            barBufferInv.Draw();
        }

        private void DrawBarsFromCenter()
        {
            shader.SetSaturation(1f);

            barBuffer.Clear();
            barBufferInv.Clear();

            //Render all bars from bottom-up
            float NextX;
            float CurrentX;
            float CurrentY;
            float LastY = 0.5f + (SoundDataProcessor.BarValues[0] * (0.5f * fromCenterBoost));
            for (int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                NextX = ((float)i * width) + width;
                CurrentX = (float)i * width;
                CurrentY = 0.5f + (SoundDataProcessor.BarValues[i] * (0.5f * fromCenterBoost));

                float xu1 = CurrentX;
                float yv1 = 0.5f;

                float xu2 = CurrentX;
                float yv2 = LastY;

                float xu3 = NextX;
                float yv3 = CurrentY;

                float xu4 = NextX;
                float yv4 = 0.5f;

                barBuffer.AddVertex(xu1, yv1);
                barBuffer.AddVertex(xu2, yv2);
                barBuffer.AddVertex(xu3, yv3);
                barBuffer.AddVertex(xu1, yv1);
                barBuffer.AddVertex(xu3, yv3);
                barBuffer.AddVertex(xu4, yv4);
                LastY = CurrentY;
            }
            barBuffer.Reload();

            //Render all bars from top-down
            LastY = 1f - (0.5f + (SoundDataProcessor.BarValues[0] * (0.5f * fromCenterBoost)));
            for (int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                NextX = ((float)i * width) + width;
                CurrentX = (float)i * width;
                CurrentY = 1f - (0.5f + (SoundDataProcessor.BarValues[i] * (0.5f * fromCenterBoost)));

                float xu1 = CurrentX;
                float yv1 = 0.5f;

                float xu2 = CurrentX;
                float yv2 = LastY;

                float xu3 = NextX;
                float yv3 = CurrentY;

                float xu4 = NextX;
                float yv4 = 0.5f;

                barBufferInv.AddVertex(xu1, yv1);
                barBufferInv.AddVertex(xu2, yv2);
                barBufferInv.AddVertex(xu3, yv3);
                barBufferInv.AddVertex(xu1, yv1);
                barBufferInv.AddVertex(xu3, yv3);
                barBufferInv.AddVertex(xu4, yv4);
                LastY = CurrentY;
            }
            barBufferInv.Reload();

            barBuffer.Draw();
            barBufferInv.Draw();
        }

        private void RenderLinesOnBars()
        {
            shader.SetTexturing(false);

            barLineBuffer.Clear();
            barLineBufferInv.Clear();

            for (int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                float x = (((float)i + 1f) * width);
                float y = SoundDataProcessor.BarValues[i];
                barLineBuffer.AddVertex(x, y);
            }

            barLineBuffer.Reload();

            for (int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                float x = 1f - ((float)i * (float)width);
                float y = 1f + verticalOffset - SoundDataProcessor.BarValues[i];

                barLineBufferInv.AddVertex(x, y);
            }

            barLineBufferInv.Reload();

            barLineBuffer.Draw();
            barLineBufferInv.Draw();
        }

        private void RenderLinesOnBarsFromCenter()
        {
            shader.SetTexturing(false);

            barLineBuffer.Clear();
            barLineBufferInv.Clear();

            for (int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                float x = (((float)i + 1f) * width);
                float y = 0.5f + (SoundDataProcessor.BarValues[i] * (0.5f * fromCenterBoost));
                barLineBuffer.AddVertex(x, y);
            }

            barLineBuffer.Reload();

            for (int i = 0; i < SoundDataProcessor.BarCount; i++)
            {
                float x = (((float)i + 1f) * width);
                float y = 1f - (0.5f + (SoundDataProcessor.BarValues[i] * (0.5f * fromCenterBoost)));
                barLineBufferInv.AddVertex(x, y);
            }

            barLineBufferInv.Reload();

            barLineBuffer.Draw();
            barLineBufferInv.Draw();
        }

        #region DragDropHandler

        private void BarRenderer_OnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Effect != DragDropEffects.Copy) return;

            object[] data = e.Data.GetData("FileDrop") as object[];
            if (data != null)
            {
                if (data.Length == 1 && data[0] is string)
                {
                    string path = (string)data[0];
                    Log.Log($"Loading {path} (DragDrop)");
                    DisplayFadeoutMessage($"DragDrop {path}");
                    backgroundController.ImmediatelyLoadNewWallpaper(path);
                }
            }
        }

        private void BarRenderer_OnDragOver(object sender, DragEventArgs e)
        {
            
        }

        private void BarRenderer_OnDragEnter(object sender, DragEventArgs e)
        {
            string[] dataFormats = e.Data.GetFormats();
            if (dataFormats.Contains("FileName") && (e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        #endregion

        private enum BarDrawMode
        {
            FromCenter = 0,
            TopAndBottom
        }

        private enum InterpolationMode
        {
            None = 0,
            Automatic = 1,
            Catmull = 2,
            Bicubic = 3,
            Bilinear = 4,
            BSpline = 5
        }
    }
}
