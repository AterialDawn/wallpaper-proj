using player.Core.Service;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace player.Utility.DropTarget
{
    public class DropTargetManager : IService, IDropTarget
    {
        public string ServiceName { get { return "DropTargetManager"; } }

        public event EventHandler<DragEventArgs> OnDragEnter;
        public event EventHandler OnDragLeave;
        public event EventHandler<DragEventArgs> OnDragDrop;
        public event EventHandler<DragEventArgs> OnDragOver;

        public DropTargetManager(IntPtr handle)
        {
            Win32.OleInitialize(0); //Required for DropTarget to work

            int result = Win32.RegisterDragDrop(new HandleRef(this, handle), new DropTarget(this));
        }

        public void Initialize()
        {

        }

        public void Cleanup()
        {

        }

        void IDropTarget.OnDragEnter(DragEventArgs e)
        {
            OnDragEnter?.Invoke(this, e);
        }

        void IDropTarget.OnDragLeave(EventArgs e)
        {
            OnDragLeave?.Invoke(this, e);
        }

        void IDropTarget.OnDragDrop(DragEventArgs e)
        {
            OnDragDrop?.Invoke(this, e);
        }

        void IDropTarget.OnDragOver(DragEventArgs e)
        {
            OnDragOver?.Invoke(this, e);
        }
    }
}
