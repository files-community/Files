using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.Preview
{
    public abstract class PreviewBase : UserControl
    {
        public static List<PreviewBase> FilePreviewControls => new List<PreviewBase>()
        {
            new ImagePreview(),
        };

        public static PreviewBase GetControlFromExtension(string ext)
        {
            foreach (var control in FilePreviewControls)
            {
                if (control.Extensions.Contains(ext))
                {
                    return control;
                }   
            }
            return null;
        }

        public abstract List<string> Extensions { get; }
        public abstract void SetFile(string path);
    }
}
