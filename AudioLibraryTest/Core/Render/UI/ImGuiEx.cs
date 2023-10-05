using ImGuiNET;

namespace player.Core.Render.UI
{
    static internal class ImGuiEx
    {
        /// <summary>
        /// draws tab buttons in-line, tab-style
        /// </summary>
        /// <param name="tabNames">name of tabs</param>
        /// <param name="selectedTabIndex">ref integer indicating what tab is active</param>
        /// <returns>true if currently active tab was changed</returns>
        public static bool TabButtons(ref int selectedTabIndex, params string[] tabNames)
        {
            var style = ImGui.GetStyle();
            var buttonInactiveColor = style.GetColor(ColorTarget.Button);
            var buttonActiveColor = style.GetColor(ColorTarget.ButtonActive);
            bool selectedTabChanged = false;
            for (int i = 0; i < tabNames.Length; i++)
            {
                string tab = tabNames[i];
                if (selectedTabIndex == i)
                {
                    style.SetColor(ColorTarget.Button, buttonActiveColor);
                }
                else
                {
                    style.SetColor(ColorTarget.Button, buttonInactiveColor);
                }
                if (i > 0) ImGui.SameLine();
                if (ImGui.Button($"{tab}##Tabs"))
                {
                    selectedTabChanged = true;
                    selectedTabIndex = i;
                }
            }

            style.SetColor(ColorTarget.Button, buttonInactiveColor);

            return selectedTabChanged;
        }
    }
}
