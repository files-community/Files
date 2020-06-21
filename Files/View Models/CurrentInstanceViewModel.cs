using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.View_Models
{
    public class CurrentInstanceViewModel : ViewModelBase
    {
        private bool _IsPageTypeNotHome = false;

        public bool IsPageTypeNotHome
        {
            get => _IsPageTypeNotHome;
            set => Set(ref _IsPageTypeNotHome, value);
        }

        private bool _IsPageTypeNotRecycleBin = false;

        public bool IsPageTypeNotRecycleBin
        {
            get => _IsPageTypeNotRecycleBin;
            set => Set(ref _IsPageTypeNotRecycleBin, value);
        }
    }
}
