using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using player.Core.Input;
using QuickFont;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using player.Utility;
using OpenTK.Graphics;
using player.Shaders;

namespace player.Core.Render.UI.Controls
{
    /// <summary>
    /// OpenGL Label
    /// </summary>
    class OLabel : ControlBase
    {
        public override bool CanFocus { get { return true; } }

        public override bool CanSelect { get { return true; } }

        public override string Name { get; }

        /// <summary>
        /// Automatically set size depending on text content
        /// </summary>
        public bool AutoSize { get; set; } = true;

        private bool textChangesFrequently = false;
        private ProcessedText processedText = null;
        private QFontAlignment fontAlignment;
        private bool processedTextDirty = false;
        private float textHeight = 0f;
        private bool ignoreSizeEvent = false;

        /// <summary>
        /// OpenGL Label
        /// </summary>
        /// <param name="name">Name of the label</param>
        /// <param name="text">Text to be displayed on the label</param>
        /// <param name="textChangesFrequently">Optimization hint</param>
        public OLabel(string name, string text, QFontAlignment textAlignment, bool textChangesFrequently = false) : base()
        {
            Name = name;
            Text = text;
            fontAlignment = textAlignment;
            this.textChangesFrequently = textChangesFrequently;
            textHeight = uiManagerInst.TextRenderer.MeasureText("TEXT").Height + 0.1f;
        }

        public override void OnTextChanged()
        {
            processedTextDirty = true;
            ignoreSizeEvent = true;
            Size = uiManagerInst.TextRenderer.MeasureText(Text);
        }

        public override void OnSizeChanged()
        {
            if (ignoreSizeEvent)
            {
                ignoreSizeEvent = false;
                return;
            }
            if (!textChangesFrequently) processedTextDirty = true;
        }

        public override void OnUISizeChanged()
        {
            if (!textChangesFrequently) processedTextDirty = true;
        }

        public override void Render(double time)
        {
            float yOffset = 0;
            if (IsAnchored(System.Windows.Forms.AnchorStyles.Bottom)) yOffset = -yOffset;
            if (processedTextDirty)
            {
                processedText = uiManagerInst.TextRenderer.PreprocessText(Text, Size.Width, fontAlignment);
            }

            uiManagerInst.TextRenderer.DropShadowActive = true;
            uiManagerInst.TextRenderer.DropShadowOpacity = 1f;
            //Render our text
            if (!textChangesFrequently) uiManagerInst.TextRenderer.RenderText(processedText, new Vector2(TransformedBounds.X, TransformedBounds.Y + yOffset));
            else uiManagerInst.TextRenderer.RenderText(Text, new Vector2(TransformedBounds.X, TransformedBounds.Y + yOffset), fontAlignment);
        }
    }
}
