using OpenTK;
using OpenTK.Graphics.OpenGL;
using player.Core;
using player.Core.Render;
using player.Core.Render.UI.Controls;
using player.Core.Service;
using player.Core.Settings;
using player.Shaders;
using player.Utility;
using System;
using Log = player.Core.Logging.Logger;

namespace player.Renderers.BarHelpers
{
    class BackgroundController
    {
        public double BlendDuration { get; set; } = 2.5;
        public float BlendTimeRemainingPercentage { get; set; } = 0f;
        public double BackgroundDuration { get { return backgroundDurationAccessor.Get(); } private set { backgroundDurationAccessor.Set(value); } }
        public bool KeepCurrentBackground { get; set; } = false;
        public BackgroundScalingMode ScalingMode { get; set; } = BackgroundScalingMode.Fit;
        public BackgroundFactory BackgroundFactory { get; private set; } = new BackgroundFactory();
        public event EventHandler InitialBackgroundLoadComplete;

        public Vector2 PrimaryTextureResolution { get; private set; } = new Vector2();
        public Vector2 SecondaryTextureResolution { get; private set; } = new Vector2();
        public bool ReadyToRender { get { return initialLoadDone; } }
        public bool LoadingNextWallpaper { get { return loadingBackground; } }

        public IBackground CurrentBackground { get { return currentBackground; } }

        private BarShader shader;
        private Vector2 windowRes = Vector2.Zero;
        private Matrix4[] texMatrices = new Matrix4[2];
        private bool swapBackgrounds = false;
        private bool loadingBackground = false;
        private bool updateBackgroundMatrix = false;
        private bool lastBackground = false;
        private bool initialLoadDone = false;
        private double currentBlendTime = 0;
        private double backgroundTimeTotal = 0;
        private double backgroundTimeLeft = 0;
        private SettingsAccessor<double> backgroundDurationAccessor;
        bool skipBlending = false;

        private IBackground currentBackground = null;
        private IBackground nextBackground = null;
        private IBackground overrideBackground = null;
        private FpsLimitOverrideContext fpsOverride = null;
        private FpsLimitOverrideContext animatedBackgroundOverride = null;

        PieChartControl pieChart;

        public BackgroundController(BarShader shader)
        {
            this.shader = shader;
            texMatrices[0] = Matrix4.Identity;
            texMatrices[1] = Matrix4.Identity;
            backgroundDurationAccessor = ServiceManager.GetService<SettingsService>().GetAccessor<double>(SettingsKeys.BarRenderer_BackgroundDuration, 45f);

            InitializeTextures();

            pieChart = new PieChartControl("BGProgress");
            pieChart.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            pieChart.Size = new System.Drawing.SizeF(30, 30);
        }

        public string GetCurrentWallpaperPath()
        {
            if (currentBackground == null) return "No Background Loaded";
            string path = currentBackground.SourcePath;
            if (path == null) return "Background does not have a path!";
            return path;
        }

        public void OnActivate()
        {
            pieChart.Enabled = true;
        }

        public void OnDeactivate()
        {
            pieChart.Enabled = false;
        }

