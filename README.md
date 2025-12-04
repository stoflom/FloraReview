Application to open SQLite3 database, select some entries, display original text and modified text for editing and approval.
The selected entries can then be exported to a csv file using '\t' as delimiter.

The application assumes the database schema as in schemas.sql. It will update the descriptions table, columns:
ApprovedText, Status, Reviewer, Comments. The original- and AI modified text is not changed.

This software is freeware and is provided "as is" without any warranty. The author is not responsible for any damages
or losses caused by the use of this software. By using this software, you agree to these terms.

The source of this software is freely available here: https://github.com/stoflom/FloraReview


For Signing the application and deployment package:

1) In an Admin powershell generate a self-certified code signing certificate valid for 5 years (replace eMail and UserName):
C:\WINDOWS\system32> $cert = New-SelfSignedCertificate -KeyUsage DigitalSignature -KeySpec Signature -KeyAlgorithm RSA -KeyLength 2048 -DNSName “eMail” -CertStoreLocation Cert:\CurrentUser\My -Type CodeSigningCert -Subject “UserName”  -NotAfter (Get-Date).AddYears(5)

2) Check the created cert:
C:\WINDOWS\system32> echo $cert


   PSParentPath: Microsoft.PowerShell.Security\Certificate::CurrentUser\My

Thumbprint                                Subject                                                                                                                                                                                   
----------                                -------                                                                                                                                                                                   
5DA3CACE9A6F4456B2FA41A658B10103529E710D  CN=UserName                                                                                                                                                                              


3) Check if it has Code Signing Enhanced Key Usage:
C:\WINDOWS\system32> $Cert | Select-Object Subject,EnhancedKeyUsageList

Subject      EnhancedKeyUsageList              
-------      --------------------              
CN=UserName  {Code Signing (1.3.6.1.5.5.7.3.3)}


4) Check with certmgr that the cert is in users store under Personal>Cerificates.


5) Use this in the Sign Manifest section of the VS Project>Properties>Publish ClickOnce section of the project,
choose "Select from store" for ClickOnce deployment.



OR to sign exe and msi files of deployment project, execute the following in a Powershell window:

1) Build project package and sign using: 

# Target paths
$TargetDir = "C:\Users\stoff\source\repos\stoflom\FloraReview"
$ExePath = "$TargetDir\FloraReview\bin\Release\net8.0-windows\FloraReview.exe"
$InstallerPath = "$TargetDir\FloraReviewDeploy\Release\FloraReviewDeployV1026.msi"

# Tool paths
$timestampUrl = "http://timestamp.digicert.com"
$signtool = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"

# To sign using the code signing cert which must be in the Certificate Store (does not require password) use:
# NOTE AMPERSAND!
& $signtool sign -a /tr $timestampUrl /td sha256 /fd sha256 $ExePath

2) Now build deployment package and sign using (do not rebuild the project exe in the configuration management):
# NOTE AMPERSAND!
& $signtool sign -a  /tr $timestampUrl /td sha256 /fd sha256 $InstallerPath

See also: https://codesigningstore.com/how-to-create-self-signed-code-signing-certificate-with-powershell
