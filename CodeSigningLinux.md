
-----

## 🐧 Signing Applications on Linux with GnuPG (GPG)

Moving from Windows' Authenticode to the **GnuPG (GPG)** standard on Linux requires a different approach to application signing. The core idea is to use an **asymmetric key pair** to generate a signature that users can verify.

[Image of asymmetric encryption public and private key concept]

### 1\. Install GnuPG

First, ensure GPG is installed on your system.

  * **For Debian/Ubuntu-based systems:**
    ```bash
    sudo apt-get update
    sudo apt-get install gnupg
    ```
  * **For Fedora/CentOS/RHEL systems:**
    ```bash
    sudo dnf install gnupg2
    ```

-----

### 2\. Generate Your GPG Key Pair

If you don't have a GPG key, generate one using an interactive process:

```bash
gpg --full-generate-key
```

  * **Key Type:** Choose a secure type like **RSA and RSA**.
  * **Key Size:** **4096 bits** is recommended for strong security.
  * **Expiration:** Set an expiration date or choose for it to never expire.
  * **Identity:** Enter your **real name** and **email address**.
  * **Passphrase:** Create a **strong passphrase** to protect your private key.

-----

### 3\. Sign Your Application

To sign your application binary (e.g., `my-linux-app`), create a **detached signature** file (`.sig`) to distribute alongside it.

```bash
gpg --detach-sign my-linux-app
```

This command will create a file named **`my-linux-app.sig`** after prompting you for your key's passphrase.

-----

### 4\. Make Your Public Key Available

Users need your **public key** to verify the signature.

1.  **Find Your Key ID:**

    ```bash
    gpg --list-keys
    ```

    The output will look similar to this, where the long string is your key ID:

    ```plaintext
    pub   rsa4096/A1B2C3D4E5F6G7H8 2025-12-05 [SC]
          AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
    uid                  [ultimate] Your Name <your.email@example.com>
    sub   rsa4096/8H7G6F5E4D3C2B1A 2025-12-05 [E]
    ```

2.  **Export Your Public Key:**

    ```bash
    gpg --armor --export A1B2C3D4E5F6G7H8 > my-public-key.asc
    ```

    **Publish** this **`my-public-key.asc`** file on your website or in your project's repository.

-----

### 5\. How Users Verify Your Application

A user who downloads your application and the signature file would:

1.  **Import your public key:**
    ```bash
    gpg --import my-public-key.asc
    ```
2.  **Verify the signature:**
    ```bash
    gpg --verify my-linux-app.sig my-linux-app
    ```
    A successful verification will display a **"Good signature"** message.

-----

### Integrating with Build Processes in VS Code

You can perform all GPG operations from the **integrated terminal in VS Code**. For automation, integrate these signing commands into your build scripts (e.g., `Makefile` or `npm scripts`) to sign the application automatically during your release process.

-----

