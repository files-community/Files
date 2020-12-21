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
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Files";
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
                        Debug.WriteLine(ex);
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