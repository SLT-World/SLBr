<div align="center">
  
  # SLBr
  
  **The browser that prioritizes a faster web**<br/>
  **Browse the web with a fast, lightweight web browser.**
</div>

## Installing SLBr
Install the latest version of SLBr and open it
- https://github.com/SLT-World/SLBr/releases/latest

## Based on CefSharp
CefSharp is an embedded Chromium browser for .NET apps such as WPF and Winforms. It is a lightweight .NET wrapper for the Chromium Embedded Framework (CEF).

CefSharp follows modern web standards, and supports HTML5, JavaScript, CSS3, HTML5 audio/video elements as well as WebGL. CefSharp has basically everything a modern browser would need.

## Special thanks to
- The Chromium Embedded Framework (CEF) by Marshall A. Greenblatt.
- The CefSharp team, and Amaitland who dedicated himself to the CefSharp project.
- Mauve who helped out on the IPFS implementation for SLBr.

## System Requirements
- Microsoft Visual C++ 2019
- .NET 6.0
- Segoe MDL2 Assets
- Windows 7 and above

## Screenshots & Videos
![SLBr Dark Mode screenshot](https://github.com/SLT-World/SLBr/blob/main/SLBr/SLBr/Images/New%20Dark%20Mode.png)
![SLBr Youtube Popout](https://github.com/SLT-World/SLBr/blob/main/SLBr/SLBr/Images/Screenshot%20Youtube%20Popout.png)

Video: https://youtu.be/PtmDRjgwmHI

## Compiling and building SLBr
This is a guide on how to compile and run the source code.
**Setup**

1. Install and set up [Visual Studio](https://visualstudio.microsoft.com/vs/)
2. Download SLBr's Source code and unzip it
3. Find the solution file

**The class "SECRETS" is missing**

The SECRETS file is removed as it contains the API Keys of SLT's SLBr. To fix it, either remove the line of code that is causing the error, which will remove support for SafeBrowsing, www.google.com sign-ins and Geolocation. Or, generate a new C# class called "SECRETS", have string variables named "GOOGLE_API_KEY", "GOOGLE_DEFAULT_CLIENT_ID", "GOOGLE_DEFAULT_CLIENT_SECRET".

SLBr is neither affiliated with Google nor Microsoft.
