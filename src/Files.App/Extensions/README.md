# `src/Files.App/Extensions`

This folder contains extension classes that provide methods for performing operations such as localization and string manipulation.

---

For example, it contains the source file `Fractions.cs` which allows for converting double values into fractions.
It also contains `LocalizationExtensions.cs` which allows the developer to insert a string with the following format:

```cs
using Files.App.Extensions;

namespace Files.Example;

class Program
{
    static void Main(string[] args)
    {
        private string _exampleResource
            => "ExampleStringID".ToLocalized();
    }
}
```
