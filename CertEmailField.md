
## 🔐 Missing Email in Windows Certificate Details

The issue of the email field appearing empty ("email is not available") when inspecting a Windows certificate, especially one generated via the GUI, stems from how the **Subject Distinguished Name (DN)** is defined during creation.

### 🔑 Why the Email is Missing

  * The **`E=` (email)** attribute is part of the X.509 **Subject Distinguished Name (DN)**.
  * When using Windows’ built-in Certificate Manager GUI or the "Create Self-Signed Certificate" wizard, it often only prompts for a "Friendly Name" and does not expose the full DN fields for population.
  * Consequently, the email field is not populated in the certificate's subject.

### 🛠 Ways to Define the Email

To explicitly include the email address, you must use command-line tools that allow you to specify the full Subject DN:

1.  **Use PowerShell with `New-SelfSignedCertificate`:**

    ```powershell
    New-SelfSignedCertificate `
      -Type CodeSigningCert `
      -Subject "CN=MyCodeSigningCert, E=me@example.com" `
      -CertStoreLocation "Cert:\CurrentUser\My"
    ```

      * The **`E=me@example.com`** part adds the email address to the Subject DN.

2.  **Use `makecert.exe` (deprecated older tool):**

    ```bash
    makecert -r -pe -n "CN=MyCodeSigningCert, E=me@example.com" -ss My -sr CurrentUser
    ```

3.  **Use `openssl`:**

      * In your OpenSSL config file, under the `[ req_distinguished_name ]` section, add:
        `emailAddress = me@example.com`
      * Then generate the certificate:
        ```bash
        openssl req -new -x509 -days 365 -key key.pem -out cert.pem
        ```

### ⚠️ Important Notes

  * The **Windows Certificate Manager GUI cannot set the email field**—you must use a tool like PowerShell, `makecert`, or `openssl`.
  * For **code signing**, the email field is **optional**. Most signing tools (like `signtool.exe`) do not require it, but if you want it for clear identification, you must explicitly include `E=` in the `-Subject`.

-----

👉 The solution is to generate your certificate using a tool that allows you to include the **`E=your@email.com`** attribute in the Subject field.


