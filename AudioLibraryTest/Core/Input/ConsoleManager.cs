using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenTK.Input;
using player.Core.Render.UI.Controls;
using player.Core.Service;
using Log = player.Core.Logging.Logger;

namespace player.Core.Input
{
    //This class will handle commands typed by the console, firing callbacks on those commands, and rendering the in-engine console
    class ConsoleManager : IService
    {
        private ConsoleRenderer consoleRenderer;

        internal ConsoleManager()
        {
            consoleRenderer = new ConsoleRenderer(this);
        }

        public event EventHandler<HandleCommandEventArgs> HandleCommand;

        private Dictionary<string, ConsoleLineHandler> CommandCallbacks = new Dictionary<string, ConsoleLineHandler>();

        public delegate void ConsoleLineHandler(object sender, ConsoleLineReadEventArgs args);
        private ConsoleLineHandler activeHandler = null;
        private string activeHandlerCommand = null;
        private BlockingCollection<ConsoleMessage> messageList = new BlockingCollection<ConsoleMessage>(200); //No more than 200 backlogged messages
        InputManager inputManager;
        bool inputGrabberOpen = false;
        OLabel grabbingInputText;

        public string ServiceName { get { return "ConsoleManager"; } }

        public void Initialize()
        {
            RegisterCommandHandler("help", helpHandler);

            consoleRenderer.Initialize();

            if (VisGameWindow.FormWallpaperMode != Utility.WallpaperMode.None)
            {
                WindowsHotkeyUtil.RegisterHotkey(new WindowsHotkeyUtil.KeyContainer(System.Windows.Forms.Keys.Oemtilde, true, true, (_) =>
                {
                    StartInputGrabberForm();
                }));
                Log.Log("Registered Global CTRL+SHIFT+TILDE hotkey");
            }

            //Register ` as our toggle console hotkey
            ServiceManager.GetService<InputManager>().RegisterKeyHandler(Key.Tilde, f3KeyPress, false);

            grabbingInputText = new OLabel(nameof(grabbingInputText), "Grabbing Input", QuickFont.QFontAlignment.Centre);
            grabbingInputText.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            grabbingInputText.Location = new System.Drawing.PointF(VisGameWindow.ThisForm.Width / 2f, 0);
            grabbingInputText.Enabled = false;
        }

        public void PostInit()
        {
            PrintNumberOfCommands();
            //Run any commands that were passed to player as +commands
            RunCLICommands();
        }

        public void Render(double time)
        {
            consoleRenderer.Render(time);
        }

        public void PrintNumberOfCommands()
        {
            Log.Log($"{CommandCallbacks.Count} total commands. Use help to list them");
        }

        public void PrintRegisteredCommands()
        {
            Log.Log($"Commands are: {string.Join(", ", CommandCallbacks.Keys)}");
        }

        public void RegisterCommandHandler(string commandToHandle, ConsoleLineHandler handler)
        {
            commandToHandle = commandToHandle.ToLower();
            if (!CommandCallbacks.ContainsKey(commandToHandle))
            {
                CommandCallbacks.Add(commandToHandle, handler);
            }
            else
            {
                Log.Log($"Command {commandToHandle} is already registered!");
            }
        }

        public void UnregisterCommand(string registeredCommand)
        {
            if (CommandCallbacks.ContainsKey(registeredCommand))
            {
                CommandCallbacks.Remove(registeredCommand);
            }
            else
            {
                Log.Log("Command {0} isn't registered!", registeredCommand);
            }
        }

        private void f3KeyPress(object source, OpenTK.Input.Key key, bool keyDown)
        {
            if (!keyDown) return; //Only worry about key presses
            consoleRenderer.ToggleDisplay();
        }

        void StartInputGrabberForm()
        {
            if (inputGrabberOpen) return;
            inputGrabberOpen = true;
            Thread thr = new Thread(() =>
            {
                if (inputManager == null) inputManager = ServiceManager.GetService<InputManager>();
                FocusedInputGrabberForm form = null;
                form = new FocusedInputGrabberForm((e) =>
                {
                    inputManager.KeyDown(InputHelper.WinformsKeyToOpenTKKey(e.KeyCode), e.Shift, e.Alt, e.Control);
                }, (e) =>
                {
                    inputManager.KeyUp(InputHelper.WinformsKeyToOpenTKKey(e.KeyCode), e.Shift, e.Alt, e.Control);
                }, () =>
                {
                    inputGrabberOpen = false;
                    Log.Log("FocusedInputGrabberForm disposed");
                    form.Dispose();
                    grabbingInputText.Enabled = false;
                });
                grabbingInputText.Enabled = true;
                System.Windows.Forms.Application.Run(form);
            });
            thr.SetApartmentState(ApartmentState.STA);
            thr.IsBackground = true;
            thr.Start();

            Log.Log("Starting FocusedInputGrabberForm");
        }

        private void RunCLICommands()
        {
            foreach (var currentOption in Program.CLIParser.ActiveOptions)
            {
                if (currentOption.Item1.StartsWith("+"))
                {
                    //Run this option and its argument
                    LineRead(string.Format("{0} {1}", currentOption.Item1.Substring(1), currentOption.Item2));
                }
            }
        }

