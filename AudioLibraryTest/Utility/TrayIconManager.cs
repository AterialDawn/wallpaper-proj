using player.Core;
using player.Core.Render;
using player.Core.Render.UI.Controls;
using player.Core.Service;
using player.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace player.Utility
{
    class TrayIconManager : IService
    {
        public string ServiceName => "TrayIconManager";

        public ToolStripSeparator Separator { get; private set; } = new ToolStripSeparator();

        NotifyIcon trayIcon;
        ContextMenu trayContextMenu;
        Thread trayIconThread;
        MenuItem reloadSettingsItem;

        public event EventHandler<TrayIconManagerEventArgs> BeforeTrayIconShown;
        MenuItem exitItem;
        NoWindowApplicationContext appContext = new NoWindowApplicationContext();

        public TrayIconManager()
        {
            trayIconThread = new Thread(() =>
            {
                trayIcon = new NotifyIcon();
                trayIcon.Visible = true;
                try
                {
                    trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
                catch
                {
                    trayIcon.Icon = System.Drawing.SystemIcons.Information;
                }
#if DEBUG
                trayIcon.Text = "player - DEBUG";
#else
                trayIcon.Text = "player";
#endif
                trayIcon.ContextMenu = trayContextMenu = new ContextMenu();

                exitItem = new MenuItem("Exit");
                exitItem.Click += ExitItem_Click;

                reloadSettingsItem = new MenuItem("Change Settings File...");
                reloadSettingsItem.Click += ReloadSettingsItem_Click;

                trayContextMenu.MenuItems.Add(exitItem);
                trayContextMenu.Popup += TrayContextMenu_Popup;


                Application.Run(appContext);

                trayIcon.Visible = false;
                trayIcon.Dispose();
            });

            trayIconThread.SetApartmentState(ApartmentState.STA);
            trayIconThread.IsBackground = true;
            trayIconThread.Start();
        }

        private void ReloadSettingsItem_Click(object sender, EventArgs e)
        {
            SettingsService settings = ServiceManager.GetService<SettingsService>();
            using (OpenFileDialog ofd = new OpenFileDialog { InitialDirectory = settings.GetFileRelativeToSettings(""), Filter = "Settings File|*.json", Title = "Select a settings file relative to the current working folder" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string selectedFilename = Path.GetFileName(ofd.FileName);
                    string absoluteSelectedFileName = settings.GetFileRelativeToSettings(selectedFilename);
                    if (!File.Exists(absoluteSelectedFileName))
                    {
                        ServiceManager.GetService<MessageCenterService>().ShowMessage($"Requested settings file does not exist in {settings.GetFileRelativeToSettings(".")}");
                        return;
                    }
                    settings.ReloadSettingsFile(Path.GetFileNameWithoutExtension(ofd.FileName));
                    ServiceManager.GetService<MessageCenterService>().ShowMessage($"Settings file changed to {selectedFilename}");
                }
            }
        }

        public void Initialize()
        {
            
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            VisGameWindow.ThisForm.Exit();
        }

        private void TrayContextMenu_Popup(object sender, EventArgs e)
        {
            trayContextMenu.MenuItems.Clear();
            BeforeTrayIconShown?.Invoke(this, new TrayIconManagerEventArgs(trayIcon, trayContextMenu));
            trayContextMenu.MenuItems.Add(reloadSettingsItem);
            trayContextMenu.MenuItems.Add(exitItem);
        }

        public void Cleanup()
        {
            appContext.OnShutdown();
        }

        public class TrayIconManagerEventArgs
        {
            public NotifyIcon Icon { get; private set; }
            public ContextMenu ContextMenu { get; private set; }

            public TrayIconManagerEventArgs(NotifyIcon icon, ContextMenu menu)
            {
                Icon = icon;
                ContextMenu = menu;
            }
        }

        class NoWindowApplicationContext : ApplicationContext
        {
            public void OnShutdown()
            {
                this.ExitThread();
            }
        }
    }
}
