using ImGuiNET;
using player.Core.Render.UI;
using player.Core.Service;
using player.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Log = player.Core.Logging.Logger;

namespace player.Renderers
{
    partial class BarRenderer
    {
        class ImGuiHandler
        {
            BarRenderer parent;
            SettingsService settings;
            bool imageInfoWindowVisible = false;

            public ImGuiHandler(BarRenderer parent)
            {
                this.parent = parent;

                ServiceManager.GetService<ImGuiManager>().OnDisplayingPopupMenu += ImGuiHandler_OnDisplayingPopupMenu;
                settings = ServiceManager.GetService<SettingsService>();
                settings.SetSettingDefault("Wallpaper.MoveToPaths", new string[0]);

                ServiceManager.GetService<ImGuiManager>().OnRenderingGui += ImGuiHandler_OnRenderingGui;
            }

            private void ImGuiHandler_OnRenderingGui(object sender, EventArgs e)
            {
                if (imageInfoWindowVisible)
                {
                    var buttonSize = new Vector2(135, 19);
                    if (ImGui.BeginWindow("Image Options"))
                    {
                        string curWallpaperPath = parent.backgroundController.GetCurrentWallpaperPath();
                        string curWallpaperName = Path.GetFileName(curWallpaperPath);

                        ImGui.Text("Image Name : ");

                        ImGui.SameLine();

                        ImGui.Text(curWallpaperName);

                        ImGui.Separator();

                        if (ImGui.Button("Next Wallpaper", buttonSize))
                        {
                            parent.NewWallpaper(false, true);
                        }

                        if (ImGui.Button("Previous Wallpaper", buttonSize))
                        {
                            parent.NewWallpaper(true, true);
                        }

                        if (ImGui.Button("Open in Explorer", buttonSize))
                        {
                            parent.backgroundController.OpenCurrentWallpaper();
                        }

                        var paths = settings.GetSettingAs<string[]>("Wallpaper.MoveToPaths", null);
                        if (paths != null && paths.Length > 0)
                        {
                            ImGui.Separator();
                            if (ImGui.BeginMenu("Move To Paths"))
                            {
                                foreach (var path in paths)
                                {
                                    try
                                    {
                                        FileAttributes fa = File.GetAttributes(path);

                                        if ((fa & FileAttributes.Directory) == FileAttributes.Directory)
                                        {
                                            if (ImGui.Button(Path.GetFileName(path)))
                                            {
                                                ImGui.CloseCurrentPopup();
                                                try
                                                {
                                                    string destination = Path.Combine(path, curWallpaperName);
                                                    Log.Log($"Moving {curWallpaperPath} to {destination}");

                                                    File.Move(curWallpaperPath, destination);

                                                    parent.NewWallpaper(false, true);
                                                }
                                                catch (Exception exc)
                                                {
                                                    Log.Log($"Error moving wallpaper : {exc}");
                                                    parent.DisplayFadeoutMessage($"Could not move wallpaper : {exc.Message}");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            ImGui.Text($"Invalid Path : {Path.GetFileName(path)}");
                                        }

                                    }
                                    catch { }
                                }
                                ImGui.EndMenu();
                            }
                        }
                        ImGui.EndWindow();
                    }
                }
            }

            private void ImGuiHandler_OnDisplayingPopupMenu(object sender, EventArgs e)
            {
                string curWallpaperPath = parent.backgroundController.GetCurrentWallpaperPath();
                string curWallpaperName = Path.GetFileName(curWallpaperPath);

                ImGui.Text($"Wallpaper {curWallpaperName}");
                ImGui.Separator();

                if (ImGui.Checkbox("Show Image Options Window", ref imageInfoWindowVisible))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }
}
