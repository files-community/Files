# Replacing File Explorer with Files (Unsupported)

*This guide involves modifying the registry, make sure to create a backup beforehand and proceed at your own risk. Please keep in mind that this method is unsupported and may not work for everyone.*

**With automatic script**
1. Create a backup of the registry, make sure to store the backup in your desktop folder so that you can access it in the event that Files won't open.
2. Download [this](https://raw.githubusercontent.com/files-community/files-community.github.io/main/data/SetFilesAsDefault.zip) archive and extract *both* of the contained .reg files *to the desktop*
3. Run `SetFilesAsDefault.reg` to set Files as default file explorer
4. Run `UnsetFilesAsDefault.reg` to restore windows explorer

**Manually**
1. Create a backup of the registry, make sure to store the backup in your desktop folder so that you can access it in the event that Files won't open.
2. Open the registry editor
3. Navigate to `HKEY_CURRENT_USER\SOFTWARE\Classes\Directory`
4. Create a key named `shell` and set the default key value to `openinfiles`
5. Create a key named `openinfiles` under `shell`
5. Create a key named `command` under `openinfiles` and set the default key value to:
`C:\Users\<YOURUSERNAME>\AppData\Local\Microsoft\WindowsApps\files.exe -Directory %1`
