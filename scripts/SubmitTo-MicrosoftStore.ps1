# Copyright (c) 2024 Files Community
# Licensed under the MIT License. See the LICENSE.

# Abstract:
#  This script uses official powershell module, microsoft/StoreBroker and
#  submits a private package flight to Microsoft Store.

# Credit:
#  https://github.com/LanceMcCarthy/MediaFileManager/blob/main/.scripts/SubmitToMsftStore.ps1

param(
    [string]$WorkingDir = "",
    [string]$AppPackageDir = "",
    [string]$PartnerCenterClientId = "",
    [string]$PartnerCenterClientSecret = "",
    [string]$PartnerCenterStoreId = "",
    [string]$PartnerCenterTenantId = ""
)

# Ensure that the appxupload exists
if (-not (Test-Path -Path $PSBoundParameters["AppPackageDir"])) {
    Write-Error "The appxupload file does not exist. Double check that you have passed the file path as a parameter and the file exists." -ErrorAction Stop
}

# Set exexution policy
Set-ExecutionPolicy RemoteSigned -Force

# Prepare credentials
$appxUploadFilePath = $PSBoundParameters["AppPackageDir"]
$username = $PartnerCenterClientId
$password = ConvertTo-SecureString $PartnerCenterClientSecret -AsPlainText -Force
$appStoreId = $PartnerCenterStoreId
$tenantId = $PartnerCenterTenantId

# Create temporary directory for submission data
$submissionPackageTempDir = New-Item -Type Directory -Force -Path (Join-Path -Path '$WorkingDir\' -ChildPath '.submission')

# Install the Store Broker PowerShell module
Set-PSRepository -Name "PSGallery" -InstallationPolicy Trusted
Install-Module -Name StoreBroker

# Authenticate Microsoft Store broker
$cred = New-Object System.Management.Automation.PSCredential ($username, $password)
Set-StoreBrokerAuthentication -TenantId $tenantId -Credential $cred

# Prepare submission package
$configFilePath = '$WorkingDir\scripts\MicrosoftStoreSubmissionConfig.json'
New-SubmissionPackage -ConfigPath $configFilePath -AppxPath $appxUploadFilePath -OutPath $submissionPackageTempDir -OutName 'submission'

# Prepare submission data
$submissionDataPath = Join-Path -Path $submissionPackageTempDir -ChildPath 'submission.json'
$submissionPackagePath = Join-Path -Path $submissionPackageTempDir -ChildPath 'submission.zip'

# Update submission & submit to Microsoft Store
Update-ApplicationSubmission -Verbose -ReplacePackages -AppId $appStoreId -SubmissionDataPath $submissionDataPath -PackagePath $submissionPackagePath -AutoCommit -Force -NoStatus
