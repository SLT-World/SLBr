using CefSharp;
using CefSharp.DevTools;
using CefSharp.Wpf.HwndHost;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SLBr.Handlers;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using static SLBr.ChromiumPermissionHandler;

namespace SLBr
{
    public enum WebEngineType { Chromium, ChromiumEdge, Trident }

    public enum WebScreenshotFormat
    {
        JPEG,
        PNG,
        //WebP //WebView2 does not support WebP, and no one wants WebP anyways
    }

    public enum WebDownloadState
    {
        InProgress,
        Paused,
        Completed,
        Canceled
    }

    public enum WebContextMenuMediaType
    {
        None = 0,
        Image = 1,
        Video = 2,
        Audio = 3,
        Canvas = 4,
        File = 5,
        Plugin = 6
    }

    [Flags]
    public enum WebContextMenuType
    {
        //None = 0,
        Page = 1,
        Link = 2,
        Media = 4,
        Selection = 8,
        Editable = 16
    }

    /*public enum WebViewBrowsingDataTypes
    {
        IndexedDb,
        LocalStorage,
        WebSQL,
        AllDomStorage,
        Cookies,
        Cache,
        DiskCache
    }*/

    public class NavigationErrorEventArgs : EventArgs
    {
        public NavigationErrorEventArgs(int _ErrorCode, string _ErrorText, string _Url)
        {
            ErrorCode = _ErrorCode;
            ErrorText = _ErrorText;
            Url = _Url;
        }

        public string Url { get; }
        public int ErrorCode { get; }
        public string ErrorText { get; }
    }

    public class WebContextMenuEventArgs : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string LinkText { get; set; }
        public string LinkUrl { get; set; }
        public string MisspelledWord { get; set; }
        public string SourceUrl { get; set; }
        public string FrameUrl { get; set; }
        public string SelectionText { get; set; }

