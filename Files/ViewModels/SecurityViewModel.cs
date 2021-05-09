using Microsoft.Toolkit.Mvvm.ComponentModel;
using NetCasbin;
using NetCasbin.Persist.FileAdapter;
using RBAC.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.ViewModels
{
    public class SecurityViewModel : ObservableObject
    {
        private ModelFixture _modelFixture;

        public ModelFixture ModelFixture { get => _modelFixture; set => _modelFixture = value; }

        private async Task<SecurityViewModel> Initialize()
        {
            return this;
        }

        public static Task<SecurityViewModel> CreateInstance()
        {
            var rbac = new SecurityViewModel();
            
            return rbac.Initialize();
        }

        public SecurityViewModel()
        {
            ModelFixture = new ModelFixture();
            var enforcer = new Enforcer(ModelFixture.GetNewRbacModel());
        }
    }
}
