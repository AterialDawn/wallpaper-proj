using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using player.Utility;
using player.Core.Render.UI;
using player.Core.Service;
using player.Shaders;
using player.Core.Logging;
using CircularBuffer;
using System.Windows.Forms;
using OpenTK.Input;
using Log = player.Core.Logging.Logger;
using OpenTK;
using QuickFont;
using player.Core.Render.UI.Controls;
using player.Core.Render;
using System.Drawing;
using ImGuiNET;

namespace player.Core.Input
{
    class ConsoleRenderer
    {
        class Pair<T,V>
        {
            public T Item1;
            public V Item2;

            public Pair(T item1, V item2)
            {
                Item1 = item1;
                Item2 = item2;
            }
        }
        class ProcessedTextInfo
        {
            public ProcessedText Text { get; set; }
            public SizeF TextHeight { get; set; }
        }

        public bool Visible { get { return enabled; } }

        private bool enabled = false;
        bool justActivated = false;
        private CircularBuffer<Pair<string, ProcessedTextInfo>> messageStack = new CircularBuffer<Pair<string, ProcessedTextInfo>>(100); //100 msgs seems fine
        private ConsoleManager consoleManager;
        private ConsoleHistoryHelper historyHelper = new ConsoleHistoryHelper();
        private FpsLimitOverrideContext fpsOverride = null;
        System.Numerics.Vector2 textScrollingHeight;

        internal ConsoleRenderer(ConsoleManager conManager)
        {
            consoleManager = conManager;

            Log.MessageLogged += Logger_MessageLogged;

            textScrollingHeight = new System.Numerics.Vector2(0, -(ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing() + 15f));
        }

        private void Logger_MessageLogged(object sender, MessageLoggedEventArgs e)
        {
            messageStack.PushFront(new Pair<string, ProcessedTextInfo>(e.Message, null));
        }

        byte[] textBuf = new byte[256];

        public void Render(double time)
        {
            if (!enabled) return;

            ImGui.BeginWindow("Console");
            ImGui.BeginChild("scrolling", textScrollingHeight, false, WindowFlags.Default);
            for (int i = messageStack.Size - 1; i > -1 ; i--)
            {
                ImGui.TextWrapped(messageStack[i].Item1);
            }
            ImGui.SetScrollHere(1.0f);
            ImGui.EndChild();
            ImGui.Separator();
            ImGui.PushItemWidth(ImGui.GetWindowWidth() - 20f);
            bool refocus = false;
            if (ImGui.InputText("", textBuf, (uint)textBuf.Length, InputTextFlags.EnterReturnsTrue, null))
            {
                var str = Encoding.ASCII.GetString(textBuf).TrimEnd((char)0);
                textBuf = new byte[256];

                messageStack.PushFront(new Pair<string, ProcessedTextInfo>(str, null));
                consoleManager.ExecuteCommand(str);
                refocus = true;
            }
            if (refocus || justActivated)
            {
                justActivated = false;
                ImGui.SetKeyboardFocusHere(-1);
            }
            ImGui.EndWindow();
        }

        public void ToggleDisplay()
        {
            enabled = !enabled;
            if (enabled)
            {
                justActivated = true;
                fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("Console", FpsLimitOverride.Maximum);
            }
            else
            {
                if (fpsOverride != null)
                {
                    fpsOverride.Dispose();
                    fpsOverride = null;
                }
            }
        }
       

        private class ConsoleHistoryHelper
        {
            LinkedList<string> list = new LinkedList<string>();
            LinkedListNode<string> currentNode = null;

            public void Push(string newState)
            {
                list.AddFirst(newState);
                currentNode = null;
            }

            public void Reset()
            {
                currentNode = null;
            }

            public string GetPrevious()
            {
                if (currentNode == null)
                {
                    currentNode = list.First;
                }
                else if (currentNode.Next != null)
                {
                    currentNode = currentNode.Next;
                }

                return currentNode?.Value ?? "";
            }

            public string GetNext()
            {
                if (currentNode != null)
                {
                    if (currentNode.Previous != null)
                    {
                        currentNode = currentNode.Previous;
                    }
                }

                return currentNode?.Value ?? "";
            }
        }
    }
}
