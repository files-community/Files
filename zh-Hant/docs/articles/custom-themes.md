# 客製化主題

Files 允許您修改應用程式中的 xaml 文件，這代表使用者們可以完全修改掉 Files 的外觀。

客製化主題將從 Files 主程式資料夾中的 `LocalState\Themes` 資料夾載入，並透過「外觀」設定選單的一處下拉式選單中選擇您的主題。

### 快速上手

1. 開啟記事本，打上下方的主題樣板。完成後將文件另存新檔並儲存至 `%userprofile%\AppData\Local\Packages\49306atecsolution.FilesUWP_et10x9a9vyk8t\LocalState\Themes\test1.xaml`。


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

2. 更改成您想要的顏色配置。

3. 進入「外觀」設定選單更改主題。

您也可以使用 [Fluent XAML Theme Editor](https://github.com/microsoft/fluent-xaml-theme-editor) 更改您想要的顏色配置。

您可以在 [這裡](https://github.com/files-community/custom-themes) 看到更多的主題樣板。
