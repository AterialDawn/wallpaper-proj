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
            var buttonInactiveColor = style.Colors[(int)ImGuiCol.Button];
            var buttonActiveColor = style.Colors[(int)ImGuiCol.ButtonActive];
            bool selectedTabChanged = false;
            for (int i = 0; i < tabNames.Length; i++)
            {
                string tab = tabNames[i];
                if (selectedTabIndex == i)
                {
                    style.Colors[(int)ImGuiCol.Button] = buttonActiveColor;
                }
                else
                {
                    style.Colors[(int)ImGuiCol.Button] = buttonInactiveColor;
                }
                if (i > 0) ImGui.SameLine();
                if (ImGui.Button($"{tab}##Tabs"))
                {
                    selectedTabChanged = true;
                    selectedTabIndex = i;
                }
            }

            style.Colors[(int)ImGuiCol.Button] = buttonInactiveColor;

            return selectedTabChanged;
        }
    }
}
