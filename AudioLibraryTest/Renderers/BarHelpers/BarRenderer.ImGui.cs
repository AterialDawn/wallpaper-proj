using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using player.Core;
using player.Core.Input;
using player.Core.Render;
using player.Core.Render.UI;
using player.Core.Service;
using player.Core.Settings;
using player.Renderers.BarHelpers;
using player.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Log = player.Core.Logging.Logger;
using Point = System.Drawing.Point;

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
            Vector2 pmButtonSize = new Vector2(33.5f, 20);
            WallpaperImageSettingsService wpSettings;
            bool forcedKeepWallpaper = false;
            Point lastMousePos = Point.Empty;
            Bitmap desktopBmp = new Bitmap(1, 1);
            Graphics desktopDC;
            IntPtr colorPickerTexture;
            Vector2 pickerSize = new Vector2(16, 16);

            string[] renderModeItems = new string[] { "Default", "Solid Background" };
            string[] anchorPosItems = new string[] { "Centered", "Left", "Right" };

            public ImGuiHandler(BarRenderer parent)
            {
                this.parent = parent;

                ServiceManager.GetService<ImGuiManager>().OnDisplayingPopupMenu += ImGuiHandler_OnDisplayingPopupMenu;
                ServiceManager.GetService<ImGuiManager>().OnRenderingGui += ImGuiHandler_OnRenderingGui;
                ServiceManager.GetService<InputManager>().MouseMoveEventRaw += ImGuiHandler_MouseMoveEventRaw;
                settings = ServiceManager.GetService<SettingsService>();
                settings.SetSettingDefault("Wallpaper.MoveToPaths", new string[0]);

                wpSettings = ServiceManager.GetService<WallpaperImageSettingsService>();
                desktopDC = Graphics.FromImage(desktopBmp);

                colorPickerTexture = new IntPtr(GL.GenTexture());

                TextureUtils.LoadBitmapIntoTexture(new Bitmap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/colPick.png")), (int)colorPickerTexture);
            }

            private void ImGuiHandler_MouseMoveEventRaw(object sender, OpenTK.Input.MouseMoveEventArgs e)
            {
                lastMousePos = new Point(e.X + WallpaperUtils.WallpaperBoundsCorrected.Left, e.Y + WallpaperUtils.WallpaperBoundsCorrected.Top);
            }

            private void ImGuiHandler_OnRenderingGui(object sender, EventArgs e)
            {
                if (imageInfoWindowVisible)
                {
                    if (ImGui.BeginWindow("Image Settings", ref imageInfoWindowVisible, WindowFlags.Default))
                    {
                        if (parent.backgroundController.LoadingNextWallpaper)
                        {
                            ImGui.Text("Busy loading a wallpaper...");
                        }
                        else if (parent.backgroundController.IsTransitioning())
                        {
                            ImGui.Text("Transitioning wallpapers...");
                            ImGui.ProgressBar(parent.backgroundController.BlendTimeRemainingPercentage, Vector2.Zero, "Blend Duration");
                        }
                        else
                        {
                            if (parent.backgroundController.CurrentBackground is StaticImageBackground bg)
                            {
                                var curPath = parent.backgroundController.GetCurrentWallpaperPath();
                                var curSettings = wpSettings.GetImageSettingsForPath(curPath);
                                if (curSettings != null && curSettings.EditingDisabled)
                                {
                                    ImGui.TextWrapped("Editing is locked. Press the unlock button to re-enable editing");
                                    if (ImGui.Button("Unlock"))
                                    {
                                        curSettings.EditingDisabled = false;
                                    }
                                }
                                else
                                {
                                    DrawStaticImageSettings(bg, curSettings, curPath);
                                }
                            }
                            else
                            {
                                ImGui.Text("Settings not yet supported for this background type");
                            }
                        }
                    }
                    ImGui.EndWindow();
                }
                else
                {
                    if (forcedKeepWallpaper)
                    {
                        forcedKeepWallpaper = false;
                        parent.backgroundController.KeepCurrentBackground = false;
                    }
                }
            }

            void DrawStaticImageSettings(StaticImageBackground bg, ImageSettings curSettings, string curPath)
            {
                int renderModeIdx = 0;
                int anchorPosIdx = 0;
                Vector4 color = Vector4.One;
                int left = 0, right = 0, top = 0, bot = 0;
                int rLeft = 0, rRight = 0, rTop = 0, rBot = 0;
                if (curSettings != null)
                {
                    renderModeIdx = (int)curSettings.Mode;
                    anchorPosIdx = (int)curSettings.AnchorPosition;
                    color = curSettings.BackgroundColor;

                    left = curSettings.TrimPixelsLeft;
                    right = curSettings.TrimPixelsRight;
                    top = curSettings.TrimPixelsTop;
                    bot = curSettings.TrimPixelsBottom;

                    rLeft = curSettings.RenderTrimLeft;
                    rRight = curSettings.RenderTrimRight;
                    rTop = curSettings.RenderTrimTop;
                    rBot = curSettings.RenderTrimBot;
                }
                int imageWidth = bg.SourceImageSize.Width;
                int imageHeight = bg.SourceImageSize.Height;
                if (ImGui.Combo("Render Mode", ref renderModeIdx, renderModeItems))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).Mode = (BackgroundMode)renderModeIdx;
                }

                if (ImGui.Combo("Anchor Position", ref anchorPosIdx, anchorPosItems))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).AnchorPosition = (BackgroundAnchorPosition)anchorPosIdx;
                }

                if (ImGui.ColorPicker4("Background Color", ref color, ColorEditFlags.RGB | ColorEditFlags.NoOptions | ColorEditFlags.NoPicker | ColorEditFlags.NoSmallPreview | ColorEditFlags.NoTooltip | ColorEditFlags.AlphaBar))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).BackgroundColor = color;
                }

                ImGui.ImageButton(colorPickerTexture, pickerSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One);
                if (ImGui.IsLastItemActive())
                {
                    desktopDC.CopyFromScreen(lastMousePos.X, lastMousePos.Y, 0, 0, new Size(1, 1), CopyPixelOperation.SourceCopy);

                    var sampled = desktopBmp.GetPixel(0, 0);
                    wpSettings.GetImageSettingsForPath(curPath, true).BackgroundColor = new Vector4(sampled.R / 255f, sampled.G / 255f, sampled.B / 255f, 1);
                }

                ImGui.SameLine();
                ImGui.Text("Source Crop (Left / Right / Top / Bot)");
                ImGui.PushItemWidth(75);
                if (ImGui.SliderInt("##Left", ref left, 0, imageWidth / 2, $"{left}"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsLeft = left;
                }
                ImGui.SameLine();
                if (ImGui.SliderInt("##Right", ref right, 0, imageWidth / 2, $"{right}"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsRight = right;
                }
                ImGui.SameLine();
                if (ImGui.SliderInt("##Top", ref top, 0, imageHeight / 2, $"{top}"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsTop = top;
                }
                ImGui.SameLine();
                if (ImGui.SliderInt("##Bottom", ref bot, 0, imageHeight / 2, $"{bot}"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsBottom = bot;
                }

                if (ImGui.Button("-##Left", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsLeft--;
                }
                ImGui.SameLine();
                if (ImGui.Button("+##Left", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsLeft++;
                }
                ImGui.SameLine();
                if (ImGui.Button("-##Right", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsRight--;
                }
                ImGui.SameLine();
                if (ImGui.Button("+##Right", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsRight++;
                }
                ImGui.SameLine();
                if (ImGui.Button("-##Top", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsTop--;
                }
                ImGui.SameLine();
                if (ImGui.Button("+##Top", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsTop++;
                }
                ImGui.SameLine();
                if (ImGui.Button("-##Bot", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsBottom--;
                }
                ImGui.SameLine();
                if (ImGui.Button("+##Bot", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsBottom++;
                }

                ImGui.Text("Render Crop");
                if (ImGui.SliderInt("##RLeft", ref rLeft, 0, 5, $"{rLeft}"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimLeft = rLeft;
                }
                ImGui.SameLine();
                if (ImGui.SliderInt("##RRight", ref rRight, 0, 5, $"{rRight}"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimRight = rRight;
                }
                ImGui.SameLine();
                if (ImGui.SliderInt("##RTop", ref rTop, 0, 5, $"{rTop}"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimTop = rTop;
                }
                ImGui.SameLine();
                if (ImGui.SliderInt("##RBottom", ref rBot, 0, 5, $"{rBot}"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimBot = rBot;
                }

                ImGui.PopItemWidth();

                if (ImGui.Button("-##RLeft", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimLeft--;
                }
                ImGui.SameLine();
                if (ImGui.Button("+##RLeft", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimLeft++;
                }
                ImGui.SameLine();
                if (ImGui.Button("-##RRight", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimRight--;
                }
                ImGui.SameLine();
                if (ImGui.Button("+##RRight", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimRight++;
                }
                ImGui.SameLine();
                if (ImGui.Button("-##RTop", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimTop--;
                }
                ImGui.SameLine();
                if (ImGui.Button("+##RTop", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimTop++;
                }
                ImGui.SameLine();
                if (ImGui.Button("-##RBot", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimBot--;
                }
                ImGui.SameLine();
                if (ImGui.Button("+##RBot", pmButtonSize))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimBot++;
                }
                if (ImGui.Button("Redraw Image"))
                {
                    parent.backgroundController.ImmediatelyLoadNewWallpaper(curPath);
                }
                ImGui.SameLine();
                if (ImGui.Button("Clear Settings"))
                {
                    wpSettings.ClearSettingsForPath(curPath);
                }
                ImGui.SameLine();
                if (ImGui.Button("Lock"))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).EditingDisabled = true;
                }
                ImGui.SameLine();
                bool hasCustomSettings = curSettings != null;
                ImGui.Checkbox("Has Settings", ref hasCustomSettings);
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
                    forcedKeepWallpaper = false;
                }

                if (ImGui.Checkbox("Image Settings", ref imageInfoWindowVisible))
                {
                    if (imageInfoWindowVisible)
                    {
                        if (!parent.backgroundController.KeepCurrentBackground)
                        {
                            parent.backgroundController.KeepCurrentBackground = true;
                            forcedKeepWallpaper = true;
                            Log.Log("Keeping current background");
                        }
                    }
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
            }
        }
    }
}
