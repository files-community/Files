# Configuring Files to launch with the Win + E shortcut

**Option 1: using an AutoHotkey script**

You can configure Files to launch using the Win + E shortcut by using this [AutoHotKey](https://www.autohotkey.com/) script:
```
; Win + E
#e::
FilesTitle := "Files ahk_class ApplicationFrameWindow ahk_exe ApplicationFrameHost.exe"
FilesLocation := USERPROFILE . "\AppData\Local\Microsoft\WindowsApps\files.exe"
    if WinExist(FilesTitle) {
        WinActivate % FilesTitle     ; Set focus
        SendInput ^t                    ; Send CTRL + t shortcut to open a new tab
    } else {
        Run % FilesLocation          ; Start Files
        WinWait % FilesTitle         ; Wait for it ...
        WinActivate % FilesTitle     ; Set focus
    }
return
```

**Option 2: modifying the registry**

You can also configure Files to launch using the Win + E shortcut without an external program by editing the registry:

*This method involves modifying the registry, make sure to create a backup beforehand and proceed at your own risk. Please keep in mind that this method is unsupported and may not work for everyone.*

**With automatic script**
1. Create a backup of the registry, make sure to store the backup in your desktop folder so that you can access it in the event that Files won't open.
2. Download [this](https://raw.githubusercontent.com/files-community/files-community.github.io/main/data/OpenFilesOnWinE.zip) archive and extract *both* of the contained .reg files *to the desktop*
3. Run `OpenFilesOnWinE.reg` to open Files on Win+E
4. Run `UndoOpenFilesOnWinE.reg` to restore windows explorer

**Manually**
1. Create a backup of the registry, make sure to store the backup in your desktop folder so that you can access it in the event that Files won't open.
2. Open the registry editor
3. Navigate to `HKEY_CURRENT_USER\SOFTWARE\Classes\CLSID`
4. Create a key named `{52205fd8-5dfb-447d-801a-d0b52f2e83e1}`
5. Create a key named `shell` under `{52205fd8-5dfb-447d-801a-d0b52f2e83e1}`
5. Create a key named `opennewwindow` under `shell`
5. Create a key named `command` under `opennewwindow` and set the default key value to:
`C:\Users\<YOURUSERNAME>\AppData\Local\Microsoft\WindowsApps\files.exe`
