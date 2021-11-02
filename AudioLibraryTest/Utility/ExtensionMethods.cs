using System;
using System.Reflection;
using OpenTK;

namespace player.Utility
{
    static class ExtensionMethods
    {
        public static IntPtr GetHandleOfGameWindow(this GameWindow window, bool ParentWindow)
        {
            Type NativeWindowType = window.GetType().BaseType.BaseType;
            FieldInfo impPropInfo = NativeWindowType.GetField("implementation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            object implementation = impPropInfo.GetValue(window);

            FieldInfo windowProp = null;
            if (ParentWindow)
            {
                windowProp = implementation.GetType().GetField("window", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            else
            {
                windowProp = implementation.GetType().GetField("child_window", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            object window_value = windowProp.GetValue(implementation);

            FieldInfo cWindHandleProp = window_value.GetType().GetField("handle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            object handle = cWindHandleProp.GetValue(window_value);

            IntPtr hnd = (IntPtr)handle;
            if (hnd == null) throw new ApplicationException("WindowHandle was null!");
            return hnd;

        }
    }
}
