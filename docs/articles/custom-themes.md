# Custom Themes

Files allows you to customize the application with custom themes by modifying xaml resource dictionaries, this allows users to override the default brushes used throughout the app.

Themes are loaded from the `Themes` folder in the app's `LocalState` folder. A drop-down (Custom theme) is located in Appearance settings that allows you to select a custom theme.

### Quick start

1. Open your text editor of choice, and paste in the template located below. Then, save the file as `%userprofile%\AppData\Local\Packages\49306atecsolution.FilesUWP_et10x9a9vyk8t\LocalState\Themes\test1.xaml`.

2. Change some of the color values around.

3. Change the selected theme from the appearance settings page.

Files will now use your custom theme. If you add more themes, you can switch between them using the dropdown.

<details>
<summary>
Default template
</summary>

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:BelowWindows10version1809="http://schemas.microsoft.com/winfx/2006/xaml/presentation?IsApiContractNotPresent(Windows.Foundation.UniversalApiContract, 7)"
    xmlns:Windows10version1809="http://schemas.microsoft.com/winfx/2006/xaml/presentation?IsApiContractPresent(Windows.Foundation.UniversalApiContract, 7)">
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key="Default">
            <!-- Background Resources -->
            <SolidColorBrush x:Key="RootBackgroundBrush" Color="#AA7F3C4E" />
            <Color x:Key="SolidBackgroundFillColorBase">#97475C</Color>
            <Color x:Key="SolidBackgroundFillColorSecondary">#7F3C4E</Color>
            <Color x:Key="SolidBackgroundFillColorTertiary">#763B4A</Color>
            <Color x:Key="SolidBackgroundFillColorQuarternary">#592A37</Color>
            <SolidColorBrush x:Key="SolidBackgroundFillColorBaseBrush" Color="{ThemeResource SolidBackgroundFillColorBase}" />
            <SolidColorBrush x:Key="SolidBackgroundFillColorSecondaryBrush" Color="{ThemeResource SolidBackgroundFillColorSecondary}" />
            <SolidColorBrush x:Key="SolidBackgroundFillColorTertiaryBrush" Color="{ThemeResource SolidBackgroundFillColorTertiary}" />
            <SolidColorBrush x:Key="SolidBackgroundFillColorQuarternaryBrush" Color="{ThemeResource SolidBackgroundFillColorQuarternary}" />
            <!-- Acrylic Resources -->
            <Color x:Key="SolidBackgroundAcrylic">#592A37</Color>
            <!-- Accent Color -->
            <Color x:Key="SystemAccentColor">#CB607C</Color>
            <Color x:Key="SystemAccentColorLight1">#CB607C</Color>
            <Color x:Key="SystemAccentColorLight2">#CB607C</Color>
            <Color x:Key="SystemAccentColorLight3">#CB607C</Color>
            <Color x:Key="SystemAccentColorDark1">#CB607C</Color>
            <Color x:Key="SystemAccentColorDark2">#CB607C</Color>
            <Color x:Key="SystemAccentColorDark3">#CB607C</Color>
        </ResourceDictionary>
        <ResourceDictionary x:Key="Light">
            <!-- Background Resources -->
            <SolidColorBrush x:Key="RootBackgroundBrush" Color="#AAF17293" />
            <Color x:Key="SolidBackgroundFillColorBase">#97475C</Color>
            <Color x:Key="SolidBackgroundFillColorSecondary">#F17293</Color>
            <Color x:Key="SolidBackgroundFillColorTertiary">#E87490</Color>
            <Color x:Key="SolidBackgroundFillColorQuarternary">#CB607C</Color>
            <SolidColorBrush x:Key="SolidBackgroundFillColorBaseBrush" Color="{ThemeResource SolidBackgroundFillColorBase}" />
            <SolidColorBrush x:Key="SolidBackgroundFillColorSecondaryBrush" Color="{ThemeResource SolidBackgroundFillColorSecondary}" />
            <SolidColorBrush x:Key="SolidBackgroundFillColorTertiaryBrush" Color="{ThemeResource SolidBackgroundFillColorTertiary}" />
            <SolidColorBrush x:Key="SolidBackgroundFillColorQuarternaryBrush" Color="{ThemeResource SolidBackgroundFillColorQuarternary}" />
            <!-- Acrylic Resources -->
            <Color x:Key="SolidBackgroundAcrylic">#CB607C</Color>
            <!-- Accent Color -->
            <Color x:Key="SystemAccentColor">#97475C</Color>
            <Color x:Key="SystemAccentColorLight1">#97475C</Color>
            <Color x:Key="SystemAccentColorLight2">#97475C</Color>
            <Color x:Key="SystemAccentColorLight3">#97475C</Color>
            <Color x:Key="SystemAccentColorDark1">#97475C</Color>
            <Color x:Key="SystemAccentColorDark2">#97475C</Color>
            <Color x:Key="SystemAccentColorDark3">#97475C</Color>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```
</details> 

You can also use the [Fluent XAML Theme Editor](https://github.com/microsoft/fluent-xaml-theme-editor) to easily modify custom themes.

You can view other themes and contribute your own [here](https://github.com/files-community/custom-themes).