        public bool IsEditable { get; set; }
        public bool SpellCheck { get; set; }
        public List<string> DictionarySuggestions { get; set; }
        public WebContextMenuMediaType MediaType { get; set; }
        public WebContextMenuType MenuType { get; set; }
    }

    public class WebDownloadItem
    {
        public string ID { get; set; }
        public string Url { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public long ReceivedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double Progress => TotalBytes > 0 ? (double)ReceivedBytes / TotalBytes : 0;
        public WebDownloadState State { get; set; }

        public Action? Pause { get; set; }
        public Action? Resume { get; set; }
        public Action? Cancel { get; set; }
    }

    public delegate Task<ProtocolResponse> ProtocolHandler(string Url, string Extra = "");

    public class ProtocolResponse
    {
        public int StatusCode { get; set; }
        public string MimeType { get; set; }
        public byte[] Data { get; set; }

        public static ProtocolResponse FromString(string Content, string _MimeType = "text/html", int _StatusCode = 200)
        {
            return new ProtocolResponse
            {
                MimeType = _MimeType,
                StatusCode = _StatusCode,
                Data = Encoding.UTF8.GetBytes(Content)
            };
        }

        public static ProtocolResponse FromBytes(byte[] Data, string _MimeType = "application/octet-stream", int _StatusCode = 200)
        {
            return new ProtocolResponse
            {
                MimeType = _MimeType,
                StatusCode = _StatusCode,
                Data = Data
            };
        }
    }

    public class BeforeNavigationEventArgs : EventArgs
    {
        public string Url { get; }
        public bool IsMainFrame { get; }
        public bool Cancel { get; set; }

        public BeforeNavigationEventArgs(string _Url, bool _IsMainFrame)
        {
            Url = _Url;
            IsMainFrame = _IsMainFrame;
        }
    }

    public enum ScriptDialogType
    {
        Alert = 0,
        Confirm = 1,
        Prompt = 2,
        BeforeUnload = 3
    }
    public class ScriptDialogEventArgs : EventArgs
    {
        public ScriptDialogType DialogType { get; }
        public string Url { get; }
        public string Text { get; }
        public string DefaultPrompt { get; }

        public bool Handled { get; set; }
        public bool IsReload { get; }
        public bool Result { get; set; }
        public string PromptResult { get; set; }

        public ScriptDialogEventArgs(ScriptDialogType _DialogType, string _Url, string _Text, string _DefaultPrompt, bool _IsReload = false)
        {
            DialogType = _DialogType;
            Url = _Url;
            Text = _Text;
            DefaultPrompt = _DefaultPrompt;
            IsReload = _IsReload;
        }
    }

    public enum ResourceRequestType
    {
        MainFrame = 0,
        SubFrame = 1,

        Stylesheet = 2,
        Script = 3,
        Image = 4,

        Font = 5,
        SubResource = 6,
        Object = 7,
        Media = 8,
        Worker = 9,
        SharedWorker = 10,
        Prefetch = 11,
        Favicon = 12,
        XMLHTTPRequest = 13,
        Ping = 14,
        ServiceWorker = 15,
        CSPReport = 16,
        PluginResource = 17,
        NavigationPreLoadMainFrame = 19,
        NavigationPreLoadSubFrame = 20
    }

    public struct FindResult
    {
        public int ActiveMatch { get; } = 0;
        public int MatchCount { get; } = 0;
        public FindResult(int _ActiveMatch, int _MatchCount)
        {
            ActiveMatch = _ActiveMatch;
            MatchCount = _MatchCount;
        }
    }

    public struct LoadingStateResult
    {
        public bool IsLoading { get; }
        public int? HttpStatusCode { get; }
        public LoadingStateResult(bool _IsLoading, int? _HttpStatusCode)
        {
            IsLoading = _IsLoading;
            HttpStatusCode = _HttpStatusCode;
        }
    }

    public struct ResourceLoadedResult
    {
        public string Url { get; }
        public bool Success { get; }
        public long ReceivedContentLength { get; }
        public ResourceRequestType ResourceRequestType { get; }
        public ResourceLoadedResult(string _Url, bool _Success, long _ReceivedContentLength, ResourceRequestType _RequestType)
        {
            Url = _Url;
            Success = _Success;
            ReceivedContentLength = _ReceivedContentLength;
            ResourceRequestType = _RequestType;
        }
    }

    public struct ResourceRespondedResult
    {
        public string Url { get; }
        public ResourceRequestType ResourceRequestType { get; }
        public ResourceRespondedResult(string _Url, ResourceRequestType _RequestType)
        {
            Url = _Url;
            ResourceRequestType = _RequestType;
        }
    }

    public class ResourceRequestEventArgs : EventArgs
    {
        public string Url { get; }
        public string FocusedUrl { get; }
        public string Method { get; }
        public ResourceRequestType ResourceRequestType { get; }
        public IDictionary<string, string> ModifiedHeaders { get; set; }

        public bool Cancel { get; set; }

        public ResourceRequestEventArgs(string _Url, string _FocusedUrl, string _Method, ResourceRequestType _RequestType, IDictionary<string, string> _Headers)
        {
            Url = _Url;
            FocusedUrl = _FocusedUrl;
            Method = _Method;
            ModifiedHeaders = _Headers;
            ResourceRequestType = _RequestType;
        }
    }

    public class WebAuthenticationRequestedEventArgs : EventArgs
    {
        public string Url { get; }

        public string? Username { get; set; }
        public string? Password { get; set; }

        public bool Cancel { get; set; }

        public WebAuthenticationRequestedEventArgs(string _Url)
        {
            Url = _Url;
        }
    }

    public class ExternalProtocolEventArgs : EventArgs
    {
        public string Url { get; }
        public string Origin { get; }
        public bool Launch { get; set; }

        public ExternalProtocolEventArgs(string _Url, string _Origin)
        {
            Url = _Url;
            Origin = _Origin;
        }
    }

    [Flags] public enum WebPermissionKind
    {
        None = 0,
        ArSession = 1 << 0,
        CameraPanTiltZoom = 1 << 1,
        CameraStream = 1 << 2,
        CapturedSurfaceControl = 1 << 3,
        Clipboard = 1 << 4,
        TopLevelStorageAccess = 1 << 5,
        DiskQuota = 1 << 6,
        LocalFonts = 1 << 7,
        Geolocation = 1 << 8,
        HandTracking = 1 << 9,
        IdentityProvider = 1 << 10,
        IdleDetection = 1 << 11,
        MicStream = 1 << 12,
        MidiSysex = 1 << 13,
        MultipleDownloads = 1 << 14,
        Notifications = 1 << 15,
        KeyboardLock = 1 << 16,
        PointerLock = 1 << 17,
        ProtectedMediaIdentifier = 1 << 18,
        RegisterProtocolHandler = 1 << 19,
        StorageAccess = 1 << 20,
        VrSession = 1 << 21,
        WebAppInstallation = 1 << 22,
        WindowManagement = 1 << 23,
        FileSystemAccess = 1 << 24,
        LocalNetworkAccess = 1 << 25,
        RecordAudio = 1 << 26,
        ScreenShare = 1 << 27,
    }

    public enum WebPermissionState
    {
        Default = 0,
        Allow = 1,
        Deny = 2,
    }

    public static class WebViewUtils
    {
        public static PermissionRequestResult ToCefPermissionState(this WebPermissionState State)
        {
            return State switch
            {
                WebPermissionState.Deny => PermissionRequestResult.Deny,
                WebPermissionState.Allow => PermissionRequestResult.Accept,
                _ => PermissionRequestResult.Ignore
            };
        }
        public static CefSharp.DevTools.Page.CaptureScreenshotFormat ToCefScreenshotFormat(this WebScreenshotFormat State)
        {
            return State switch
            {
                WebScreenshotFormat.PNG => CefSharp.DevTools.Page.CaptureScreenshotFormat.Png,
                WebScreenshotFormat.JPEG => CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg,
                _ => CefSharp.DevTools.Page.CaptureScreenshotFormat.Png
            };
        }
        public static CoreWebView2CapturePreviewImageFormat ToWebView2ScreenshotFormat(this WebScreenshotFormat State)
        {
            return State switch
            {
                WebScreenshotFormat.PNG => CoreWebView2CapturePreviewImageFormat.Png,
                WebScreenshotFormat.JPEG => CoreWebView2CapturePreviewImageFormat.Jpeg,
                _ => CoreWebView2CapturePreviewImageFormat.Png
            };
        }
        public static CoreWebView2PermissionState ToWebView2PermissionState(this WebPermissionState State)
        {
            return State switch
            {
                WebPermissionState.Deny => CoreWebView2PermissionState.Deny,
                WebPermissionState.Allow => CoreWebView2PermissionState.Allow,
                _ => CoreWebView2PermissionState.Deny
                //_ => CoreWebView2PermissionState.Default
            };
        }
        public static WebContextMenuMediaType ToWebContextMenuMediaType(this ContextMenuMediaType Type)
        {
            return Type switch
            {
                ContextMenuMediaType.Audio => WebContextMenuMediaType.Audio,
                ContextMenuMediaType.Video => WebContextMenuMediaType.Video,
                ContextMenuMediaType.Plugin => WebContextMenuMediaType.Plugin,
                ContextMenuMediaType.Canvas => WebContextMenuMediaType.Canvas,
                ContextMenuMediaType.Image => WebContextMenuMediaType.Image,
                ContextMenuMediaType.File => WebContextMenuMediaType.File,
                _ => WebContextMenuMediaType.None
            };
        }
        /*public static WebContextMenuType ToWebContextMenuType(this ContextMenuType Type)
        {
            return Type switch
            {
                ContextMenuType.None => WebContextMenuType.None,
                ContextMenuType.Page => WebContextMenuType.Page,
                ContextMenuType.Media => WebContextMenuType.Media,
                ContextMenuType.Frame => WebContextMenuType.Frame,
                ContextMenuType.Link => WebContextMenuType.Link,
                ContextMenuType.Selection => WebContextMenuType.Selection,
                ContextMenuType.Editable => WebContextMenuType.Editable,
                _ => WebContextMenuType.None
            };
        }*/
        public static WebContextMenuType ToWebContextMenuType(this ContextMenuType Flags)
        {
            WebContextMenuType Type = 0;
            if (Flags.HasFlag(ContextMenuType.Page)) Type |= WebContextMenuType.Page;
            if (Flags.HasFlag(ContextMenuType.Link)) Type |= WebContextMenuType.Link;
            //if (Flags.HasFlag(ContextMenuType.Frame)) Type |= WebContextMenuType.Frame; It just says frame for literally everything
            if (Flags.HasFlag(ContextMenuType.Media)) Type |= WebContextMenuType.Media;
            if (Flags.HasFlag(ContextMenuType.Selection)) Type |= WebContextMenuType.Selection;
            if (Flags.HasFlag(ContextMenuType.Editable)) Type |= WebContextMenuType.Editable;
            return Type;
        }
        public static WebContextMenuMediaType ToWebContextMenuMediaType(this CoreWebView2ContextMenuTargetKind Type)
        {
            return Type switch
            {
                CoreWebView2ContextMenuTargetKind.Audio => WebContextMenuMediaType.Audio,
                CoreWebView2ContextMenuTargetKind.Video => WebContextMenuMediaType.Video,
                CoreWebView2ContextMenuTargetKind.Image => WebContextMenuMediaType.Image,
                _ => WebContextMenuMediaType.None
            };
        }
        public static ResourceRequestType ToResourceRequestType(this ResourceType Type)
        {
            return Type switch
            {
                ResourceType.MainFrame => ResourceRequestType.MainFrame,
                ResourceType.SubFrame => ResourceRequestType.SubFrame,
                ResourceType.Stylesheet => ResourceRequestType.Stylesheet,
                ResourceType.Script => ResourceRequestType.Script,
                ResourceType.Image => ResourceRequestType.Image,
                ResourceType.FontResource => ResourceRequestType.Font,
                ResourceType.SubResource => ResourceRequestType.SubResource,
                ResourceType.Object => ResourceRequestType.Object,
                ResourceType.Media => ResourceRequestType.Media,
                ResourceType.Worker => ResourceRequestType.Worker,
                ResourceType.SharedWorker => ResourceRequestType.SharedWorker,
                ResourceType.Prefetch => ResourceRequestType.Prefetch,
                ResourceType.Favicon => ResourceRequestType.Favicon,
                ResourceType.Xhr => ResourceRequestType.XMLHTTPRequest,
                ResourceType.Ping => ResourceRequestType.Ping,
                ResourceType.ServiceWorker => ResourceRequestType.ServiceWorker,
                ResourceType.CspReport => ResourceRequestType.CSPReport,
                ResourceType.PluginResource => ResourceRequestType.PluginResource,
                ResourceType.NavigationPreLoadMainFrame => ResourceRequestType.NavigationPreLoadMainFrame,
                ResourceType.NavigationPreLoadSubFrame => ResourceRequestType.NavigationPreLoadSubFrame,
            };
        }
        /*public static WebContextMenuType ToWebContextMenuType(this CoreWebView2ContextMenuTargetKind Type)
        {
            switch (Type)
            {
                case CoreWebView2ContextMenuTargetKind.Page:
                    return WebContextMenuType.Page;
                case CoreWebView2ContextMenuTargetKind.SelectedText:
                    return WebContextMenuType.Selection;
                case CoreWebView2ContextMenuTargetKind.Image:
                case CoreWebView2ContextMenuTargetKind.Audio:
                case CoreWebView2ContextMenuTargetKind.Video:
                    return WebContextMenuType.Media;
                default:
                    return WebContextMenuType.None;
            };
        }*/
        public static WebContextMenuType MapWebContextMenuTarget(this CoreWebView2ContextMenuTarget Target)
        {
            WebContextMenuType Flags = 0;

            switch (Target.Kind)
            {
                case CoreWebView2ContextMenuTargetKind.Page:
                    Flags |= WebContextMenuType.Page;
                    break;
                case CoreWebView2ContextMenuTargetKind.SelectedText:
                    Flags |= WebContextMenuType.Selection;
                    break;
                case CoreWebView2ContextMenuTargetKind.Image:
                case CoreWebView2ContextMenuTargetKind.Audio:
                case CoreWebView2ContextMenuTargetKind.Video:
                    Flags |= WebContextMenuType.Media;
                    break;
            }
            try
            {
                if (!string.IsNullOrEmpty(Target.LinkUri))
                    Flags |= WebContextMenuType.Link;
            }
            catch { }
            if (Target.IsEditable)
                Flags |= WebContextMenuType.Editable;
            return Flags;
        }
        /*public static CoreWebView2PermissionKind ToWebView2Permission(this WebPermissionKind Kind)
        {
            switch (Kind)
            {
                case WebPermissionKind.MicStream:
                    return CoreWebView2PermissionKind.Microphone;
                case WebPermissionKind.CameraStream:
                    return CoreWebView2PermissionKind.Camera;
                case WebPermissionKind.Geolocation:
                    return CoreWebView2PermissionKind.Geolocation;
                case WebPermissionKind.Notifications:
                    return CoreWebView2PermissionKind.Notifications;
                case WebPermissionKind.Clipboard:
                    return CoreWebView2PermissionKind.ClipboardRead;
                case WebPermissionKind.MultipleDownloads:
                    return CoreWebView2PermissionKind.MultipleAutomaticDownloads;
                case WebPermissionKind.FileSystemAccess:
                    return CoreWebView2PermissionKind.FileReadWrite;
                case WebPermissionKind.LocalFonts:
                    return CoreWebView2PermissionKind.LocalFonts;
                case WebPermissionKind.MidiSysex:
                    return CoreWebView2PermissionKind.MidiSystemExclusiveMessages;
                case WebPermissionKind.WindowManagement:
                    return CoreWebView2PermissionKind.WindowManagement;
                //case WebPermissionKind.Autop:
                //    return CoreWebView2PermissionKind.Autoplay;
                //case WebPermissionKind.:
                //    return CoreWebView2PermissionKind.OtherSensors;
                default:
                    return CoreWebView2PermissionKind.UnknownPermission;
            }
        }*/
        public static WebPermissionKind ToWebPermission(this MediaAccessPermissionType Kind)
        {
            WebPermissionKind Flags = WebPermissionKind.None;
            if (Kind.HasFlag(MediaAccessPermissionType.AudioCapture))
                Flags |= WebPermissionKind.MicStream;
            if (Kind.HasFlag(MediaAccessPermissionType.VideoCapture))
                Flags |= WebPermissionKind.CameraStream;
            if (Kind.HasFlag(MediaAccessPermissionType.DesktopAudioCapture))
                Flags |= WebPermissionKind.RecordAudio;
            if (Kind.HasFlag(MediaAccessPermissionType.DesktopVideoCapture))
                Flags |= WebPermissionKind.ScreenShare;
            return Flags;
        }
        public static WebPermissionKind ToWebPermission(this FixedPermissionRequestType Kind)
        {
            WebPermissionKind Flags = WebPermissionKind.None;

            if (Kind.HasFlag(FixedPermissionRequestType.ArSession))
                Flags |= WebPermissionKind.ArSession;

            if (Kind.HasFlag(FixedPermissionRequestType.CameraPanTiltZoom))
                Flags |= WebPermissionKind.CameraPanTiltZoom;

            if (Kind.HasFlag(FixedPermissionRequestType.CameraStream))
                Flags |= WebPermissionKind.CameraStream;

            if (Kind.HasFlag(FixedPermissionRequestType.CapturedSurfaceControl))
                Flags |= WebPermissionKind.CapturedSurfaceControl;

            if (Kind.HasFlag(FixedPermissionRequestType.Clipboard))
                Flags |= WebPermissionKind.Clipboard;

            if (Kind.HasFlag(FixedPermissionRequestType.TopLevelStorageAccess))
                Flags |= WebPermissionKind.TopLevelStorageAccess;

            if (Kind.HasFlag(FixedPermissionRequestType.DiskQuota))
                Flags |= WebPermissionKind.DiskQuota;

            if (Kind.HasFlag(FixedPermissionRequestType.LocalFonts))
                Flags |= WebPermissionKind.LocalFonts;

            if (Kind.HasFlag(FixedPermissionRequestType.Geolocation))
                Flags |= WebPermissionKind.Geolocation;

            if (Kind.HasFlag(FixedPermissionRequestType.IdentityProvider))
                Flags |= WebPermissionKind.IdentityProvider;

            if (Kind.HasFlag(FixedPermissionRequestType.IdleDetection))
                Flags |= WebPermissionKind.IdleDetection;

            if (Kind.HasFlag(FixedPermissionRequestType.MicStream))
                Flags |= WebPermissionKind.MicStream;

            if (Kind.HasFlag(FixedPermissionRequestType.MidiSysex))
                Flags |= WebPermissionKind.MidiSysex;

            if (Kind.HasFlag(FixedPermissionRequestType.MultipleDownloads))
                Flags |= WebPermissionKind.MultipleDownloads;

            if (Kind.HasFlag(FixedPermissionRequestType.Notifications))
                Flags |= WebPermissionKind.Notifications;

            if (Kind.HasFlag(FixedPermissionRequestType.KeyboardLock))
                Flags |= WebPermissionKind.KeyboardLock;

            if (Kind.HasFlag(FixedPermissionRequestType.PointerLock))
                Flags |= WebPermissionKind.PointerLock;

            if (Kind.HasFlag(FixedPermissionRequestType.ProtectedMediaIdentifier))
                Flags |= WebPermissionKind.ProtectedMediaIdentifier;

            if (Kind.HasFlag(FixedPermissionRequestType.RegisterProtocolHandler))
                Flags |= WebPermissionKind.RegisterProtocolHandler;

            if (Kind.HasFlag(FixedPermissionRequestType.StorageAccess))
                Flags |= WebPermissionKind.StorageAccess;

            if (Kind.HasFlag(FixedPermissionRequestType.VrSession))
                Flags |= WebPermissionKind.VrSession;

            if (Kind.HasFlag(FixedPermissionRequestType.WebAppInstallation))
                Flags |= WebPermissionKind.WebAppInstallation;

            if (Kind.HasFlag(FixedPermissionRequestType.WindowManagement))
                Flags |= WebPermissionKind.WindowManagement;

            if (Kind.HasFlag(FixedPermissionRequestType.FileSystemAccess))
                Flags |= WebPermissionKind.FileSystemAccess;

            if (Kind.HasFlag(FixedPermissionRequestType.LocalNetworkAccess))
                Flags |= WebPermissionKind.LocalNetworkAccess;

            return Flags;
        }
        public static WebPermissionKind ToWebPermission(this CoreWebView2PermissionKind Kind)
        {
            WebPermissionKind Flags = 0;

            switch (Kind)
            {
                case CoreWebView2PermissionKind.Microphone:
                    Flags |= WebPermissionKind.MicStream;
                    break;
                case CoreWebView2PermissionKind.Camera:
                    Flags |= WebPermissionKind.CameraStream;
                    break;
                case CoreWebView2PermissionKind.Geolocation:
                    Flags |= WebPermissionKind.Geolocation;
                    break;
                case CoreWebView2PermissionKind.Notifications:
                    Flags |= WebPermissionKind.Notifications;
                    break;
                case CoreWebView2PermissionKind.ClipboardRead:
                    Flags |= WebPermissionKind.Clipboard;
                    break;
                case CoreWebView2PermissionKind.MultipleAutomaticDownloads:
                    Flags |= WebPermissionKind.MultipleDownloads;
                    break;
                case CoreWebView2PermissionKind.FileReadWrite:
                    Flags |= WebPermissionKind.FileSystemAccess;
                    break;
                case CoreWebView2PermissionKind.LocalFonts:
                    Flags |= WebPermissionKind.LocalFonts;
                    break;
                case CoreWebView2PermissionKind.MidiSystemExclusiveMessages:
                    Flags |= WebPermissionKind.MidiSysex;
                    break;
                case CoreWebView2PermissionKind.WindowManagement:
                    Flags |= WebPermissionKind.WindowManagement;
                    break;
                //case CoreWebView2PermissionKind.Autoplay:
                //    return WebPermissionKind.Autoplay;
                //case CoreWebView2PermissionKind.OtherSensors:
                //    return WebPermissionKind.OtherSensors;
                default:
                    return WebPermissionKind.None;
            }
            return Flags;
        }
        public static ResourceRequestType ToResourceRequestType(this CoreWebView2WebResourceContext Kind)
        {
            switch (Kind)
            {
                case CoreWebView2WebResourceContext.Document:
                    return ResourceRequestType.MainFrame;
                case CoreWebView2WebResourceContext.Stylesheet:
                    return ResourceRequestType.Stylesheet;
                case CoreWebView2WebResourceContext.Image:
                    return ResourceRequestType.Image;
                case CoreWebView2WebResourceContext.Media:
                    return ResourceRequestType.Media;
                case CoreWebView2WebResourceContext.Font:
                    return ResourceRequestType.Font;
                case CoreWebView2WebResourceContext.Script:
                    return ResourceRequestType.Script;
                case CoreWebView2WebResourceContext.XmlHttpRequest:
                    return ResourceRequestType.XMLHTTPRequest;
                case CoreWebView2WebResourceContext.Fetch:
                case CoreWebView2WebResourceContext.TextTrack:
                case CoreWebView2WebResourceContext.EventSource:
                case CoreWebView2WebResourceContext.Websocket:
                case CoreWebView2WebResourceContext.Manifest:
                case CoreWebView2WebResourceContext.SignedExchange:
                    return ResourceRequestType.SubResource;
                case CoreWebView2WebResourceContext.Ping:
                    return ResourceRequestType.Ping;
                case CoreWebView2WebResourceContext.CspViolationReport:
                    return ResourceRequestType.CSPReport;
                /*case CoreWebView2WebResourceContext.All:
                case CoreWebView2WebResourceContext.Other:*/
                default:
                    return ResourceRequestType.SubResource;
            }
        }
        public static ScriptDialogType ToScriptDialogType(this CoreWebView2ScriptDialogKind Kind)
        {
            return Kind switch
            {
                CoreWebView2ScriptDialogKind.Alert => ScriptDialogType.Alert,
                CoreWebView2ScriptDialogKind.Confirm => ScriptDialogType.Confirm,
                CoreWebView2ScriptDialogKind.Prompt => ScriptDialogType.Prompt,
                CoreWebView2ScriptDialogKind.Beforeunload => ScriptDialogType.BeforeUnload,
                _ => ScriptDialogType.Alert
            };
        }
    }

    public class PermissionRequestedEventArgs : EventArgs
    {
        public string Url { get; }
        public WebPermissionKind Kind { get; }
        public WebPermissionState State { get; set; } = WebPermissionState.Default;

        public PermissionRequestedEventArgs(string _Url, WebPermissionKind _Kind)
        {
            Url = _Url;
            Kind = _Kind;
        }
    }

    public class NewTabRequestEventArgs : EventArgs
    {
        public string Url { get; }
        public bool Background { get; }
        public Rect? Popup { get; }
        public IWebView? WebView { get; set; }

        public NewTabRequestEventArgs(string _Url, bool _Background, Rect? _Popup)
        {
            Url = _Url;
            Background = _Background;
            Popup = _Popup;
        }
    }

    public struct WebUserAgentBrand
    {
        public string Brand { get; set; }
        public string Version { get; set; }
    }

    public struct WebUserAgentMetaData
    {
        public IList<WebUserAgentBrand> Brands { get; set; }
        public string FullVersion { get; set; }
        public string Platform { get; set; }
        public string PlatformVersion { get; set; }
        public string Architecture { get; set; }
        public string Model { get; set; }
        public bool Mobile { get; set; }
    }

    public class WebViewBrowserSettings
    {
        public bool JavaScript = true;
        public bool JavaScriptMessage = true;
        public bool Private = false;
        public bool AudioListener = false;
    }

    public interface IWebView : IDisposable
    {
        string Address { get; set; }
        string Title { get; }
        WebEngineType Engine { get; }

        bool CanGoBack { get; }
        bool CanGoForward { get; }
        bool CanReload { get; }
        bool IsLoading { get; }
        bool IsBrowserInitialized { get; }

        bool IsSecure { get; }

        bool AudioPlaying { get; }
        bool IsMuted { get; set; }

        double ZoomFactor { get; set; }

        void Navigate(string Url);
        void Back();
        void Forward();
        void Refresh(bool IgnoreCache = false, bool ClearCache = false);
        void Stop();
        void Print();

        void Cut();
        void Copy();
        void Paste();
        void Delete();
        void SelectAll();
        void Undo();
        void Redo();

        void Find(string Text, bool Forward, bool MatchCase, bool FindNext);
        void StopFind();
        void SaveAs();

        event EventHandler AudioPlayingChanged;
        event EventHandler<bool> FullscreenChanged;
        event EventHandler<ScriptDialogEventArgs> ScriptDialogOpened;
        event EventHandler<BeforeNavigationEventArgs> BeforeNavigation;
        event EventHandler<NewTabRequestEventArgs> NewTabRequested;

        event EventHandler<string> FrameLoadStart;
        event EventHandler<string> FrameLoadEnd;

        event EventHandler IsBrowserInitializedChanged;

        event EventHandler<LoadingStateResult> LoadingStateChanged;
        event EventHandler<string> TitleChanged;
        event EventHandler<string> StatusMessage;
        event EventHandler<string> FaviconChanged;
        event EventHandler<FindResult> FindResult;
        event EventHandler<string> JavaScriptMessageReceived;

        event EventHandler<ResourceRequestEventArgs> ResourceRequested;
        //Consider switching to things like EventHandler<(string Url, ResourceRequestType ResourceRequestType)>
        event EventHandler<ResourceRespondedResult> ResourceResponded;
        event EventHandler<ResourceLoadedResult> ResourceLoaded;
        event EventHandler<PermissionRequestedEventArgs> PermissionRequested;

        /*event Action<WebDownloadItem> DownloadStarted;
        event Action<WebDownloadItem> DownloadUpdated;
        event Action<WebDownloadItem> DownloadCompleted;*/

        event EventHandler<WebContextMenuEventArgs> ContextMenuRequested;
        event EventHandler<WebAuthenticationRequestedEventArgs> AuthenticationRequested;
        event EventHandler<ExternalProtocolEventArgs> ExternalProtocolRequested;
        event EventHandler<NavigationErrorEventArgs> NavigationError;

        Task<byte[]> TakeScreenshotAsync(WebScreenshotFormat Format, Size? Viewport = null);
        Task<string> GetSourceAsync();

        void Download(string Url);

        bool CanExecuteJavascript { get; }
        void ExecuteScript(string Script);
        Task<string> EvaluateScriptAsync(string Script);
        Task<string> CallDevToolsAsync(string Method, object? Parameters = null);
        //Task ClearBrowsingDataAsync(WebViewBrowsingDataTypes DataType);

        public FrameworkElement Control { get; }
    }

    public class ChromiumWebView : IWebView, IDisposable, IRequestHandler, IDisplayHandler
    {
        private ChromiumWebBrowser Browser;
        public WebViewBrowserSettings Settings;
        private readonly List<string> InitialUrls;

        public ChromiumWebView(List<string> Urls = null, WebViewBrowserSettings _Settings = null)
        {
            InitialUrls = Urls ?? ["about:blank"];
            Settings = _Settings ?? new WebViewBrowserSettings();
            WebViewManager.WebViews.Add(this);

            if (!WebViewManager.IsCefInitialized)
                WebViewManager.InitializeCEF();
            Browser = new ChromiumWebBrowser();
            WebViewManager.ChromiumWebViews[Browser] = this;

            BrowserSettings _BrowserSettings = new BrowserSettings()
            {
                BackgroundColor = 0xFF000000,
                ChromeStatusBubble = CefState.Disabled,
                ChromeZoomBubble = CefState.Disabled,
                Javascript = Settings.JavaScript ? CefState.Default : CefState.Disabled,
                WebGl = WebViewManager.Settings.Performance == PerformancePreset.Low ? CefState.Disabled : CefState.Default
            };
            Browser.BrowserSettings = _BrowserSettings;

            if (Settings.Private)
            {
                _BrowserSettings.LocalStorage = CefState.Disabled;
                _BrowserSettings.Databases = CefState.Disabled;
                _BrowserSettings.JavascriptAccessClipboard = CefState.Disabled;
                _BrowserSettings.JavascriptDomPaste = CefState.Disabled;
                RequestContextSettings ContextSettings = new RequestContextSettings
                {
                    PersistSessionCookies = false,
                    CachePath = null
                };
                RequestContext PrivateRequestContext = new RequestContext(ContextSettings);
                /*IRequestContext _RequestContext = Browser.RequestContext;
                if (_RequestContext != null && !_RequestContext.IsGlobal)
                {*/
                foreach (var Scheme in WebViewManager.Settings.Schemes)
                    PrivateRequestContext.RegisterSchemeHandlerFactory(Scheme.Key, string.Empty, new ChromiumProtocolHandlerFactory(Scheme.Value));
                //}

                Browser.RequestContext = PrivateRequestContext;
            }

            Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
            Browser.FrameLoadStart += Browser_FrameLoadStart;
            Browser.FrameLoadEnd += Browser_FrameLoadEnd;
            Browser.LoadingStateChanged += Browser_LoadingStateChanged;
            Browser.TitleChanged += Browser_TitleChanged;
            Browser.StatusMessage += Browser_StatusMessage;
            Browser.LoadError += Browser_LoadError;
            Browser.JavascriptObjectRepository.Settings.JavascriptBindingApiEnabled = Settings.JavaScriptMessage;
            if (Settings.JavaScriptMessage || Settings.AudioListener)
            {
                Browser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = "engine";
                //Browser.JavascriptMessageReceived += (s, e) => JavaScriptMessageReceived.RaiseUIAsync(this, e.Message?.ToString() ?? "");
                Browser.JavascriptMessageReceived += Browser_JavascriptMessageReceived;
            }

            Browser.DisplayHandler = this;
            Browser.RequestHandler = this;
            Browser.LifeSpanHandler = WebViewManager.GlobalLifeSpanHandler;
            Browser.JsDialogHandler = WebViewManager.GlobalJsDialogHandler;
            Browser.KeyboardHandler = WebViewManager.GlobalKeyboardHandler;
            Browser.PermissionHandler = WebViewManager.GlobalPermissionHandler;
            Browser.DownloadHandler = WebViewManager.GlobalDownloadHandler;
            Browser.MenuHandler = WebViewManager.GlobalContextMenuHandler;
            Browser.FindHandler = WebViewManager.GlobalFindHandler;
            Browser.DialogHandler = WebViewManager.GlobalDialogHandler;
            Browser.ResourceRequestHandlerFactory = new OverrideResourceRequestHandlerFactory(this);
            ZoomFactor = 1;
        }

        private void Browser_JavascriptMessageReceived(object? sender, JavascriptMessageReceivedEventArgs e)
        {
            string Json;
            try
            {
                Json = JsonSerializer.Serialize(e.Message);
            }
            catch
            {
                Json = e.Message?.ToString() ?? string.Empty;
            }
            if (Settings.AudioListener && Json.EndsWith(@"""type"":""__cef_audio__""}"))
            {
                SetAudioPlaying(Json.StartsWith(@"{""playing"":1"));
                return;
            }
            JavaScriptMessageReceived.RaiseUIAsync(this, Json);
        }

        private void Browser_LoadError(object? sender, LoadErrorEventArgs e)
        {
            if (e.ErrorCode is CefErrorCode.Aborted or CefErrorCode.IoPending or CefErrorCode.BlockedByClient or CefErrorCode.BlockedByResponse)
                return;
            if (InitializingHistory) return;
            NavigationError?.RaiseUIAsync(this, new NavigationErrorEventArgs((int)e.ErrorCode, e.ErrorText, e.FailedUrl));
        }
        private void Browser_StatusMessage(object? sender, StatusMessageEventArgs e) => StatusMessage?.RaiseUIAsync(this, e.Value);
        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (InitializingHistory) return;
            TitleChanged?.RaiseUIAsync(this, Browser.Title);
        }
        private void Browser_FrameLoadEnd(object? sender, FrameLoadEndEventArgs e)
        {
            if (InitializingHistory) return;
            FrameLoadEnd?.RaiseUIAsync(this, e.Url);
        }
        bool InitializingHistory = false;
        private async void Browser_IsBrowserInitializedChanged(object? sender, EventArgs e)
        {
            Browser?.Dispatcher.BeginInvoke(() => IsBrowserInitializedChanged?.Invoke(this, EventArgs.Empty));

            for (int i = 0; i < InitialUrls.Count; i++)
            {
                string Url = InitialUrls[i];
                bool IsHistory = i < InitialUrls.Count - 1;
                if (IsHistory)
                    WebViewManager.RegisterOverrideRequest(Url, ResourceHandler.GetByteArray(App.HistoryPlaceholder, Encoding.UTF8), "text/html", 1);
                await CallDevToolsAsync("Page.navigate", new
                {
                    url = Url,
                    transitionType = "generated"
                });
                InitializingHistory = IsHistory;
                if (IsHistory)
                    await Task.Delay(TimeSpan.FromMilliseconds(5));
            }
            /*if (Settings.Private)
            {
                nint HWND = Browser.GetBrowserHost().GetWindowHandle();
                IntPtr ChildHWND = DllUtils.GetWindow(HWND, DllUtils.GetWindowCommand.GW_CHILD);
                DllUtils.SetWindowDisplayAffinity(ChildHWND, DllUtils.WindowDisplayAffinity.WDA_MONITOR);
            }*/
        }

        private void Browser_FrameLoadStart(object? sender, FrameLoadStartEventArgs e)
        {
            if (InitializingHistory) return;
            FrameLoadStart?.RaiseUIAsync(this, e.Url);
            if (Settings.AudioListener)
                ExecuteScript(Scripts.CefAudioScript);
        }

        private async void Browser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (InitializingHistory) return;
            CanGoBack = e.CanGoBack;
            CanGoForward = e.CanGoForward;
            CanReload = e.CanReload;
            IsLoading = e.IsLoading;
            NavigationEntry _NavigationEntry = await Browser.GetVisibleNavigationEntryAsync();
            IsSecure = _NavigationEntry != null ? _NavigationEntry.SslStatus.IsSecureConnection : Address.StartsWith("https:");
            LoadingStateChanged?.RaiseUIAsync(this, new LoadingStateResult(e.IsLoading, _NavigationEntry?.HttpStatusCode));
        }

        public WebEngineType Engine => WebEngineType.Chromium;
        public string Address
        {
            get => Browser?.Address ?? InitialUrls.Last();
            set => Navigate(value);
        }
        public string Title => Browser.Title;

        public bool CanGoBack { get; private set; }
        public bool CanGoForward { get; private set; }
        public bool CanReload { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsBrowserInitialized => Browser.IsBrowserInitialized;

        public bool IsSecure { get; private set; }
        public bool AudioPlaying { get; private set; }

        private bool _IsMuted;
        public bool IsMuted
        {
            get => _IsMuted;
            /*{
                //This causes a freeze when reloading on PDF
                return Cef.UIThreadTaskFactory.StartNew(() =>
                {
                    return Browser.GetBrowserHost()?.IsAudioMuted ?? false;
                }).Result;
            }*/
            set
            {
                _IsMuted = value;
                Application.Current.Dispatcher.BeginInvoke(() => Cef.UIThreadTaskFactory.StartNew(() => Browser.GetBrowserHost()?.SetAudioMuted(value)));
            }
        }

        public double ZoomFactor
        {
            get => Browser.ZoomLevel + 1;
            set { Browser.ZoomLevel = value - 1; }
        }

        public void Navigate(string Url) => Browser?.Dispatcher.Invoke(() => Browser.Load(Url));
        public void Back() { if (CanGoBack) Browser.Back(); }
        public void Forward() { if (CanGoForward) Browser.Forward(); }
        public void Refresh(bool IgnoreCache = false, bool ClearCache = false)
        {
            if (ClearCache)
            {
                using (DevToolsClient _DevToolsClient = Browser?.GetDevToolsClient())
                {
                    _DevToolsClient.Page.ClearCompilationCacheAsync();
                    _DevToolsClient.Network.ClearBrowserCacheAsync();
                }
            }
            Browser.Reload(IgnoreCache);
        }
        public void Stop() => Browser.Stop();
        public void Print() => Browser.Print();
        public void SetFindResult(int ActiveMatch, int MatchCount) => FindResult.RaiseUIAsync(this, new FindResult(ActiveMatch, MatchCount));
        public void Find(string Text, bool Forward, bool MatchCase, bool FindNext) => Browser.Find(Text, Forward, MatchCase, FindNext);
        public void StopFind() => Browser.StopFinding(true);
        public void SaveAs() => Browser.StartDownload(Address);
        /*{
            SaveFileDialog SaveDialog = new SaveFileDialog
            {
                Filter = "Webpage, Complete (*.htm;*.html)|*.htm;*.html|Webpage, Single File (*.mhtml)|*.mhtml",
                FileName = "page"
            };
        }*/

        public void Cut() => Browser.Cut();
        public void Copy() => Browser.Copy();
        public void Paste() => Browser.Paste();
        public void Delete() => Browser.Delete();
        public void SelectAll() => Browser.SelectAll();
        public void Undo() => Browser.Undo();
        public void Redo() => Browser.Redo();

        public void SetAudioPlaying(bool Playing)
        {
            AudioPlaying = Playing;
            AudioPlayingChanged?.RaiseUIAsync(this);
        }
        public event EventHandler AudioPlayingChanged;
        public event EventHandler<bool> FullscreenChanged;
        public event EventHandler<ScriptDialogEventArgs> ScriptDialogOpened;
        public void RaiseScriptDialog(ScriptDialogEventArgs e)
        {
            Browser?.Dispatcher.Invoke(() => ScriptDialogOpened?.Invoke(this, e));
        }
        public event EventHandler<BeforeNavigationEventArgs> BeforeNavigation;
        public event EventHandler<NewTabRequestEventArgs> NewTabRequested;
        public event EventHandler<string> FrameLoadStart;
        public event EventHandler<string> FrameLoadEnd;
        public event EventHandler IsBrowserInitializedChanged;
        public event EventHandler<LoadingStateResult> LoadingStateChanged;
        public event EventHandler<string> TitleChanged;
        public event EventHandler<string> StatusMessage;
        public event EventHandler<string> FaviconChanged;
        public event EventHandler<FindResult> FindResult;
        public event EventHandler<string> JavaScriptMessageReceived;
        public event EventHandler<ResourceRequestEventArgs> ResourceRequested;
        public event EventHandler<ResourceRespondedResult> ResourceResponded;
        public event EventHandler<ResourceLoadedResult> ResourceLoaded;
        public void RaiseResourceRequest(ResourceRequestEventArgs e)
        {
            if (InitializingHistory) return;
            Browser?.Dispatcher.Invoke(() => ResourceRequested?.Invoke(this, e));
        }
        public void RaiseResourceResponded(ResourceRespondedResult e)
        {
            if (InitializingHistory) return;
            ResourceResponded?.RaiseUIAsync(this, e);
        }
        public void RaiseResourceLoaded(ResourceLoadedResult Result)
        {
            if (InitializingHistory) return;
            ResourceLoaded?.RaiseUIAsync(this, Result);
        }
        public event EventHandler<PermissionRequestedEventArgs> PermissionRequested;
        internal void RaisePermissionRequested(PermissionRequestedEventArgs e)
        {
            if (InitializingHistory) return;
            Browser?.Dispatcher.Invoke(() => PermissionRequested?.Invoke(this, e));
        }
        public void NotifyNewTabRequested(NewTabRequestEventArgs e) => Browser?.Dispatcher.Invoke(() => NewTabRequested?.Invoke(this, e));


        /*public event Action<WebDownloadItem> DownloadStarted;
        public event Action<WebDownloadItem> DownloadUpdated;
        public event Action<WebDownloadItem> DownloadCompleted;
        public void DownloadOnStarted(WebDownloadItem e) => DownloadStarted?.RaiseUIAsync(e);
        public void DownloadOnUpdated(WebDownloadItem e) => DownloadUpdated?.RaiseUIAsync(e);
        public void DownloadOnCompleted(WebDownloadItem e) => DownloadCompleted?.RaiseUIAsync(e);*/

        public event EventHandler<WebContextMenuEventArgs> ContextMenuRequested;
        public void RaiseContextMenu(WebContextMenuEventArgs e) => ContextMenuRequested?.RaiseUIAsync(this, e);

        public event EventHandler<WebAuthenticationRequestedEventArgs> AuthenticationRequested;
        public event EventHandler<ExternalProtocolEventArgs> ExternalProtocolRequested;
        internal void RaiseExternalProtocolRequested(ExternalProtocolEventArgs e)
        {
            Browser?.Dispatcher.Invoke(() => ExternalProtocolRequested?.Invoke(this, e));
        }
        public event EventHandler<NavigationErrorEventArgs> NavigationError;

        public void Download(string Url) => Browser.StartDownload(Url);

        public void ExecuteScript(string Script)
        {
            if (Browser.CanExecuteJavascriptInMainFrame)
                Browser.ExecuteScriptAsync(Script);
        }

        public bool CanExecuteJavascript => Browser.CanExecuteJavascriptInMainFrame;
        public async Task<string> EvaluateScriptAsync(string Script)
        {
            if (!CanExecuteJavascript)
                return null;
            var Result = await Browser.EvaluateScriptAsync(Script);
            return Result.Success ? Result.Result?.ToString() : null;
        }

        public async Task<byte[]> TakeScreenshotAsync(WebScreenshotFormat Format, Size? Viewport = null)
        {
            if (Viewport == null)
            {
                var ContentSize = await Browser.GetContentSizeAsync();
                Viewport = new Size { Height = ContentSize.Height, Width = ContentSize.Width };
            }
            return await Browser.CaptureScreenshotAsync(Format.ToCefScreenshotFormat(), null, new CefSharp.DevTools.Page.Viewport { Width = (int)Viewport?.Width, Height = (int)Viewport?.Height }, true, true);
        }
        public async Task<string> GetSourceAsync() => await Browser.GetSourceAsync();
        public async Task<string> CallDevToolsAsync(string Method, object? Parameters = null)
        {
            try
            {
                IDictionary<string, object>? Dict = null;

                if (Parameters != null)
                {
                    var JSON = JsonSerializer.Serialize(Parameters, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    Dict = ConvertJsonElement(JsonDocument.Parse(JSON).RootElement) as IDictionary<string, object>;
                }

                var Response = await Browser.GetDevToolsClient().ExecuteDevToolsMethodAsync(Method, Dict);
                return JsonSerializer.Serialize(Response);
            }
            catch (Exception _Exception) { return JsonSerializer.Serialize(new { error = _Exception.Message }); }
        }
        /*public async Task ClearBrowsingDataAsync(WebViewBrowsingDataTypes DataType)
        {
            var Context = Browser.GetBrowserHost()?.RequestContext;
            if (Context == null)
                return;
            DevToolsClient DevToolsClient = Browser.GetDevToolsClient();
            if (DataType == WebViewBrowsingDataTypes.Cookies)
                Cef.GetGlobalCookieManager()?.DeleteCookies("", "");
            else if (DataType == WebViewBrowsingDataTypes.DiskCache)
                await DevToolsClient.Page.ClearCompilationCacheAsync();
            else if (DataType == WebViewBrowsingDataTypes.Cache)
                await DevToolsClient.Network.ClearBrowserCacheAsync();
            else if (DataType == WebViewBrowsingDataTypes.LocalStorage)
                await DevToolsClient.DOMStorage.();
        }*/
        private static object? ConvertJsonElement(JsonElement Element)
        {
            switch (Element.ValueKind)
            {
                case JsonValueKind.Object:
                    return Element.EnumerateObject().ToDictionary(_Property => _Property.Name, _Property => ConvertJsonElement(_Property.Value)!);
                case JsonValueKind.Array:
                    return Element.EnumerateArray().Select(ConvertJsonElement).ToList();
                case JsonValueKind.String:
                    return Element.GetString();
                case JsonValueKind.Number:
                    if (Element.TryGetInt64(out var _Int)) return _Int;
                    if (Element.TryGetDouble(out var _Double)) return _Double;
                    return Element.GetDecimal();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                default: return null;
            }
        }

        public FrameworkElement Control => Browser;

        private bool _Disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool Disposing)
        {
            if (_Disposed) return;
            if (Disposing)
            {
                WebViewManager.WebViews.Remove(this);
                WebViewManager.ChromiumWebViews.Remove(Browser);

                //WARNING: Removing these crashes SLBr while switching for some unknown reason
                /*Browser.IsBrowserInitializedChanged -= Browser_IsBrowserInitializedChanged;
                Browser.FrameLoadStart -= Browser_FrameLoadStart;
                Browser.FrameLoadEnd -= Browser_FrameLoadEnd;
                Browser.LoadingStateChanged -= Browser_LoadingStateChanged;
                Browser.TitleChanged -= Browser_TitleChanged;
                Browser.StatusMessage -= Browser_StatusMessage;*/

                Browser.DisplayHandler = null;
                Browser.LifeSpanHandler = null;
                Browser.RequestHandler = null;
                Browser.JsDialogHandler = null;
                Browser.KeyboardHandler = null;
                Browser.PermissionHandler = null;
                Browser.DownloadHandler = null;
                Browser.MenuHandler = null;
                Browser.DialogHandler = null;
                Browser.ResourceRequestHandlerFactory = null;

                if (Browser.Parent is Panel Parent)
                    Parent.Children.Remove(Browser);
                Browser.Dispose();
                Browser = null;
            }
            _Disposed = true;
        }

        #region RequestHandler
        public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            WebAuthenticationRequestedEventArgs Args = new WebAuthenticationRequestedEventArgs(originUrl);
            Browser?.Dispatcher.Invoke(() => AuthenticationRequested?.Invoke(this, Args));
            if (Args.Cancel)
                callback.Cancel();
            else
                callback.Continue(Args.Username, Args.Password);
            return true;
        }
        public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            BeforeNavigationEventArgs Args = new BeforeNavigationEventArgs(request.Url, frame.IsMain);
            Browser?.Dispatcher.Invoke(() => BeforeNavigation?.Invoke(this, Args));

            return Args.Cancel;
        }
        public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback) => true;
        public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            if (targetDisposition == WindowOpenDisposition.NewBackgroundTab)
            {
                NotifyNewTabRequested(new NewTabRequestEventArgs(targetUrl, true, null));
                return true;
            }
            return false;
        }
        public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser) { }
        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            if (WebViewManager.OverrideRequests.Keys.Contains(request.Url))
                return null;
            return new ChromiumResourceRequestHandler(this);
        }
        public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser) { }
        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
        {
            callback.Dispose();
            return false;
        }
        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status, int errorCode, string errorMessage) { }
        #endregion

        #region DisplayHandler
        public void OnAddressChanged(IWebBrowser chromiumWebBrowser, AddressChangedEventArgs addressChangedArgs) { }
        public bool OnAutoResize(IWebBrowser chromiumWebBrowser, IBrowser browser, CefSharp.Structs.Size newSize) => false;
        public bool OnConsoleMessage(IWebBrowser chromiumWebBrowser, ConsoleMessageEventArgs consoleMessageArgs) => false;
        public bool OnCursorChange(IWebBrowser chromiumWebBrowser, IBrowser browser, nint cursor, CefSharp.Enums.CursorType type, CefSharp.Structs.CursorInfo customCursorInfo) => false;
        public void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> urls)
        {
            if (InitializingHistory)
                return;
            if (urls.Count != 0)
            {
                string Url = urls.OrderBy(url => url.EndsWith(".ico") ? 0 : url.EndsWith(".png") ? 1 : 2).ToList().First();
                if (!Url.EndsWith(".svg"))
                    FaviconChanged?.RaiseUIAsync(this, Url);
            }
        }
        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
            FullscreenChanged?.RaiseUIAsync(this, fullscreen);
        }
        public void OnLoadingProgressChange(IWebBrowser chromiumWebBrowser, IBrowser browser, double progress) { }
        public void OnStatusMessage(IWebBrowser chromiumWebBrowser, StatusMessageEventArgs statusMessageArgs) { }
        public void OnTitleChanged(IWebBrowser chromiumWebBrowser, TitleChangedEventArgs titleChangedArgs) { }
        public bool OnTooltipChanged(IWebBrowser chromiumWebBrowser, ref string text) => false;
        #endregion
    }

    public class ChromiumEdgeWebView : IWebView, IDisposable
    {
        private WebView2 Browser;
        private WebViewBrowserSettings Settings;
        private readonly List<string> InitialUrls;
        CoreWebView2 BrowserCore;

        public ChromiumEdgeWebView(List<string> Urls = null, WebViewBrowserSettings _Settings = null)
        {
            InitialUrls = Urls ?? ["about:blank"];
            Settings = _Settings ?? new WebViewBrowserSettings();
            WebViewManager.WebViews.Add(this);
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            if (!WebViewManager.IsWebView2Initialized)
                WebViewManager.InitializeWebView2();
            Browser = new WebView2();
            Browser.CoreWebView2InitializationCompleted += Browser_CoreWebView2InitializationCompleted;

            try
            {
                Browser.EnsureCoreWebView2Async(WebViewManager.WebView2Environment, Settings.Private ? WebViewManager.WebView2PrivateControllerOptions : WebViewManager.WebView2ControllerOptions);
            }
            catch (COMException _COMException) when ((uint)_COMException.HResult == 0x8007139F)
            {
                //https://github.com/MicrosoftEdge/WebView2Feedback/issues/3008#issuecomment-1916313157
                WebViewManager.DeleteWebView2HighDPIRegistry();
                Browser.EnsureCoreWebView2Async(WebViewManager.WebView2Environment, Settings.Private ? WebViewManager.WebView2PrivateControllerOptions : WebViewManager.WebView2ControllerOptions);
            }
            Browser.KeyDown += (s, e) => HotKeyManager.HandleKeyDown(e);
            ZoomFactor = 1;
        }

        private async void Browser_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            BrowserCore = Browser.CoreWebView2;
            Browser.Dispatcher.BeginInvoke(() => IsBrowserInitializedChanged?.Invoke(this, EventArgs.Empty));
            BrowserCore.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Basic;
            BrowserCore.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
            if (!Utils.IsEmptyOrWhiteSpace(WebViewManager.Settings.DownloadFolderPath))
                BrowserCore.Profile.DefaultDownloadFolderPath = WebViewManager.Settings.DownloadFolderPath;
            BrowserCore.Settings.AreHostObjectsAllowed = false;
            BrowserCore.Settings.IsScriptEnabled = Settings.JavaScript;
            BrowserCore.Settings.IsStatusBarEnabled = false;
            BrowserCore.Settings.IsZoomControlEnabled = true;
            BrowserCore.Settings.AreBrowserAcceleratorKeysEnabled = true;
            BrowserCore.Settings.IsPasswordAutosaveEnabled = false;
            BrowserCore.Settings.IsSwipeNavigationEnabled = false;
            BrowserCore.Settings.IsPinchZoomEnabled = false;

            BrowserCore.Settings.IsReputationCheckingRequired = Settings.Private ? false : App.Instance.WebRiskService != WebRiskHandler.SecurityService.None;

            BrowserCore.Settings.IsGeneralAutofillEnabled = false;
            BrowserCore.Settings.AreDefaultScriptDialogsEnabled = false;
            //Core.Settings.HiddenPdfToolbarItems = CoreWebView2PdfToolbarItems.Bookmarks;
            BrowserCore.Settings.IsWebMessageEnabled = Settings.JavaScriptMessage;
            if (WebViewManager.Settings.Performance == PerformancePreset.High)
            {
                BrowserCore.Settings.PreferredForegroundTimerWakeInterval = TimeSpan.FromMilliseconds(4);
                BrowserCore.Settings.PreferredBackgroundTimerWakeInterval = TimeSpan.FromMilliseconds(16);
                BrowserCore.Settings.PreferredIntensiveTimerWakeInterval = TimeSpan.FromMilliseconds(4);
                BrowserCore.Settings.PreferredOverrideTimerWakeInterval = TimeSpan.Zero;
            }
            else if (WebViewManager.Settings.Performance == PerformancePreset.Low)
            {
                BrowserCore.Settings.PreferredForegroundTimerWakeInterval = TimeSpan.FromMilliseconds(16);
                BrowserCore.Settings.PreferredBackgroundTimerWakeInterval = TimeSpan.FromMilliseconds(250);
                BrowserCore.Settings.PreferredIntensiveTimerWakeInterval = TimeSpan.FromMilliseconds(16);
                BrowserCore.Settings.PreferredOverrideTimerWakeInterval = TimeSpan.Zero;
            }
            BrowserCore.MemoryUsageTargetLevel = WebViewManager.Settings.Performance == PerformancePreset.High ? CoreWebView2MemoryUsageTargetLevel.Normal : CoreWebView2MemoryUsageTargetLevel.Low;

            BrowserCore.IsDefaultDownloadDialogOpenChanged += Core_IsDefaultDownloadDialogOpenChanged;

            BrowserCore.NavigationStarting += Browser_NavigationStarting;
            BrowserCore.FrameNavigationStarting += Browser_FrameNavigationStarting;
            BrowserCore.FrameNavigationCompleted += Browser_FrameNavigationCompleted;
            BrowserCore.NavigationCompleted += Browser_NavigationCompleted;
            BrowserCore.ServerCertificateErrorDetected += Browser_ServerCertificateErrorDetected;

            BrowserCore.DocumentTitleChanged += Browser_DocumentTitleChanged;
            BrowserCore.FaviconChanged += Browser_FaviconChanged;
            BrowserCore.StatusBarTextChanged += (s, e) => StatusMessage?.RaiseUIAsync(this, BrowserCore.StatusBarText);

            BrowserCore.PermissionRequested += Browser_PermissionRequested;
            BrowserCore.ScriptDialogOpening += Browser_ScriptDialogOpening;
            BrowserCore.NewWindowRequested += Browser_NewWindowRequested;
            if (Settings.AudioListener)
            {
                BrowserCore.IsDocumentPlayingAudioChanged += (s, e) => AudioPlayingChanged?.RaiseUIAsync(this);
                BrowserCore.IsMutedChanged += (s, e) => AudioPlayingChanged?.RaiseUIAsync(this);
            }
            BrowserCore.ContainsFullScreenElementChanged += (s, e) => FullscreenChanged?.RaiseUIAsync(this, BrowserCore.ContainsFullScreenElement);
            BrowserCore.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            BrowserCore.WebResourceRequested += Browser_WebResourceRequested;
            BrowserCore.WebResourceResponseReceived += Browser_WebResourceResponseReceived;
            if (Settings.JavaScriptMessage)
            {
                BrowserCore.WebMessageReceived += (s, e) => JavaScriptMessageReceived?.RaiseUIAsync(this, e.WebMessageAsJson);
                BrowserCore.AddScriptToExecuteOnDocumentCreatedAsync("window.engine = window.chrome.webview;");
            }

            BrowserCore.DownloadStarting += Browser_DownloadStarting;

            BrowserCore.ContextMenuRequested += Browser_ContextMenuRequested;
            BrowserCore.Find.ActiveMatchIndexChanged += (s, e) => FindResult.RaiseUIAsync(this, new FindResult(BrowserCore.Find.ActiveMatchIndex, BrowserCore.Find.MatchCount));
            BrowserCore.Find.MatchCountChanged += (s, e) => FindResult.RaiseUIAsync(this, new FindResult(BrowserCore.Find.ActiveMatchIndex, BrowserCore.Find.MatchCount));
            BrowserCore.ProcessFailed += ProcessFailed;

            BrowserCore.BasicAuthenticationRequested += Browser_BasicAuthenticationRequested;

            BrowserCore.LaunchingExternalUriScheme += Browser_LaunchingExternalUriScheme;

            for (int i = 0; i < InitialUrls.Count; i++)
            {
                string Url = InitialUrls[i];
                bool IsHistory = i < InitialUrls.Count - 1;
                if (IsHistory)
                    WebViewManager.RegisterOverrideRequest(Url, ResourceHandler.GetByteArray(App.HistoryPlaceholder, Encoding.UTF8), "text/html", 1);
                await CallDevToolsAsync("Page.navigate", new
                {
                    url = Url,
                    transitionType = "generated"
                });
                InitializingHistory = IsHistory;
                if (IsHistory)
                    await Task.Delay(TimeSpan.FromMilliseconds(5));
            }
        }

        private void Browser_FaviconChanged(object? sender, object e)
        {
            if (InitializingHistory) return;
            FaviconChanged?.RaiseUIAsync(this, BrowserCore.FaviconUri);
        }

        private void Browser_DocumentTitleChanged(object? sender, object e)
        {
            if (InitializingHistory) return;
            TitleChanged?.RaiseUIAsync(this, BrowserCore.DocumentTitle);
        }

        private void Browser_FrameNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (InitializingHistory) return;
            FrameLoadEnd?.RaiseUIAsync(this, Address);
        }

        private void Browser_FrameNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (InitializingHistory) return;
            FrameLoadStart?.RaiseUIAsync(this, e.Uri);
        }

        bool InitializingHistory = false;

        bool CertificateError = false;
        private void Browser_ServerCertificateErrorDetected(object? sender, CoreWebView2ServerCertificateErrorDetectedEventArgs e)
        {
            if (InitializingHistory) return;
            CertificateError = true;
            IsSecure = false;
        }

        private void Core_IsDefaultDownloadDialogOpenChanged(object? sender, object e)
        {
            if (BrowserCore.IsDefaultDownloadDialogOpen)
                BrowserCore.CloseDefaultDownloadDialog();
        }

        private void ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            string Url = Address;
            Panel? Parent = null;
            if (Browser.Parent is Panel _Parent)
            {
                Parent = _Parent;
                Parent.Children.Remove(Browser);
            }
            Browser.Dispose();
            Browser = null;
            InitializeAsync();
            Parent?.Children.Add(Browser);
        }

        private void Browser_LaunchingExternalUriScheme(object? sender, CoreWebView2LaunchingExternalUriSchemeEventArgs e)
        {
            e.Cancel = true;
            if (InitializingHistory) return;
            ExternalProtocolEventArgs Args = new ExternalProtocolEventArgs(e.Uri, e.InitiatingOrigin);
            Browser?.Dispatcher.Invoke(() => ExternalProtocolRequested?.Invoke(this, Args));
            if (Args.Launch)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri,
                    UseShellExecute = true
                });
            }
        }

        private void Browser_BasicAuthenticationRequested(object? sender, CoreWebView2BasicAuthenticationRequestedEventArgs e)
        {
            WebAuthenticationRequestedEventArgs Args = new WebAuthenticationRequestedEventArgs(e.Uri);
            Browser?.Dispatcher.Invoke(() => AuthenticationRequested?.Invoke(this, Args));
            if (Args.Cancel)
                e.Cancel = true;
            else
            {
                e.Response.UserName = Args.Username;
                e.Response.Password = Args.Password;
            }
        }

        private void Browser_WebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            if (!RequestContexts.TryGetValue(e.Request.Uri, out CoreWebView2WebResourceContext ResourceContext))
                ResourceContext = CoreWebView2WebResourceContext.Other;
            ResourceResponded?.RaiseUIAsync(this, new ResourceRespondedResult(e.Request.Uri, ResourceContext.ToResourceRequestType()));
            if (ResourceLoaded != null)
            {
                try
                {
                    if (e.Response.Headers.Contains("Content-Length"))
                    {
                        if (long.TryParse(e.Response.Headers.GetHeader("Content-Length"), out long ContentLength))
                            ResourceLoaded?.RaiseUIAsync(this, new ResourceLoadedResult(e.Request.Uri, true, ContentLength, ResourceContext.ToResourceRequestType()));
                    }
                }
                catch { }
            }
            RequestContexts.Remove(e.Request.Uri);
        }

        private Dictionary<string, CoreWebView2WebResourceContext> RequestContexts = new Dictionary<string, CoreWebView2WebResourceContext>();

        private void Browser_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            //https://github.com/MicrosoftEdge/WebView2Feedback/issues/2340
            //God knows when the WebView2 developers will fix this

            CoreWebView2ContextMenuTarget? Target = e.ContextMenuTarget;

            string LinkText = string.Empty;
            string LinkUrl = string.Empty;
            string SelectionText = string.Empty;
            string SourceUrl = string.Empty;
            string FrameUrl = string.Empty;
            try { LinkText = Target.LinkText ?? string.Empty; } catch { }
            try { LinkUrl = Target.LinkUri ?? string.Empty; } catch { }
            try { SelectionText = Target.SelectionText ?? string.Empty; } catch { }
            try { SourceUrl = Target.SourceUri ?? string.Empty; } catch { }
            try { FrameUrl = Target.FrameUri ?? string.Empty; } catch { }
            
            e.MenuItems?.Clear();
            e.Handled = true;

            var args = new WebContextMenuEventArgs
            {
                X = e.Location.X,
                Y = e.Location.Y,
                LinkUrl = LinkUrl,
                LinkText = LinkText,
                SelectionText = SelectionText,
                IsEditable = Target?.IsEditable ?? false,
                DictionarySuggestions = new List<string>(),
                MisspelledWord = string.Empty,
                SourceUrl = SourceUrl,
                FrameUrl = FrameUrl,
                SpellCheck = false,
                MediaType = Target?.Kind.ToWebContextMenuMediaType() ?? WebContextMenuMediaType.None,
                MenuType = Target?.MapWebContextMenuTarget() ?? WebContextMenuType.Page
            };

            ContextMenuRequested?.RaiseUIAsync(this, args);
        }

        private void Browser_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            ProtocolResponse OverrideResponse = WebViewManager.OverrideHandler(e.Request.Uri);
            if (OverrideResponse != null)
            {
                e.Response = BrowserCore?.Environment.CreateWebResourceResponse(new MemoryStream(OverrideResponse.Data), 200, "OK", $"Content-Type: {OverrideResponse.MimeType}");
                return;
            }
            if (!RequestContexts.ContainsKey(e.Request.Uri))
                RequestContexts[e.Request.Uri] = e.ResourceContext;
            ResourceRequestEventArgs Args = new ResourceRequestEventArgs(e.Request.Uri, Address, e.Request.Method, e.ResourceContext.ToResourceRequestType(), new Dictionary<string, string>());//e.Request.Headers.ToDictionary()
            ResourceRequested?.Invoke(this, Args);
            if (Args.Cancel)
                e.Response = WebViewManager.WebView2CancelResponse;
            else
            {
                if (Utils.IsCustomScheme(e.Request.Uri))
                    _ = HandleWebResourceRequestedAsync(e, e.GetDeferral());
                else
                {
                    if (Args.ModifiedHeaders != null && Args.ModifiedHeaders.Count != 0)
                    {
                        foreach (var Header in Args.ModifiedHeaders)
                        {
                            try
                            {
                                e.Request.Headers.SetHeader(Header.Key, Header.Value);
                            }
                            catch { }
                        }
                    }
                }
            }
        }


        private async Task HandleWebResourceRequestedAsync(CoreWebView2WebResourceRequestedEventArgs e, CoreWebView2Deferral Deferral)
        {
            try
            {
                if (WebViewManager.Settings.Schemes.TryGetValue(Utils.GetScheme(e.Request.Uri), out var Handler))
                {
                    ProtocolResponse Response = await Handler(e.Request.Uri, Settings.Private.ToInt().ToString());
                    e.Response = BrowserCore?.Environment.CreateWebResourceResponse(new MemoryStream(Response.Data), Response.StatusCode, "OK", $"Content-Type: {Response.MimeType}");
                }
            }
            finally
            {
                Deferral.Complete();
            }
        }

        private void Browser_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            WebDownloadItem Item = new WebDownloadItem
            {
                ID = Guid.NewGuid().ToString(),
                Url = e.DownloadOperation.Uri,
                FileName = Path.GetFileName(e.DownloadOperation.ResultFilePath),
                FullPath = e.DownloadOperation.ResultFilePath,
                TotalBytes = (long)(e.DownloadOperation.TotalBytesToReceive ?? 0),
                State = WebDownloadState.InProgress
            };

            Item.Pause = e.DownloadOperation.Pause;

            Item.Resume = () =>
            {
                if (e.DownloadOperation.CanResume)
                    e.DownloadOperation.Resume();
            };

            Item.Cancel = e.DownloadOperation.Cancel;


            WebViewManager.DownloadManager.Started(Item);
            //DownloadStarted?.RaiseUIAsync(Item);

            e.DownloadOperation.BytesReceivedChanged += (s2, e2) =>
            {
                Item.ReceivedBytes = e.DownloadOperation.BytesReceived;
                Item.TotalBytes = (long)(e.DownloadOperation.TotalBytesToReceive ?? 0);
                WebViewManager.DownloadManager.Updated(Item);
                //DownloadUpdated?.RaiseUIAsync(Item);
            };
            e.DownloadOperation.StateChanged += (s2, e2) =>
            {
                switch (e.DownloadOperation.State)
                {
                    case CoreWebView2DownloadState.Completed:
                        Item.State = WebDownloadState.Completed;
                        WebViewManager.DownloadManager.Completed(Item);
                        break;
                    case CoreWebView2DownloadState.Interrupted:
                        if (e.DownloadOperation.InterruptReason == CoreWebView2DownloadInterruptReason.UserPaused)
                        {
                            Item.State = WebDownloadState.Paused;
                            WebViewManager.DownloadManager.Updated(Item);
                        }
                        else
                        {
                            Item.State = WebDownloadState.Canceled;
                            WebViewManager.DownloadManager.Completed(Item);
                        }
                        break;
                    case CoreWebView2DownloadState.InProgress:
                        Item.State = WebDownloadState.InProgress;
                        WebViewManager.DownloadManager.Updated(Item);
                        break;
                }
            };
        }

        private void Browser_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            var Args = new PermissionRequestedEventArgs(Address, e.PermissionKind.ToWebPermission());
            Browser?.Dispatcher.Invoke(() => { PermissionRequested?.Invoke(this, Args); });
            e.State = Args.State.ToWebView2PermissionState();
            e.Handled = true;
        }

        private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            CurrentAddress = e.Uri;
            if (InitializingHistory) return;
            BeforeNavigationEventArgs Args = new BeforeNavigationEventArgs(e.Uri, true);
            Browser?.Dispatcher.Invoke(() => BeforeNavigation?.Invoke(this, Args));
            if (Args.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (!WebViewManager.RuntimeSettings.PDFViewer && Utils.GetFileExtension(e.Uri) == ".pdf")
            {
                e.Cancel = true;
                Browser?.Dispatcher.BeginInvoke(() => WebViewManager.DownloadManager.StartDownloadAsync(e.Uri, string.Empty, WebViewManager.Settings.DownloadPrompt, "PDF File (*.pdf)|*.pdf"));
                return;
            }
            IsLoading = true;
            LoadingStateChanged?.RaiseUIAsync(this, new LoadingStateResult(IsLoading, null));
        }

        public Task<CoreWebView2> WaitForCoreWebView2Async()
        {
            var TaskSource = new TaskCompletionSource<CoreWebView2>();
            if (BrowserCore != null)
                TaskSource.TrySetResult(BrowserCore);
            else
            {
                void WaitHandler(object? s, EventArgs e)
                {
                    IsBrowserInitializedChanged -= WaitHandler;
                    if (BrowserCore != null)
                        TaskSource.TrySetResult(BrowserCore);
                    else
                        TaskSource.TrySetResult(null);
                }
                IsBrowserInitializedChanged += WaitHandler;
            }

            return TaskSource.Task;
        }

        /*private void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            Rect? Popup = null;
            if (e.WindowFeatures.HasSize || e.WindowFeatures.HasPosition)
                Popup = new Rect(e.WindowFeatures.Left, e.WindowFeatures.Top, e.WindowFeatures.Width, e.WindowFeatures.Height);
            //MessageBox.Show($"{e.WindowFeatures.Left}, {e.WindowFeatures.Top}, {e.WindowFeatures.Width}, {e.WindowFeatures.Height}");
            var Args = new NewTabRequestEventArgs(e.Uri, false, Popup);
            Browser?.Dispatcher.Invoke(() => NewTabRequested?.Invoke(this, Args));
            if (Args.WebView is ChromiumEdgeWebView EdgeWebView)
                e.NewWindow = ((WebView2)EdgeWebView.Control).CoreWebView2;
            e.Handled = true;
        }*/

        private void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            _ = HandleNewWindowAsync(e, e.GetDeferral());
        }

        private async Task HandleNewWindowAsync(CoreWebView2NewWindowRequestedEventArgs e, CoreWebView2Deferral Deferral)
        {
            //TODO: Fix PiP popups & determine whether a tab is background or foreground
            //MessageBox.Show($"{e.WindowFeatures.ShouldDisplayStatus} {e.WindowFeatures.ShouldDisplayToolbar} {e.WindowFeatures.ShouldDisplayMenuBar} {e.WindowFeatures.ShouldDisplayScrollBars}");
            try
            {
                Rect? Popup = null;
                if (e.WindowFeatures.HasSize || e.WindowFeatures.HasPosition)
                    Popup = new Rect(e.WindowFeatures.Left, e.WindowFeatures.Top, e.WindowFeatures.Width, e.WindowFeatures.Height);
                var Args = new NewTabRequestEventArgs(e.Uri, false, Popup);
                Browser?.Dispatcher.Invoke(() => NewTabRequested?.Invoke(this, Args));
                if (Args.WebView is ChromiumEdgeWebView EdgeWebView)
                {
                    await EdgeWebView.WaitForCoreWebView2Async();
                    e.NewWindow = EdgeWebView.BrowserCore;
                }

                e.Handled = true;
            }
            finally
            {
                Deferral.Complete();
            }
        }

        private void Browser_ScriptDialogOpening(object? sender, CoreWebView2ScriptDialogOpeningEventArgs e)
        {
            ScriptDialogEventArgs Args = new ScriptDialogEventArgs(e.Kind.ToScriptDialogType(), e.Uri, e.Message, e.DefaultText);
            Browser?.Dispatcher.Invoke(() => { ScriptDialogOpened?.Invoke(this, Args); });
            if (Args.Result)
            {
                e.Accept();
                if (!string.IsNullOrEmpty(Args.PromptResult))
                    e.ResultText = Args.PromptResult;
            }
        }

        private void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (InitializingHistory) return;
            IsLoading = false;
            IsSecure = false;

            if (BrowserCore.Source.StartsWith("https:"))
                IsSecure = !CertificateError;

            if (!e.IsSuccess && e.WebErrorStatus != CoreWebView2WebErrorStatus.ConnectionAborted)
                NavigationError?.RaiseUIAsync(this, new NavigationErrorEventArgs(e.HttpStatusCode, e.WebErrorStatus.ToString(), Address));
            LoadingStateChanged?.RaiseUIAsync(this, new LoadingStateResult(IsLoading, e.HttpStatusCode));
        }

        public WebEngineType Engine => WebEngineType.ChromiumEdge;
        private string CurrentAddress;
        public string Address
        {
            get => Browser.Source != null ? CurrentAddress : InitialUrls.Last();
            set => Navigate(value);
        }
        public string Title => BrowserCore?.DocumentTitle ?? string.Empty;

        public bool CanGoBack => Browser.CanGoBack;
        public bool CanGoForward => Browser.CanGoForward;
        public bool CanReload => !IsLoading;
        public bool IsLoading { get; private set; }
        public bool IsBrowserInitialized => BrowserCore != null;

        public bool IsSecure { get; private set; }
        public bool AudioPlaying => BrowserCore?.IsDocumentPlayingAudio ?? false;
        public bool IsMuted
        {
            get => BrowserCore?.IsMuted ?? false;
            set { BrowserCore?.IsMuted = value; }
        }
        public double ZoomFactor
        {
            get => Browser.ZoomFactor;
            set { Browser.ZoomFactor = value; }
        }

        public void Navigate(string Url) { try { BrowserCore?.Navigate(Url); } catch { } }
        public void Back() { if (CanGoBack) Browser?.GoBack(); }
        public void Forward() { if (CanGoForward) Browser?.GoForward(); }

        public void Refresh(bool IgnoreCache = false, bool ClearCache = false)
        {
            /*Browser.CoreWebView2.Profile.ClearBrowsingDataAsync(
    CoreWebView2BrowsingDataKinds.Cookies | CoreWebView2BrowsingDataKinds.CacheStorage
);*/
            if (ClearCache)
                BrowserCore?.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.DiskCache);
            Browser?.Reload();
        }
        public void Stop() => Browser.Stop();
        public void Print() => BrowserCore?.ShowPrintUI();

        public void Find(string Text, bool Forward, bool MatchCase, bool FindNext)
        {
            WebViewManager.WebView2FindOptions.IsCaseSensitive = MatchCase;
            WebViewManager.WebView2FindOptions.FindTerm = Text;
            BrowserCore.Find.StartAsync(WebViewManager.WebView2FindOptions);
            if (FindNext)
            {
                if (Forward)
                    BrowserCore.Find.FindNext();
                else
                    BrowserCore.Find.FindPrevious();
            }
        }
        public void StopFind() => BrowserCore.Find.Stop();
        public void SaveAs() => BrowserCore.ShowSaveAsUIAsync();

        public void Cut() => ExecuteScript("document.execCommand('cut');");
        public void Copy() => ExecuteScript("document.execCommand('copy');");
        public void Paste() => ExecuteScript("document.execCommand('paste');");
        public void Delete() => ExecuteScript("document.execCommand('delete');");
        public void SelectAll() => ExecuteScript("document.execCommand('selectAll');");
        public void Undo() => ExecuteScript("document.execCommand('undo');");
        public void Redo() => ExecuteScript("document.execCommand('redo');");

        public event EventHandler AudioPlayingChanged;
        public event EventHandler<bool> FullscreenChanged;
        public event EventHandler<ScriptDialogEventArgs> ScriptDialogOpened;
        public event EventHandler<BeforeNavigationEventArgs> BeforeNavigation;
        public event EventHandler<NewTabRequestEventArgs> NewTabRequested;
        public event EventHandler<string> FrameLoadStart;
        public event EventHandler<string> FrameLoadEnd;
        public event EventHandler IsBrowserInitializedChanged;
        public event EventHandler<LoadingStateResult> LoadingStateChanged;
        public event EventHandler<string> TitleChanged;
        public event EventHandler<string> StatusMessage;
        public event EventHandler<string> FaviconChanged;
        public event EventHandler<FindResult> FindResult;
        public event EventHandler<string> JavaScriptMessageReceived;
        public event EventHandler<ResourceRequestEventArgs> ResourceRequested;
        public event EventHandler<ResourceRespondedResult> ResourceResponded;
        public event EventHandler<ResourceLoadedResult> ResourceLoaded;
        public event EventHandler<PermissionRequestedEventArgs> PermissionRequested;

        /*public event Action<WebDownloadItem> DownloadStarted;
        public event Action<WebDownloadItem> DownloadUpdated;
        public event Action<WebDownloadItem> DownloadCompleted;*/

        public event EventHandler<WebContextMenuEventArgs> ContextMenuRequested;
        public event EventHandler<WebAuthenticationRequestedEventArgs> AuthenticationRequested;
        public event EventHandler<ExternalProtocolEventArgs> ExternalProtocolRequested;
        public event EventHandler<NavigationErrorEventArgs> NavigationError;

        public void Download(string Url) => WebViewManager.DownloadManager.StartDownloadAsync(Url, string.Empty, WebViewManager.Settings.DownloadPrompt, "");

        public void ExecuteScript(string Script) => Browser.ExecuteScriptAsync(Script);
        public bool CanExecuteJavascript => BrowserCore != null;
        public async Task<string> EvaluateScriptAsync(string Script)
        {
            string Result = await BrowserCore.ExecuteScriptAsync(Script);
            if (Result.StartsWith("\"") && Result.EndsWith("\""))
                return JsonSerializer.Deserialize<string>(Result);
            return Result;
        }

        public async Task<byte[]> TakeScreenshotAsync(WebScreenshotFormat Format, Size? Viewport = null)
        {
            using var Stream = new MemoryStream();
            await BrowserCore.CapturePreviewAsync(Format.ToWebView2ScreenshotFormat(), Stream);
            return Stream.ToArray();
        }
        public async Task<string> GetSourceAsync() => await EvaluateScriptAsync("document.documentElement.outerHTML");
        public async Task<string> CallDevToolsAsync(string Method, object? Parameters = null)
        {
            try
            {
                string Json = Parameters != null ? JsonSerializer.Serialize(Parameters, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }) : "{}";
                return await BrowserCore?.CallDevToolsProtocolMethodAsync(Method, Json) ?? "";
            }
            catch
            {
                return "";
            }
        }

        public FrameworkElement Control => Browser;

        private bool _Disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool Disposing)
        {
            if (_Disposed) return;
            if (Disposing)
            {
                WebViewManager.WebViews.Remove(this);
                CoreWebView2 Core = BrowserCore;

                Browser.CoreWebView2InitializationCompleted -= (s, e) => Browser?.Dispatcher.BeginInvoke(() => IsBrowserInitializedChanged?.Invoke(this, EventArgs.Empty));
                if (Core != null)
                {
                    Core.IsDefaultDownloadDialogOpenChanged -= Core_IsDefaultDownloadDialogOpenChanged;

                    Core.NavigationStarting -= Browser_NavigationStarting;
                    Core.FrameNavigationStarting -= Browser_FrameNavigationStarting;
                    Core.FrameNavigationCompleted -= Browser_FrameNavigationCompleted;
                    Core.NavigationCompleted -= Browser_NavigationCompleted;
                    Core.ServerCertificateErrorDetected -= Browser_ServerCertificateErrorDetected;

                    Core.DocumentTitleChanged -= Browser_DocumentTitleChanged;
                    Core.FaviconChanged -= Browser_FaviconChanged;
                    Core.StatusBarTextChanged -= (s, e) => StatusMessage?.RaiseUIAsync(this, Core.StatusBarText);

                    Core.PermissionRequested -= Browser_PermissionRequested;
                    Core.ScriptDialogOpening -= Browser_ScriptDialogOpening;
                    Core.NewWindowRequested -= Browser_NewWindowRequested;
                    Core.ContainsFullScreenElementChanged -= (s, e) => FullscreenChanged?.RaiseUIAsync(this, Core.ContainsFullScreenElement);
                    Core.WebResourceRequested -= Browser_WebResourceRequested;
                    Core.WebResourceResponseReceived -= Browser_WebResourceResponseReceived;
                    if (Settings.JavaScriptMessage)
                        Core.WebMessageReceived -= (s, e) => JavaScriptMessageReceived?.RaiseUIAsync(this, e.TryGetWebMessageAsString());

                    Core.DownloadStarting -= Browser_DownloadStarting;

                    Core.ContextMenuRequested -= Browser_ContextMenuRequested;
                    Core.Find.ActiveMatchIndexChanged -= (s, e) => FindResult.RaiseUIAsync(this, new FindResult(Core.Find.ActiveMatchIndex, Core.Find.MatchCount));
                    Core.Find.MatchCountChanged -= (s, e) => FindResult.RaiseUIAsync(this, new FindResult(Core.Find.ActiveMatchIndex, Core.Find.MatchCount));
                    Core.ProcessFailed -= ProcessFailed;

                    Core.BasicAuthenticationRequested -= Browser_BasicAuthenticationRequested;

                    Core.LaunchingExternalUriScheme -= Browser_LaunchingExternalUriScheme;
                }
                Browser.KeyDown -= (s, e) => HotKeyManager.HandleKeyDown(e);

                if (Browser.Parent is Panel Parent)
                    Parent.Children.Remove(Browser);
                Browser.Dispose();
                Browser = null;
            }
            _Disposed = true;
        }
    }

    public class TridentWebView : IWebView, IDisposable
    {
        [ComVisible(true)]
        public class Bridge
        {
            private readonly TridentWebView Parent;
            public Bridge(TridentWebView _Parent) => Parent = _Parent;
            public void postMessage(string message) => Parent.JavaScriptMessageReceived?.RaiseUIAsync(Parent, message);
            /*public void audioChanged(int State)
            {
                Parent.SetAudioPlaying(State == 1);
            }*/
        }
        /*public void SetAudioPlaying(bool Playing)
        {
            AudioPlaying = Playing;
            AudioPlayingChanged?.RaiseUIAsync(this);
        }*/

        private WebBrowser Browser;
        SHDocVw.IWebBrowser2 AxBrowser;
        SHDocVw.WebBrowser BrowserCore;
        private WebViewBrowserSettings Settings;
        private readonly List<string> InitialUrls;

        public TridentWebView(List<string> Urls = null, WebViewBrowserSettings _Settings = null)
        {
            InitialUrls = Urls ?? ["about:blank"];
            //ExecuteScript(@"window.engine = { postMessage: function(message) { window.external.postMessage(message); } };");
            Settings = _Settings ?? new WebViewBrowserSettings();
            WebViewManager.WebViews.Add(this);
            if (!WebViewManager.IsTridentInitialized)
                WebViewManager.InitializeTrident();
            Browser = new WebBrowser();

            Browser.Loaded += Loaded;
            Browser.Navigating += Navigating;
            Browser.Navigated += Navigated;
            Browser.LoadCompleted += LoadCompleted;
            Browser.KeyDown += (s, e) => HotKeyManager.HandleKeyDown(e);

            AxBrowser = typeof(WebBrowser).GetProperty("AxIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Browser, null) as SHDocVw.IWebBrowser2;

            SHDocVw.DWebBrowserEvents_Event Events = (SHDocVw.DWebBrowserEvents_Event)AxBrowser;
            Events.NewWindow += NewWindow;
            BrowserCore = (SHDocVw.WebBrowser)AxBrowser;
            BrowserCore.StatusTextChange += StatusTextChange;
            BrowserCore.TitleChange += TitleChange;
            BrowserCore.OnFullScreen += (e) => FullscreenChanged?.RaiseUIAsync(this, e);
            BrowserCore.NavigateError += NavigateError;
            BrowserCore.OnTheaterMode += (e) => FullscreenChanged?.RaiseUIAsync(this, e);
            BrowserCore.SetSecureLockIcon += (e) => IsSecure = e == 2;//TODO: Display yellow triangular warning for mixed content.
            BrowserCore.DocumentComplete += DocumentComplete;
            BrowserCore.RegisterAsDropTarget = true;

            if (Settings.JavaScriptMessage)// || Settings.AudioListener)
                Browser.ObjectForScripting = new Bridge(this);
            Navigate(InitialUrls.Last());
            ZoomFactor = 1;
        }

        private void StatusTextChange(string Text)
        {
            if (Text == "Done")
                Text = string.Empty;
            StatusMessage?.RaiseUIAsync(this, Text);
        }

        private void DocumentComplete(object pDisp, ref object URL)
        {
            Zoom(ZoomFactor);
        }

        /*private const string IERootKeyx32 = @"SOFTWARE\Microsoft\Internet Explorer\";
        private const string IERootKeyx64 = @"SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\";
        private const string IEEmulationPath = @"MAIN\FeatureControl\FEATURE_BROWSER_EMULATION";
        private const string IEEmulationPathx32 = IERootKeyx32 + IEEmulationPath;
        private const string IEEmulationPathx64 = IERootKeyx64 + IEEmulationPath;*/

        // private void SetSecureLockIcon(int SecureLockIcon) => IsSecure = (WebBrowserEncryptionLevel)SecureLockIcon != WebBrowserEncryptionLevel.Insecure;

        /*//https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.webbrowserencryptionlevel
        public enum WebBrowserEncryptionLevel
        {
            Insecure,
            Mixed,
            Unknown,
            Bit40,
            Bit56,
            Fortezza,
            Bit128
        }*/

        /*private void BeforeNavigate(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Cancel)
        {
            //Headers += string.Format("User-Agent: {0}\r\n", UserAgent);
        }*/

        private void NavigateError(object pDisp, ref object URL, ref object Frame, ref object StatusCode, ref bool Cancel)
        {
            NavigationError?.RaiseUIAsync(this, new NavigationErrorEventArgs((int)StatusCode, string.Empty, (string)URL));
        }

        private void TitleChange(string Text)
        {
            Title = Text;
            TitleChanged?.RaiseUIAsync(this, Text);
        }

        private void NewWindow(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Processed)
        {
            Processed = true;
            /*if (Regex.IsMatch(URL, "^.*javascript\\:.*$", RegexOptions.IgnoreCase))
                return;*/
            NewTabRequested?.RaiseUIAsync(this, new NewTabRequestEventArgs(URL, false, null));
        }

        /*public static void SetSilent(WebBrowser browser, bool silent)
        {
            IOleServiceProvider sp = browser.Document as IOleServiceProvider;
            if (sp != null)
            {
                Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");
                object webBrowser;
                sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
                if (webBrowser != null)
                    webBrowser.GetType().InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty, null, webBrowser, [silent]);
            }
        }
        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }*/

        private void Navigated(object sender, NavigationEventArgs e)
        {
            try
            {
                FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fiComWebBrowser == null) return;
                object objComWebBrowser = fiComWebBrowser.GetValue(Browser);
                if (objComWebBrowser == null) return;
                objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, [true]);
            }
            catch { }
            //if (Settings.AudioListener)
            //    ExecuteScript(Scripts.TridentAudioScript);
        }

        private void Loaded(object sender, RoutedEventArgs e)
        {
            /*FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            object objComWebBrowser = fiComWebBrowser.GetValue(Browser);
            if (objComWebBrowser == null) return;
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, [true]);*/
            IsBrowserInitialized = true;
            IsBrowserInitializedChanged?.Invoke(this, EventArgs.Empty);
            if (!string.IsNullOrWhiteSpace(InitialUrls.Last()))
                Navigate(InitialUrls.Last());
            Browser.Loaded -= Loaded;
        }

        private void Navigating(object sender, NavigatingCancelEventArgs e)
        {
            /*if (e.Uri != null)
            {
                ProtocolResponse OverrideResponse = WebViewManager.OverrideHandler(e.Uri.AbsolutePath);
                if (OverrideResponse != null)
                {
                    OverrideAddress = e.Uri.AbsolutePath;
                    Browser.NavigateToStream(new MemoryStream(OverrideResponse.Data));
                }
            }*/
            string Url = e.Uri?.AbsoluteUri ?? OverrideAddress;
            BeforeNavigationEventArgs Args = new BeforeNavigationEventArgs(Url, true);
            BeforeNavigation?.Invoke(this, Args);
            if (Args.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (!WebViewManager.RuntimeSettings.PDFViewer && e.Uri != null && e.Uri.Segments[e.Uri.Segments.Length - 1].EndsWith(".pdf"))
            {
                e.Cancel = true;
                Browser?.Dispatcher.BeginInvoke(() => WebViewManager.DownloadManager.StartDownloadAsync(Url, string.Empty, WebViewManager.Settings.DownloadPrompt, "PDF File (*.pdf)|*.pdf"));
                return;
            }
            if (Utils.IsCustomScheme(Url))
            {
                if (OverrideAddress != Url)
                {
                    e.Cancel = true;
                    if (WebViewManager.Settings.Schemes.TryGetValue(Utils.GetScheme(Url), out var Handler))
                    {
                        OverrideAddress = Url;
                        Browser.NavigateToStream(new MemoryStream(Handler(Url, Settings.Private.ToInt().ToString()).Result.Data));
                    }
                }
            }
            else
                OverrideAddress = null;
            /*if (e.WebRequest != null)
            {
                IDictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (e.WebRequest.Headers != null)
                {
                    foreach (string Key in e.WebRequest.Headers.AllKeys)
                        Headers[Key] = e.WebRequest.Headers[Key];
                }
                ResourceRequestType Type = e.WebRequest.Method.ToLowerInvariant() switch
                {
                    "get" => ResourceRequestType.MainFrame,
                    "post" => ResourceRequestType.XMLHTTPRequest,
                    "put" => ResourceRequestType.XMLHTTPRequest,
                    "delete" => ResourceRequestType.XMLHTTPRequest,
                    _ => ResourceRequestType.Object
                };
                var ResourceArgs = new ResourceRequestEventArgs(e.WebRequest.RequestUri.AbsoluteUri, e.Uri.AbsoluteUri, e.WebRequest.Method, Type, Headers);
                ResourceRequested?.Invoke(this, ResourceArgs);
                if (Args.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }*/
            IsLoading = true;
            FrameLoadStart?.RaiseUIAsync(this, e.Uri?.AbsoluteUri ?? Address);
            LoadingStateChanged?.RaiseUIAsync(this, new LoadingStateResult(IsLoading, null));
            //AxBrowser.Silent = true;
        }
        private void LoadCompleted(object sender, NavigationEventArgs e)
        {
            ResourceResponded?.RaiseUIAsync(this, new ResourceRespondedResult(e.Uri?.AbsoluteUri ?? Address, ResourceRequestType.SubResource));
            IsLoading = false;
            FrameLoadEnd?.RaiseUIAsync(this, e.Uri?.AbsoluteUri ?? Address);
            LoadingStateChanged?.RaiseUIAsync(this, new LoadingStateResult(IsLoading, null));
            //TODO: Include http status code in result.

            try
            {
                string Icon = Browser.InvokeScript("eval", Scripts.GetFaviconScript).ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(Icon))
                    FaviconChanged?.RaiseUIAsync(this, Icon);
            }
            catch { }
        }

        public WebEngineType Engine => WebEngineType.Trident;

        private string? OverrideAddress = null;
        public string Address
        {
            get => string.IsNullOrEmpty(OverrideAddress) ? (Browser.Source?.AbsoluteUri ?? InitialUrls.Last()) : OverrideAddress;
            set => Navigate(value);
        }
        public string Title { get; private set; } = string.Empty;

        public bool CanGoBack => Browser.CanGoBack;
        public bool CanGoForward => Browser.CanGoForward;
        public bool CanReload => !IsLoading;
        public bool IsLoading { get; private set; }
        public bool IsBrowserInitialized { get; private set; }

        public bool IsSecure { get; private set; }
        public bool AudioPlaying { get; private set; }
        public bool IsMuted
        {
            get { return false; }
            set { }
        }

        private double _ZoomFactor = 1;
        public double ZoomFactor
        {
            get => _ZoomFactor;
            set
            {
                _ZoomFactor = value;
                Zoom(value);
            }
        }

        void Zoom(double Zoom)
        {
            try
            {
                //WARNING: Do not remove Int32
                object ZoomLevel = (Int32)(Zoom * 100);
                SafeExecWB(SHDocVw.OLECMDID.OLECMDID_OPTICAL_ZOOM, SHDocVw.OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, ZoomLevel);
                //https://weblog.west-wind.com/posts/2016/aug/22/detecting-and-setting-zoom-level-in-the-wpf-webbrowser-control
                /*var wb = (dynamic)Browser.GetType().GetField("_axIWebBrowser2",
          BindingFlags.Instance | BindingFlags.NonPublic)
          .GetValue(Browser);
                int zoomLevel = 100; // Between 10 and 1000
                wb.ExecWB(63, 2, zoomLevel, ref zoomLevel);   // OLECMDID_OPTICAL_ZOOM (63) - don't prompt (2)*/
            }
            catch { }
        }

        private void SafeExecWB(SHDocVw.OLECMDID CommandID, SHDocVw.OLECMDEXECOPT ExecOpt = SHDocVw.OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER, object? Input = default, IntPtr Output = default)
        {
            try
            {
                if (Input == null)
                    BrowserCore?.ExecWB(CommandID, ExecOpt);
                else
                    BrowserCore?.ExecWB(CommandID, ExecOpt, ref Input, Output);
            }
            catch { }
        }
        //https://github.com/tpn/winsdk-10/blob/9b69fd26ac0c7d0b83d378dba01080e93349c2ed/Include/10.0.10240.0/um/ExDisp.h#L375
        public static class Navigate2Flags
        {
            public const int navOpenInNewWindow = 0x01;
            public const int navNoHistory = 0x02;
            public const int navNoReadFromCache = 0x04;
            public const int navNoWriteToCache = 0x08;
            public const int navAllowAutosearch = 0x10;
            public const int navBrowserBar = 0x20;
            public const int navHyperlink = 0x40;
            public const int navEnforceRestricted = 0x80;
            public const int navNewWindowsManaged = 0x0100;
            public const int navUntrustedForDownload = 0x0200;
            public const int navTrustedForActiveX = 0x0400;
            public const int navOpenInNewTab = 0x0800;
            public const int navOpenInBackgroundTab = 0x1000;
            public const int navKeepWordWheelText = 0x2000;
            public const int navVirtualTab = 0x4000;
            public const int navBlockRedirectsXDomain = 0x8000;
            public const int navOpenNewForegroundTab = 0x10000;
        }
        //about:blank doesn't work?
        public void Navigate(string Url)
        {
            try
            {
                if (Settings.Private && BrowserCore != null)
                {
                    object flags = Navigate2Flags.navNoHistory | Navigate2Flags.navNoReadFromCache | Navigate2Flags.navNoWriteToCache;
                    object targetFrame = Type.Missing;
                    object postData = Type.Missing;
                    object headers = Type.Missing;
                    BrowserCore.Navigate(Url, ref flags, ref targetFrame, ref postData, ref headers);
                }
                else
                    Browser?.Navigate(Url);
            }
            catch { }
        }
        public void Back() { if (CanGoBack) Browser?.GoBack(); }
        public void Forward() { if (CanGoForward) Browser?.GoForward(); }
        public void Refresh(bool IgnoreCache = false, bool ClearCache = false) { if (string.IsNullOrEmpty(OverrideAddress)) Browser?.Refresh(); }
        public void Stop() => AxBrowser.Stop();
        public void Print() => SafeExecWB(SHDocVw.OLECMDID.OLECMDID_PRINTPREVIEW, SHDocVw.OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER);
        public void Find(string Text, bool Forward, bool MatchCase, bool FindNext) { }
        public void StopFind() { }
        public void SaveAs() => SafeExecWB(SHDocVw.OLECMDID.OLECMDID_SAVEAS, SHDocVw.OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER);

        public void Cut() => Browser.InvokeScript("execCommand", "cut", false, null);
        public void Copy() => Browser.InvokeScript("execCommand", "copy", false, null);
        public void Paste() => Browser.InvokeScript("execCommand", "paste", false, null);
        public void Delete() => Browser.InvokeScript("execCommand", "delete", false, null);
        public void SelectAll() => Browser.InvokeScript("execCommand", "undo", false, null);
        public void Undo() => Browser.InvokeScript("execCommand", "redo", false, null);
        public void Redo() => Browser.InvokeScript("execCommand", "selectAll", false, null);

        public event EventHandler AudioPlayingChanged;
        public event EventHandler<bool> FullscreenChanged;
        public event EventHandler<ScriptDialogEventArgs> ScriptDialogOpened;
        public event EventHandler<BeforeNavigationEventArgs> BeforeNavigation;
        public event EventHandler<NewTabRequestEventArgs> NewTabRequested;
        public event EventHandler<string> FrameLoadStart;
        public event EventHandler<string> FrameLoadEnd;
        public event EventHandler IsBrowserInitializedChanged;
        public event EventHandler<LoadingStateResult> LoadingStateChanged;
        public event EventHandler<string> TitleChanged;
        public event EventHandler<string> StatusMessage;
        public event EventHandler<string> FaviconChanged;
        public event EventHandler<FindResult> FindResult;
        public event EventHandler<string> JavaScriptMessageReceived;
        public event EventHandler<ResourceRequestEventArgs> ResourceRequested;
        public event EventHandler<ResourceRespondedResult> ResourceResponded;
        public event EventHandler<ResourceLoadedResult> ResourceLoaded;
        public event EventHandler<PermissionRequestedEventArgs> PermissionRequested;

        public event EventHandler<WebContextMenuEventArgs> ContextMenuRequested;
        public event EventHandler<WebAuthenticationRequestedEventArgs> AuthenticationRequested;
        public event EventHandler<ExternalProtocolEventArgs> ExternalProtocolRequested;
        public event EventHandler<NavigationErrorEventArgs> NavigationError;

        public void Download(string Url) => WebViewManager.DownloadManager.StartDownloadAsync(Url, string.Empty, WebViewManager.Settings.DownloadPrompt, "");

        public void ExecuteScript(string Script) { try { Browser?.InvokeScript("execScript", [Script, "JavaScript"]); } catch { } }

        public bool CanExecuteJavascript => Browser.Document != null;
        public Task<string?> EvaluateScriptAsync(string Script)
        {
            var Task = new TaskCompletionSource<string?>();

            Browser?.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var Result = Browser?.InvokeScript("eval", [Script])?.ToString();
                    Task.SetResult(Result);
                }
                catch (Exception Error)
                {
                    Task.SetException(Error);
                }
            });

            return Task.Task;
        }

        public async Task<byte[]> TakeScreenshotAsync(WebScreenshotFormat Format, Size? Viewport = null)
        {
            var HWND = Browser.Handle;
            if (HWND == IntPtr.Zero) return Array.Empty<byte>();

            var Width = (int)(Viewport?.Width ?? Browser.ActualWidth);
            var Height = (int)(Viewport?.Height ?? Browser.ActualHeight);

            var hdcSrc = DllUtils.GetWindowDC(HWND);
            var hdcDest = DllUtils.CreateCompatibleDC(hdcSrc);
            var hBitmap = DllUtils.CreateCompatibleBitmap(hdcSrc, Width, Height);
            var hOld = DllUtils.SelectObject(hdcDest, hBitmap);
            DllUtils.BitBlt(hdcDest, 0, 0, Width, Height, hdcSrc, 0, 0, DllUtils.SRCCOPY);
            DllUtils.SelectObject(hdcDest, hOld);
            DllUtils.DeleteDC(hdcDest);
            DllUtils.ReleaseDC(HWND, hdcSrc);

            var BitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DllUtils.DeleteObject(hBitmap);

            PngBitmapEncoder Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(BitmapSource));
            using MemoryStream Stream = new MemoryStream();
            Encoder.Save(Stream);
            return Stream.ToArray();
        }
        public async Task<string> GetSourceAsync() => await EvaluateScriptAsync("document.documentElement.outerHTML") ?? string.Empty;
        public async Task<string> CallDevToolsAsync(string Method, object? Parameters = null) => string.Empty;

        public FrameworkElement Control => Browser;

        private bool _Disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool Disposing)
        {
            if (_Disposed) return;
            if (Disposing)
            {
                WebViewManager.WebViews.Remove(this);
                Browser.Navigating -= Navigating;
                Browser.LoadCompleted -= LoadCompleted;
                try
                {
                    Browser.Navigate("about:blank");
                    Browser.Source = null;
                }
                catch { }
                if (Browser.Document != null)
                {
                    try { Marshal.FinalReleaseComObject(Browser.Document); }
                    catch { }
                }
                if (Browser.Parent is Panel Parent)
                    Parent.Children.Remove(Browser);
                Browser = null;
            }
            _Disposed = true;
        }
    }
}
