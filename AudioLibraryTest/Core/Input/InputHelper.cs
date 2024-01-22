using OpenTK.Input;
using System.Windows.Forms;

namespace player.Core.Input
{
    static class InputHelper
    {
        public static bool IsPrintable(Key key)
        {
            int keyval = (int)key;
            return ((keyval == 51) || (keyval == 52) || (keyval >= 67 && keyval <= 81) || (keyval >= 83 && keyval <= 130)); //82 is enter
        }

        //Arrow keys, home/insert/etc, enter/esc/etc
        public static bool IsControlKey(Key key)
        {
            int keyVal = (int)key;
            if (keyVal >= 10 && keyVal <= 44) return true; //F1-F35 (f35???)
            switch (key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key.Enter:
                case Key.Escape:
                case Key.BackSpace:
                case Key.Insert:
                case Key.Delete:
                case Key.PageUp:
                case Key.PageDown:
                case Key.Home:
                case Key.End:
                case Key.PrintScreen:
                case Key.Pause:
                case Key.Clear:
                case Key.Sleep:
                case Key.KeypadEnter:
                    return true;
                default: return false;
            }
        }

        //i hate my life
        public static string GetStringRepresentation(Key key, bool upperCase)
        {
            int keyVal = (int)key;
            if (keyVal >= 83 && keyVal <= 108)
            {
                string keyText = key.ToString();
                if (!upperCase) keyText = keyText.ToLower();
                return keyText;
            }
            switch (key)
            {
                case Key.Keypad0: return "0";
                case Key.Number0:
                    {
                        if (upperCase) return ")";
                        return "0";
                    }
                case Key.Keypad1: return "1";
                case Key.Number1:
                    {
                        if (upperCase) return "!";
                        return "1";
                    }
                case Key.Keypad2: return "2";
                case Key.Number2:
                    {
                        if (upperCase) return "@";
                        return "2";
                    }
                case Key.Keypad3: return "3";
                case Key.Number3:
                    {
                        if (upperCase) return "#";
                        return "3";
                    }
                case Key.Keypad4: return "4";
                case Key.Number4:
                    {
                        if (upperCase) return "$";
                        return "4";
                    }
                case Key.Keypad5: return "5";
                case Key.Number5:
                    {
                        if (upperCase) return "%";
                        return "5";
                    }
                case Key.Keypad6: return "6";
                case Key.Number6:
                    {
                        if (upperCase) return "^";
                        return "6";
                    }
                case Key.Keypad7: return "7";
                case Key.Number7:
                    {
                        if (upperCase) return "&";
                        return "7";
                    }
                case Key.Keypad8: return "8";
                case Key.Number8:
                    {
                        if (upperCase) return "*";
                        return "8";
                    }
                case Key.Keypad9: return "9";
                case Key.Number9:
                    {
                        if (upperCase) return "(";
                        return "9";
                    }
                case Key.Period: return ".";

                case Key.Space: return " ";
                case Key.Tab: return "\t";
                case Key.Semicolon:
                    {
                        if (upperCase) return ":";
                        return ";";
                    }
                default:
                    return "";
            }
        }

        //update 2021, i still hate my life
        public static Key WinformsKeyToOpenTKKey(Keys key)
        {
            switch (key)
            {
                // 0 - 15
                case Keys.Escape: return Key.Escape;
                case Keys.D1: return Key.Number1;
                case Keys.D2: return Key.Number2;
                case Keys.D3: return Key.Number3;
                case Keys.D4: return Key.Number4;
                case Keys.D5: return Key.Number5;
                case Keys.D6: return Key.Number6;
                case Keys.D7: return Key.Number7;
                case Keys.D8: return Key.Number8;
                case Keys.D9: return Key.Number9;
                case Keys.D0: return Key.Number0;
                case Keys.OemMinus: return Key.Minus;
                case Keys.Oemplus: return Key.Plus;
                case Keys.Back: return Key.BackSpace;
                case Keys.Tab: return Key.Tab;

                // 16-31
                case Keys.Q: return Key.Q;
                case Keys.W: return Key.W;
                case Keys.E: return Key.E;
                case Keys.R: return Key.R;
                case Keys.T: return Key.T;
                case Keys.Y: return Key.Y;
                case Keys.U: return Key.U;
                case Keys.I: return Key.I;
                case Keys.O: return Key.O;
                case Keys.P: return Key.P;
                case Keys.OemOpenBrackets: return Key.BracketLeft;
                case Keys.OemCloseBrackets: return Key.BracketRight;
                case Keys.Enter: return Key.Enter;
                case Keys.LControlKey: return Key.ControlLeft;
                case Keys.A: return Key.A;
                case Keys.S: return Key.S;

                // 32 - 47
                case Keys.D: return Key.D;
                case Keys.F: return Key.F;
                case Keys.G: return Key.G;
                case Keys.H: return Key.H;
                case Keys.J: return Key.J;
                case Keys.K: return Key.K;
                case Keys.L: return Key.L;
                case Keys.OemSemicolon: return Key.Semicolon;
                case Keys.OemQuotes: return Key.Quote;
                case Keys.Oemtilde: return Key.Grave;
                case Keys.LShiftKey: return Key.ShiftLeft;
                case Keys.OemBackslash: return Key.BackSlash;
                case Keys.Z: return Key.Z;
                case Keys.X: return Key.X;
                case Keys.C: return Key.C;
                case Keys.V: return Key.V;

                // 48 - 63
                case Keys.B: return Key.B;
                case Keys.N: return Key.N;
                case Keys.M: return Key.M;
                case Keys.Oemcomma: return Key.Comma;
                case Keys.OemPeriod: return Key.Period;
                case Keys.OemQuestion: return Key.Slash;
                case Keys.RShiftKey: return Key.ShiftRight;
                case Keys.PrintScreen: return Key.PrintScreen;
                case Keys.LMenu: return Key.AltLeft;
                case Keys.Space: return Key.Space;
                case Keys.CapsLock: return Key.CapsLock;
                case Keys.F1: return Key.F1;
                case Keys.F2: return Key.F2;
                case Keys.F3: return Key.F3;
                case Keys.F4: return Key.F4;
                case Keys.F5: return Key.F5;

                // 64 - 79
                case Keys.F6: return Key.F6;
                case Keys.F7: return Key.F7;
                case Keys.F8: return Key.F8;
                case Keys.F9: return Key.F9;
                case Keys.F10: return Key.F10;
                case Keys.NumLock: return Key.NumLock;
                case Keys.Scroll: return Key.ScrollLock;
                case Keys.Home: return Key.Home;
                case Keys.Up: return Key.Up;
                case Keys.PageUp: return Key.PageUp;
                case Keys.Subtract: return Key.KeypadMinus;
                case Keys.Left: return Key.Left;
                case Keys.Oem5: return Key.Keypad5;
                case Keys.Right: return Key.Right;
                case Keys.Add: return Key.KeypadPlus;
                case Keys.End: return Key.End;

                // 80 - 95
                case Keys.Down: return Key.Down;
                case Keys.PageDown: return Key.PageDown;
                case Keys.Insert: return Key.Insert;
                case Keys.Delete: return Key.Delete;
                case Keys.F11: return Key.F11;
                case Keys.F12: return Key.F12;
                case Keys.Pause: return Key.Pause;
                case Keys.LWin: return Key.WinLeft;
                case Keys.RWin: return Key.WinRight;
                case Keys.Menu: return Key.Menu;

                //Abominations
                case Keys.ShiftKey: return Key.ShiftLeft;
                case Keys.ControlKey: return Key.ControlLeft;

                default: return Key.Unknown;
            }
        }
    }
}
