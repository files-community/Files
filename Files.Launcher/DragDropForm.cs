using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FilesFullTrust
{
    internal class DragDropForm : Form
    {
        private string dropPath;

        public List<string> DropTargets { get; private set; } = new List<string>();

        public DragDropForm(string dropPath, System.Threading.CancellationToken token)
        {
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.ShowInTaskbar = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Files";
            this.TopMost = true;
            this.DragOver += DragDropForm_DragOver;
            this.DragDrop += DragDropForm_DragDrop;
            this.DragLeave += DragDropForm_DragLeave;
            this.AllowDrop = true;
            this.Size = new System.Drawing.Size(200, 180);
            this.Location = new System.Drawing.Point(Cursor.Position.X - Size.Width / 2, Cursor.Position.Y - Size.Height / 2);
            this.StartPosition = FormStartPosition.Manual;
            var label = new Label();
            label.AutoSize = false;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            label.Dock = DockStyle.Fill;
            label.Text = "Drop here";
            this.Controls.Add(label);
            this.dropPath = dropPath;

            token.Register(() => {
                if (this.IsHandleCreated)
                {
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
                    this.Close();
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
            if (e.Data.GetDataPresent("FileDrop"))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
    }
}
