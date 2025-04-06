param (
    [string]$Channel, # Dev, Preview, or Stable
    [string]$WorkingDir
)

# Define source and destination paths
$SourcePath = "$WorkingDir\src\Files.App\Assets\AppTiles\$Channel"
$DestinationPath = "$WorkingDir\src\Files.App\Assets\AppTiles\Current"

# Check if the source directory exists
if (-Not (Test-Path -Path $SourcePath)) {
    Write-Host "Source path '$SourcePath' does not exist. Please check the branch name." -ForegroundColor Red
    exit 1
}

# Ensure the destination directory exists
if (-Not (Test-Path -Path $DestinationPath)) {
    Write-Host "Destination path '$DestinationPath' does not exist. Creating it now..." -ForegroundColor Yellow
	New-Item -ItemType Directory -Path $DestinationPath | Out-Null
} else {
    # Delete the contents of the destination directory
    Write-Host "Clearing contents of '$DestinationPath'..."
    Get-ChildItem -Path $DestinationPath -Recurse | Remove-Item -Force -Recurse
}

# Copy files from source to destination
Write-Host "Copying files from '$SourcePath' to '$DestinationPath'..."
Copy-Item -Path "$SourcePath\*" -Destination $DestinationPath -Recurse -Force

Write-Host "Files copied successfully!" -ForegroundColor Green