# To allow this script do (in administrator powershell):
#  >powershell -ExecutionPolicy Bypass -File "C:\Users\stoff\Workspace\SignApp.ps1"
# or for permanet change do
#  Set-ExecutionPolicy RemoteSigned -Scope LocalMachine
# to revert" >Set-ExecutionPolicy Restricted -Scope LocalMachine

# Target paths
$TargetDir = "C:\Users\stoff\source\repos\stoflom\FloraReview"
$ExePath = "$TargetDir\FloraReview\bin\Release\net8.0-windows\FloraReview.exe"
$InstallerDir = "$TargetDir\FloraReviewDeploy\Release"
$InstallerPath = "$InstallerDir\FloraReviewDeployV1026.msi"

# Tool Paths and settings
$timestampUrl = "http://timestamp.digicert.com"
$signtool = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"

Write-Host "Signing EXE..."
# To sign using pfx file (requires the passowrd use)
# & $signtool sign /f $CertPath /p $Password /tr $timestampUrl /td sha256 /fd sha256 $ExePath

# To sign using the code signing cert which must be in the Certificate Store (does not require password) use:
& $signtool sign -a /tr $timestampUrl /td sha256 /fd sha256 $ExePath


### BEFORE RUNNING THIS PART DO THE DEPLYMENT BUILD TO PACKAGE THE SIGNED EXE FILE
# Do not rebuild the project in the Configuration Manager

Write-Host "Signing Installer..."
#& $signtool sign /f $CertPath /p $Password /tr $timestampUrl /td sha256 /fd sha256 $InstallerPath

& $signtool sign -a  /tr $timestampUrl /td sha256 /fd sha256 $InstallerPath



# These verifications will fail for a self-signed certificate
Write-Host "Verifying signatures..."
& $signtool verify /pa $ExePath
& $signtool verify /pa $InstallerPath

Write-Host "Signing completed successfully."
