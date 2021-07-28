# Terminal Profiles

Files supports multiple options for configuring terminal profiles. Aside from setting the default terminal that you can launch from the "Open in terminal" option, you can also adjust the launch arguments. You can also launch any terminal profile by typing the name or path in the navigation bar.

_Profiles will only function, if the corresponding terminals are installed. Starting in v0.9.2, Files will automatically detect if Windows Terminal and Fluent Terminal are installed._

## Sample profiles

Cmd:

```
{
      "name": "CMD",
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

PowerShell Core:

```
{
      "name": "PowerShell Core",
      "path": "pwsh.exe",
      "arguments": "-WorkingDirectory \"{0}\"",
      "icon": ""
}
```

Windows Terminal:

```
{
      "name": "Windows Terminal",
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
