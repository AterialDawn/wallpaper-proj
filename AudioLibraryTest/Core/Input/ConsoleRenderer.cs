using CircularBuffer;
using ImGuiNET;
using player.Core.Logging;
using player.Core.Render;
using player.Core.Service;
using player.Core.Settings;
using System.Collections.Generic;
using System.Text;
using Log = player.Core.Logging.Logger;

namespace player.Core.Input
{
    class ConsoleRenderer
    {
        public bool Visible { get { return enabled.Value; } }

        bool justActivated = false;
        private CircularBuffer<string> messageStack = new CircularBuffer<string>(500); //MORE MESSAGES
        private ConsoleManager consoleManager;
        private ConsoleHistoryHelper historyHelper = new ConsoleHistoryHelper();
        private FpsLimitOverrideContext fpsOverride = null;
        System.Numerics.Vector2 textScrollingHeight;
        SettingsAccessor<bool> enabled;

        internal ConsoleRenderer(ConsoleManager conManager)
        {
            consoleManager = conManager;

            Log.MessageLogged += Logger_MessageLogged;

            enabled = ServiceManager.GetService<SettingsService>().GetAccessor("Console.Visible", false);
        }

        public void Init()
        {
            textScrollingHeight = new System.Numerics.Vector2(0, -(ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing() + 15f));
        }

        private void Logger_MessageLogged(object sender, MessageLoggedEventArgs e)
        {
            messageStack.PushFront(e.Message);
        }

        byte[] textBuf = new byte[256];

        public void Render(double time)
        {
            if (!enabled.Value) return;

            ImGui.Begin("Console");
            ImGui.BeginChild("scrolling", textScrollingHeight, ImGuiChildFlags.None);
            for (int i = messageStack.Size - 1; i > -1; i--)
            {
                ImGui.TextWrapped(messageStack[i]);
            }
            ImGui.SetScrollHereY(1.0f);
            ImGui.EndChild();
            ImGui.Separator();
            ImGui.PushItemWidth(ImGui.GetWindowWidth() - 20f);
            bool refocus = false;
            if (ImGui.InputText("##", textBuf, (uint)textBuf.Length, ImGuiInputTextFlags.EnterReturnsTrue, null))
            {
                var str = Encoding.ASCII.GetString(textBuf).TrimEnd((char)0);
                textBuf = new byte[256];

                messageStack.PushFront(str);
                consoleManager.ExecuteCommand(str);
                refocus = true;
            }
            if (refocus || justActivated)
            {
                justActivated = false;
                ImGui.SetKeyboardFocusHere(-1);
            }
            ImGui.End();
        }

        public void ToggleDisplay()
        {
            enabled.Value = !enabled.Value;
            if (enabled.Value)
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
