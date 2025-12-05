# FloraReview Application

This application allows users to open an SQLite3 database, select entries, and display original and AI-modified text for editing and approval. Approved entries can then be exported to a CSV file using `\t` as a delimiter.

## Database Schema

The application assumes the database schema as defined in `schemas.sql`. It updates the `descriptions` table, specifically the `ApprovedText`, `Status`, `Reviewer`, and `Comments` columns. The original and AI-modified text columns are not altered.

## License and Disclaimer

This software is freeware and is provided "as is" without any warranty. The author is not responsible for any damages or losses caused by the use of this software. By using this software, you agree to these terms.

The source code for this software is freely available on GitHub: https://github.com/stoflom/FloraReview

## Signing the Application and Deployment Package

There are two primary methods for signing the application:

### Method 1: Signing for ClickOnce Deployment

1.  **Generate a Self-Signed Code Signing Certificate:**
    In an administrative PowerShell, generate a self-signed code signing certificate valid for 5 years. Replace `eMail` and `UserName` with your details.
    ```powershell
    $cert = New-SelfSignedCertificate -KeyUsage DigitalSignature -KeySpec Signature -KeyAlgorithm RSA -KeyLength 2048 -DNSName "eMail" -CertStoreLocation Cert:\CurrentUser\My -Type CodeSigningCert -Subject "UserName" -NotAfter (Get-Date).AddYears(5)
    ```

2.  **Verify the Created Certificate:**
    ```powershell
    echo $cert
    ```
    This should output details similar to:
    ```
       PSParentPath: Microsoft.PowerShell.Security\Certificate::CurrentUser\My
    Thumbprint                                Subject
    ----------                                -------
    5DA3CACE9A6F4456B2FA41A658B10103529E710D  CN=UserName
    ```

3.  **Check for Code Signing Enhanced Key Usage:**
    ```powershell
    $Cert | Select-Object Subject,EnhancedKeyUsageList
    ```
    The output should confirm `Code Signing (1.3.6.1.5.5.7.3.3)` is present.

4.  **Verify Certificate in User Store:**
    Confirm using `certmgr.msc` that the certificate is located under `Personal > Certificates` in the user's store.

5.  **Apply to Visual Studio Project:**
    In your Visual Studio project, navigate to `Project > Properties > Publish > ClickOnce` section. In the "Sign Manifests" area, choose "Select from store" for ClickOnce deployment and select the newly created certificate.

### Method 2: Signing `exe` and `msi` Files Directly

This method involves signing the executable and MSI installer files directly using `signtool.exe`.

1.  **Build Project Package and Sign Executable:**
    First, build your project package. Then, execute the following PowerShell commands to sign the executable.
    ```powershell
    # Target paths - UPDATE THESE TO YOUR PROJECT'S PATHS
    $TargetDir = "PathToProject\FloraReview"
    $ExePath = "$TargetDir\FloraReview\bin\Release\net8.0-windows\FloraReview.exe"
    $InstallerPath = "$TargetDir\FloraReviewDeploy\Release\FloraReviewDeployV1026.msi" # This path is for the MSI, but we're signing the EXE here.

    # Tool paths
    $timestampUrl = "http://timestamp.digicert.com"
    $signtool = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"

    # To sign using the code signing cert which must be in the Certificate Store (does not require password) use:
    # NOTE THE AMPERSAND (&) IS CRUCIAL FOR EXECUTING EXTERNAL COMMANDS IN POWERSHELL!
    & $signtool sign -a /tr $timestampUrl /td sha256 /fd sha256 $ExePath
    ```

2.  **Build Deployment Package and Sign MSI:**
    After building the deployment package (ensure you do not rebuild the project executable in the configuration management if it was already signed), sign the MSI installer.
    ```powershell
    # NOTE THE AMPERSAND (&) IS CRUCIAL FOR EXECUTING EXTERNAL COMMANDS IN POWERSHELL!
    & $signtool sign -a /tr $timestampUrl /td sha256 /fd sha256 $InstallerPath
    ```

## Further Reading

For more details on creating self-signed code signing certificates with PowerShell, refer to:
https://codesigningstore.com/how-to-create-self-signed-code-signing-certificate-with-powershell


## Signing msi deployment during build
When building the project the following pots-build actions are called:
(NOTE: the msi actually uses the output from the obj/Rlease..path)

exe and dll files in bin/Release/...

    call "$(ProjectDir)SignEXE.bat" "$(TargetDir)$(TargetFileName)"
    call "$(ProjectDir)SignEXE.bat" "$(TargetPath)"

exe and dll files in obj/Release/... (Note the name of the executable is apphost.exe here)

    call "$(ProjectDir)SignEXE.bat" "$(IntermediateOutputPath)apphost.exe"
    call "$(ProjectDir)SignEXE.bat" "$(IntermediateOutputPath)$(AssemblyName).dll"

When the deployment package is built the following post-build action is called:

    call "$(ProjectDir)SignMSI.bat" "$(TargetDir)$(TargetName)V$(TargetVersion).msi"

The bat files are not included in the msi package but are in the repository. They only differ
in comments but are kept separate for future flexibility.