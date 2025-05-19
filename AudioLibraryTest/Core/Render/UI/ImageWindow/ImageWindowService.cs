using ImGuiNET;
using player.Core.Service;
using player.Core.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using Log = player.Core.Logging.Logger;

namespace player.Core.Render.UI.ImageWindow
{
    unsafe partial class ImageWindowService : IService
    {
        static readonly Vector2 MaxVect = new Vector2(float.MaxValue, float.MaxValue);
        public string ServiceName => "ImageWindow";
        List<ImageWindow> windows = new List<ImageWindow>();
        FpsLimitOverrideContext ctx = null;
        ImageWindow curWindow = null;
        ImGuiIOPtr io;
        Settings settings;
        MessageCenterService messageCenter;
        bool addingImage = false;

        //imgui state tracking
        int selectedItemIndex = -1;
        IMGWindowData selectedData = null;
        IMGWindowData hoveredData = null;

        public void Initialize()
        {
            messageCenter = ServiceManager.GetService<MessageCenterService>();
            VisGameWindow.OnBeforeThreadedRender += VisGameWindow_OnBeforeThreadedRender;
            var img = ServiceManager.GetService<ImGuiManager>();
            img.OnRenderingGui += ImageWindowService_OnRenderingGui;
            img.AddOptionToTitlebarMenu(ImGuiManager.KnownTitlebars.Options, onOptionsDrawn);

            settings = new Settings(ServiceManager.GetService<SettingsService>());

            ThreadedLoaderContext.Instance.ExecuteOnLoaderThread(() =>
            {
                var data = settings.ImagesToDisplay.Value;
                for (int i = data.Count - 1; i >= 0; i--)
                {
                    var curData = data[i];
                    try
                    {
                        var window = new ImageWindow(curData);
                        windows.Add(window);
                    }
                    catch (FileNotFoundException)
                    {
                        data.RemoveAt(i);
                    }
                }
            });

            io = ImGui.GetIO();
        }

        private void onOptionsDrawn(ImGuiRenderingEventArgs obj)
        {
            ImGui.Checkbox("Image Window Options...", ref settings.ShowMenu.Value);
        }

        private void ImageWindowService_OnRenderingGui(object sender, EventArgs e)
        {
            foreach (var window in windows)
            {
                curWindow = window;
                ImGui.SetNextWindowSizeConstraints(Vector2.Zero, MaxVect, constrainToAspectRatio);

                var flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoCollapse;
                if (window.Data == hoveredData)
                {
                    flags = ImGuiWindowFlags.NoCollapse;
                }

                if (window.ImguiSetSize)
                {
                    window.ImguiSetSize = false;
                    ImGui.SetNextWindowSize(new Vector2(window.ImageSize.Width, window.ImageSize.Height));
                }
                if (ImGui.Begin(window.Name, flags))
                {
                    if (!window.Initialized)
                    {
                        var size = ImGui.GetWindowSize();
                        var pos = ImGui.GetWindowPos();

                        window.Size = new SizeF(size.X, size.Y);
                        window.Location = new PointF(pos.X, pos.Y);
                        window.Initialized = true;
                    }
                    ImGui.End();
                }
            }

            if (settings.ShowMenu.Value)
            {
                if (ImGui.Begin("Image Window Options"))
                {
                    IMGWindowData curHovered = null;
                    if (ImGui.BeginListBox("All Images"))
                    {
                        int i = 0;
                        foreach (var toDisplay in settings.ImagesToDisplay.Value)
                        {
                            bool isCurrentSelected = selectedItemIndex == i;

                            if (ImGui.Selectable(toDisplay.Path, isCurrentSelected))
                            {
                                selectedItemIndex = i;
                                selectedData = toDisplay;
                            }

                            if (isCurrentSelected) ImGui.SetItemDefaultFocus();
                            if (ImGui.IsItemHovered()) curHovered = toDisplay;

                            i++;
                        }
                        ImGui.EndListBox();
                    }

                    hoveredData = curHovered;

                    bool lAdding = addingImage; //create local copy to avoid threading issues
                    if (lAdding) ImGui.BeginDisabled();
                    if (ImGui.Button("Add New Image..."))
                    {
                        addingImage = true;
                        startAddImageDialog();
                    }
                    if (lAdding) ImGui.EndDisabled();
                    ImGui.SameLine();
                    if (selectedItemIndex == -1) ImGui.BeginDisabled();

                    if (ImGui.Button("Remove Selected Image"))
                    {
                        messageCenter.ShowMessage($"Image {settings.ImagesToDisplay.Value[selectedItemIndex]} removed!");
                        settings.ImagesToDisplay.Value.RemoveAt(selectedItemIndex);
                        var window = windows.FirstOrDefault(w => w.Data == selectedData);
                        if (window != null)
                        {
                            window.Dispose();
                            windows.Remove(window);
                            selectedData = null;
                        }
                        else
                        {
                            Log.Log($"IMG Window not found for path {selectedData.Path} when deleting!");
                        }
                        
                    }

                    if (selectedItemIndex == -1) ImGui.EndDisabled();
                    ImGui.End();
                }
            }
        }

