# 翻譯 Files

#### 在開始之前，請先建立 Files 的儲存庫分支以便之後提交翻譯。
### 軟體需求
- Visual Studio 2017 及以上版本（選用）
- [Multilingual App Toolkit Extension](https://marketplace.visualstudio.com/items?itemName=MultilingualAppToolkit.MultilingualAppToolkit-18308)（選用）
- [Multilingual Editor](https://developer.microsoft.com/en-us/windows/develop/multilingual-app-toolkit)

### 開始翻譯
#### 以下步驟需要使用 Visual Studio 及 Multilingual App Toolkit Extension
- 開啟 Files 程式資料夾中的 Visual Studio 解決方案文件
- 找到「方案總管」選單
- 對 `Files(Universal Windows)` 點擊右鍵
- 點擊「Multilingual App Toolkit」 > 「Add translation languages...」
- 可能會顯示一個提示說「Translation provider manager issue」，請點擊「OK」按鈕忽略此。
- ✅ 您想要翻譯的語言。
- 翻譯文件位置：`Files(Universal Windows)\MultilingualResources\Files.[language_code].xlf`

### 使用 Multilingual Editor 翻譯
- 開啟 Files 資料夾
- 找到 XLF 文件（位置如下 - `Files\Files\MultilingualResources\Files.[language_code].xlf`）
- 使用「Multilingual Editor」開啟文件

### 開始貢獻翻譯
- 您可以使用「轉譯」選單來幫助您進行翻譯，並透過「轉譯」選單的 翻譯 > 轉譯所有 功能以確認您翻譯的準確性。
- 您可以透過下方功能區的「建議」選單中查看對當前文本的建議操作。
- 如果您使用現有的語言文件進行翻譯，試試「狀態篩選器」。您可以取消選取「轉譯」以隱藏已被翻譯的文本。
- 您只需要在「轉譯」輸入框中輸入文字即可完成該文本的翻譯。
- 當您在「轉譯」輸入框輸入翻譯後，系統將自動更改狀態為「轉譯」。

### 翻譯狀態

- 「需要檢閱」代表已翻譯，但原文中已更改的文本。
- 「新」代表尚未翻譯的文本。
- 「轉譯」代表已翻譯的文本。
- 「Source」代表以原文顯示的文本。

### 儲存您的更改
- 在 Visual Studio 或 Multilingual Editor 中儲存檔案，並將其推送至您先前建立的 GitHub 儲存庫中。
- 新建一個 Pull Request 並等待審閱。
- 在新建 Pull Request 前，檢查 `Files.[語言代碼].xlf` 是否為唯一被修改的文件。`resw` 文件將由 Files 的開發人員負責生成。如果您是新增語言而非從現有的語言進行修改，請確認 `cproj` 檔案是否有連結到您的語言。
