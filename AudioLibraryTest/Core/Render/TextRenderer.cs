using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using player.Shaders;
using player.Utility.Shader;
using QuickFont;
using System;
using System.Drawing;
using Log = player.Core.Logging.Logger;

namespace player.Core.Render
{
    public class TextRenderer
    {
        public Color4 RenderingColor { get; set; } = new Color4(1f, 1f, 1f, 1f);
        public float FontHeight { get; set; } = 0;
        public bool DropShadowActive = false;
        public float DropShadowOpacity = 1f;

        private Size RenderSize = new Size();
        QFont _qFont = null;
        private Shader fontShader;
        private Font cachedFont = null;
        private bool rebuildQfont = false;

        internal TextRenderer()
        {
            fontShader = new FontShader();

            InitFont();
        }

        /// <summary>
        /// Set a new font for the TextRenderer. Calling this disposes any previous fonts passed to this method
        /// </summary>
        /// <param name="newFont"></param>
        public void ChangeFont(Font newFont)
        {
            if (cachedFont != null)
            {
                cachedFont.Dispose();
                cachedFont = null;
            }
            cachedFont = newFont;
            rebuildQfont = true;
        }

        public void Initialize(int w, int h)
        {
            RenderSize.Width = w;
            RenderSize.Height = h;

            if (_qFont != null)
            {
                _qFont.Dispose();
                _qFont = null;
            }

            var config = new QFontBuilderConfiguration(true);
            //config.SuperSampleLevels = 2;
            config.ShadowConfig.blurPasses = 2;
            config.ShadowConfig.blurRadius = 1;
            config.ShadowConfig.Scale = 1.2f;

            _qFont = new QFont(cachedFont, config);
            _qFont.Options.DropShadowActive = true;
            _qFont.Options.DropShadowOpacity = 1f;
            _qFont.Options.UseDefaultBlendFunction = false;
            _qFont.Options.Monospacing = QFontMonospacing.Natural;
            _qFont.Options.Colour = new Color4(1f, 1f, 1f, 1f);

            FontHeight = _qFont.Measure("TEXT").Height;

            QFont.PushSoftwareViewport(new Viewport(0, 0, w, h));
        }

        public void Resize(int w, int h)
        {
            RenderSize.Width = w;
            RenderSize.Height = h;

            QFont.PushSoftwareViewport(new Viewport(0, 0, w, h));
        }

        /// <summary>
        /// Prepare text rendering
        /// </summary>
        public void Begin()
        {
            if (rebuildQfont)
            {
                Initialize(RenderSize.Width, RenderSize.Height); //Rebuild when font changed
                rebuildQfont = false;
            }

            _qFont.ResetVBOs();
            QFont.Begin();
        }

        /// <summary>
        /// Render all text now and reset vbos
        /// </summary>
        public void RenderAllTextNow() //bandaid fix to maintain SOME semblance of performance when rendering console
        {
            fontShader.Activate();
            _qFont.LoadVBOs();
            _qFont.DrawVBOs();

            _qFont.ResetVBOs();
        }

        public SizeF MeasureText(string text)
        {
            return _qFont.Measure(text);
        }

        public SizeF MeasureText(ProcessedText text)
        {
            return _qFont.Measure(text);
        }

        /// <summary>
        /// For text that does not change frequently, its faster to preprocess it and call RenderText with the ProcessedText overload
        /// </summary>
        /// <param name="text">The text to assign to the ProcessedText instance</param>
        /// <param name="maxWidth"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public ProcessedText PreprocessText(string text, float maxWidth, QFontAlignment alignment)
        {
            return _qFont.ProcessText(text, maxWidth, alignment);
        }

        public void RenderText(string text, Vector2 location, QFontAlignment alignment)
        {
            _qFont.Options.Colour = RenderingColor;
            _qFont.Options.DropShadowActive = DropShadowActive;
            _qFont.Options.DropShadowOpacity = DropShadowOpacity;
            _qFont.PrintVBO(text, location, alignment);
        }

        public void RenderText(ProcessedText text, Vector2 location)
        {
            _qFont.Options.Colour = RenderingColor;
            _qFont.Options.DropShadowActive = DropShadowActive;
            _qFont.Options.DropShadowOpacity = DropShadowOpacity;
            _qFont.PrintVBO(text, location);
        }

        public void End()
        {
            fontShader.Activate();
            _qFont.LoadVBOs();
            _qFont.DrawVBOs();
            QFont.End();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private void InitFont()
        {
            const string UsageString = "Font option invalid! Specify it as '-Font \"Font Name,FontSize\"";
            foreach (var option in Program.CLIParser.ActiveOptions)
            {
                if (option.Item1 == "Font")
                {
                    string[] splitText = option.Item2.Split(',');
                    if (splitText.Length != 2)
                    {
                        Log.Log(UsageString);
                        return;
                    }
                    float fontSize;
                    if (!float.TryParse(splitText[1], out fontSize))
                    {
                        Log.Log(UsageString);
                        return;
                    }

                    try
                    {
                        Font newFont = new Font(splitText[0], fontSize);
                        cachedFont = newFont;
                    }
                    catch (ArgumentException)
                    {
                        Log.Log($"Unable to create a font named '{splitText[0]}'!");
                    }
                }
            }

            if (cachedFont == null)
            {
                cachedFont = new Font(SystemFonts.DefaultFont.Name, 10.0f);
            }
        }
    }
}
