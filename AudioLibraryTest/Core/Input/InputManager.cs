using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Input;
using player.Core.Service;
using Log = player.Core.Logging.Logger;

namespace player.Core.Input
{
    class InputManager : IService
    {
        internal InputManager() { }

        #region Public Fields and Properties
        public delegate void KeyEvent(object source, Key key, bool keyDown);
        public delegate bool InputHook(object source, KeyStateChangedEventArgs args); //bleh

        public event EventHandler<KeyStateChangedEventArgs> KeyStateEvent;
        public event EventHandler<MouseEventArgs> MouseEvent;
        public event EventHandler<MouseMoveEventArgs> MouseMoveEvent;
        public event EventHandler<MouseWheelEventArgs> MouseWheelEvent;
        #endregion

        #region Private Fields and Properties
        private Dictionary<Key, List<KeyInfoContainer>> hotkeysDict = new Dictionary<Key, List<KeyInfoContainer>>(new KeyEnumComparer());
        private Dictionary<Key, bool> keyStatusDict = new Dictionary<Key, bool>(new KeyEnumComparer());
        private List<KeyboardHookInfo> hookQueue = new List<KeyboardHookInfo>();
        private Queue<InputEventContainer> inputQueue = new Queue<InputEventContainer>();
        private object inputQueueLock = new object();

