using ImGuiNET;
using player.Core;
using player.Core.Render;
using player.Core.Render.UI;
using player.Core.Service;
using player.Core.Settings;
using player.Renderers.BarHelpers;
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
            Vector2 buttonSize = new Vector2(135, 19);
            WallpaperImageSettingsService wpSettings;
            FpsLimitOverrideContext fpsOverride = null;

            string[] renderModeItems = new string[] { "Default", "Solid Background" };
            string[] anchorPosItems = new string[] { "Centered", "Left", "Right" };

            public ImGuiHandler(BarRenderer parent)
            {
                this.parent = parent;

                ServiceManager.GetService<ImGuiManager>().OnDisplayingPopupMenu += ImGuiHandler_OnDisplayingPopupMenu;
                ServiceManager.GetService<ImGuiManager>().OnRenderingGui += ImGuiHandler_OnRenderingGui;
                settings = ServiceManager.GetService<SettingsService>();
                settings.SetSettingDefault("Wallpaper.MoveToPaths", new string[0]);

                wpSettings = ServiceManager.GetService<WallpaperImageSettingsService>();
            }

            private void ImGuiHandler_OnRenderingGui(object sender, EventArgs e)
            {
                if (imageInfoWindowVisible)
                {
                    if (ImGui.BeginWindow("Image Settings"))
                    {
                        if (fpsOverride == null)
                        {
                            fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("Image Settings", FpsLimitOverride.Maximum);
                        }
                        var curPath = parent.backgroundController.GetCurrentWallpaperPath();

                        if (parent.backgroundController.CurrentBackground is StaticImageBackground)
                        {
                            var curSettings = wpSettings.GetImageSettingsForPath(curPath);
                            int renderModeIdx = 0;
                            int anchorPosIdx = 0;
                            Vector4 color = Vector4.One;
                            int left = 0, right = 0, top = 0, bot = 0;
                            if (curSettings != null)
                            {
                                renderModeIdx = (int)curSettings.Mode;
                                anchorPosIdx = (int)curSettings.AnchorPosition;
                                color = curSettings.BackgroundColor;

                                left = curSettings.TrimPixelsLeft;
                                right = curSettings.TrimPixelsRight;
                                top = curSettings.TrimPixelsTop;
                                bot = curSettings.TrimPixelsBottom;
                            }
                            if (ImGui.Combo("Render Mode", ref renderModeIdx, renderModeItems))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).Mode = (BackgroundMode)renderModeIdx;
                            }

                            if (ImGui.Combo("Anchor Position", ref anchorPosIdx, anchorPosItems))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).AnchorPosition = (BackgroundAnchorPosition)anchorPosIdx;
                            }

                            if (ImGui.ColorPicker4("Background Color", ref color, ColorEditFlags.RGB | ColorEditFlags.NoOptions | ColorEditFlags.NoPicker | ColorEditFlags.NoSmallPreview | ColorEditFlags.NoTooltip))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).BackgroundColor = color;
                            }

                            ImGui.Text("Pixel Crop Offsets");
                            ImGui.PushItemWidth(75);
                            if (ImGui.SliderInt("##Left", ref left, 0, 30, $"{left}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsLeft = left;
                            }
                            ImGui.SameLine();
                            if (ImGui.SliderInt("##Right", ref right, 0, 30, $"{right}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsRight = right;
                            }
                            ImGui.SameLine();
                            if (ImGui.SliderInt("##Top", ref top, 0, 30, $"{top}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsTop = top;
                            }
                            ImGui.SameLine();
                            if (ImGui.SliderInt("##Bottom", ref bot, 0, 30, $"{bot}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsBottom = bot;
                            }

                            ImGui.PopItemWidth();

                            if (ImGui.Button("Redraw Image"))
                            {
                                parent.backgroundController.ImmediatelyLoadNewWallpaper(curPath);
                            }
                        }
                        else
                        {
                            ImGui.Text("Settings not yet supported for this background type");
                        }


                        ImGui.EndWindow();
                    }
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

            private void ImGuiHandler_OnDisplayingPopupMenu(object sender, EventArgs e)
            {
                string curWallpaperPath = parent.backgroundController.GetCurrentWallpaperPath();
                string curWallpaperName = Path.GetFileName(curWallpaperPath);

                ImGui.Text($"Wallpaper {curWallpaperName}");
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
                    ImGui.CloseCurrentPopup();
                }

                bool keep = parent.backgroundController.KeepCurrentBackground;
                if (ImGui.Checkbox("Keep Current Wallpaper", ref keep))
                {
                    parent.ToggleKeepRotation();
                }

                ImGui.Checkbox("Image Settings", ref imageInfoWindowVisible);

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
            }
        }
    }
}
