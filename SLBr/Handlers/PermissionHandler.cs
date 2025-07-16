using CefSharp;
using SLBr.Controls;
using System.Windows;
using Windows.Devices.Geolocation;
using Windows.UI.Notifications;

namespace SLBr.Handlers
{
    public class PermissionHandler : IPermissionHandler
    {
        public void OnDismissPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, PermissionRequestResult result)
        {
        }

        public bool OnRequestMediaAccessPermission(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string requestingOrigin, MediaAccessPermissionType requestedPermissions, IMediaAccessCallback callback)
        {
            if (callback == null)
                return false;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }
                string Permissions = "";
                string PermissionIcons = "";
                foreach (MediaAccessPermissionType Option in Enum.GetValues(typeof(MediaAccessPermissionType)))
                {
                    if (requestedPermissions.HasFlag(Option) && Option != MediaAccessPermissionType.None)
                    {
                        switch (Option)
                        {
                            case MediaAccessPermissionType.VideoCapture:
                                Permissions += "Use your camera\n";
                                PermissionIcons += "\xE714\n";
                                break;
                            case MediaAccessPermissionType.AudioCapture:
                                Permissions += "Use your microphone\n";
                                PermissionIcons += "\xE720\n";
                                break;
                            case MediaAccessPermissionType.DesktopVideoCapture:
                                Permissions += "Share your screen\n";
                                PermissionIcons += "\xE7F4\n";
                                break;
                            case MediaAccessPermissionType.DesktopAudioCapture:
                                Permissions += "Capture desktop audio\n";
                                PermissionIcons += "\xE7F3\n";
                                break;
                        }
                    }
                }

                Permissions = Permissions.TrimEnd('\n');
                PermissionIcons = PermissionIcons.TrimEnd('\n');
                if (string.IsNullOrEmpty(Permissions))
                    Permissions = requestedPermissions.ToString();

