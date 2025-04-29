using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using player.Core;
using player.Core.Input;
using player.Core.Render.UI;
using player.Core.Service;
using player.Core.Settings;
using player.Renderers.BarHelpers;
using player.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using Log = player.Core.Logging.Logger;
using Point = System.Drawing.Point;

namespace player.Renderers
{
    partial class BarRenderer
    {
        class ImGuiHandler
        {
            const float HOLD_ROTATION_TIME = 5f;
            BarRenderer parent;
            SettingsService settings;
            Vector2 buttonSize = new Vector2(135, 19);
            Vector2 pmButtonSize = new Vector2(33.5f, 20);
            Vector2 stretchButtonSize = new Vector2(67f, 20);
            WallpaperImageSettingsService wpSettings;
            Point lastMousePos = Point.Empty;
            Bitmap desktopBmp = new Bitmap(1, 1);
            Graphics desktopDC;
            IntPtr colorPickerTexture;
            Vector2 pickerSize = new Vector2(16, 16);
            InputManager inputs;
            int selectedTabIdx = 0;
            int bgStyleIndex;
            SettingsAccessor<List<string>> quickloadImagesSetting;
            SettingsAccessor<bool> imageInfoWindowVisible;

            float timeToHoldRotation = 0;

            string[] renderModeItems = new string[] { "Default", "Solid Background" };
            string[] anchorPosItems = new string[] { "Centered", "Left", "Right" };
            string[] imageFlipItems = new string[] { "None", "Flip X", "Flip Y", "Flip X Y" };
            string[] bgStyleItems = new string[] { "Solid Color", "Source Region (Mirror)", "Source Region (Stretch)", "Stretch Edges" };

            public ImGuiHandler(BarRenderer parent)
            {
                this.parent = parent;

                ServiceManager.GetService<ImGuiManager>().OnDisplayingPopupMenu += ImGuiHandler_OnDisplayingPopupMenu;
                ServiceManager.GetService<ImGuiManager>().OnRenderingGui += ImGuiHandler_OnRenderingGui;
                ServiceManager.GetService<InputManager>().MouseMoveEventRaw += ImGuiHandler_MouseMoveEventRaw;
                inputs = ServiceManager.GetService<InputManager>();
                settings = ServiceManager.GetService<SettingsService>();
                settings.SetSettingDefault("Wallpaper.MoveToPaths", new string[0]);
                quickloadImagesSetting = settings.GetAccessor("bar.quickloadimages", new List<string>());
                imageInfoWindowVisible = settings.GetAccessor("Wallpaper.ShowEditor", true);

                wpSettings = ServiceManager.GetService<WallpaperImageSettingsService>();
                desktopDC = Graphics.FromImage(desktopBmp);

                colorPickerTexture = new IntPtr(GL.GenTexture());

                TextureUtils.LoadBitmapIntoTexture(new Bitmap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/colPick.png")), (int)colorPickerTexture);
            }

            private void ImGuiHandler_MouseMoveEventRaw(object sender, MouseMoveEventArgs e)
            {
                lastMousePos = new Point(e.X + WallpaperUtils.WallpaperBoundsCorrected.Left, e.Y + WallpaperUtils.WallpaperBoundsCorrected.Top);
            }

            private void ImGuiHandler_OnRenderingGui(object sender, EventArgs e)
            {
                if (imageInfoWindowVisible.Value)
                {
                    if (ImGui.Begin("Image Settings", ref imageInfoWindowVisible.Value, ImGuiWindowFlags.None))
                    {
                        if (parent.backgroundController.LoadingNextWallpaper)
                        {
                            ImGui.Text("Busy loading a wallpaper...");
                        }
                        else if (parent.backgroundController.IsTransitioning)
                        {
                            ImGui.Text("Transitioning wallpapers...");
                            ImGui.ProgressBar(parent.backgroundController.BlendTimeRemainingPercentage, Vector2.Zero, "Blend Duration");
                        }
                        else
                        {
                            var curPath = parent.backgroundController.GetCurrentWallpaperPath();
                            var curSettings = wpSettings.GetImageSettingsForPath(curPath);
                            if (curSettings != null && curSettings.EditingDisabled)
                            {
                                ImGui.TextWrapped("Editing is locked. Press the unlock button to re-enable editing");
                                if (ImGui.Button("Unlock"))
                                {
                                    wpSettings.GetImageSettingsForPath(curPath, true).EditingDisabled = false;
                                }
                            }
                            else
                            {
                                RenderBackgroundModeSettings(parent.backgroundController.CurrentBackground, curSettings, curPath);
                            }
                        }
                    }
                    ImGui.End();
                }

                if (timeToHoldRotation > 0)
                {
                    timeToHoldRotation -= TimeManager.Delta;
                    if (timeToHoldRotation <= 0)
                    {
                        parent.backgroundController.KeepCurrentBackground = false;
                        Log.Log("Hold rotation expired, disabling KeepCurrentBackground");
                    }
                }

                if (wpSettings.WereSettingsUpdatedThisFrame && !parent.backgroundController.KeepCurrentBackground)
                {
                    timeToHoldRotation = HOLD_ROTATION_TIME;
                    parent.backgroundController.KeepCurrentBackground = true;
                    Log.Log($"Settings Updated. Holding current wallpaper for {timeToHoldRotation} seconds");
                }
            }

            void RenderBackgroundModeSettings(IBackground bg, ImageSettings curSettings, string curPath)
            {
                int renderModeIdx = 0;
                int anchorPosIdx = 0;
                int flipIdx = 0;
                Vector4 color = Vector4.One;
                int left = 0, right = 0, top = 0, bot = 0;
                int rLeft = 0, rRight = 0, rTop = 0, rBot = 0;
                if (curSettings != null)
                {
                    renderModeIdx = (int)curSettings.Mode;
                    anchorPosIdx = (int)curSettings.AnchorPosition;
                    switch (curSettings.FlipMode)
                    {
                        case FlipMode.FlipX: flipIdx = 1; break;
                        case FlipMode.FlipY: flipIdx = 2; break;
                        case FlipMode.FlipXY: flipIdx = 3; break;
                    }
                    color = curSettings.BackgroundColor;

                    left = curSettings.TrimPixelsLeft;
                    right = curSettings.TrimPixelsRight;
                    top = curSettings.TrimPixelsTop;
                    bot = curSettings.TrimPixelsBottom;

                    rLeft = curSettings.RenderTrimLeft;
                    rRight = curSettings.RenderTrimRight;
                    rTop = curSettings.RenderTrimTop;
                    rBot = curSettings.RenderTrimBot;

                    bgStyleIndex = (int)curSettings.BackgroundStyle;
                }
                int imageWidth = (int)bg.Resolution.Width;
                int imageHeight = (int)bg.Resolution.Height;
                StaticImageBackground sib = bg as StaticImageBackground;
                if (sib != null)
                {
                    imageWidth = sib.SourceImageSize.Width;
                    imageHeight = sib.SourceImageSize.Width;
                }

                if (ImGui.Combo("Render Mode", ref renderModeIdx, renderModeItems, renderModeItems.Length))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).Mode = (BackgroundMode)renderModeIdx;
                }