        public void ExecuteCommand(string line) //fancy text wrapper lul
        {
            LineRead(line);
        }

        void LineRead(string line)
        {
            if (activeHandler != null)
            {
                ConsoleLineReadEventArgs args = GetArgsFromLine(line, true);
                CallHandler(activeHandler, args);
            }
            else
            {
                ConsoleLineReadEventArgs args = GetArgsFromLine(line, false);
                //Check if a handler is registered for this command
                if (CommandCallbacks.ContainsKey(args.Command))
                {
                    //Call handler without basecommand
                    CallHandler(CommandCallbacks[args.Command], args);
                }
                else
                {
                    //Fire off the HandleCommand event to let other classes handle commands on-demand
                    HandleCommandEventArgs commandArgs = GetArgsFromLine(line);
                    if (HandleCommand != null) HandleCommand(this, commandArgs);

                    if (!commandArgs.Handled)
                    {
                        //No handler exists for command, invalid command!
                        InvalidCommandMesage();
                    }
                }
            }
        }

        private HandleCommandEventArgs GetArgsFromLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return new HandleCommandEventArgs("", new string[] { }, "");
            }
            else
            {
                string[] splitLine = ParseArguments(line);
                if (splitLine.Length == 1)
                {
                    return new HandleCommandEventArgs(splitLine[0].ToLower(), new string[] { }, line);
                }
                else
                {
                    return new HandleCommandEventArgs(splitLine[0].ToLower(), splitLine.Skip(1).ToArray(), line);
                }
            }
        }

        private ConsoleLineReadEventArgs GetArgsFromLine(string line, bool inHandler = false)
        {
            if (inHandler)
            {
                //While in handler, set arguments to line
                return new ConsoleLineReadEventArgs(activeHandlerCommand, ParseArguments(line), line);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return new ConsoleLineReadEventArgs("", new string[] { }, "");
                }
                string[] splitLine = ParseArguments(line);
                if (splitLine.Length == 1)
                {
                    return new ConsoleLineReadEventArgs(splitLine[0].ToLower(), new string[] { }, line);
                }
                else
                {
                    return new ConsoleLineReadEventArgs(splitLine[0].ToLower(), splitLine.Skip(1).ToArray(), line);
                }
            }
        }

        private void InvalidCommandMesage()
        {
            Log.Log("Invalid command!");
        }

        private string[] ParseArguments(string sourceString)
        {
            var parmChars = sourceString.ToCharArray();
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    parmChars[index] = '\n';
                }
                if (parmChars[index] == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    parmChars[index] = '\n';
                }
                if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void CallHandler(ConsoleLineHandler handler, ConsoleLineReadEventArgs args)
        {
            handler(this, args);
            if (args.ForwardLineToThisHandler)
            {
                //Set handler as active handler, all lines are forwarded to this handler
                activeHandler = handler;
                activeHandlerCommand = args.Command;
            }
            else
            {
                activeHandler = null;
                activeHandlerCommand = null;
            }
        }

        private void helpHandler(object sender, ConsoleLineReadEventArgs args)
        {
            PrintRegisteredCommands();
        }

        public void Cleanup()
        {

        }

        private class ConsoleMessage
        {
            public string Message { get; private set; }
            public bool IsInputMessage { get; private set; }
            public bool IsNewLine { get; private set; }

            public ConsoleMessage(string message, bool isInput, bool isNewLine)
            {
                Message = message;
                IsInputMessage = isInput;
                IsNewLine = isNewLine;
            }
        }

        private class OverridenTextWriter : TextWriter
        {
            private TextWriter baseWriter;
            private Action<string> writeHandler;
            private Action<string> writeLineHandler;

            public OverridenTextWriter(TextWriter originalWriter, Action<string> writeCallback, Action<string> writeLineCallback)
            {
                baseWriter = originalWriter;
                writeHandler = writeCallback;
                writeLineHandler = writeLineCallback;
            }

            public override Encoding Encoding
            {
                get { return baseWriter.Encoding; }
            }

            public override void Write(string value)
            {
                baseWriter.Write(value);
                writeHandler(value);
            }

            public override void WriteLine(string value)
            {
                baseWriter.WriteLine(value);
                writeLineHandler(value);
            }

            public void WriteLineToBase(string value)
            {
                baseWriter.WriteLine(value);
            }
        }
    }

    public class ConsoleLineReadEventArgs : EventArgs
    {
        public string Command { get; private set; }
        public string[] Arguments { get; private set; }
        public string Line { get; private set; }
        public bool ForwardLineToThisHandler { get; set; }

        public ConsoleLineReadEventArgs(string command, string[] args, string line)
        {
            Command = command;
            Arguments = args;
            Line = line;
            ForwardLineToThisHandler = false;
        }
    }

    public class HandleCommandEventArgs : EventArgs
    {
        public string Command { get; private set; }
        public string Line { get; private set; }
        public string[] Arguments { get; private set; }
        public bool Handled { get; set; }

        public HandleCommandEventArgs(string command, string[] args, string line)
        {
            Command = command;
            Arguments = args;
            Line = line;
            Handled = false;
        }
    }
}
