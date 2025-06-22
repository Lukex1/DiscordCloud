# **Discord Bot File Manager**

This project is a file management application that uses a Discord bot as a backend for storing and retrieving files. It allows users to upload, download, list, and delete files directly from a Discord channel, while encrypting them for security.

## **Table of Contents**

1. [Features](#features)
2. [Requirements](#requirements)
3. [Configuration](#configuration)
   * [Step 1: Create a Discord Bot](#step-1-create-a-discord-bot)
   * [Step 2: Set Bot Token and Channel ID](#step-2-set-bot-token-and-channel-id)
   * [Step 3: Configure Server URL](#step-3-configure-server-url)
   * [Step 4: Set Encryption Password](#step-4-set-encryption-password)
4. [Running the Application](#running-the-application)
5. [Usage](#usage)
6. [Technologies](#technologies)

---

## **Features**

* **File Upload:** Encrypts and uploads files to a selected Discord channel. Large files are split into parts to overcome Discord's size limits.
* **File Download:** Downloads encrypted files from Discord, combines parts, and decrypts them.
* **File Listing:** Displays a list of all available files on the Discord channel via a web interface.
* **File Deletion:** Deletes files (including their parts) from the Discord channel.
* **Encryption/Decryption:** Automatically encrypts files before upload and decrypts them upon download using a defined password.

---

## **Requirements**

* [.NET SDK 6.0](https://dotnet.microsoft.com/download/dotnet/6.0) or newer
* Discord Account
* Basic knowledge of using a terminal/command line

---

## **Configuration**

For the application to function correctly, you need to configure several key elements.

### **Step 1: Create a Discord Bot**

1.  Go to the [Discord Developer Portal](https://discord.com/developers/applications).
2.  Log in or register.
3.  Click **"New Application"**.
4.  Provide a name for your application and click **"Create"**.
5.  In the left navigation panel, go to the **"Bot"** section.
6.  Click **"Add Bot"** and confirm.
7.  Under **"Privileged Gateway Intents"**, enable **"MESSAGE CONTENT INTENT"**.
8.  Copy the bot's **token** (click **"Reset Token"** if you don't see it, then **"Copy"**). Keep it in a safe place as it will be needed in the next step. **Never share your bot token publicly!**

#### **Inviting the Bot to Your Discord Server**

1.  In the Discord Developer Portal, navigate to the **"OAuth2" -> "URL Generator"** section.
2.  Under "SCOPES", select `bot`.
3.  Under "BOT PERMISSIONS", select at least:
    * Send Messages
    * Read Message History
    * Attach Files
    * Manage Messages (for deleting files)
4.  Copy the generated URL and paste it into your browser.
5.  Select the server you wish to invite the bot to and authorize it.

#### **Obtaining the Channel ID**

1.  In the Discord client, enable Developer Mode (User Settings -> Advanced -> Developer Mode).
2.  Right-click on the text channel you intend to use for file storage and select **"Copy ID"**.

### **Step 2: Set Bot Token and Channel ID**

Open the `DiscordCloud.Core/DiscordBotService/DiscordBot.cs` file.

* **Bot Token:**
    Find the line declaring `Token` and replace "YOUR DISCORD BOT TOKEN" with your Discord bot token.
    ```csharp
    // DiscordCloud.Core/DiscordBotService/DiscordBot.cs
    private readonly string Token = "YOUR_DISCORD_BOT_TOKEN"; // Replace with your bot token
    ```
* **Channel ID:**
    Find the line declaring `channelId` and replace `0` with the Discord channel ID you copied earlier.
    ```csharp
    // DiscordCloud.Core/DiscordBotService/DiscordBot.cs
    private readonly ulong channelId = 123456789012345678; // Set your own channel ID
    ```

### **Step 3: Configure Server URL**

There are two places where you need to update the server URL:

* **In `Program.cs` file:**
    In the CORS configuration section, ensure your server's URL is added to the allowed origins list.
    ```csharp
    // Program.cs
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7083", "http://localhost:5000", "YOUR_SERVER_URL") // Add your server URLs here
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
    ```
    Replace `"YOUR_SERVER_URL"` with the actual URL where your application will run (e.g., `https://your-server.com`).
* **In `Pages/Index.razor` file (Blazor Frontend):**
    Set the `URL` variable to your server's URL so the frontend knows where to send API requests.
    ```csharp
    // Pages/Index.razor
    @code {
        // ...
        private string URL = "http://localhost:5000/"; // Set your server URL (e.g., "[https://your-server.com/](https://your-server.com/)")
        // ...
    }
    ```
    Ensure this URL matches the server URL defined in `Program.cs`.

### **Step 4: Set Encryption Password**

Open the `DiscordCloud.Core/Controllers/FileController.cs` file.

* **Encryption Password:**
    Set your own password for file encryption in the `pass` variable.
    ```csharp
    // DiscordCloud.Core/Controllers/FileController.cs
    private readonly string pass = "SET_YOUR_OWN_ENCRYPTION_PASSWORD"; // Set your own encryption password for files
    ```
    **Important:** This password is crucial for encrypting and decrypting files. Keep it secret and do not change it after uploading files, as it will make them undecryptable.

---

## **Running the Application**

After configuring all the points above, you can run the application:

1.  Open a terminal or command prompt.
2.  Navigate to the main project directory (where the `.sln` file or the main project's `.csproj` file is located).
3.  Run the following commands:
    ```bash
    dotnet restore
    dotnet run
    ```
    The application will compile and start. You will be informed of the URLs where the application is listening (typically `http://localhost:5000` and `https://localhost:7083` in development mode).

---

## **Usage**

Once the application is running, open your browser and navigate to the URL where your Blazor frontend is hosted (e.g., `http://localhost:5000`).

* **Uploading files:** Click the "Upload Files" button in the interface, select your files, and click "Upload".
* **Listing files:** The table on the main page will automatically display all files available on the Discord channel. You can also use the search bar to filter files.
* **Downloading files:** Click the "Download" button next to the file name to download and decrypt the file.
* **Deleting files:** Click the "Remove File" button next to the file name. You will be asked for confirmation to delete.

---

## **Technologies**

* **Backend:** ASP.NET Core (C#)
* **Frontend:** Blazor
* **Discord Bot:** Discord.Net
* **Encryption:** Standard .NET cryptographic libraries (encryption/decryption implementation)