                if (ImGui.Combo("Anchor Position", ref anchorPosIdx, anchorPosItems, anchorPosItems.Length))
                {
                    wpSettings.GetImageSettingsForPath(curPath, true).AnchorPosition = (BackgroundAnchorPosition)anchorPosIdx;
                }

                ImGuiEx.TabButtons(ref selectedTabIdx, "Color&Pos", "Background");

                if (selectedTabIdx == 0) //Color&Pos
                {
                    if (ImGui.Combo("Image Flip", ref flipIdx, imageFlipItems, imageFlipItems.Length))
                    {
                        var newFlipMode = FlipMode.None;
                        switch (flipIdx)
                        {
                            case 1: newFlipMode = FlipMode.FlipX; break;
                            case 2: newFlipMode = FlipMode.FlipY; break;
                            case 3: newFlipMode = FlipMode.FlipXY; break;
                        }
                        wpSettings.GetImageSettingsForPath(curPath, true).FlipMode = newFlipMode;
                    }

                    if (ImGui.ColorPicker4("Background Color", ref color, ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoSmallPreview | ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.AlphaBar))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).BackgroundColor = color;
                    }

                    ImGui.ImageButton("colorPickerBtn", colorPickerTexture, pickerSize, Vector2.Zero, Vector2.One);
                    if (ImGui.IsItemActive())
                    {
                        desktopDC.CopyFromScreen(lastMousePos.X, lastMousePos.Y, 0, 0, new Size(1, 1), CopyPixelOperation.SourceCopy);

                        var sampled = desktopBmp.GetPixel(0, 0);
                        wpSettings.GetImageSettingsForPath(curPath, true).BackgroundColor = new Vector4(sampled.R / 255f, sampled.G / 255f, sampled.B / 255f, 1);
                    }

