# 終端機配置編輯範例

Files 支援設定終端機配置。除了能設定從「在終端機中開啟...」中預設開啟的終端機外，您還可以調整終端機應用程式的啟動參數。您也能設定：透過在導航欄中輸入名稱或位置執行終端機配置。

_終端機配置僅會從已安裝的終端機應用程式中作用。自 v0.9.2 起，Files 已將 Windows 終端機 和 Fluent Terminal 加入預設的終端機配置中。_

## 程式片段

命令提示字元：

```
{
      "name": "命令提示字元",
      "path": "cmd.exe",
      "arguments": "/k \"cd /d {0} && title Command Prompt\"",
      "icon": ""
}
```

PowerShell

```
{
      "name": "PowerShell",
      "path": "powershell.exe",
      "arguments": "-noexit -command \"cd '{0}'\"",
      "icon": ""
}
```

PowerShell Core：

```
{
      "name": "PowerShell Core",
      "path": "pwsh.exe",
      "arguments": "-WorkingDirectory \"{0}\"",
      "icon": ""
}
```
         
Windows 終端機：

```
{
      "name": "Windows 終端機",
      "path": "wt.exe",
      "arguments": "-d \"{0}\"",
      "icon": ""
}
```

Fluent Terminal:

```
{
      "name": "Fluent Terminal",
      "path": "flute.exe",
      "arguments": "new \"{0}\"",
      "icon": ""
}
```
