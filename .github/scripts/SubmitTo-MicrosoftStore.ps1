# Copyright (c) 2024 Files Community
# Licensed under the MIT License. See the LICENSE.

param(
    [string]$SubmissionDirPath = "",
    [string]$StoreBrokerConfigPath = "",
    [string]$AppxPackagePath = "",
    [string]$PartnerCenterClientId = "",
    [string]$PartnerCenterClientSecret = "",
    [string]$PartnerCenterStoreId = "",
    [string]$PartnerCenterTenantId = ""
)

# Setup
Set-ExecutionPolicy RemoteSigned -Force
Set-PSRepository -Name "PSGallery" -InstallationPolicy Trusted
Install-Module -Name StoreBroker

# Authenticate StoreBroker
$UserName = $PartnerCenterClientId
$Password = ConvertTo-SecureString $PartnerCenterClientSecret
$Credential = New-Object System.Management.Automation.PSCredential ($UserName, $Password)
Set-StoreBrokerAuthentication -TenantId $PartnerCenterTenantId -Credential $Credential

# Prepare the submission package
New-SubmissionPackage -ConfigPath $StoreBrokerConfigPath -AppxPath $AppxPackagePath -OutPath $SubmissionDirPath -OutName 'submission'
$SubmissionDataPath = Join-Path -Path $SubmissionDirPath -ChildPath 'submission.json'
$SubmissionPackagePath = Join-Path -Path $SubmissionDirPath -ChildPath 'submission.zip'

# Upload the package
Update-ApplicationSubmission -Verbose -ReplacePackages -AppId $PartnerCenterStoreId -SubmissionDataPath $SubmissionDataPath -PackagePath $SubmissionPackagePath -AutoCommit -Force -NoStatus
