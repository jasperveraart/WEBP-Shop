# Dev Tunnel Setup Guide

This guide explains how to set up a **Dev Tunnel** to expose your local API to the internet, allowing the Android Emulator (and other external devices) to connect to it securely.

## 1. Installation

### macOS / Linux
Run the following command in your terminal:
```bash
curl -sL https://aka.ms/DevTunnelCliInstall | bash
```
After installation, ensure the binary is in your PATH or alias it.
*If installed to `~/bin/devtunnel`, you can run it as `~/bin/devtunnel`.*

### Windows
Run the following command in PowerShell:
```powershell
iex "& { $(irm https://aka.ms/DevTunnelCliInstall.ps1) } V2"
```

## 2. Authentication

Before using the tunnel, you must log in:
```bash
devtunnel user login
```
Follow the on-screen instructions to authenticate via your browser (using a Microsoft or GitHub account).

## 3. Creating a Persistent Tunnel (Recommended)

By default, `devtunnel host` generates a random URL every time. To keep the URL stable (so you don't have to update the app constantly), create a named tunnel:

1.  **Create the tunnel:**
    ```bash
    devtunnel create pwebshop-tunnel --allow-anonymous
    ```
    *Replace `pwebshop-tunnel` with a unique name of your choice.*

2.  **Host the tunnel:**
    ```bash
    devtunnel host pwebshop-tunnel -p 5091
    ```
    *Port `5091` matches your API's HTTP port.*

3.  **Get the URL:**
    The output will show a URL like: `https://pwebshop-tunnel.uks1.devtunnels.ms`
    **This URL will remain the same** as long as you use this tunnel name.

## 4. Configuring the App

Once you have your persistent URL:

1.  Open `PWebShop.Hybrid/MauiProgram.cs`.
2.  Update the `baseAddress` for Android:
    ```csharp
    if (DeviceInfo.Platform == DevicePlatform.Android)
    {
        baseAddress = "https://YOUR-TUNNEL-ID.uks1.devtunnels.ms";
    }
    ```
3.  Rebuild and deploy the app.

## 5. Using with Web App

If you want to access the Web App from an external device (e.g., mobile browser):
1.  Host the Web App port (e.g., `5136`) on the tunnel as well, or create a second tunnel.
2.  Currently, the Web App connects to the API via `localhost` (server-side). If the Web App is running on the same machine as the API, no changes are needed for the Web App to talk to the API.

## Troubleshooting

*   **"Blazor has already started"**: Ensure `index.html` has `autostart="false"` removed (or handled correctly).
*   **"Loading..." hang**: Check the logs (`adb logcat`) for Auth timeouts. Ensure the tunnel is running and accessible (`curl -I <tunnel-url>/api/products`).
