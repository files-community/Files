# Copyright (c) 2024 Files Community
# Licensed under the MIT License. See the LICENSE.

param(
    [string]$WorkingDir = ""
    [string]$Target = "",
    [string]$New = ""
)

Get-ChildItem "$WorkingDir\src" -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach -Process
{ `
    (Get-Content $_ -Raw | ForEach -Process { $_ -replace "$Target", "$New" }) |
    Set-Content $_ -NoNewline
}
