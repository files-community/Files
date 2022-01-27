using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.ViewModels.Layouts;

namespace Files.Backend.ViewModels.Shell
{
    public sealed class FuturisticShellPageViewModel : ObservableObject
    {
        public BaseLayoutViewModel ActiveLayoutViewModel { get; }
    }
}