                    ImGui.SameLine();

                    if (sib == null)
                    {
                        ImGui.Text("Source Crop (Disabled for this type)");
                        ImGui.BeginDisabled();
                    }
                    else
                    {
                        ImGui.Text("Source Crop (Left / Right / Top / Bot)");
                    }
                    ImGui.PushItemWidth(75);
                    if (ImGui.SliderInt("##Left", ref left, 0, imageWidth / 2, $"{left}"))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsLeft = left;
                        if (inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight))
                        {
                            right = left;
                            wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsRight = left;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.SliderInt("##Right", ref right, 0, imageWidth / 2, $"{right}"))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsRight = right;
                        if (inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight))
                        {
                            left = right;
                            wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsLeft = right;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.SliderInt("##Top", ref top, 0, imageHeight / 2, $"{top}"))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsTop = top;
                        if (inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight))
                        {
                            bot = top;
                            wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsBottom = top;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.SliderInt("##Bottom", ref bot, 0, imageHeight / 2, $"{bot}"))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsBottom = bot;
                        if (inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight))
                        {
                            top = bot;
                            wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsTop = bot;
                        }
                    }

                    int buttonAmount = inputs.AreAnyKeysPressed(Key.ControlLeft, Key.ControlRight) ? 5 : 1; //ctrl is 5
                    buttonAmount *= inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight) ? 10 : 1; //shift is *10

                    if (ImGui.Button("-##Left", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsLeft -= buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+##Left", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsLeft += buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("-##Right", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsRight -= buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+##Right", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsRight += buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("-##Top", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsTop -= buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+##Top", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsTop += buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("-##Bot", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsBottom -= buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+##Bot", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).TrimPixelsBottom += buttonAmount;
                    }

                    if (sib == null)
                    {
                        ImGui.EndDisabled();
                    }

                    ImGui.Text("Render Crop");
                    if (ImGui.SliderInt("##RLeft", ref rLeft, 0, 5, $"{rLeft}"))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimLeft = rLeft;
                        if (inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight))
                        {
                            rRight = rLeft;
                            wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimLeft = rLeft;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.SliderInt("##RRight", ref rRight, 0, 5, $"{rRight}"))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimRight = rRight;
                        if (inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight))
                        {
                            rLeft = rRight;
                            wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimRight = rLeft;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.SliderInt("##RTop", ref rTop, 0, 5, $"{rTop}"))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimTop = rTop;
                        if (inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight))
                        {
                            rBot = rTop;
                            wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimBot = rTop;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.SliderInt("##RBottom", ref rBot, 0, 5, $"{rBot}"))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimBot = rBot;
                        if (inputs.AreAnyKeysPressed(Key.ShiftLeft, Key.ShiftRight))
                        {
                            rTop = rBot;
                            wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimTop = rBot;
                        }
                    }

                    ImGui.PopItemWidth();

                    if (ImGui.Button("-##RLeft", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimLeft -= buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+##RLeft", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimLeft += buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("-##RRight", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimRight -= buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+##RRight", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimRight += buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("-##RTop", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimTop -= buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+##RTop", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimTop += buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("-##RBot", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimBot -= buttonAmount;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("+##RBot", pmButtonSize))
                    {
                        wpSettings.GetImageSettingsForPath(curPath, true).RenderTrimBot += buttonAmount;
                    }

                }
                else if (selectedTabIdx == 1) //Testing
                {
                    if (curSettings == null || curSettings.Mode != BackgroundMode.SolidBackground) //null implies default rendering mode, no special settings available in default mode here anyways (for now)
                    {
                        ImGui.Text("No settings available for this mode");
                    }
                    else
                    {
                        if (ImGui.Combo("Background Style", ref bgStyleIndex, bgStyleItems, bgStyleItems.Length))
                        {
                            //update background style mode
                            wpSettings.GetImageSettingsForPath(curPath, true).BackgroundStyle = (SolidBackgroundStyle)bgStyleIndex;
                        }

                        if (bgStyleIndex == 0)
                        {
                            //Solid Color
                            ImGui.Text("Solid Color, Set color in Color&Pos menu");
                        }
                        else if (bgStyleIndex == 3)
                        {
                            int xPos = 0;
                            int width = 10;
                            if (curSettings != null)
                            {
                                xPos = curSettings.StretchXPos;
                                width = curSettings.StretchWidth;
                            }
                            //stretch edges
                            ImGui.Text("Stretch X Position | Stretch Width");
                            ImGui.PushItemWidth(150);
                            if (ImGui.SliderInt("##StretchX", ref xPos, 0, imageWidth, $"{xPos}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).StretchXPos = xPos;
                            }
                            ImGui.SameLine();
                            if (ImGui.SliderInt("##StretchW", ref width, 1, imageWidth, $"{width}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).StretchWidth = width;
                            }

                            if (ImGui.Button("-##StretchX", stretchButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).StretchXPos--;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("+##StretchX", stretchButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).StretchXPos++;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("-##StretchW", stretchButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).StretchWidth--;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("+##StretchW", stretchButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).StretchWidth++;
                            }

                            ImGui.PopItemWidth();
                        }
                        else
                        {
                            ImGui.Text("Soon TM");
                            /*
                            int topPos = 0;
                            int leftPos = 0;
                            int botPos = 10;
                            int rightPos = 10;
                            if (curSettings != null)
                            {
                                topPos = curSettings.SrcSampleTop;
                                leftPos = curSettings.SrcSampleLeft;
                                botPos = curSettings.SrcSampleBot;
                                rightPos = curSettings.SrcSampleRight;
                            }


                            ImGui.Text("Source Sample Position");
                            ImGui.PushItemWidth(75);
                            if (ImGui.SliderInt("##Top", ref topPos, 0, imageHeight, $"{topPos}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleTop = topPos;
                            }
                            ImGui.SameLine();
                            if (ImGui.SliderInt("##Left", ref leftPos, 0, imageWidth, $"{leftPos}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleLeft = leftPos;
                            }
                            ImGui.SameLine();
                            if (ImGui.SliderInt("##Bottom", ref botPos, 0, imageHeight, $"{botPos}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleBot = botPos;
                            }
                            ImGui.SameLine();
                            if (ImGui.SliderInt("##Right", ref rightPos, 0, imageWidth, $"{rightPos}"))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleRight = rightPos;
                            }

                            if (ImGui.Button("-##SamTop", pmButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleTop--;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("+##SamTop", pmButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleTop++;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("-##SamLeft", pmButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleLeft--;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("+##SamLeft", pmButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleLeft++;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("-##SamBot", pmButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleBot--;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("+##SamBot", pmButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleBot++;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("-##SamRight", pmButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleRight--;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("+##SamRight", pmButtonSize))
                            {
                                wpSettings.GetImageSettingsForPath(curPath, true).SrcSampleRight++;
                            }

                            ImGui.PopItemWidth();
                            */
                        }

                    }
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
                }

                ImGui.Checkbox("Image Settings", ref imageInfoWindowVisible.Value);

                ImGui.Separator();

                if (ImGui.BeginMenu("Quickload Images"))
                {
                    var lst = quickloadImagesSetting.Value;
                    var currentWp = parent.backgroundController.CurrentBackground;
                    if (ImGui.Button("Add Current to Quickload"))
                    {
                        ImGui.CloseCurrentPopup();
                        if (currentWp == null)
                        {
                            parent.DisplayFadeoutMessage($"No loaded wallpaper?");
                        }
                        else
                        {
                            if (lst.Contains(currentWp.SourcePath))
                            {
                                parent.DisplayFadeoutMessage("Image already exists in quickload setting!");
                            }
                            else
                            {
                                lst.Add(currentWp.SourcePath);
                                parent.DisplayFadeoutMessage($"Added {currentWp.SourcePath} to quickload!");
                            }
                        }
                    }
                    if (currentWp != null && lst.Contains(currentWp.SourcePath) && ImGui.Button("Remove Current from Quickload"))
                    {
                        ImGui.CloseCurrentPopup();
                        lst.Remove(currentWp.SourcePath);
                        Log.Log($"{currentWp.SourcePath} removed from Quickload");
                    }

                    if (lst.Count > 0)
                    {
                        ImGui.Separator();
                        foreach (var imgPath in lst)
                        {
                            if (ImGui.Button(Path.GetFileName(imgPath)))
                            {
                                ImGui.CloseCurrentPopup();

                                if (parent.backgroundController.ImmediatelyLoadNewWallpaper(imgPath))
                                {
                                    parent.DisplayFadeoutMessage($"{imgPath} loaded!");
                                }
                                else
                                {
                                    parent.DisplayFadeoutMessage("Unable to load image");
                                }
                            }
                        }
                    }

                    ImGui.EndMenu();
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
