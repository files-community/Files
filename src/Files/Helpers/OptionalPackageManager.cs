using System.Collections.Generic;

namespace Files.Helpers
{
    public class OptionalPackageManager
    {
        public List<Windows.ApplicationModel.Package> Packages { get; } = new List<Windows.ApplicationModel.Package>();

        public OptionalPackageManager()
        {
            foreach (var package in Windows.ApplicationModel.Package.Current.Dependencies)
            {
                if (package.IsOptional)
                {
                    Packages.Add(package);
                }
            }
        }

        public bool IsOptionalPackageInstalled(string familyName)
        {
            return Packages.Exists(x => x.Id.Name == familyName);
        }

        public bool TryGetOptionalPackage(string familyName, out Windows.ApplicationModel.Package package)
        {
            package = Packages.Find(x => x.Id.Name == familyName);
            return package != null;
        }
    }
}