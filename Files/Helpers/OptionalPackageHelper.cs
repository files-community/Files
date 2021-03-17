using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public class OptionalPackageHelper
    {
        public List<string> OptionalPackageList { get; private set; }

        public OptionalPackageHelper()
        {
            OptionalPackageList = new List<string>();
            foreach (var package in Windows.ApplicationModel.Package.Current.Dependencies)
            {
                if (package.IsOptional)
                {
                    OptionalPackageList.Add(package.Id.Name);
                }
            }
        }

        public bool IsPackageInstalled(string name)
        {
            return OptionalPackageList.Contains(name);
        }

        public static class Packages
        {
            public const string SampleWidgetName = "6889f09e-8508-4cce-8819-546f904e48b0";
        }
    }
}