        void startAddImageDialog()
        {
            var thr = new System.Threading.Thread(() =>
            {
                try
                {
                    using (var ofd = new System.Windows.Forms.OpenFileDialog() { Title = "Select a valid Image/Video file" })
                    {
                        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            var selectedPath = ofd.FileName;

                            var data = new IMGWindowData { Path = selectedPath };

                            settings.ImagesToDisplay.Value.Add(data);

                            MainThreadDelegator.InvokeOn(InvocationTarget.BeforeRender, () =>
                            {
                                windows.Add(new ImageWindow(data) { ImguiSetSize = true });

                                messageCenter.ShowMessage($"Added IMW : {Path.GetFileName(selectedPath)}");
                            });
                        }
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Log.Log($"Could not load file : {ex}");
                    messageCenter.ShowMessage($"Could not load file! : {ex.Message}");
                }
                finally
                {
                    addingImage = false;
                }
            });
            thr.SetApartmentState(System.Threading.ApartmentState.STA);
            thr.IsBackground = true;
            thr.Start();
        }

        private unsafe void constrainToAspectRatio(ImGuiSizeCallbackData* data)
        {
            //check if we should constrain
            if (ImGui.IsWindowFocused() && io.KeyShift && io.MouseDown[0])
            {
                //current window is focused, user is holding shift and dragging with mouse
                float aspectRatio = curWindow.ImageSize.Width / curWindow.ImageSize.Height;
                var newSize = data->DesiredSize.X / aspectRatio;
                data->DesiredSize.Y = newSize;
            }

            if (data->DesiredSize.X != data->CurrentSize.Y || data->DesiredSize.Y != data->CurrentSize.Y)
            {
                curWindow.Initialized = false; //force internal basecontrol window to resize
            }
        }

        private void VisGameWindow_OnBeforeThreadedRender(object sender, EventArgs e)
        {
            bool any = windows.Any();

            if (any && ctx == null)
            {
                ctx = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("IMGImageWindow", FpsLimitOverride.Maximum);
            }
            else if (!any && ctx != null)
            {
                ctx.Dispose();
                ctx = null;
            }
        }

        public void Cleanup()
        {
            foreach (var window in windows)
            {
                window.Dispose();
            }
        }

        class Settings
        {
            public SettingsAccessor<List<IMGWindowData>> ImagesToDisplay { get; private set; }
            public SettingsAccessor<bool> ShowMenu { get; private set; }
            public Settings(SettingsService svc)
            {
                ImagesToDisplay = svc.GetAccessor("IMW.Images", new List<IMGWindowData>());
                ShowMenu = svc.GetAccessor("IMG.ShowMenu", true);
            }
        }
    }
}
