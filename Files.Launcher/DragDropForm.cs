using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace FilesFullTrust
{
    internal class DragDropForm : Form
    {
        private string dropPath;

        public List<string> DropTargets { get; private set; } = new List<string>();

        public DragDropForm(string dropPath, string dropText, System.Threading.CancellationToken token)
        {
            var appTheme = GetAppTheme();

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Files";
            this.BackColor = appTheme switch
            {
                Windows.UI.Xaml.ElementTheme.Light => System.Drawing.Color.White,
                _ => System.Drawing.Color.Black
            };
            this.Opacity = 0.5;
            this.TopMost = true;
            this.DragOver += DragDropForm_DragOver;
            this.DragDrop += DragDropForm_DragDrop;
            this.DragLeave += DragDropForm_DragLeave;
            this.AllowDrop = true;

            var label = new Label();
            label.AutoSize = false;
            label.Font = new System.Drawing.Font("Segoe UI", 24);
            label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            label.ForeColor = appTheme switch
            {
                Windows.UI.Xaml.ElementTheme.Light => System.Drawing.Color.Black,
                _ => System.Drawing.Color.White
            };
            label.Dock = DockStyle.Fill;
            label.Text = dropText;
            this.Controls.Add(label);
            this.dropPath = dropPath;

            // Create window over Files window
            this.StartPosition = FormStartPosition.Manual;
            var Handle = Vanara.PInvoke.User32.WindowFromPoint(Cursor.Position);
            Vanara.PInvoke.User32.GetWindowRect(Handle, out var lpRect);
            this.Size = new System.Drawing.Size(lpRect.Width, lpRect.Height);
            this.Location = new System.Drawing.Point(lpRect.Location.X, lpRect.Location.Y);

            token.Register(() =>
            {
                if (this.IsHandleCreated)
                {
                    // If another window is created, close this one
                    this.Invoke(new InvokeDelegate(() => this.Close()));
                }
            });
            this.HandleCreated += DragDropForm_HandleCreated;
        }

        private Windows.UI.Xaml.ElementTheme GetAppTheme()
        {
            var appTheme = Windows.UI.Xaml.ElementTheme.Default;
            var savedTheme = Windows.Storage.ApplicationData.Current.LocalSettings.Values["theme"]?.ToString();
            if (!string.IsNullOrEmpty(savedTheme))
            {
                Enum.TryParse(savedTheme, out appTheme);
            }
            if (appTheme == Windows.UI.Xaml.ElementTheme.Default)
            {
                var settings = new Windows.UI.ViewManagement.UISettings();
                appTheme = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).ToString() switch
                {
                    "#FFFFFFFF" => Windows.UI.Xaml.ElementTheme.Light,
                    "#FF000000" => Windows.UI.Xaml.ElementTheme.Dark,
                    _ => Windows.UI.Xaml.ElementTheme.Default // Unknown theme
                };
            }
            return appTheme;
        }

        public delegate void InvokeDelegate();

        private void DragDropForm_HandleCreated(object sender, EventArgs e)
        {
            var timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += (s, e) =>
            {
                if (!this.DesktopBounds.Contains(Cursor.Position))
                {
                    // After some time check whether the mouse is still inside the drop window
                    this.Close();
                    (s as Timer).Dispose();
                }
            };
            timer.Start();
        }

        private void DragDropForm_DragLeave(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DragDropForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop"))
            {
                var str = e.Data.GetData("FileDrop") as string[];
                DropTargets.AddRange(str);
                foreach (var file in DropTargets)
                {
                    try
                    {
                        // Move files to destination
                        // Works for 7zip, Winrar which unpack the items in the temp folder
                        var destName = Path.GetFileName(file.TrimEnd(Path.PathSeparator));
                        Directory.Move(file, Path.Combine(dropPath, destName));
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Warn(ex, "Failed to drop items");
                    }
                }
            }
            this.Close();
        }

        private void DragDropForm_DragOver(object sender, DragEventArgs e)
        {
            // Should handle "Shell ID List" as well
            if (e.Data.GetDataPresent("FileDrop"))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                // Turn on WS_EX_TOOLWINDOW style bit
                // Window won't show in alt-tab
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
    }
}