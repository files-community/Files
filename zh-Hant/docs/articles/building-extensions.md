# 開發擴充功能

### 範例 - 新建擴充功能

Files 會透過您的 App Manifest 文件來獲取擴充功能的名稱。以 Markdown 文件預覽擴充功能為例，應呈現如下表所示：
```xml  
<Extensions>
  <uap3:Extension Category="windows.appExtension">
    <uap3:AppExtension Name="com.files.filepreview"
                        Id="markdown"
                        DisplayName="Markdown 文件預覽"
                        Description="新增對 Markdown 文件預覽的支援。"
                        PublicFolder="Public">
      <uap3:Properties>
        <Service>com.markdownpreview.controlservice</Service>
      </uap3:Properties>
    </uap3:AppExtension>
  </uap3:Extension>
  <uap:Extension Category="windows.appService" EntryPoint="FilePreviewService.Preview">
    <uap:AppService Name="com.markdownpreview.controlservice" />
  </uap:Extension>
</Extensions>
```

## 範例 - 控制

這是一個可以回傳字串至 Files 的程式片段。
```xml
<controls:MarkdownTextBlock xml:space="preserve" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:controls="using:Microsoft.Toolkit.Uwp.UI.Controls">
  <controls:MarkdownTextBlock.Text>
    Hello, world!
  </controls:MarkdownTextBlock.Text>
</controls:MarkdownTextBlock>
```
## 範例 - 新增圖片

由於無法在響應（Response）中發送圖片，因此 Files 允許您透過 Base64 載入圖片。
這是如何將編碼為 base64 的圖片新增至 xaml 的程式片段。
```cs
var base64string = "";
var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesHome.png"));
var stream = await file.OpenReadAsync();

using (var memoryStream = new MemoryStream())
{
    memoryStream.Capacity = (int)stream.Size;
    var ibuffer = memoryStream.GetWindowsRuntimeBuffer();
    ibuffer = await stream.ReadAsync(ibuffer, (uint)stream.Size, InputStreamOptions.None).AsTask().ConfigureAwait(false);
    var byteArray = ibuffer.ToArray();
    base64string = Convert.ToBase64String(byteArray);
}
var xaml = $"<ScrollViewer xml:space=\"preserve\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:controls=\"using:Files.UserControls\"> 
  <controls:StringEncodedImage>
    <controls:StringEncodedImage.EncodedImage>
      {base64string}
    </controls:StringEncodedImage.EncodedImage>
  <controls:StringEncodedImage>
  </ScrollViewer>
```

## 範例 - 詳細資料
Files 中所顯示的詳細資料是透過一個名為「details」的 Json 檔案回傳，經處理過後在預覽視窗中顯示。
為此，您需要的兩個分別為 ```string``` 和 ```object``` 的「LocalizedName」和「Value」。
以下為程式片段。

```json
[
  {
    "LocalizedName": "Hello",
    "Value": "World!",
  }
]
```

## 指定檔案類型
Files 還會查看 FileExtensions.json 文件以得知此擴充功能該適用於何種檔案類型。如果所選檔案的類型在此文件中出現，則會自動啟用此擴充功能。

欲知詳情請前往「[preview extensions sample repository](https://github.com/files-community/preview-pane-sample-extension) 」。
