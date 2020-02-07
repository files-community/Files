## How to help in translating the app

#### The first step is to create a fork of the repository where you will make changes.
### Software requirements
- Visual Studio 2017 or newer
- [Multilingual App Toolkit Extension](https://marketplace.visualstudio.com/items?itemName=MultilingualAppToolkit.MultilingualAppToolkit-18308)
- [Multilingual App Toolkit Editor](https://developer.microsoft.com/en-us/windows/develop/multilingual-app-toolkit)

### Getting XLF Resource
#### Visual Studio with the Multilingual App Toolkit Extension installed
- Open Files UWP in Visual Studio
- Find a "Solution Explorer" window
- Right-click on the project Files(Universal Windows)
- Click on "Multilingual App Toolkit" > "Add translation languages..."
- There may be a dialog that says, "Translation provider manager issue", you can just click "OK" to ignore it.
- Select the language you want to translate by ticking the ✅ in front of that language.
- When you find the language you want, press "OK" and the extension should pull the latest text from Resource .resw for you.
- The file should reside in "Files(Universal Windows)\MultilingualResources\Files.[language_code].xlf

### Open from File Explorer
- Navigate to the project folder
- The XLF File should be located at \files-uwp\Files\MultilingualResources\
- Open the file with the "Multilingual App Toolkit Editor"

### Working with the file
- You can use the translation menu to help on translating, and you can review it later to see if it is correct by using "Translation" > "Translate all" on the Translation section.
- You can use the suggestions for the current text you are translating by using "Suggest" on the Translation section.
- The "State Filter" section is useful if you were working with that language before. You can uncheck "Translated" to hide all the text you already translated.
- Put your translations in the "Translation" box.
- All the text you have to translate is stored in the "Strings" tab below.
- You can filter it out by using "State Filter" above.
- The text is automatically in the "Translated" state when you write text into the translation section.

*Different States*

- "Need Review" is when the text from en-US source is changed.
- "New" state is when the translation has not been translated yet.
- "Source" shows the original text of the string. 

### Saving your changes
- Save your changes and push to your fork on GitHub.
- Create a pull request and a developer working on Files UWP will review it.
- Before Submitting the pull request, make sure the only file changed is the Files.[language_code].xlf one.

### Q&A
#### Why do I have to translate some text more than once.
Answer: Some text has extra hotkeys added into it, for example "Open" has a tooltip with "Open (Ctrl + O)" and both of them are on different controls, one is on AppBarButton and the other is on "TextBlock" this requires us to add more than one key to handle the text like "CMDOpen.Content" and "CMDOpenTooltip.Text" We'll try our best to make the text reusable.