        public string ServiceName { get { return "InputManager"; } }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialization method
        /// </summary>
        public void Initialize()
        {
            InitKeyStatusDict();
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Cleanup()
        {

        }

        /// <summary>
        /// Set a KeyEvent proc that all keyboard events will be forwarded to until SetKeyboardHook(null) is called
        /// </summary>
        /// <param name="hook">The hook, or null to disable</param>
        public KeyboardHookInfo AddKeyboardHook(InputHook hook)
        {
            KeyboardHookInfo info = new KeyboardHookInfo(hook);
            hookQueue.Add(info);
            return info;
        }

        public void RemoveHook(KeyboardHookInfo info)
        {
            if (hookQueue.Contains(info))
            {
                hookQueue.Remove(info);
            }
        }

        /// <summary>
        /// Registers a new HotkeyHandler
        /// </summary>
        /// <param name="hotkey">The key to respond to</param>
        /// <param name="eventDelegate">The KeyEvent delegate to call</param>
        /// <param name="keyRepeat">Whether or not to send multiple KeyDown events while key is pressed</param>
        public void RegisterKeyHandler(Key hotkey, KeyEvent eventDelegate, bool keyRepeat)
        {
            if (!hotkeysDict.ContainsKey(hotkey))
            {
                hotkeysDict.Add(hotkey, new List<KeyInfoContainer>());
            }

            hotkeysDict[hotkey].Add(new KeyInfoContainer(hotkey, eventDelegate, keyRepeat));
        }

        /// <summary>
        /// Rendering thread calls this to fire all input events synchronously on the rendering thread
        /// </summary>
        public void ProcessInputs()
        {
            //this is pretty nasty tbh, not sure of a better way to do this tho
            lock (inputQueueLock)
            {
                while (inputQueue.Count > 0)
                {
                    InputEventContainer evt = inputQueue.Dequeue();
                    if (evt.IsKeyEventArg)
                    {
                        ProcessKeyEvent(evt.KeyboardKeyEventArg, evt.KeyPressed);
                    }
                    else if (evt.IsMouseEventArg)
                    {
                        MouseEvent?.Invoke(this, evt.MouseEventArg);
                    }
                    else if (evt.IsMouseWheelEventArg)
                    {
                        MouseWheelEvent?.Invoke(this, evt.MouseWheelEventArg);
                    }
                    else if (evt.IsMouseMoveEventArg)
                    {
                        MouseMoveEvent?.Invoke(this, evt.MouseMoveEventArg);
                    }
                    else
                    {
                        Log.Log("Unknown input event!!!");
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private void ProcessKeyEvent(KeyStateChangedEventArgs e, bool isPressed)
        {
            if (HandleKeyhooks(e)) return;

            bool lastStatus = keyStatusDict[e.Key]; //lastStatus contains the previous status of the current key for keyRepeat feature
            keyStatusDict[e.Key] = isPressed;


            if (hotkeysDict.ContainsKey(e.Key))
            {
                foreach (KeyInfoContainer curHotkey in hotkeysDict[e.Key])
                {
                    if (isPressed)
                    {
                        if (lastStatus)
                        {
                            if (curHotkey.KeyRepeat)
                            {
                                curHotkey.HotkeyDelegate(this, e.Key, true);
                            }
                            continue;
                        }

                        curHotkey.HotkeyDelegate(this, e.Key, true);
                    }
                    else
                    {
                        curHotkey.HotkeyDelegate(this, e.Key, false);
                    }
                }
            }

            KeyStateEvent?.Invoke(this, e);
        }

        private void InitKeyStatusDict()
        {
            foreach (Key curKey in Enum.GetValues(typeof(Key)))
            {
                if (!keyStatusDict.ContainsKey(curKey))
                {
                    keyStatusDict.Add(curKey, false);
                }
            }
        }

        private bool HandleKeyhooks(KeyStateChangedEventArgs args)
        {
            if (hookQueue.Count == 0) return false;

            foreach (var hook in hookQueue)
            {
                if (hook.hookProc(this, args))
                {
                    return true;
                }
            }
            return false;
        }

        internal void KeyDown(Key key, bool shift, bool alt, bool control)
        {
            KeyStateChangedEventArgs keystateArgs = new KeyStateChangedEventArgs(key, true, shift, alt, control);
            lock (inputQueueLock)
            {
                inputQueue.Enqueue(new InputEventContainer(keystateArgs, true));
            }
        }

        internal void KeyDown(KeyboardKeyEventArgs args)
        {
            KeyStateChangedEventArgs keystateArgs = new KeyStateChangedEventArgs(args.Key, true, args.Shift, args.Alt, args.Control);
            lock (inputQueueLock)
            {
                inputQueue.Enqueue(new InputEventContainer(keystateArgs, true));
            }
        }

        internal void KeyUp(Key key, bool shift, bool alt, bool control)
        {
            KeyStateChangedEventArgs keystateArgs = new KeyStateChangedEventArgs(key, false, shift, alt, control);
            lock (inputQueueLock)
            {
                inputQueue.Enqueue(new InputEventContainer(keystateArgs, false));
            }
        }

        internal void KeyUp(KeyboardKeyEventArgs args)
        {
            KeyStateChangedEventArgs keystateArgs = new KeyStateChangedEventArgs(args.Key, false, args.Shift, args.Alt, args.Control);
            lock (inputQueueLock)
            {
                inputQueue.Enqueue(new InputEventContainer(keystateArgs, false));
            }
        }

        internal void MouseDown(MouseEventArgs args)
        {
            MouseEventArgs clonedEvents = new MouseEventArgs(args); //clone args as required by opentk
            lock (inputQueueLock)
            {
                inputQueue.Enqueue(new InputEventContainer(clonedEvents));
            }
        }

        internal void MouseUp(MouseEventArgs args)
        {
            MouseEventArgs clonedEvents = new MouseEventArgs(args); //clone args as required by opentk
            lock (inputQueueLock)
            {
                inputQueue.Enqueue(new InputEventContainer(clonedEvents));
            }
        }

        internal void MouseWheel(MouseWheelEventArgs args)
        {
            MouseEventArgs clonedEvents = new MouseEventArgs(args); //clone args as required by opentk
            lock (inputQueueLock)
            {
                inputQueue.Enqueue(new InputEventContainer(clonedEvents));
            }
        }

        internal void MouseMove(MouseMoveEventArgs args)
        {
            MouseEventArgs clonedEvents = new MouseEventArgs(args); //clone args as required by opentk
            lock (inputQueueLock)
            {
                inputQueue.Enqueue(new InputEventContainer(clonedEvents));
            }
        }
        #endregion

        #region Helper classes/structs
        //Helper class
        private class KeyInfoContainer
        {
            public Key Key;
            public KeyEvent HotkeyDelegate;
            public bool KeyRepeat;

            public KeyInfoContainer(Key key, KeyEvent hotkeyDelegate, bool keyRepeat)
            {
                this.Key = key;
                this.HotkeyDelegate = hotkeyDelegate;
                this.KeyRepeat = keyRepeat;
            }
        }

        private class InputEventContainer
        {
            public bool IsKeyEventArg { get { return KeyboardKeyEventArg != null; } }
            public bool KeyPressed { get; private set; } = false;
            public KeyStateChangedEventArgs KeyboardKeyEventArg { get; private set; } = null;

            public bool IsMouseEventArg { get { return MouseEventArg != null; } }
            public MouseEventArgs MouseEventArg { get; private set; } = null;

            public bool IsMouseWheelEventArg { get { return MouseWheelEventArg != null; } }
            public MouseWheelEventArgs MouseWheelEventArg { get; private set; } = null;

            public bool IsMouseMoveEventArg { get { return MouseMoveEventArg != null; } }
            public MouseMoveEventArgs MouseMoveEventArg { get; private set; } = null;

            public InputEventContainer(KeyStateChangedEventArgs args, bool keyPressed)
            {
                KeyboardKeyEventArg = args;
                KeyPressed = keyPressed;
            }

            public InputEventContainer(MouseEventArgs args)
            {
                MouseEventArg = args;
            }

            public InputEventContainer(MouseWheelEventArgs args)
            {
                MouseWheelEventArg = args;
            }

            public InputEventContainer(MouseMoveEventArgs args)
            {
                MouseMoveEventArg = args;
            }
        }

        //Avoid boxing/unboxing of key enum in dictionaries
        private struct KeyEnumComparer : IEqualityComparer<Key>
        {
            public bool Equals(Key x, Key y)
            {
                return x == y;
            }

            public int GetHashCode(Key obj)
            {
                return (int)obj;
            }
        }

        public class KeyboardHookInfo
        {
            internal InputHook hookProc;

            public KeyboardHookInfo(InputHook hook)
            {
                hookProc = hook;
            }
        }
        #endregion
    }

    public class KeyStateChangedEventArgs : EventArgs
    {
        public Key Key { get; private set; }
        public bool Pressed { get; private set; }
        public bool Shift { get; private set; }
        public bool Alt { get; private set; }
        public bool Ctrl { get; private set; }

        public KeyStateChangedEventArgs(Key key, bool pressed, bool shiftState, bool altState, bool ctrlState)
        {
            this.Key = key;
            this.Pressed = pressed;
            this.Shift = shiftState;
            this.Alt = altState;
            this.Ctrl = ctrlState;
        }

        public override string ToString()
        {
            return $"{(Ctrl?"Ctrl + " : "")}{(Alt ? "Alt + " : "")}{(Ctrl ? "Shift + " : "")}{Key.ToString()}";
        }
    }
}