                var InfoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(requestingOrigin)} to", Permissions, "\uE8D7", "Allow", "Block", PermissionIcons);
                InfoWindow.Topmost = true;

                bool? Result = InfoWindow.ShowDialog();

                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }

                if (Result == true)
                    callback.Continue(requestedPermissions);
                else
                    callback.Cancel();
                callback.Dispose();
            }));
            return true;
        }

        //CefSharp's PermissionRequestType enum isn't synced with CEF's
        //https://github.com/chromiumembedded/cef/blob/master/include/internal/cef_types.h
        //https://github.com/cefsharp/CefSharp/blob/master/CefSharp/Enums/PermissionRequestType.cs
        public enum FixedPermissionRequestType : uint 
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
            LocalNetworkAccess = 1 << 25
        }

        public bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
        {
            if (callback == null)
                return false;
            FixedPermissionRequestType _ProperPermissionRequestType = (FixedPermissionRequestType)(int)requestedPermissions;
            //Know your location [Geolocation] https://github.com/cefsharp/CefSharp/discussions/3719
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }
                string Permissions = "";
                string PermissionIcons = "";
                foreach (FixedPermissionRequestType Option in Enum.GetValues(typeof(FixedPermissionRequestType)))
                {
                    if (_ProperPermissionRequestType.HasFlag(Option) && Option != FixedPermissionRequestType.None)
                    {
                        switch (Option)
                        {
                            /*case ProperPermissionRequestType.AccessibilityEvents:
                                Permissions += "Respond to Accessibility Events";
                                break;*/
                            case FixedPermissionRequestType.ArSession:
                                Permissions += "Use your camera to create a 3D map of your surroundings";
                                PermissionIcons += "\xE809";
                                break;
                            case FixedPermissionRequestType.CameraPanTiltZoom:
                                Permissions += "Move your camera";
                                PermissionIcons += "\xE714";
                                break;
                            case FixedPermissionRequestType.CameraStream:
                                Permissions += "Use your camera";
                                PermissionIcons += "\xE714";
                                break;
                            case FixedPermissionRequestType.CapturedSurfaceControl:
                                Permissions += "Scroll and zoom the contents of your shared tab";
                                PermissionIcons += "\xec6c";
                                break;
                            case FixedPermissionRequestType.Clipboard:
                                Permissions += "See text and images in clipboard";
                                PermissionIcons += "\xF0E3";
                                break;
                            case FixedPermissionRequestType.TopLevelStorageAccess:
                                Permissions += "Access cookies and site data";
                                PermissionIcons += "\xE8B7";
                                break;
                            case FixedPermissionRequestType.DiskQuota:
                                Permissions += "Store files on this device";
                                PermissionIcons += "\xE8B7";
                                break;
                            case FixedPermissionRequestType.LocalFonts:
                                Permissions += "Use your computer fonts";
                                PermissionIcons += "\xE8D2";
                                break;
                            case FixedPermissionRequestType.Geolocation:
                                Permissions += "Know your location";
                                PermissionIcons += "\xECAF";
                                break;
                            case FixedPermissionRequestType.IdentityProvider:
                                Permissions += "Use your accounts to login to websites";
                                PermissionIcons += "\xef58";
                                break;
                            case FixedPermissionRequestType.IdleDetection:
                                Permissions += "Know when you're actively using this device";
                                PermissionIcons += "\xEA6C";
                                break;
                            case FixedPermissionRequestType.MicStream:
                                Permissions += "Use your microphone";
                                PermissionIcons += "\xE720";
                                break;
                            case FixedPermissionRequestType.MidiSysex:
                                Permissions += "Use your MIDI devices";
                                PermissionIcons += "\xEC4F";
                                break;
                            case FixedPermissionRequestType.MultipleDownloads:
                                Permissions += "Download multiple files";
                                PermissionIcons += "\xE896";
                                break;
                            case FixedPermissionRequestType.Notifications:
                                Permissions += "Show notifications";
                                PermissionIcons += "\xEA8F";
                                break;
                            case FixedPermissionRequestType.KeyboardLock:
                                Permissions += "Lock and use your keyboard";
                                PermissionIcons += "\xf26b";
                                break;
                            case FixedPermissionRequestType.PointerLock:
                                Permissions += "Lock and use your mouse";
                                PermissionIcons += "\xf271";
                                break;
                            case FixedPermissionRequestType.ProtectedMediaIdentifier:
                                Permissions += "Know your unique device identifier";
                                PermissionIcons += "\xef3f";
                                break;
                            case FixedPermissionRequestType.RegisterProtocolHandler:
                                Permissions += "Open web links";
                                PermissionIcons += "\xE71B";
                                break;
                            case FixedPermissionRequestType.StorageAccess:
                                Permissions += "Access cookies and site data";
                                PermissionIcons += "\xE8B7";
                                break;
                            case FixedPermissionRequestType.VrSession:
                                Permissions += "Use your virtual reality devices";
                                PermissionIcons += "\xEC94";
                                break;
                            case FixedPermissionRequestType.WindowManagement:
                                Permissions += "Manage windows on all your displays";
                                PermissionIcons += "\xE737";
                                break;
                            case FixedPermissionRequestType.FileSystemAccess:
                                Permissions += "Access file system";
                                PermissionIcons += "\xEC50";//E8B7
                                break;
                        }
                        Permissions += "\n";
                        PermissionIcons += "\n";
                    }
                }

                Permissions = Permissions.TrimEnd('\n');
                PermissionIcons = PermissionIcons.TrimEnd('\n');
                if (string.IsNullOrEmpty(Permissions))
                    Permissions = requestedPermissions.ToString();

                var InfoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(requestingOrigin)} to", Permissions, "\uE8D7", "Allow", "Block", PermissionIcons);
                InfoWindow.Topmost = true;

                bool? Result = InfoWindow.ShowDialog();

                if (chromiumWebBrowser.IsDisposed || !browser.IsValid)
                {
                    callback.Dispose();
                    return;
                }

                if (Result == true)
                    callback.Continue(PermissionRequestResult.Accept);
                else
                    callback.Continue(PermissionRequestResult.Deny);
                callback.Dispose();
            }));
            return true;
        }
    }
}
