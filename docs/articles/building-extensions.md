# Building Extensions

### File preview service

Files locates the name of the preview service by reading the service property registered in the app manifest. Your manifest should look something like this:
```xml  
<Extensions>
  <uap3:Extension Category="windows.appExtension">
    <uap3:AppExtension Name="com.files.filepreview"
                        Id="markdown"
                        DisplayName="Markdown File Preview"
                        Description="Adds support for viewing Markdown Files in the preview pane."
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
Files also looks at the FileExtensions.json file to get a list of file extensions that the preview service is registered for. If the selected file has an extension listed within this file, then Files will call the extension's service.

## Preview controls
Previews are sent as a string containing xaml that is then loaded into the preview pane using the ```XamlReader```. You can define this string as the "preview" parameter in the response. This does have it's limitations, as only controls that are already avaliable to Files can be used. This means that extensions are limited to standard WinUI controls, and controls from the Windows Community Toolkit.

This is an example of a string that can be sent back to Files.
```xml
<controls:MarkdownTextBlock xml:space="preserve" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:controls="using:Microsoft.Toolkit.Uwp.UI.Controls">
  <controls:MarkdownTextBlock.Text>
    Hello, world!
  </controls:MarkdownTextBlock.Text>
</controls:MarkdownTextBlock>
```
## Preview Images
Since images can't be sent in the response, Files allows images to be loaded as Base64 string that represents the images buffer. See the sample service for an example of this.
This is an
This is an example of how you would encode your image as a base64 string, and add that to the xaml string.
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

## Details
File details are sent as a Json string with the parameter name "details", which is deserialized by Files, and then added to the details list in the preview pane.
The two Properties you will need for this are "LocalizedName" and "Value" of types ```string``` and ```object```, respectively.
This is an example of valid Json data that can be used by Files.
```json
[
  {
    "LocalizedName": "Hello",
    "Value": "World!",
  }
]
```

## Specifying file types
Preview extensions can specify the types of files they support by adding them to a json list in a file named ```FileExtensions.json``` in the extension's public folder.

Also check out the [preview extensions sample repository](https://github.com/files-community/preview-pane-sample-extension).