        public void OpenCurrentWallpaper()
        {
            string currentFilePath = currentBackground?.SourcePath;
            if (currentFilePath == null)
            {
                Log.Log("Attempted to open current wallpaper, but currentBackground is null.");
                return;
            }
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{currentFilePath}\"");
                Log.Log($"Opened {currentFilePath} in explorer");
            }
            catch (Exception e)
            {
                Log.Log($"Error opening current wallpaper! {e}");
            }
        }

        public void Update(double elapsedTime)
        {
            if (!initialLoadDone) return; //do nothing if not ready
            if (!KeepCurrentBackground && !BackgroundFactory.SingleBackgroundMode) backgroundTimeLeft -= elapsedTime;

            bool blended = false;
            if (swapBackgrounds)
            {
                backgroundTimeLeft += elapsedTime; //Don't count transition time
                currentBlendTime -= elapsedTime;
                double timeFactor = currentBlendTime / BlendDuration;
                float blendFactor = EasingMethods.CubicInOut((float)timeFactor);

                if (blendFactor <= 0 || currentBlendTime <= 0)
                {
                    if (fpsOverride != null)
                    {
                        fpsOverride.Dispose();
                        fpsOverride = null;
                    }
                    //No more blending
                    //Swap and update matrices
                    texMatrices[0] = texMatrices[1];
                    shader.SetPrimaryMatrix(texMatrices[0]);
                    PrimaryTextureResolution = SecondaryTextureResolution;
                    SecondaryTextureResolution = new Vector2(0, 0);

                    //Swap backgrounds
                    currentBackground.Destroy();
                    currentBackground = nextBackground;
                    nextBackground = null;
                    backgroundTimeLeft = BackgroundDuration;
                    if (currentBackground.OverrideBackgroundDuration != null)
                    {
                        backgroundTimeLeft = currentBackground.OverrideBackgroundDuration.Value.TotalSeconds;
                        if (backgroundTimeLeft < BackgroundDuration) backgroundTimeLeft = BackgroundDuration;
                        Log.Log($"Background Duration overriden to {backgroundTimeLeft}");
                    }
                    backgroundTimeTotal = backgroundTimeLeft;
                    swapBackgrounds = false;

                    shader.SetBlendingState(false);

                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, 0); //Unbind tex (Not sure if necessary)
                    GL.ActiveTexture(TextureUnit.Texture0);
                    CheckAnimatedBackgroundStatus();
                }
                else
                {
                    BlendTimeRemainingPercentage = (float)timeFactor;
                    pieChart.SetFillPercentage(1f - BlendTimeRemainingPercentage);
                    blended = true;

                    shader.Activate();
                    shader.SetBlendingAmount(blendFactor);

                    GL.PushAttrib(AttribMask.TextureBit);
                    nextBackground.Update(elapsedTime);
                    GL.PopAttrib();

                    shader.Activate();

                    GL.ActiveTexture(TextureUnit.Texture1);
                    nextBackground.BindTexture();
                    GL.ActiveTexture(TextureUnit.Texture0);

                    if (updateBackgroundMatrix)
                    {
                        updateBackgroundMatrix = false;
                        shader.SetSecondaryMatrix(texMatrices[1]);
                        shader.SetBlendingState(true);
                    }
                }
            }

            if (backgroundTimeLeft <= 0 && !loadingBackground && !swapBackgrounds)
            {
                GetNextBackground();
            }

            if (currentBackground != null)
            {
                GL.PushAttrib(AttribMask.TextureBit);
                currentBackground.Update(elapsedTime);
                shader.Activate();
                GL.PopAttrib();
                currentBackground.BindTexture();
                if (!blended && !loadingBackground) pieChart.SetFillPercentage((float)(backgroundTimeLeft / backgroundTimeTotal));
            }
        }

        public bool ImmediatelyLoadNewWallpaper(string path)
        {
            IBackground possibleBg = BackgroundFactory.LoadFromPath(path);
            if (possibleBg != null)
            {
                overrideBackground = possibleBg;
                backgroundTimeLeft = 0;
                skipBlending = true;
                return true;
            }
            return false;
        }

        public void ChangeBackgroundDuration(double newDuration)
        {
            double difference = newDuration - BackgroundDuration;
            BackgroundDuration = newDuration;
            backgroundTimeLeft += difference;
        }

        public void UpdateWindowResolution(Vector2 newRes)
        {
            windowRes = newRes;

            shader.Activate();
            if (currentBackground != null)
            {
                ScaleBackgroundMatrix(currentBackground, ref texMatrices[0]);
                shader.SetPrimaryMatrix(texMatrices[0]);
            }
            if (nextBackground != null)
            {
                ScaleBackgroundMatrix(nextBackground, ref texMatrices[1]);
                shader.SetSecondaryMatrix(texMatrices[1]);
            }
        }

        public bool NewBackground(bool disableTransition = false)
        {
            if (!CanChangeBackground())
            {
                return false;
            }
            backgroundTimeLeft = 0;
            skipBlending = disableTransition;
            return true;
        }

        public bool PreviousBackground(bool disableTransition = false)
        {
            if (!CanChangeBackground())
            {
                return false;
            }
            lastBackground = true;
            backgroundTimeLeft = 0;
            skipBlending = disableTransition;
            return true;
        }

        public void EndTransition()
        {
            currentBlendTime = 0;
        }

        public bool IsTransitioning()
        {
            return currentBlendTime > 0;
        }

        private bool CanChangeBackground()
        {
            return !loadingBackground; //if loading background, dont allow change
        }

        public void Destroy()
        {
            if (currentBackground != null) currentBackground.Destroy();
            if (nextBackground != null) nextBackground.Destroy();
        }

        private void InitializeTextures()
        {
            ThreadedLoaderContext.Instance.ExecuteOnLoaderThread(() =>
            {
                try
                {
                    BackgroundFactory.Initialize();
                    currentBackground = BackgroundFactory.GetNextBackground();
                    currentBackground.RenderResolution = windowRes;
                }
                catch (InvalidOperationException)
                {
                    System.Windows.Forms.MessageBox.Show($"There is no image folder, or the folder is empty or does not exist!\nSelect a folder that contains images or specify a new\nfolder on next application start.", "Error!", System.Windows.Forms.MessageBoxButtons.OK);
                    Environment.Exit(-1);
                }

                bool backgroundLoaded = false;
                int backgroundsFailed = 0;
                while (!backgroundLoaded)
                {
                    backgroundLoaded = currentBackground.Preload();
                    if (!backgroundLoaded)
                    {
                        backgroundsFailed++;
                        if (backgroundsFailed >= 3)
                        {
                            System.Windows.Forms.MessageBox.Show("Failed initial loading of 3 backgrounds in a row! Aborting!");
                            Environment.Exit(-1);
                        }

                        currentBackground.Destroy();
                        currentBackground = BackgroundFactory.GetNextBackground();
                    }
                }

                backgroundTimeLeft = BackgroundDuration;
                if (currentBackground.OverrideBackgroundDuration != null)
                {
                    backgroundTimeLeft = currentBackground.OverrideBackgroundDuration.Value.TotalSeconds;
                    if (backgroundTimeLeft < BackgroundDuration) backgroundTimeLeft = BackgroundDuration;
                    Log.Log($"Background Duration overriden to {backgroundTimeLeft}");
                }

                backgroundTimeTotal = backgroundTimeLeft;
                ScaleBackgroundMatrix(currentBackground, ref texMatrices[0]);
                shader.SetPrimaryMatrix(texMatrices[0]);
                PrimaryTextureResolution = new Vector2(currentBackground.Resolution.Width, currentBackground.Resolution.Height);
                CheckAnimatedBackgroundStatus();
                initialLoadDone = true;
                MainThreadDelegator.InvokeOn(InvocationTarget.BeforeRender, () =>
                {
                    InitialBackgroundLoadComplete?.Invoke(this, EventArgs.Empty);
                });
            });
        }

        private void CheckAnimatedBackgroundStatus()
        {
            if (currentBackground.Animated)
            {
                if (animatedBackgroundOverride != null)
                {
                    if (currentBackground.OverrideFps.HasValue && animatedBackgroundOverride.CustomFps != currentBackground.OverrideFps.Value)
                    {
                        animatedBackgroundOverride.Dispose();
                        animatedBackgroundOverride = null;
                    }
                }
                
                if(animatedBackgroundOverride == null)
                {
                    if (currentBackground.OverrideFps.HasValue)
                    {
                        animatedBackgroundOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("Animated Background", FpsLimitOverride.Custom, currentBackground.OverrideFps.Value);
                    }
                    else
                    {
                        animatedBackgroundOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("Animated Background", FpsLimitOverride.Maximum);
                    }
                }
            }
            else if (!currentBackground.Animated && animatedBackgroundOverride != null)
            {
                animatedBackgroundOverride.Dispose();
                animatedBackgroundOverride = null;
            }
        }

        private void ScaleBackgroundMatrix(IBackground background, ref Matrix4 matrix)
        {
            switch (ScalingMode)
            {
                case BackgroundScalingMode.Fit:
                    {
                        float windowAspect = windowRes.X / windowRes.Y;
                        float textureAspect = background.Resolution.Width / background.Resolution.Height;
                        float textureScalar = textureAspect / windowAspect;

                        matrix = Matrix4.Identity;
                        if (textureScalar > 1f)
                        {
                            Vector3 translationVec = new Vector3(0, (1f - textureScalar) / 2f, 0);
                            Vector3 scaleVec = new Vector3(1f, textureScalar, 1f);

                            matrix *= Matrix4.CreateScale(scaleVec);
                            matrix *= Matrix4.CreateTranslation(translationVec);
                        }
                        else
                        {
                            float windowScalar = windowAspect / textureAspect;

                            Vector3 translationVec = new Vector3((1f - windowScalar) * 0.5f, 0, 0);
                            Vector3 scaleVec = new Vector3(1f / textureScalar, 1f, 1f);

                            matrix *= Matrix4.CreateScale(scaleVec);
                            matrix *= Matrix4.CreateTranslation(translationVec);
                        }
                        break;
                    }
                case BackgroundScalingMode.Fill:
                    {
                        matrix = Matrix4.Identity;

                        break;
                    }
                case BackgroundScalingMode.Stretch:
                    {
                        matrix = Matrix4.Identity;

                        break;
                    }
                case BackgroundScalingMode.Center:
                    {
                        matrix = Matrix4.Identity;

                        break;
                    }
            }
        }

        private void GetNextBackground()
        {
            if (BackgroundFactory.SingleBackgroundMode)
            {
                backgroundTimeLeft = BackgroundDuration;
                return;
            }

            IBackground newBackground = null;
            if (overrideBackground != null)
            {
                newBackground = overrideBackground;
                overrideBackground = null;
            }
            else if (lastBackground)
            {
                newBackground = BackgroundFactory.GetLastBackground();
                lastBackground = false;
            }
            else
            {
                newBackground = BackgroundFactory.GetNextBackground();
            }
            newBackground.RenderResolution = windowRes;
            loadingBackground = true;
            nextBackground = newBackground;
            backgroundTimeLeft = BackgroundDuration;
            backgroundTimeTotal = backgroundTimeLeft;

            ThreadedLoaderContext.Instance.ExecuteOnLoaderThread(() =>
            {
                PreloadCompleteHandler(newBackground, newBackground.Preload());
            });
        }

        private void PreloadCompleteHandler(IBackground background, bool loaded)
        {
            if (!loaded)
            {
                BackgroundFactory.RemoveFile(nextBackground.SourcePath);
                nextBackground.Destroy();
                GetNextBackground();
            }
            else
            {
                fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("Background Transition", FpsLimitOverride.Maximum);
                ScaleBackgroundMatrix(background, ref texMatrices[1]);
                SecondaryTextureResolution = new Vector2(nextBackground.Resolution.Width, nextBackground.Resolution.Height);
                currentBlendTime = skipBlending ? 0 : BlendDuration;
                loadingBackground = false;
                updateBackgroundMatrix = true;
                swapBackgrounds = true;
                skipBlending = false;
            }
        }
    }
}
