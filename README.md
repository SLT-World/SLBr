# SLBr
The new, clean, lightweight .NET Web browser

SLBr is one of the best C# WPF Web Browser there is! More lightweight and less memory usage compared to Google Chrome, with a modern UI design.

# Taking advantage of the powerful Chromium platform

SLBr uses the fast, light speed [**CEFSharp**](https://github.com/cefsharp/CefSharp) which is a wrapper around the [**Chromium Embedded Framework (CEF)**](https://bitbucket.org/chromiumembedded/cef/src/master/) to render webpages.

# Important
- SLBr now works with Intel i5 processors
- Microsoft Visual C++ 2019 is required
- .NET Framework 4.8 and above is required
- No support for Windows XP/2003 and Windows Vista/Server 2008 (non R2)
- Segoe MDL2 Assets is required to be installed in Windows 7 and below

# Screenshots
![SLBr Dark Mode screenshot](https://github.com/SLT-World/SLBr/blob/main/SLBr/SLBr/Images/Dark%20mode%20banner%20github.png)

# Roadmap
- [x] Google Account Sign-In
- [x] SafeBrowsing API
- [x] Javascript Binding
- [x] Default, Private, Developer, Chromium, IE modes
- [x] Drag & Drop content
- [x] Default Browser
- [x] Ad & Tracker Blocker
- [x] Auto hide contents of "chrome://"
- [x] Docked Inspector/DevTools
- [x] Tab Unloading
- [x] Download Handler
- [x] Force dark theme on webpages
- [x] Themes
- [x] Smooth scrolling
- [x] Link & Image preview
- [x] Context Menu not closing bug fixed
- [x] Suggestions

# Run the source code
**Setup**

1. If you haven't installed visual studio yet (NOT VISUAL STUDIO CODE), install it.

2. After that, download the project as a ZIP file from Github.

3. Unzip the file, find the solution file and open it.

4. Tada, now you can change the code the way you like and contribute some features to SLBr!

**The class "SECRETS" is missing**

The SECRETS file is removed as it contains the API Keys of SLT's SLBr. To fix it, either remove the line of code that is causing the error, which will remove support for SafeBrowsing, Google Sign-ins and Geolocation. Or, generate a new C# class called "SECRETS", have string variables named "GOOGLE_API_KEY", "GOOGLE_DEFAULT_CLIENT_ID", "GOOGLE_DEFAULT_CLIENT_SECRET".

**Why CefSharp?**

[**CefSharp**](https://github.com/cefsharp/CefSharp) is well supported and has a large community, it's been around much longer than the Microsoft WebView2 and CefSharp is more easy for my standard.

**Does SLBr send browsing habits to companies or tracks activities**

SLBr does send urls to the SafeBrowsing API for security.
