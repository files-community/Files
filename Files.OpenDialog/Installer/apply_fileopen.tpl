Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\SOFTWARE\Classes\CustomOpenDialog.FilesOpenDialog]
@="CustomOpenDialog.FilesOpenDialog"

[HKEY_CURRENT_USER\SOFTWARE\Classes\CustomOpenDialog.FilesOpenDialog\CLSID]
@="{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}"


; Overrides Open Dialog in HKEY_LOCAL_MACHINE\SOFTWARE\Classes
[HKEY_CURRENT_USER\SOFTWARE\Classes\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}]
@="File Open Dialog"

[HKEY_CURRENT_USER\SOFTWARE\Classes\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}\ProgId]
@="CustomOpenDialog.FilesOpenDialog"

[HKEY_CURRENT_USER\SOFTWARE\Classes\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}\Implemented Categories\{{62C8FE65-4EBB-45E7-B440-6E39B2CDBF29}}]

[HKEY_CURRENT_USER\SOFTWARE\Classes\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}\InProcServer32]
@="mscoree.dll"
"ThreadingModel"="Both"
"Class"="CustomOpenDialog.FilesOpenDialog"
"Assembly"="CustomOpenDialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=02fdb5bc5db3ac4c"
"RuntimeVersion"="v4.0.30319"
"CodeBase"="file:///{0}"

[HKEY_CURRENT_USER\SOFTWARE\Classes\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}\InprocServer32\1.0.0.0]
"Class"="CustomOpenDialog.FilesOpenDialog"
"Assembly"="CustomOpenDialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=02fdb5bc5db3ac4c"
"RuntimeVersion"="v4.0.30319"
"CodeBase"="file:///{0}"


; Overrides Open Dialog in HKEY_LOCAL_MACHINE\SOFTWARE\Classes\WOW6432Node
[HKEY_CURRENT_USER\SOFTWARE\Classes\WOW6432Node\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}]
@="File Open Dialog"

[HKEY_CURRENT_USER\SOFTWARE\Classes\WOW6432Node\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}\ProgId]
@="CustomOpenDialog.FilesOpenDialog"

[HKEY_CURRENT_USER\SOFTWARE\Classes\WOW6432Node\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}\Implemented Categories\{{62C8FE65-4EBB-45E7-B440-6E39B2CDBF29}}]

[HKEY_CURRENT_USER\SOFTWARE\Classes\WOW6432Node\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}\InProcServer32]
@="mscoree.dll"
"ThreadingModel"="Both"
"Class"="CustomOpenDialog.FilesOpenDialog"
"Assembly"="CustomOpenDialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=02fdb5bc5db3ac4c"
"RuntimeVersion"="v4.0.30319"
"CodeBase"="file:///{0}"

[HKEY_CURRENT_USER\SOFTWARE\Classes\WOW6432Node\CLSID\{{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}}\InprocServer32\1.0.0.0]
"Class"="CustomOpenDialog.FilesOpenDialog"
"Assembly"="CustomOpenDialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=02fdb5bc5db3ac4c"
"RuntimeVersion"="v4.0.30319"
"CodeBase"="file:///{0}"
