<div align="center">
  
  # SLBr
  
  **The browser that prioritizes a faster web**<br/>
  **Browse the web with a fast, lightweight web browser.**
</div>

## Based on the Chromium platform

With the Chromium browser engine, SLBr has access to Chromium's useful features that can be implemented easily.

## System Requirements
- Microsoft Visual C++ 2019
- .NET 6.0
- Segoe MDL2 Assets
- Windows 7 and above

## Roadmap
- [x] Google Account Sign-In
- [x] SafeBrowsing API
- [x] Javascript Binding
- [x] Default, Private, Developer, Chromium, IE modes
- [x] Drag & Drop content
- [x] Default Browser
- [x] Built-in Ad & Tracker blocker
- [x] Auto hide contents of "chrome://"
- [x] Docked Inspector/DevTools
- [x] Tab Unloading
- [x] Download Handler
- [x] Force dark theme on webpages
- [x] Themes
- [ ] Smooth scrolling
- [ ] Link & Image preview
- [x] Context Menu not closing bug fixed
- [x] Suggestions
- [x] Render modes [Hardware, Software]
- [x] Parallel downloading
- [x] Built-in News
- [x] Google Weblight
- [x] Self Host Chromium

## How to download
Go to https://github.com/SLT-World/SLBr/releases and download the latest version. Don't mess this up with the source code.

## Screenshots
![SLBr Dark Mode screenshot](https://github.com/SLT-World/SLBr/blob/main/SLBr/SLBr/Images/New%20Dark%20Mode.png)

## How to run the source code
Go to https://github.com/SLT-World/SLBr/releases and download the latest version. This is a guide on how to compile and run the source code.
**Setup**

1. If you haven't installed visual studio yet (NOT VISUAL STUDIO CODE), install it.
2. After that, download the project as a ZIP file from Github.
3. Unzip the file, find the solution file and open it.
4. Tada, now you can change the code the way you like and contribute some features to SLBr!

**The class "SECRETS" is missing**

The SECRETS file is removed as it contains the API Keys of SLT's SLBr. To fix it, either remove the line of code that is causing the error, which will remove support for SafeBrowsing, Google Sign-ins and Geolocation. Or, generate a new C# class called "SECRETS", have string variables named "GOOGLE_API_KEY", "GOOGLE_DEFAULT_CLIENT_ID", "GOOGLE_DEFAULT_CLIENT_SECRET".

**Why CefSharp?**

[**CefSharp**](https://github.com/cefsharp/CefSharp) is well supported and has a large community, it's been around much longer than the Microsoft WebView2 and CefSharp is easier for my standard.

**Does SLBr send browsing habits to companies or tracks activities**

SLBr does send urls to the SafeBrowsing API for security.<br/>
And when Weblight is enabled, SLBr sends the address to Google Weblight servers for compression
