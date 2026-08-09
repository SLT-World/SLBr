/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using CefSharp;
using CefSharp.Wpf.HwndHost;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SLBr.Pages;
using SLBr.WebView;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SLBr.Extensions
{
    public class Extension : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public WebEngineType EngineType { get; set; }

        private string _ID;
        public string ID
        {
            get => _ID;
            set
            {
                _ID = value;
                RaisePropertyChanged();
            }
        }

        private string _Version;
        public string Version
        {
            get => _Version;
            set
            {
                _Version = value;
                RaisePropertyChanged();
            }
        }

        private string _Name;
        public string Name
        {
            get => _Name;
            set
            {
                _Name = value;
                RaisePropertyChanged();
            }
        }

        private string _Description;
        public string Description
        {
            get => _Description;
            set
            {
                _Description = value;
                RaisePropertyChanged();
            }
        }

        private bool? _IsEnabled = null;
        public bool? IsEnabled
        {
            get { return _IsEnabled; }
            set
            {
                if (value != _IsEnabled)
                {
                    _IsEnabled = value;
                    RaisePropertyChanged(nameof(IsEnabled));
                    if (value.HasValue)
                        App.Instance.ExtensionManager.ToggleExtension(this, value.Value);
                }
            }
        }

        private string _ActionIcon;
        public string ActionIcon
        {
            get { return _ActionIcon; }
            set
            {
                _ActionIcon = value;
                RaisePropertyChanged();
            }
        }

        private string _ActionPopup;
        public string ActionPopup
        {
            get => _ActionPopup;
            set
            {
                _ActionPopup = value;
                RaisePropertyChanged();
            }
        }
    }

    public class ExtensionManager
    {
        private ObservableCollection<Extension> CEFExtensions = [];
        private ObservableCollection<Extension> WebView2Extensions = [];

        public ObservableCollection<Extension>? GetExtensions(WebEngineType Engine)
        {
            return Engine switch
            {
                WebEngineType.Chromium => CEFExtensions,
                WebEngineType.ChromiumEdge => WebView2Extensions,
                _ => null,
            };
        }

        public event EventHandler Changed;

        public ExtensionManager()
        {
            CEFExtensions.CollectionChanged += OnExtensionsCollectionChanged;
            WebView2Extensions.CollectionChanged += OnExtensionsCollectionChanged;
        }

        private void OnExtensionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Changed?.Invoke(this, e);
        }

        public async Task LoadCEFExtensions()
        {
            if (WebViewManager.CEFExtensionsPath == null)
                return;
            List<Extension> Results = await Cef.UIThreadTaskFactory.StartNew(() =>
            {
                List<Extension> List = [];
                IRequestContext GlobalRequestContext = Cef.GetGlobalRequestContext();
                if (GlobalRequestContext.HasPreference("extensions.settings"))
                {
                    object Settings = GlobalRequestContext.GetPreference("extensions.settings");
                    if (Settings is IDictionary<string, object> SettingsData)
                    {
                        foreach (KeyValuePair<string, object> KeyValue in SettingsData)
                        {
                            IDictionary<string, object>? ExtensionData = KeyValue.Value as IDictionary<string, object>;
                            if (ExtensionData == null)
                                continue;
                            if (ExtensionData.TryGetValue("creation_flags", out var CreationProperty) && CreationProperty is int Creation && Creation == 1)
                                continue;
                            if (ExtensionData.TryGetValue("path", out var ExtensionPathProperty) && ExtensionPathProperty is string ExtensionPath)
                            {
                                if (!Path.Exists(ExtensionPath))
                                    ExtensionPath = Path.Combine(WebViewManager.CEFExtensionsPath, ExtensionPath);
                                Extension _Extension = new()
                                {
                                    EngineType = WebEngineType.Chromium,
                                    ID = KeyValue.Key,
                                    IsEnabled = null
                                };
                                if (ExtensionData.TryGetValue("manifest", out var ManifestProperty) && ManifestProperty is IDictionary<string, object> ManifestData)
                                {
                                    if (ManifestData.TryGetValue("name", out var NameProperty))
                                        _Extension.Name = NameProperty?.ToString();
                                    if (ManifestData.TryGetValue("description", out var DescriptionProperty))
                                        _Extension.Description = DescriptionProperty?.ToString();
                                    if (ManifestData.TryGetValue("version", out var VersionProperty))
                                        _Extension.Version = VersionProperty?.ToString();
                                    if (ManifestData.TryGetValue("action", out var ActionProperty) && ActionProperty is IDictionary<string, object> ActionData)
                                    {
                                        if (ActionData.TryGetValue("default_popup", out var PopupProperty))
                                            _Extension.ActionPopup = $"chrome-extension://{_Extension.ID}/{PopupProperty?.ToString()}";
                                        if (ActionData.TryGetValue("default_icon", out var IconProperty))
                                        {
                                            if (IconProperty is IDictionary<string, object> IconData)
                                                _Extension.ActionIcon = Path.Combine(ExtensionPath, PickBestIcon(IconData));
                                            else
                                                _Extension.ActionIcon = Path.Combine(ExtensionPath, IconProperty?.ToString());
                                        }
                                    }
                                }
                                else
                                {
                                    string ManifestPath = Path.Combine(ExtensionPath, "manifest.json");
                                    if (File.Exists(ManifestPath))
                                    {
                                        using JsonDocument Document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
                                        JsonElement Manifest = Document.RootElement;
                                        if (Manifest.TryGetProperty("version", out JsonElement VersionProperty))
                                            _Extension.Version = VersionProperty.GetString();
                                        if (Manifest.TryGetProperty("action", out JsonElement ExtensionAction))
                                        {
                                            if (ExtensionAction.TryGetProperty("default_popup", out JsonElement ExtensionPopup))
                                                _Extension.ActionPopup = $"chrome-extension://{_Extension.ID}/{ExtensionPopup.GetString()}";
                                            else if (ExtensionAction.TryGetProperty("default_icon", out JsonElement IconProperty))
                                            {
                                                JsonProperty FirstIcon = IconProperty.EnumerateObject().OrderBy(i => int.Parse(i.Name)).FirstOrDefault();
                                                _Extension.ActionIcon = Path.Combine(ExtensionPath, FirstIcon.Value.GetString());
                                            }
                                        }
                                        List<string> VarsInMessages = [];
                                        if (Manifest.TryGetProperty("name", out JsonElement NameProperty))
                                        {
                                            string Name = NameProperty.GetString();
                                            if (Name.StartsWith("__MSG_"))
                                                VarsInMessages.Add($"Name<|>{Name}");
                                            else
                                                _Extension.Name = Name;
                                        }
                                        if (Manifest.TryGetProperty("description", out JsonElement DescriptionProperty))
                                        {
                                            string Description = DescriptionProperty.GetString();
                                            if (Description.StartsWith("__MSG_"))
                                                VarsInMessages.Add($"Description<|>{Description}");
                                            else
                                                _Extension.Description = Description;
                                        }

                                        foreach (string Var in VarsInMessages)
                                        {
                                            string _Locale = "en";
                                            string[] LocalesDirectory = Directory.GetDirectories(Path.Combine(ExtensionPath, "_locales"));
                                            foreach (string LocaleDirectory in LocalesDirectory)
                                            {
                                                string CompareLocale = App.Instance.Locale.Name.Replace("-", "_");
                                                if (Path.GetFileName(LocaleDirectory) == CompareLocale)
                                                {
                                                    _Locale = CompareLocale;
                                                    break;
                                                }
                                            }
                                            string[] MessagesFiles = Directory.GetFiles(Path.Combine(ExtensionPath, "_locales", _Locale), "messages.json", SearchOption.TopDirectoryOnly);
                                            foreach (string MessagesFile in MessagesFiles)
                                            {
                                                using JsonDocument MDocument = JsonDocument.Parse(File.ReadAllText(MessagesFile));
                                                JsonElement Messages = MDocument.RootElement;
                                                string[] Vars = Var.Split("<|>");
                                                if (Vars[0] == "Description")
                                                {
                                                    _Extension.Description = Messages.GetProperty(Vars[1][5..].Trim('_')).GetProperty("message").ToString();
                                                    break;
                                                }
                                                else if (Vars[0] == "Name")
                                                {
                                                    _Extension.Name = Messages.GetProperty(Vars[1][5..].Trim('_')).GetProperty("message").ToString();
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                List.Add(_Extension);
                            }
                        }
                    }
                }
                return List;
            });
            CEFExtensions.Clear();
            foreach (Extension _Extension in Results)
                CEFExtensions.Add(_Extension);
        }

        private string PickBestIcon(IDictionary<string, object> IconData)
        {
            string BestIcon = null;
            int MaxResolution = -1;
            foreach (KeyValuePair<string, object> Icon in IconData)
            {
                if (int.TryParse(Icon.Key, out int Resolution))
                {
                    if (Resolution > MaxResolution)
                    {
                        MaxResolution = Resolution;
                        BestIcon = Icon.Value?.ToString();
                    }
                }
            }
            return BestIcon ?? IconData.Values.FirstOrDefault()?.ToString();
        }

        public async Task LoadWebView2Extensions()
        {
            List<Extension> Results = [];
            if (WebViewManager.WebView2ExtensionsPath == null)
                return;

            if (WebViewManager.HeadlessEdgeCore == null || WebViewManager.HeadlessEdgeController == null)
                await WebViewManager.CreateHeadlessWebView2();

            try
            {
                _ = WebViewManager.HeadlessEdgeCore.Profile;
            }
            catch (InvalidOperationException)
            {
                WebViewManager.HeadlessEdgeController?.Close();
                await WebViewManager.CreateHeadlessWebView2();
            }
            CoreWebView2Profile Profile = WebViewManager.HeadlessEdgeCore.Profile;
            try
            {
                IReadOnlyList<CoreWebView2BrowserExtension> WebView2InternalExtensions = await Profile.GetBrowserExtensionsAsync();
                foreach (CoreWebView2BrowserExtension WebView2Extension in WebView2InternalExtensions)
                {
                    switch (WebView2Extension.Name)
                    {
                        case "Microsoft Clipboard Extension":
                            continue;
                        case "Microsoft Edge PDF Viewer":
                            continue;
                    }
                    Extension _Extension = new()
                    {
                        EngineType = WebEngineType.ChromiumEdge,
                        ID = WebView2Extension.Id,
                        IsEnabled = WebView2Extension.IsEnabled,
                        Name = WebView2Extension.Name,
                    };
                    string ProfilePath = Path.Combine(WebViewManager.WebView2ExtensionsPath, _Extension.ID);
                    if (Directory.Exists(ProfilePath))
                    {
                        string? ExtensionPath = Directory.GetDirectories(ProfilePath).FirstOrDefault();
                        if (!Directory.Exists(ExtensionPath))
                            ExtensionPath = ProfilePath;
                        string ManifestPath = Path.Combine(ExtensionPath, "manifest.json");
                        if (File.Exists(ManifestPath))
                        {
                            using JsonDocument Document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
                            JsonElement Manifest = Document.RootElement;
                            if (Manifest.TryGetProperty("version", out JsonElement VersionProperty))
                                _Extension.Version = VersionProperty.GetString();
                            if (Manifest.TryGetProperty("action", out JsonElement ExtensionAction))
                            {
                                if (ExtensionAction.TryGetProperty("default_popup", out JsonElement ExtensionPopup))
                                    _Extension.ActionPopup = $"chrome-extension://{_Extension.ID}/{ExtensionPopup.GetString()}";
                                else if (ExtensionAction.TryGetProperty("default_icon", out JsonElement IconProperty))
                                {
                                    JsonProperty FirstIcon = IconProperty.EnumerateObject().OrderBy(i => int.Parse(i.Name)).FirstOrDefault();
                                    _Extension.ActionIcon = Path.Combine(ExtensionPath, FirstIcon.Value.GetString());
                                }
                            }
                            List<string> VarsInMessages = [];
                            if (Manifest.TryGetProperty("name", out JsonElement NameProperty))
                            {
                                string Name = NameProperty.GetString();
                                if (Name.StartsWith("__MSG_"))
                                    VarsInMessages.Add($"Name<|>{Name}");
                                else
                                    _Extension.Name = Name;
                            }
                            if (Manifest.TryGetProperty("description", out JsonElement DescriptionProperty))
                            {
                                string Description = DescriptionProperty.GetString();
                                if (Description.StartsWith("__MSG_"))
                                    VarsInMessages.Add($"Description<|>{Description}");
                                else
                                    _Extension.Description = Description;
                            }

                            foreach (string Var in VarsInMessages)
                            {
                                string _Locale = "en";
                                string[] LocalesDirectory = Directory.GetDirectories(Path.Combine(ExtensionPath, "_locales"));
                                foreach (string LocaleDirectory in LocalesDirectory)
                                {
                                    string CompareLocale = App.Instance.Locale.Name.Replace("-", "_");
                                    if (Path.GetFileName(LocaleDirectory) == CompareLocale)
                                    {
                                        _Locale = CompareLocale;
                                        break;
                                    }
                                }
                                string[] MessagesFiles = Directory.GetFiles(Path.Combine(ExtensionPath, "_locales", _Locale), "messages.json", SearchOption.TopDirectoryOnly);
                                foreach (string MessagesFile in MessagesFiles)
                                {
                                    using JsonDocument MDocument = JsonDocument.Parse(File.ReadAllText(MessagesFile));
                                    JsonElement Messages = MDocument.RootElement;
                                    string[] Vars = Var.Split("<|>");
                                    if (Vars[0] == "Description")
                                    {
                                        _Extension.Description = Messages.GetProperty(Vars[1][5..].Trim('_')).GetProperty("message").ToString();
                                        break;
                                    }
                                    else if (Vars[0] == "Name")
                                    {
                                        _Extension.Name = Messages.GetProperty(Vars[1][5..].Trim('_')).GetProperty("message").ToString();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    Results.Add(_Extension);
                }
            }
            catch { }
            WebView2Extensions.Clear();
            foreach (Extension _Extension in Results)
                WebView2Extensions.Add(_Extension);
            /*if (Directory.Exists(WebViewManager.WebView2ExtensionsPath))
            {
                string[] ExtensionsDirectory = Directory.GetDirectories(WebViewManager.WebView2ExtensionsPath);
                foreach (string ExtensionParentDirectory in ExtensionsDirectory)
                {
                    try
                    {
                        Extension _Extension = new()
                        {
                            ID = Path.GetFileName(ExtensionParentDirectory),
                            IsEnabled = true
                        };
                        string ExtensionPath = Directory.EnumerateDirectories(ExtensionParentDirectory).FirstOrDefault();
                        if (!Directory.Exists(ExtensionPath))
                            ExtensionPath = ExtensionParentDirectory;
                        string ManifestPath = Path.Combine(ExtensionPath, "manifest.json");
                        if (File.Exists(ManifestPath))
                        {
                            using JsonDocument Document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
                            JsonElement Manifest = Document.RootElement;
                            if (Manifest.TryGetProperty("version", out JsonElement VersionProperty))
                                _Extension.Version = VersionProperty.GetString();
                            if (Manifest.TryGetProperty("action", out JsonElement ExtensionAction))
                            {
                                if (ExtensionAction.TryGetProperty("default_popup", out JsonElement ExtensionPopup))
                                    _Extension.ActionPopup = $"chrome-extension://{_Extension.ID}/{ExtensionPopup.GetString()}";
                                else if (ExtensionAction.TryGetProperty("default_icon", out JsonElement IconProperty))
                                {
                                    JsonProperty FirstIcon = IconProperty.EnumerateObject().OrderBy(i => int.Parse(i.Name)).FirstOrDefault();
                                    _Extension.ActionIcon = Path.Combine(ExtensionPath, FirstIcon.Value.GetString());
                                }
                            }
                            List<string> VarsInMessages = [];
                            if (Manifest.TryGetProperty("name", out JsonElement NameProperty))
                            {
                                string Name = NameProperty.GetString();
                                if (Name.StartsWith("__MSG_"))
                                    VarsInMessages.Add($"Name<|>{Name}");
                                else
                                    _Extension.Name = Name;
                            }
                            if (Manifest.TryGetProperty("description", out JsonElement DescriptionProperty))
                            {
                                string Description = DescriptionProperty.GetString();
                                if (Description.StartsWith("__MSG_"))
                                    VarsInMessages.Add($"Description<|>{Description}");
                                else
                                    _Extension.Description = Description;
                            }

                            foreach (string Var in VarsInMessages)
                            {
                                string _Locale = "en";
                                string[] LocalesDirectory = Directory.GetDirectories(Path.Combine(ExtensionPath, "_locales"));
                                foreach (string LocaleDirectory in LocalesDirectory)
                                {
                                    string CompareLocale = App.Instance.Locale.Name.Replace("-", "_");
                                    if (Path.GetFileName(LocaleDirectory) == CompareLocale)
                                    {
                                        _Locale = CompareLocale;
                                        break;
                                    }
                                }
                                string[] MessagesFiles = Directory.GetFiles(Path.Combine(ExtensionPath, "_locales", _Locale), "messages.json", SearchOption.TopDirectoryOnly);
                                foreach (string MessagesFile in MessagesFiles)
                                {
                                    using JsonDocument MDocument = JsonDocument.Parse(File.ReadAllText(MessagesFile));
                                    JsonElement Messages = MDocument.RootElement;
                                    string[] Vars = Var.Split("<|>");
                                    if (Vars[0] == "Description")
                                    {
                                        _Extension.Description = Messages.GetProperty(Vars[1][5..].Trim('_')).GetProperty("message").ToString();
                                        break;
                                    }
                                    else if (Vars[0] == "Name")
                                    {
                                        _Extension.Name = Messages.GetProperty(Vars[1][5..].Trim('_')).GetProperty("message").ToString();
                                        break;
                                    }
                                }
                            }
                        }
                        WebView2Extensions.Add(_Extension);
                    }
                    catch { }
                }
            }*/
        }

        public async Task ToggleExtension(Extension _Extension, bool Enable)
        {
            if (_Extension.IsEnabled != Enable)
                _Extension.IsEnabled = Enable;
            else
            {
                if (_Extension.EngineType == WebEngineType.ChromiumEdge)
                {
                    CoreWebView2Profile Profile = WebViewManager.HeadlessEdgeCore.Profile;
                    IReadOnlyList<CoreWebView2BrowserExtension> WebView2InternalExtensions = await Profile.GetBrowserExtensionsAsync();
                    await WebView2InternalExtensions.FirstOrDefault(i => i.Id == _Extension.ID)?.EnableAsync(Enable);
                }
            }
        }

        #region Popup
        Window? PopupWindow;
        ChromiumWebBrowser? CEFPopupBrowser;
        WebView2? WebView2PopupBrowser;
        DispatcherTimer PopupDismissTimer = null;

        public async void TriggerAction(Extension _Extension, Browser Target, FrameworkElement Anchor)
        {
            if (PopupWindow != null)
                CloseAction();
            if (Target.WebView == null)
                return;
            PopupWindow = new();

            nint ExtensionHandle = new WindowInteropHelper(PopupWindow).EnsureHandle();
            HwndSource.FromHwnd(ExtensionHandle).AddHook(WndProc);
            int DarkModeValue = App.Instance.CurrentTheme.DarkTitleBar ? 0x01 : 0x00;
            DllUtils.DwmSetWindowAttribute(ExtensionHandle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref DarkModeValue, Marshal.SizeOf(typeof(int)));
            int TrueValue = 0x01;
            DllUtils.DwmSetWindowAttribute(ExtensionHandle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref TrueValue, Marshal.SizeOf(typeof(int)));
            //int CornerPreferenceValue = 2;
            //DllUtils.DwmSetWindowAttribute(ExtensionHandle, DwmWindowAttribute.DWMWA_WINDOW_CORNER_PREFERENCE, ref CornerPreferenceValue, Marshal.SizeOf(typeof(int)));

            PopupWindow.WindowStyle = WindowStyle.None;
            PopupWindow.SizeToContent = SizeToContent.WidthAndHeight;
            PopupWindow.ShowInTaskbar = false;
            PopupWindow.Title = "Extension";
            PopupWindow.ResizeMode = ResizeMode.NoResize;
            PopupWindow.Topmost = true;

            Point ButtonTopLeft = Anchor.PointToScreen(new Point(0, 0));

            PresentationSource Source = PresentationSource.FromVisual(Anchor);
            double DPIX = Source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double DPIY = Source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            double TargetLeft = ButtonTopLeft.X + Anchor.ActualWidth * DPIX;
            double TargetTop = ButtonTopLeft.Y + Anchor.ActualHeight * DPIY;

            PopupWindow.Left = TargetLeft / DPIX;
            PopupWindow.Top = TargetTop / DPIY;

            PopupWindow.SizeChanged += (s, args) =>
            {
                PopupWindow.Left = (TargetLeft / DPIX) - PopupWindow.ActualWidth;
                PopupWindow.Top = TargetTop / DPIY;
            };

            if (Target.WebView.Engine == WebEngineType.Chromium)
            {
                CEFPopupBrowser = await BuildCEF(_Extension, Target);
                PopupWindow.Content = CEFPopupBrowser;
            }
            else
            {
                WebView2PopupBrowser = await BuildWebView2(_Extension, Target);
                PopupWindow.Content = WebView2PopupBrowser;
            }

            PopupWindow.Show();
            PopupWindow.Activate();

            PopupWindow.Deactivated += (s, args) =>
            {
                PopupDismissTimer?.Stop();
                PopupDismissTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(150)
                };

                PopupDismissTimer.Tick += (_, _) =>
                {
                    PopupDismissTimer.Stop();
                    nint ActiveWindowHandle = DllUtils.GetForegroundWindow();
                    if (ActiveWindowHandle == IntPtr.Zero || ActiveWindowHandle == ExtensionHandle)
                        return;

                    uint CurrentProcessId = (uint)Environment.ProcessId;
                    DllUtils.GetWindowThreadProcessId(ActiveWindowHandle, out uint FocusedProcessId);

                    if (FocusedProcessId != CurrentProcessId)
                    {
                        string TargetProcessName = "";
                        try
                        {
                            using Process _Process = Process.GetProcessById((int)FocusedProcessId);
                            TargetProcessName = _Process.ProcessName.ToLower();
                        }
                        catch { }

                        if (TargetProcessName.Contains("webview2"))
                            return;

                        try { CloseAction(); } catch { }
                        return;
                    }

                    if (ActiveWindowHandle == Target.Tab.ParentWindow.Handle)
                    {
                        try { CloseAction(); } catch { }
                        return;
                    }

                    nint RootAncestor = DllUtils.GetAncestor(ActiveWindowHandle, 2);
                    if (RootAncestor == Target.Tab.ParentWindow.Handle)
                    {
                        try { CloseAction(); } catch { }
                    }
                };
                PopupDismissTimer.Start();
            };
            
            PopupWindow.Activated += Window_Activated;
        }

        private void Window_Activated(object? sender, EventArgs e)
        {
            PopupDismissTimer?.Stop();
        }

        private void CEF_JavascriptMessageReceived(object? sender, JavascriptMessageReceivedEventArgs e)
        {
            if (CEFPopupBrowser == null)
                return;
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    dynamic Data = e.Message;
                    if (Data == null || CEFPopupBrowser == null)
                        return;
                    CEFPopupBrowser.Height = Data.height;
                    CEFPopupBrowser.Width = Data.width;
                }
                catch { }
            });
        }

        public void CloseAction()
        {
            if (PopupWindow == null)
                return;

            PopupWindow.Content = null;

            CEFPopupBrowser?.Dispose();
            CEFPopupBrowser = null;
            WebView2PopupBrowser?.Dispose();
            WebView2PopupBrowser = null;

            PopupDismissTimer?.Stop();
            PopupDismissTimer = null;

            PopupWindow.Activated -= Window_Activated;
            PopupWindow.Close();
        }

        //https://source.chromium.org/chromium/chromium/src/+/main:chrome/browser/ui/views/extensions/extension_popup.h?q=kMaxSize%20%3D%20%7B800,%20600%7D

        //WARNING: Avoid IWebView usage.
        private async Task<ChromiumWebBrowser> BuildCEF(Extension _Extension, Browser _Browser)
        {
            if (!WebViewManager.IsCefInitialized)
                await WebViewManager.InitializeCEF();
            ChromiumWebBrowser View = new("about:blank")
            {
                SnapsToDevicePixels = true,
                AllowDrop = true,
                IsManipulationEnabled = true,
                MinWidth = 25,
                MinHeight = 25,
                MaxWidth = 800,
                MaxHeight = 600,
            };

            View.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
            View.JavascriptObjectRepository.Settings.LegacyBindingEnabled = false;
            View.JavascriptObjectRepository.Register("slbrExtensionContext", new CefExtensionContextProvider(_Browser.WebView), BindingOptions.DefaultBinder);

            View.IsBrowserInitializedChanged += async (_, _) =>
            {
                if (View.IsBrowserInitialized)
                {
                    await View.GetDevToolsClient().Page.AddScriptToEvaluateOnNewDocumentAsync(Scripts.ExtensionPolyfillScript, null, null, true);
                    await View.GetDevToolsClient().Page.EnableAsync();
                    View.Load(_Extension.ActionPopup);
                }
            };
            View.LoadingStateChanged += (_, _) =>
            {
                View.ExecuteScriptAsync(Scripts.ExtensionPopupScript);
            };
            View.JavascriptMessageReceived += CEF_JavascriptMessageReceived;
            return View;
        }

        private async Task<WebView2> BuildWebView2(Extension _Extension, Browser _Browser)
        {
            if (!WebViewManager.IsWebView2Initialized)
                await WebViewManager.InitializeWebView2();
            WebView2 View = new()
            {
                SnapsToDevicePixels = true,
                AllowDrop = true,
                IsManipulationEnabled = true,
                MinWidth = 25,
                MinHeight = 25,
                MaxWidth = 800,
                MaxHeight = 600,
            };

            View.WebMessageReceived += WebView2_WebMessageReceived;
            View.CoreWebView2InitializationCompleted += async (_, _) =>
            {
                try
                {
                    View.CoreWebView2.AddHostObjectToScript("slbrExtensionContext", new WebView2ExtensionContextProvider(_Browser.WebView));

                    await View.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(Scripts.WebView2DocumentCreatedScript);

                    string BridgeAdapter = @"engine.bindObjectAsync = function(name) { return Promise.resolve(true); };
window.slbrExtensionContext = {
    getActiveTab: async function() {
        const raw = await window.chrome.webview.hostObjects.slbrExtensionContext.GetActiveTab();
        return JSON.parse(raw);
    }
};";
                    await View.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BridgeAdapter + Scripts.ExtensionPolyfillScript);
                    await View.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(Scripts.ExtensionPopupScript);
                    View.CoreWebView2.Navigate(_Extension.ActionPopup);
                }
                catch { }
            };
            View.EnsureCoreWebView2Async(WebViewManager.WebView2Environment, WebViewManager.WebView2ControllerOptions);
            return View;
        }

        private void WebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (WebView2PopupBrowser == null)
                return;
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    using JsonDocument Document = JsonDocument.Parse(e.WebMessageAsJson);
                    WebView2PopupBrowser.Height = Document.RootElement.GetProperty("height").GetInt32();
                    WebView2PopupBrowser.Width = Document.RootElement.GetProperty("width").GetInt32();
                }
                catch { }
            });
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case DllUtils.WM_SYSCOMMAND:
                    int Command = wParam.ToInt32() & 0xFFF0;
                    if (Command == DllUtils.SC_MOVE)
                        handled = true;
                    break;
            }
            return IntPtr.Zero;
        }
        #endregion
    }
}
