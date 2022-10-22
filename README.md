<div align="center">
  
  # SLBr
  
  **The browser that prioritizes a faster web**<br/>
  **Browse the web with a fast, lightweight web browser.**

[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://github.com/SLT-World/SLBr)
[![XAML](https://img.shields.io/static/v1?style=for-the-badge&message=XAML&color=0C54C2&logo=XAML&logoColor=FFFFFF&label=)](https://github.com/SLT-World/SLBr)
[![.Net](https://img.shields.io/static/v1?style=for-the-badge&message=.NET&color=512BD4&logo=.NET&logoColor=FFFFFF&label=)](https://github.com/SLT-World/SLBr)
[![Visual Studio](https://img.shields.io/static/v1?style=for-the-badge&message=Visual+Studio&color=5C2D91&logo=Visual+Studio&logoColor=FFFFFF&label=)](https://github.com/SLT-World/SLBr)<br/>
[![Latest release](https://img.shields.io/static/v1?style=for-the-badge&message=Latest%20release&color=0092FF&logoColor=FFFFFF&label=)](https://github.com/SLT-World/SLBr/releases/latest)
</div>

## Based on CefSharp/CEF
CefSharp is an embedded Chromium browser for .NET apps such as WPF and Winforms. It is a lightweight .NET wrapper for the Chromium Embedded Framework (CEF).

CefSharp follows modern web standards, and supports HTML5, JavaScript, CSS3, HTML5 audio/video elements as well as WebGL. CefSharp has basically everything a modern browser has.

## Using SLBr
Download the latest version of SLBr and open it
- https://github.com/SLT-World/SLBr/releases/latest

## Notes
If you're encountering issues with SLBr rendering weirdly (Examples are: controls disappearing, text oversizing, colors changing abnormally), then you're most likely encountering this [issue](https://github.com/dotnet/wpf/issues/4141). Either switch to software rendering using SLBr's WPF render mode setting, or disable the nahimic service to resolve the issue.

## Special thanks to
- The Chromium Embedded Framework (CEF) by Marshall A. Greenblatt.
- The CefSharp team, and Amaitland who dedicated himself to the CefSharp project.
- Mauve who helped out on the IPFS implementation for SLBr.

## Requirements
- [Microsoft Visual C++ 2019](https://learn.microsoft.com/en-US/cpp/windows/latest-supported-vc-redist?view=msvc-170)
- [.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- Segoe Fluent Icons
- Segoe MDL2 Assets
- Windows 7 and above

## Screenshots & Videos

![Dark mode](https://raw.githubusercontent.com/SLT-World/SLBr/main/SLBr/Resources/SLBr%20Dark%20Mode.png)

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
