# Win + E 快捷鍵配置

**方法一：使用 AutoHotkey 腳本**

您可以透過使用 [AutoHotKey](https://www.autohotkey.com/) 腳本配置 Win + E：
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

**方法二：修改登錄檔**

您可以透過修改登錄檔且無須借助第三方應用程式的情況下配置 Win+E：

*由於此方法涉及到系統文件，請確保事先新建還原點並自行承擔風險，且此方法並不適用於所有人。*

**使用登錄項目修改登錄檔**
1. 請先建立登錄檔備份，並確保將其儲存在桌面，以便在 Files 無法開啟時還原操作。
2. 下載 [此檔案](https://raw.githubusercontent.com/files-community/files-community.github.io/main/data/OpenFilesOnWinE.zip) 並解壓縮 *兩個*  副檔名為 .reg 的檔案*到桌面*
3. 執行 `OpenFilesOnWinE.reg` 後即可透過 Win+E 開啟 Files
4. 執行 `UndoOpenFilesOnWinE.reg` 後 Win+E 將復原成開啟檔案總管

**手動修改登錄檔**
1. 請先建立登錄檔備份，並確保將其儲存在桌面，以便在 Files 無法開啟時還原操作。
2. 開啟登錄編輯程式
3. 前往 `HKEY_CURRENT_USER\SOFTWARE\Classes\CLSID`
4. 新建一個機碼名為 `{52205fd8-5dfb-447d-801a-d0b52f2e83e1}`
5. 於 `{52205fd8-5dfb-447d-801a-d0b52f2e83e1}` 機碼中新建一個機碼名為 `shell`
5. 再於 `shell` 機碼中新建一個機碼名為 `opennewwindow`
5. 最後於 `opennewwindow` 機碼中新建一個機碼名為 `command` 並將 `command` 機碼中的 `(預設值)` 字串值設為：
`C:\Users\<您的使用者名稱>\AppData\Local\Microsoft\WindowsApps\files.exe`
