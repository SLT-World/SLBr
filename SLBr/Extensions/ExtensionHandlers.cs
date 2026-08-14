/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using CefSharp;
using CefSharp.Wpf.HwndHost;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SLBr.Pages;
using SLBr.Protobuf;
using SLBr.WebView;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
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

        private string? _AlternateID;
        public string? AlternateID
        {
            get => _AlternateID;
            set
            {
                _AlternateID = value;
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

        private string? _ActionIcon;
        public string? ActionIcon
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

        public string LocalPath { get; set; }
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
            if (string.IsNullOrEmpty(WebViewManager.CEFExtensionsPath))
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
                            if (KeyValue.Value is not IDictionary<string, object> ExtensionData)
                                continue;
                            if (ExtensionData.TryGetValue("creation_flags", out var CreationProperty) && CreationProperty is int Creation && Creation == 1)
                                continue;
                            if (ExtensionData.TryGetValue("path", out var ExtensionPathProperty) && ExtensionPathProperty is string ExtensionPath)
                            {
                                string? AlternateID = null;
                                if (!Path.Exists(ExtensionPath))
                                {
                                    string ProfilePath = Path.Combine(WebViewManager.CEFExtensionsPath, ExtensionPath);
                                    ExtensionPath = ProfilePath;
                                    if (!File.Exists(Path.Combine(ProfilePath, "manifest.json")))
                                        ExtensionPath = Directory.GetDirectories(ProfilePath).FirstOrDefault(i => File.Exists(Path.Combine(i, "manifest.json"))) ?? ProfilePath;
                                }
                                else
                                {
                                    string FolderName = Path.GetFileName(ExtensionPath);
                                    if (FolderName != KeyValue.Key)
                                        AlternateID = FolderName;
                                }
                                Extension _Extension = new()
                                {
                                    EngineType = WebEngineType.Chromium,
                                    ID = KeyValue.Key,
                                    IsEnabled = null,
                                    LocalPath = ExtensionPath,
                                    AlternateID = AlternateID
                                };
                                /*TODO: Implement CefPreferenceManager.AddPreferenceObserver.
                                 * https://github.com/cefsharp/CefSharp/pull/5279
                                 * 
                                 * https://github.com/chromiumembedded/cef/blob/master/include/cef_preference.h
                                 * https://github.com/cefsharp/CefSharp/blob/master/CefSharp.Core.Runtime/RequestContext.cpp
                                 * IsEnabled is to be determined via accessing "disable_reasons" preference.
                                 * AddPreferenceObserver is required in the detection of any modifications to this preference.
                                 * "disable_reasons" is an array instance.
                                 * Integers are contained within when extension is disabled.
                                 * https://source.chromium.org/chromium/chromium/src/+/main:extensions/browser/disable_reason.h
                                 * 
                                 * In addition, AddPreferenceObserver may aid in the detection of new or removed extensions, though further testing is required.
                                 */
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
                                                _Extension.ActionIcon = Path.Combine(_Extension.LocalPath, PickBestIcon(IconData));
                                            else
                                                _Extension.ActionIcon = Path.Combine(_Extension.LocalPath, IconProperty?.ToString());
                                        }
                                    }
                                }
                                else
                                    SetManifest(_Extension, Path.Combine(_Extension.LocalPath, "manifest.json"));
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

        private static string PickBestIcon(IDictionary<string, object> IconData)
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
            //UnpackExtension(File.ReadAllBytes(@"C:\Users\User\Downloads\darkreader-4.9.129.xpi"), WebViewManager.WebView2UnpackedExtensionsPath, false);
            //UnpackExtension(File.ReadAllBytes(@"C:\Users\User\Downloads\4d14030d-c174-4abf-a2b2-0c225ce50a38.crx"), WebViewManager.WebView2UnpackedExtensionsPath, true);
            List<Extension> Results = [];
            if (string.IsNullOrEmpty(WebViewManager.WebView2ExtensionsPath))
                return;
            await WebViewManager.CheckHeadlessWebView2Availability();
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
                    if (!Directory.Exists(ProfilePath) && !string.IsNullOrEmpty(WebViewManager.WebView2UnpackedExtensionsPath))
                    {
                        foreach (string UnpackedPath in Directory.GetDirectories(WebViewManager.WebView2UnpackedExtensionsPath))
                        {
                            if (_Extension.ID == GenerateUnpackedExtensionID(UnpackedPath))
                            {
                                ProfilePath = UnpackedPath;
                                string FolderName = Path.GetFileName(UnpackedPath);
                                if (FolderName != _Extension.ID)
                                    _Extension.AlternateID = FolderName;
                                break;
                            }
                        }
                    }
                    if (Directory.Exists(ProfilePath))
                    {
                        string ExtensionPath = ProfilePath;
                        if (!File.Exists(Path.Combine(ProfilePath, "manifest.json")))
                            ExtensionPath = Directory.GetDirectories(ProfilePath).FirstOrDefault(i => File.Exists(Path.Combine(i, "manifest.json"))) ?? ProfilePath;
                        _Extension.LocalPath = ExtensionPath;
                        SetManifest(_Extension, Path.Combine(ExtensionPath, "manifest.json"));
                        //TODO: Alternatively, procure manifest from "EBWebView\Default\Secure Preferences".
                    }
                    Results.Add(_Extension);
                }
            }
            catch { }
            WebView2Extensions.Clear();
            foreach (Extension _Extension in Results)
                WebView2Extensions.Add(_Extension);
        }

        private static void SetManifest(Extension _Extension, string ManifestPath)
        {
            try
            {
                if (File.Exists(ManifestPath))
                {
                    using JsonDocument Document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
                    JsonElement Manifest = Document.RootElement;
                    if (Manifest.TryGetProperty("version", out JsonElement VersionProperty))
                        _Extension.Version = VersionProperty.GetString();
                    if (Manifest.TryGetProperty("action", out JsonElement ExtensionAction))
                    {
                        if (ExtensionAction.TryGetProperty("default_popup", out JsonElement ExtensionPopup) && ExtensionPopup.GetString() is string PopupUrl)
                            _Extension.ActionPopup = $"chrome-extension://{_Extension.ID}/{PopupUrl}";
                        if (ExtensionAction.TryGetProperty("default_icon", out JsonElement IconProperty))
                        {
                            if (IconProperty.ValueKind == JsonValueKind.Object)
                            {
                                JsonProperty? FirstIcon = IconProperty.EnumerateObject().OrderByDescending(i => int.Parse(i.Name)).FirstOrDefault();
                                if (FirstIcon.HasValue && FirstIcon.Value.Value.GetString() is string IconUrl)
                                    _Extension.ActionIcon = Path.Combine(_Extension.LocalPath, IconUrl);
                            }
                            else if (IconProperty.ValueKind == JsonValueKind.String && IconProperty.GetString() is string IconUrl)
                                _Extension.ActionIcon = Path.Combine(_Extension.LocalPath, IconUrl);
                        }
                    }
                    Dictionary<string, string> MessageVariables = [];
                    if (Manifest.TryGetProperty("name", out JsonElement NameProperty) && NameProperty.GetString() is string Name)
                    {
                        if (Name.StartsWith("__MSG_"))
                            MessageVariables.Add("Name", Name[5..].Trim('_'));
                        else
                            _Extension.Name = Name;
                    }
                    if (Manifest.TryGetProperty("description", out JsonElement DescriptionProperty) && DescriptionProperty.GetString() is string Description)
                    {
                        if (Description.StartsWith("__MSG_"))
                            MessageVariables.Add("Description", Description[5..].Trim('_'));
                        else
                            _Extension.Description = Description;
                    }

                    if (MessageVariables.Count != 0)
                    {
                        string _Locale = "en";
                        string[] LocalesDirectory = Directory.GetDirectories(Path.Combine(_Extension.LocalPath, "_locales"));
                        foreach (string LocaleDirectory in LocalesDirectory)
                        {
                            string CompareLocale = App.Instance.Locale.Name.Replace("-", "_");
                            if (Path.GetFileName(LocaleDirectory) == CompareLocale)
                            {
                                _Locale = CompareLocale;
                                break;
                            }
                        }
                        string LocaleMessagesPath = Path.Combine(_Extension.LocalPath, "_locales", _Locale, "messages.json");
                        if (File.Exists(LocaleMessagesPath))
                        {
                            using JsonDocument MessagesDocument = JsonDocument.Parse(File.ReadAllText(LocaleMessagesPath));
                            JsonElement Messages = MessagesDocument.RootElement;
                            foreach (KeyValuePair<string, string> KVP in MessageVariables)
                            {
                                switch (KVP.Key)
                                {
                                    case "Description":
                                        _Extension.Description = Messages.GetProperty(KVP.Value).GetProperty("message").ToString();
                                        break;
                                    case "Name":
                                        _Extension.Name = Messages.GetProperty(KVP.Value).GetProperty("message").ToString();
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public async Task ToggleExtension(Extension _Extension, bool Enable)
        {
            try
            {
                if (_Extension.IsEnabled != Enable)
                    _Extension.IsEnabled = Enable;
                else
                {
                    if (_Extension.EngineType == WebEngineType.ChromiumEdge)
                    {
                        await WebViewManager.CheckHeadlessWebView2Availability();
                        CoreWebView2Profile Profile = WebViewManager.HeadlessEdgeCore.Profile;
                        IReadOnlyList<CoreWebView2BrowserExtension> WebView2InternalExtensions = await Profile.GetBrowserExtensionsAsync();
                        await WebView2InternalExtensions.FirstOrDefault(i => i.Id == _Extension.ID)?.EnableAsync(Enable);
                    }
                }
            }
            catch { }
        }

        public async Task<bool> UninstallExtension(Extension _Extension)
        {
            try
            {
                if (_Extension.EngineType == WebEngineType.ChromiumEdge)
                {
                    await WebViewManager.CheckHeadlessWebView2Availability();
                    CoreWebView2Profile Profile = WebViewManager.HeadlessEdgeCore.Profile;
                    IReadOnlyList<CoreWebView2BrowserExtension> WebView2InternalExtensions = await Profile.GetBrowserExtensionsAsync();
                    CoreWebView2BrowserExtension? WebView2Extension = WebView2InternalExtensions.FirstOrDefault(i => i.Id == _Extension.ID);
                    if (WebView2Extension != null)
                    {
                        try
                        {
                            await WebView2Extension.RemoveAsync();
                        }
                        catch { }
                    }
                    if (Directory.Exists(_Extension.LocalPath))
                        Directory.Delete(_Extension.LocalPath, true);
                    WebView2Extensions.Remove(_Extension);
                    return true;
                }
                else if (_Extension.EngineType == WebEngineType.Chromium)
                {
                    if (Directory.Exists(_Extension.LocalPath))
                        Directory.Delete(_Extension.LocalPath, true);
                    CEFExtensions.Remove(_Extension);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public async Task<(bool, string?)> InstallExtension(byte[] Bytes, WebEngineType Engine)
        {
            string? ExtensionsPath = Engine switch
            {
                WebEngineType.Chromium => WebViewManager.CEFUnpackedExtensionsPath,
                _ => WebViewManager.WebView2UnpackedExtensionsPath,
            };

            if (string.IsNullOrEmpty(ExtensionsPath))
                return (false, null);

            string? FinalPath = UnpackExtension(Bytes, ExtensionsPath);
            if (string.IsNullOrEmpty(FinalPath))
                return (false, "Failed to unpack extension.");
            string ManifestPath = Path.Combine(FinalPath, "manifest.json");
            if (File.Exists(ManifestPath))
            {
                using JsonDocument Document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
                if (Document.RootElement.TryGetProperty("manifest_version", out JsonElement ManifestVersionProperty) && ManifestVersionProperty.GetInt32() is int ManifestVersion)
                {
                    if (ManifestVersion == 3)
                    {
                        if (Engine == WebEngineType.ChromiumEdge)
                            await WebViewManager.InstallWebView2Extensions();
                        //TODO: Locate workaround for programmatical loading of unpacked CEF extensions at runtime.
                        return (true, null);
                    }
                    else
                    {
                        if (Directory.Exists(FinalPath))
                            Directory.Delete(FinalPath, true);
                        return (false, $"Manifest version {ManifestVersion} is not supported by SLBr.");
                    }
                }
                if (Directory.Exists(FinalPath))
                    Directory.Delete(FinalPath, true);
                return (false, "Manifest file is invalid.");
            }
            else
            {
                if (Directory.Exists(FinalPath))
                    Directory.Delete(FinalPath, true);
                return (false, "Manifest file is missing or unreachable.");
            }
        }

        private static string? UnpackExtension(byte[] Bytes, string ExtensionsPath)
        {
            try
            {
                string? TargetFolderName = null;

                using MemoryStream _Stream = new(Bytes);
                using BinaryReader Reader = new(_Stream);
                if (new string(Reader.ReadChars(4)) == "Cr24")
                {
                    int Version = Reader.ReadInt32();
                    byte[] PublicKeyBytes = null;

                    if (Version == 2)
                    {
                        int PublishKeyLength = Reader.ReadInt32();
                        int SignatureLength = Reader.ReadInt32();
                        PublicKeyBytes = Reader.ReadBytes(PublishKeyLength);
                        Reader.ReadBytes(SignatureLength);
                    }
                    else if (Version == 3)
                    {
                        int HeaderLength = Reader.ReadInt32();
                        byte[] headerBytes = Reader.ReadBytes(HeaderLength);
                        PublicKeyBytes = ExtractCRX3PublicKey(headerBytes);
                    }

                    if (PublicKeyBytes != null)
                        TargetFolderName = GenerateExtensionID(PublicKeyBytes);

                    if (string.IsNullOrEmpty(TargetFolderName))
                        TargetFolderName = "unpacked_crx_" + Guid.NewGuid().ToString("N");
                    else
                        TargetFolderName = Utils.SanitizeFileName(TargetFolderName);

                    long ZipLength = _Stream.Length - _Stream.Position;
                    byte[] ZipBytes = Reader.ReadBytes((int)ZipLength);

                    string FinalPath = Path.Combine(ExtensionsPath, TargetFolderName);

                    using MemoryStream ZipStream = new(ZipBytes);
                    using ZipArchive Archive = new(ZipStream);
                    Archive.ExtractToDirectory(FinalPath);
                    return FinalPath;
                }
                else
                {
                    string TemporaryDirectory = Path.Combine(ExtensionsPath, "unpacked_temp_" + Guid.NewGuid().ToString("N"));

                    using ZipArchive Archive = new(_Stream);
                    Archive.ExtractToDirectory(TemporaryDirectory, overwriteFiles: true);

                    TargetFolderName = GetXPIAddonId(TemporaryDirectory);

                    if (string.IsNullOrEmpty(TargetFolderName))
                        TargetFolderName = "unpacked_xpi_" + Guid.NewGuid().ToString("N");
                    else
                        TargetFolderName = Utils.SanitizeFileName(TargetFolderName);

                    string FinalPath = Path.Combine(ExtensionsPath, TargetFolderName);

                    if (Directory.Exists(FinalPath))
                        Directory.Delete(FinalPath, true);

                    Directory.Move(TemporaryDirectory, FinalPath);
                    return FinalPath;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string? GetXPIAddonId(string ExtractedDirectory)
        {
            string RecommendationPath = Path.Combine(ExtractedDirectory, "mozilla-recommendation.json");
            if (File.Exists(RecommendationPath))
            {
                try
                {
                    using JsonDocument Document = JsonDocument.Parse(File.ReadAllText(RecommendationPath));
                    if (Document.RootElement.TryGetProperty("addon_id", out JsonElement AddonIDProperty) && AddonIDProperty.GetString() is string AddonID)
                        return AddonID;
                }
                catch { }
            }
            string ManifestPath = Path.Combine(ExtractedDirectory, "manifest.json");
            if (File.Exists(ManifestPath))
            {
                try
                {
                    using JsonDocument Document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
                    if (Document.RootElement.TryGetProperty("browser_specific_settings", out JsonElement SettingsProperty) && SettingsProperty.TryGetProperty("gecko", out JsonElement GeckoProperty) && GeckoProperty.TryGetProperty("id", out JsonElement IDProperty) && IDProperty.GetString() is string ID)
                        return ID;
                    if (Document.RootElement.TryGetProperty("applications", out JsonElement AppsProperty) && AppsProperty.TryGetProperty("gecko", out JsonElement LegacyGeckoProperty) && LegacyGeckoProperty.TryGetProperty("id", out JsonElement LegacyIDProperty) && LegacyIDProperty.GetString() is string LegacyID)
                        return LegacyID;
                }
                catch { }
            }

            return null;
        }

        public static string GenerateUnpackedExtensionID(string Directory) =>
            GenerateExtensionID(Encoding.Unicode.GetBytes(Path.GetFullPath(Directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

        private static string GenerateExtensionID(byte[] KeyBytes)
        {
            //https://source.chromium.org/chromium/chromium/src/+/main:components/crx_file/id_util.cc
            byte[] HashBytes = SHA256.HashData(KeyBytes);

            StringBuilder ID = new(32);
            for (int i = 0; i < 16; i++)
            {
                byte Byte = HashBytes[i];
                ID.Append((char)('a' + ((Byte >> 4) & 0x0F)));
                ID.Append((char)('a' + (Byte & 0x0F)));
            }
            return ID.ToString();
        }

        private static byte[] ExtractCRX3PublicKey(byte[] HeaderBytes)
        {
            //https://source.chromium.org/chromium/chromium/src/+/main:components/crx_file/crx3.proto
            ProtobufReader Reader = new(HeaderBytes);
            while (!Reader.IsConsumed)
            {
                if (!Reader.TryReadTag(out int FieldNumber, out int WireType))
                    break;

                if (FieldNumber == 2 && WireType == 2)
                {
                    ReadOnlySpan<byte> ProofBytes = Reader.ReadLengthDelimited();
                    ProtobufReader InnerReader = new(ProofBytes);
                    while (!InnerReader.IsConsumed)
                    {
                        if (!InnerReader.TryReadTag(out int InnerFieldNumber, out int InnerWireType))
                            break;
                        if (InnerFieldNumber == 1 && InnerWireType == 2)
                            return InnerReader.ReadLengthDelimited().ToArray();
                        else
                            InnerReader.SkipField(InnerWireType);
                    }
                }
                else
                    Reader.SkipField(WireType);
            }

            return null;
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
