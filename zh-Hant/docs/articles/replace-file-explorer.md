# 開啟項目時預設啟動 Files

*由於此方法涉及到系統文件，請確保事先新建還原點並自行承擔風險，且此方法並不適用於所有人。*

**使用登錄項目修改登錄檔**
1. 請先建立登錄檔備份，並確保將其儲存在桌面，以便在 Files 無法開啟時還原操作。
2. 下載 [此檔案](https://raw.githubusercontent.com/files-community/files-community.github.io/main/data/SetFilesAsDefault.zip) 並解壓縮 *兩個*  副檔名為 .reg 的檔案*到桌面*
3. 執行 `SetFilesAsDefault.reg` 後開啟項目時預設啟動 Files
4. 執行 `UnsetFilesAsDefault.reg` 後開啟項目時預設啟動檔案總管

**手動修改登錄檔**
1. 請先建立登錄檔備份，並確保將其儲存在桌面，以便在 Files 無法開啟時還原操作。
2. 開啟登錄編輯程式
3. 前往 `HKEY_CURRENT_USER\SOFTWARE\Classes\Directory`
4. 新建一個機碼名為 `shell` 並將 `shell` 機碼中的 `(預設值)` 字串值設為：`openinfiles`
5. 再於 `{shell}` 機碼中新建一個機碼名為 `openinfiles`
6. 最後於 `{openinfiles}` 機碼中新建一個機碼名為 `openinfiles` 並將 `openinfiles` 機碼中的 `(預設值)` 字串值設為：
`C:\Users\<您的使用者名稱>\AppData\Local\Microsoft\WindowsApps\files.exe -Directory %1`
