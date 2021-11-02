using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static player.Utility.Win32;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace player.Utility.DropTarget
{
    internal class DropTarget : IOleDropTarget
    {
        const int S_OK = 0x00000000;
        const int S_FALSE = 0x00000001;

        private IDataObject lastDataObject = null;
        private DragDropEffects lastEffect = DragDropEffects.None;
        private IDropTarget owner;

        public DropTarget(IDropTarget owner)
        {
            this.owner = owner;
        }

        private DragEventArgs CreateDragEventArgs(object pDataObj, int grfKeyState, POINTL pt, int pdwEffect)
        {
            IDataObject data = null;

            if (pDataObj == null)
            {
                data = lastDataObject;
            }
            else
            {
                if (pDataObj is IDataObject)
                {
                    data = (IDataObject)pDataObj;
                }
                else if (pDataObj is IComDataObject)
                {
                    data = new DataObject(pDataObj);
                }
                else
                {
                    return null; // Unknown data object interface; we can't work with this so return null
                }
            }

            DragEventArgs drgevent = new DragEventArgs(data, grfKeyState, pt.x, pt.y, (DragDropEffects)pdwEffect, lastEffect);
            lastDataObject = data;
            return drgevent;
        }

        int IOleDropTarget.OleDragEnter(object pDataObj, int grfKeyState,
                                                      POINTSTRUCT pt,
                                                      ref int pdwEffect)
        {
            POINTL ptl = new POINTL();
            ptl.x = pt.x;
            ptl.y = pt.y;
            DragEventArgs drgevent = CreateDragEventArgs(pDataObj, grfKeyState, ptl, pdwEffect);

            if (drgevent != null)
            {
                owner.OnDragEnter(drgevent);
                pdwEffect = (int)drgevent.Effect;
                lastEffect = drgevent.Effect;
            }
            else
            {
                pdwEffect = (int)DragDropEffects.None;
            }
            return S_OK;
        }
        int IOleDropTarget.OleDragOver(int grfKeyState, POINTSTRUCT pt, ref int pdwEffect)
        {
            POINTL ptl = new POINTL();
            ptl.x = pt.x;
            ptl.y = pt.y;
            DragEventArgs drgevent = CreateDragEventArgs(null, grfKeyState, ptl, pdwEffect);
            owner.OnDragOver(drgevent);
            pdwEffect = (int)drgevent.Effect;
            lastEffect = drgevent.Effect;
            return S_OK;
        }
        int IOleDropTarget.OleDragLeave()
        {
            owner.OnDragLeave(EventArgs.Empty);
            return S_OK;
        }
        int IOleDropTarget.OleDrop(object pDataObj, int grfKeyState, POINTSTRUCT pt, ref int pdwEffect)
        {
            POINTL ptl = new POINTL();
            ptl.x = pt.x;
            ptl.y = pt.y;
            DragEventArgs drgevent = CreateDragEventArgs(pDataObj, grfKeyState, ptl, pdwEffect);

            if (drgevent != null)
            {
                owner.OnDragDrop(drgevent);
                pdwEffect = (int)drgevent.Effect;
            }
            else
            {
                pdwEffect = (int)DragDropEffects.None;
            }

            lastEffect = DragDropEffects.None;
            lastDataObject = null;
            return S_OK;
        }
    }
}
