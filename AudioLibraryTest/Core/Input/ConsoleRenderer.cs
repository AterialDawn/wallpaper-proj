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
        private VertexFloatBuffer backgroundBuffer = new VertexFloatBuffer(VertexFormat.XY_COLOR, bufferHint: BufferUsageHint.StaticDraw);
        private VertexFloatBuffer lineBuffer = new VertexFloatBuffer(VertexFormat.XY_COLOR, bufferHint: BufferUsageHint.StaticDraw, beginMode: PrimitiveType.Lines);
        private VertexFloatBuffer caretBuffer = new VertexFloatBuffer(VertexFormat.XY_COLOR, bufferHint: BufferUsageHint.StaticDraw, beginMode: PrimitiveType.Triangles);
        private UIManager uiManager;
        private InputManager inputManager;
        private OscilloscopeShader shader;
        private CircularBuffer<Pair<string, ProcessedTextInfo>> messageStack = new CircularBuffer<Pair<string, ProcessedTextInfo>>(100); //100 msgs seems fine
        private float messageHeight = 0;
        private string currentText = "";
        private int caretPosition = 0;
        private bool caretState = false;
        private double caretBlink = 0;
        private double currentCaretTime = 0;
        private InputManager.KeyboardHookInfo kbHook = null;
        private ConsoleManager consoleManager;
        private ConsoleHistoryHelper historyHelper = new ConsoleHistoryHelper();
        private FpsLimitOverrideContext fpsOverride = null;

        internal ConsoleRenderer(ConsoleManager conManager)
        {
            uint caretBlinkUint = Win32.GetCaretBlinkTime();
            if (caretBlinkUint == Win32.INFINITE) caretBlink = -1f;
            else caretBlink = caretBlinkUint / 1000.0;
            Log.Log($"Caret blink time is {caretBlink}");

            consoleManager = conManager;

            backgroundBuffer.AddVertex(0f, 0f, 0f, 0f, 0f, 0.6f);
            backgroundBuffer.AddVertex(0f, 1f, 0f, 0f, 0f, 0.6f);
            backgroundBuffer.AddVertex(1f, 1f, 0f, 0f, 0f, 0.6f);
            backgroundBuffer.AddVertex(0f, 0f, 0f, 0f, 0f, 0.6f);
            backgroundBuffer.AddVertex(1f, 1f, 0f, 0f, 0f, 0.6f);
            backgroundBuffer.AddVertex(1f, 0f, 0f, 0f, 0f, 0.6f);
            backgroundBuffer.Load();

            caretBuffer.AddVertex(0f, 0f, 1f, 1f, 1f, 1f);
            caretBuffer.AddVertex(0f, 1f, 1f, 1f, 1f, 1f);
            caretBuffer.AddVertex(1f, 1f, 1f, 1f, 1f, 1f);
            caretBuffer.AddVertex(0f, 0f, 1f, 1f, 1f, 1f);
            caretBuffer.AddVertex(1f, 1f, 1f, 1f, 1f, 1f);
            caretBuffer.AddVertex(1f, 0f, 1f, 1f, 1f, 1f);
            caretBuffer.Load();

            lineBuffer.AddVertex(0f, 0f, 1f, 1f, 1f, 1f);
            lineBuffer.AddVertex(1f, 0f, 1f, 1f, 1f, 1f);
            lineBuffer.Load();

            Log.MessageLogged += Logger_MessageLogged;
        }

        private void Logger_MessageLogged(object sender, MessageLoggedEventArgs e)
        {
            messageStack.PushFront(new Pair<string, ProcessedTextInfo>(e.Message, null));
        }

        public void Initialize()
        {
            uiManager = ServiceManager.GetService<UIManager>();
            shader = new OscilloscopeShader();
            inputManager = ServiceManager.GetService<InputManager>();
            messageHeight = uiManager.TextRenderer.MeasureText("TEXT").Height;
        }

        public void Render(double time)
        {
            if (!enabled) return;
            if (caretBlink != -1f) currentCaretTime += time;
            if (currentCaretTime >= caretBlink)
            {
                currentCaretTime -= caretBlink;
                caretState = !caretState;
            }

            float linePosition = 0;
            float inputBoxPos = 0;

            shader.Activate();

            GL.PushMatrix(); //0
            GL.Scale(uiManager.UISize.X, linePosition + messageHeight, 1f); //add an extra line for text input
            backgroundBuffer.Draw();
            GL.PopMatrix(); //0

            uiManager.TextRenderer.RenderText(currentText, new Vector2(0, inputBoxPos), QFontAlignment.Left);

            if (caretState)
            {
                GL.PushMatrix(); //0
                float textWidth = uiManager.TextRenderer.MeasureText(currentText).Width;

                GL.Translate(textWidth + 1f, linePosition + (messageHeight *.2f), 0f);
                GL.Scale(2f, messageHeight, 1f);
                
                caretBuffer.Draw();
                GL.PopMatrix(); //0
            }

            ImGui.BeginWindow("Console");
            ImGui.BeginChild("scrolling");
            for (int i = messageStack.Size - 1; i > -1 ; i--)
            {
                ImGui.TextWrapped(messageStack[i].Item1);
            }
            ImGui.SetScrollHere(1.0f);
            ImGui.EndChild();
            ImGui.EndWindow();
        }

        public void ToggleDisplay()
        {
            enabled = !enabled;
            if (enabled)
            {
                kbHook = inputManager.AddKeyboardHook(inputHook);
                currentText = "";
                caretPosition = 0;
                fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("Console", FpsLimitOverride.Maximum);
            }
            else
            {
                inputManager.RemoveHook(kbHook);
                if (fpsOverride != null)
                {
                    fpsOverride.Dispose();
                    fpsOverride = null;
                }
            }
        }
       

        private bool inputHook(object source, KeyStateChangedEventArgs args)
        {
            if (args.Key == Key.Tilde) return false; //Don't swallow console key
            if (!args.Pressed) return true; //Only care about presses

            //i hate my life
            //All i really care about is delete, backspace, arrows, and text input
            Command command = IsCommand(args);
            if (command != Command.None)
            {
                HandleCommand(command);
            }
            else if (InputHelper.IsPrintable(args.Key))
            {
                string charStr = InputHelper.GetStringRepresentation(args.Key, args.Shift);
                currentText = currentText.Insert(caretPosition, charStr);
                caretPosition += charStr.Length;
            }
            else if (InputHelper.IsControlKey(args.Key))//:AWOOO:
            {
                switch (args.Key)
                {
                    case Key.BackSpace:
                        {
                            if (caretPosition == 0) break;
                            currentText = currentText.Remove(caretPosition - 1, 1);
                            caretPosition--;
                            break;
                        }
                    case Key.Delete:
                        {
                            if (caretPosition >= currentText.Length) break;
                            currentText = currentText.Remove(caretPosition, 1);
                            break;
                        }
                    case Key.Left:
                        {
                            caretPosition--;
                            if (caretPosition < 0) caretPosition = 0;
                            break;
                        }
                    case Key.Right:
                        {
                            caretPosition++;
                            if (caretPosition > currentText.Length) caretPosition = currentText.Length;
                            break;
                        }
                    case Key.Up:
                        {
                            currentText = historyHelper.GetPrevious();
                            caretPosition = currentText.Length;
                            break;
                        }
                    case Key.Down:
                        {
                            currentText = historyHelper.GetNext();
                            caretPosition = currentText.Length;
                            break;
                        }
                    case Key.KeypadEnter:
                    case Key.Enter:
                        {
                            caretPosition = 0;
                            consoleManager.ExecuteCommand(currentText);
                            historyHelper.Push(currentText);
                            currentText = "";
                            break;
                        }
                }
            }

            return true; //While console is open, don't forward events
        }

        private void HandleCommand(Command command)
        {
            if (command == Command.Paste)
            {
                string pasteText = null;
                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                {
                    pasteText = Clipboard.GetText(TextDataFormat.UnicodeText);
                }
                else if (Clipboard.ContainsText(TextDataFormat.Text))
                {
                    pasteText = Clipboard.GetText(TextDataFormat.Text);
                }
                if (pasteText == null) return;
                currentText += pasteText;
                caretPosition += pasteText.Length;
            }
            else if (command == Command.Clear)
            {
                currentText = "";
                caretPosition = 0;
                historyHelper.Reset();
            }
        }

        private Command IsCommand(KeyStateChangedEventArgs args)
        {
            if (args.Ctrl && args.Key == Key.C && args.Pressed)
            {
                return Command.Copy;
            }
            else if (args.Ctrl && args.Key == Key.V && args.Pressed)
            {
                return Command.Paste;
            }
            else if (args.Ctrl && args.Key == Key.A && args.Pressed)
            {
                return Command.SelectAll;
            }
            else if (args.Key == Key.Escape)
            {
                return Command.Clear;
            }
            return Command.None;
        }

        private enum Command
        {
            Copy,
            Paste,
            SelectAll,
            Clear,
            None = 0
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
