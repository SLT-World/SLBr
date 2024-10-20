using CefSharp;
using SLBr.Controls;
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
            using (callback)
            {
                string Permissions = "";
                string PermissionIcons = "";
                foreach (MediaAccessPermissionType option in Enum.GetValues(typeof(MediaAccessPermissionType)))
                {
                    if (requestedPermissions.HasFlag(option) && option != MediaAccessPermissionType.None)
                    {
                        switch (option)
                        {
                            case MediaAccessPermissionType.VideoCapture:
                                Permissions += "Use your camera";
                                PermissionIcons += "\xE714";
                                break;
                            case MediaAccessPermissionType.AudioCapture:
                                Permissions += "Use your microphone";
                                PermissionIcons += "\xE720";
                                break;
                            case MediaAccessPermissionType.DesktopVideoCapture:
                                Permissions += "Share your screen";
                                PermissionIcons += "\xE7F4";
                                break;
                            case MediaAccessPermissionType.DesktopAudioCapture:
                                Permissions += "Capture desktop audio";
                                PermissionIcons += "\xE7F3";
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
                var infoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(requestingOrigin)} to", Permissions, "\uE8D7", "Allow", "Block", PermissionIcons);
                infoWindow.Topmost = true;

                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(requestedPermissions);
                    return true;
                }
                return false;
            }
        }

        public bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
        {
            //Know your location [Geolocation] https://github.com/cefsharp/CefSharp/discussions/3719
            using (callback)
            {
                string Permissions = "";
                string PermissionIcons = "";
                foreach (PermissionRequestType option in Enum.GetValues(typeof(PermissionRequestType)))
                {
                    if (requestedPermissions.HasFlag(option) && option != PermissionRequestType.None)
                    {
                        switch (option)
                        {
                            case PermissionRequestType.AccessibilityEvents:
                                Permissions += "Respond to Accessibility Events";
                                break;
                            case PermissionRequestType.ArSession:
                                Permissions += "Use your camera to create a 3D map of your surroundings";
                                PermissionIcons += "\xE809";
                                break;
                            case PermissionRequestType.CameraPanTiltZoom:
                                Permissions += "Move your camera";
                                PermissionIcons += "\xE714";
                                break;
                            case PermissionRequestType.CameraStream:
                                Permissions += "Use your camera";
                                PermissionIcons += "\xE714";
                                break;
                            case PermissionRequestType.CapturedSurfaceControl:
                                Permissions += "Scroll and zoom the contents of your shared tab";
                                PermissionIcons += "\xec6c";
                                break;
                            case PermissionRequestType.Clipboard:
                                Permissions += "See text and images in clipboard";
                                PermissionIcons += "\xF0E3";
                                break;
                            case PermissionRequestType.TopLevelStorageAccess:
                                Permissions += "Access cookies and site data";
                                PermissionIcons += "\xE8B7";
                                break;
                            case PermissionRequestType.DiskQuota:
                                Permissions += "Store files on this device";
                                PermissionIcons += "\xE8B7";
                                break;
                            case PermissionRequestType.LocalFonts:
                                Permissions += "Use your computer fonts";
                                PermissionIcons += "\xE8D2";
                                break;
                            case PermissionRequestType.Geolocation:
                                Permissions += "Know your location";
                                PermissionIcons += "\xECAF";
                                break;
                            case PermissionRequestType.Identity_Provider:
                                Permissions += "Use your accounts to login to websites";
                                PermissionIcons += "\xef58";
                                break;
                            case PermissionRequestType.IdleDetection:
                                Permissions += "Know when you're actively using this device";
                                PermissionIcons += "\xEA6C";
                                break;
                            case PermissionRequestType.MicStream:
                                Permissions += "Use your microphone";
                                PermissionIcons += "\xE720";
                                break;
                            case PermissionRequestType.MidiSysex:
                                Permissions += "Use your MIDI devices";
                                PermissionIcons += "\xEC4F";
                                break;
                            case PermissionRequestType.MultipleDownloads:
                                Permissions += "Download multiple files";
                                PermissionIcons += "\xE896";
                                break;
                            case PermissionRequestType.Notifications:
                                Permissions += "Show notifications";
                                PermissionIcons += "\xEA8F";
                                break;
                            case PermissionRequestType.KeyboardLock:
                                Permissions += "Lock and use your keyboard";
                                PermissionIcons += "\xf26b";
                                break;
                            case PermissionRequestType.PointerLock:
                                Permissions += "Lock and use your mouse";
                                PermissionIcons += "\xf271";
                                break;
                            case PermissionRequestType.ProtectedMediaIdentifier:
                                Permissions += "Know your unique device identifier";
                                PermissionIcons += "\xef3f";
                                break;
                            case PermissionRequestType.RegisterProtocolHandler:
                                Permissions += "Open web links";
                                PermissionIcons += "\xE71B";
                                break;
                            case PermissionRequestType.StorageAccess:
                                Permissions += "Access cookies and site data";
                                PermissionIcons += "\xE8B7";
                                break;
                            case PermissionRequestType.VrSession:
                                Permissions += "Use your virtual reality devices";
                                PermissionIcons += "\xEC94";
                                break;
                            case PermissionRequestType.WindowManagement:
                                Permissions += "Manage windows on all your displays";
                                PermissionIcons += "\xE737";
                                break;
                            case PermissionRequestType.FileSystemAccess:
                                Permissions += "FileSystemAccess";
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
                var infoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(requestingOrigin)} to", Permissions, "\uE8D7", "Allow", "Block", PermissionIcons);
                infoWindow.Topmost = true;

                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(PermissionRequestResult.Accept);
                    return true;
                }
                return false;
            }
        }
    }
}
