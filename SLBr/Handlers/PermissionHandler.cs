using CefSharp;
using SLBr.Controls;

namespace SLBr.Handlers
{
    public class PermissionHandler : CefSharp.Handler.PermissionHandler
    {
        protected override void OnDismissPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, PermissionRequestResult result)
        {
            base.OnDismissPermissionPrompt(chromiumWebBrowser, browser, promptId, result);
        }

        protected override bool OnRequestMediaAccessPermission(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string requestingOrigin, MediaAccessPermissionType requestedPermissions, IMediaAccessCallback callback)
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

        protected override bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
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
                            case PermissionRequestType.MultipleDownloads:
                                Permissions += "Download multiple files automatically";
                                PermissionIcons += "\xE896";
                                break;
                            case PermissionRequestType.CameraStream:
                                Permissions += "Use your camera";
                                PermissionIcons += "\xE714";
                                break;
                            case PermissionRequestType.MicStream:
                                Permissions += "Use your microphone";
                                PermissionIcons += "\xE720";
                                break;
                            case PermissionRequestType.CameraPanTiltZoom:
                                PermissionIcons += "\xE89E";
                                break;
                            case PermissionRequestType.Notifications:
                                Permissions += "Show desktop notifications";
                                PermissionIcons += "\xEA8F";
                                break;

                            case PermissionRequestType.Geolocation:
                                Permissions += "Know your location";
                                PermissionIcons += "\xECAF";
                                break;
                            case PermissionRequestType.WindowPlacement:
                                PermissionIcons += "\xE737";
                                break;

                            case PermissionRequestType.ProtectedMediaIdentifier:
                                Permissions += "-";
                                PermissionIcons += "\xEA69";
                                break;
                            case PermissionRequestType.MidiSysex:
                                Permissions += "Use your MIDI devices";
                                PermissionIcons += "\xEC4F";
                                break;

                            case PermissionRequestType.StorageAccess:
                                Permissions += "Store files on this device";
                                PermissionIcons += "\xE8B7";
                                break;
                            case PermissionRequestType.DiskQuota:
                                break;
                            case PermissionRequestType.LocalFonts:
                                Permissions += "-";
                                PermissionIcons += "\xE8D2";
                                break;
                            case PermissionRequestType.Clipboard:
                                Permissions += "See text and images copied to the clipboard";
                                PermissionIcons += "\xF0E3";
                                break;

                            case PermissionRequestType.VrSession:
                                Permissions += "Use your virtual reality devices";
                                PermissionIcons += "\xEC94";
                                break;
                            case PermissionRequestType.ArSession:
                                Permissions += "Use your camera to create a 3D map of your surroundings";
                                PermissionIcons += "\xE809";
                                break;

                            case PermissionRequestType.U2FApiRequest:
                                break;

                            case PermissionRequestType.IdleDetection:
                                Permissions += "Know when you're actively using this device";
                                PermissionIcons += "\xEA6C";
                                break;
                            case PermissionRequestType.RegisterProtocolHandler:
                                Permissions += "Open web links";
                                PermissionIcons += "\xE71B";
                                break;
                            case PermissionRequestType.AccessibilityEvents:
                                break;
                            case PermissionRequestType.SecurityAttestation:
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
