using OpenTK.Graphics.OpenGL;
using player.Core.Input;
using player.Core.Service;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace player.Core.Render.UI.Controls
{
    abstract class ControlBase : IDisposable
    {
        #region Concrete members
        public bool HasFocus { get { return uiManagerInst.FocusedControl == this; } }
        public AnchorStyles Anchor { get; set; } = AnchorStyles.None;
        public string Text { get { return _text; } set { _text = value; OnTextChanged(); } }
        public bool Enabled { get; set; } = true;

        public bool MouseEntered { get; set; } = false;

        public RectangleF Bounds { get { return new RectangleF(Location.X, Location.Y, Size.Width, Size.Height); } }
        public Padding Margin { get { return _margin; } set { _margin = value; CalculateTransformedBounds(); OnMarginChanged(); } }
        public PointF Location { get { return _location; } set { _location = value; CalculateTransformedBounds(); OnLocationChanged(); } }
        public SizeF Size { get { return _size; } set { _size = value; CalculateTransformedBounds(); OnSizeChanged(); } }

        public RectangleF TransformedBounds { get { return _transformedBounds; } }
        protected UIManager uiManagerInst = null;
        public int ID { get; private set; } = -1;

        private PointF _location = new PointF(0, 0);
        private SizeF _size = new SizeF(0, 0);
        private Padding _margin = new Padding(0, 0, 0, 0);
        private RectangleF _transformedBounds = new RectangleF(0, 0, 0, 0);
        private string _text = "";
        #endregion

        #region Abstract members
        public abstract bool CanSelect { get; }
        public abstract bool CanFocus { get; }
        public abstract string Name { get; }
        #endregion

        #region Concrete methods
        /// <summary>
        /// Default constructor
        /// </summary>
        protected ControlBase()
        {
            uiManagerInst = ServiceManager.GetService<UIManager>();

            uiManagerInst.RegisterControl(this);
        }

        public bool Focus()
        {
            if (!CanFocus) return false;

            return uiManagerInst.FocusControl(this);
        }

        public void SetId(int newId)
        {
            if (ID != -1) throw new InvalidOperationException("ID already set! Cannot call SetId multiple times!!!");
            ID = newId;
        }

        public void UISizeChanged()
        {
            CalculateTransformedBounds();

            OnUISizeChanged();
        }

        protected void CalculateTransformedBounds()
        {
            RectangleF cachedBounds = Bounds;
            float x = cachedBounds.X, y = cachedBounds.Y, w = cachedBounds.Width, h = cachedBounds.Height;
            if (Anchor == AnchorStyles.None)
            {
                _transformedBounds = new RectangleF(x, y, w, h);
                return;
            }
            if (!IsAnchored(AnchorStyles.Bottom) && !IsAnchored(AnchorStyles.Right))
            {
                _transformedBounds = new RectangleF(x, y, w, h);
                return;
            }

            float uiX = uiManagerInst.UISize.X, uiY = uiManagerInst.UISize.Y;
            //We only care about anchoring top, and right, since coord system is based from bottom left being 0,0

            if (IsAnchored(AnchorStyles.Bottom))
            {
                y = uiY - cachedBounds.Height;
            }
            if (IsAnchored(AnchorStyles.Right))
            {
                x = uiX - cachedBounds.Width;
            }

            _transformedBounds = new RectangleF(x, y, w, h);
        }

        protected void LoadMatrix()
        {
            GL.PushMatrix();

            GL.Translate(TransformedBounds.X, TransformedBounds.Y, 0);
            GL.Scale(TransformedBounds.Width, TransformedBounds.Height, 1f);
        }

        protected void PopMatrix()
        {
            GL.PopMatrix();
        }

        protected bool IsAnchored(AnchorStyles style)
        {
            return (Anchor & style) == style;
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// On control clicked, only called if CanFocus returns true!
        /// </summary>
        public virtual void OnClick() { }

        /// <summary>
        /// On control has focus, either by something calling ControlBase.Focus, or control clicked, or tab brought us to this control
        /// </summary>
        public virtual void OnFocus() { }

        /// <summary>
        /// On control losing focus
        /// </summary>
        public virtual void OnFocusLost() { }

        /// <summary>
        /// When the control's size is changed
        /// </summary>
        public virtual void OnSizeChanged() { }

        /// <summary>
        /// When the control's location is changed
        /// </summary>
        public virtual void OnLocationChanged() { }

        /// <summary>
        /// When the control's margin is changed
        /// </summary>
        public virtual void OnMarginChanged() { }

        /// <summary>
        /// When the control's text is changed
        /// </summary>
        public virtual void OnTextChanged() { }

        /// <summary>
        /// When the UI size has changed.
        /// </summary>
        public virtual void OnUISizeChanged() { }

        /// <summary>
        /// When the control has focus, keyboard events are passed to this control
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnKeyEvent(KeyStateChangedEventArgs args) { }

        public virtual void OnMouseEnter() { }

        public virtual void OnMouseLeave() { }

        /// <summary>
        /// Let the control render itself, time is the delta from the last call to render
        /// </summary>
        /// <param name="time"></param>
        public abstract void Render(double time);

        #endregion

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ControlBase otherCtrl = obj as ControlBase;
            if (otherCtrl == null) return false;

            return otherCtrl.ID == ID;
        }

        public void Dispose()
        {
            uiManagerInst.UnregisterControl(this);
        }
    }

}
