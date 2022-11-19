using CefSharp;
using SLBr.Controls;
using System;
using System.Windows;

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
                foreach (string _Permission in requestedPermissions.ToString().Replace(" ", "").Split(','))
                {
                    if (_Permission == "VideoCapture")
                    {
                        Permissions += "Use your camera";
                        PermissionIcons += "\xE714";
                    }
                    else if (_Permission == "AudioCapture")
                    {
                        Permissions += "Use your microphone";
                        PermissionIcons += "\xE720";
                    }
                    else if (_Permission == "DesktopVideoCapture")
                    {
                        Permissions += "Share your screen";
                        PermissionIcons += "\xE7F4";
                    }
                    else if (_Permission == "DesktopAudioCapture")
                    {
                        Permissions += "Capture desktop audio";
                        PermissionIcons += "\xE7F3";
                    }
                    Permissions += "\n";
                    PermissionIcons += "\n";
                }
                Permissions = Permissions.TrimEnd('\n');
                PermissionIcons = PermissionIcons.TrimEnd('\n');
                if (string.IsNullOrEmpty(Permissions))
                    Permissions = requestedPermissions.ToString();
                //Use your camera [VideoCapture]
                //Use your microphone [AudioCapture]
                //Use your desktop camera [DesktopAudioCapture]
                //Use your desktop microphone [DesktopVideoCapture]
                var infoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(requestingOrigin)} to", Permissions, "Allow", "Block", PermissionIcons);
                infoWindow.Topmost = true;

                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(requestedPermissions);
                    return true;
                }
                return false;
                //System.Diagnostics.Debug.WriteLine($"{promptId}|{requestedPermissions} {requestingOrigin}");
            }
        }

        protected override bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
        {
            //Know your location [Geolocation] https://github.com/cefsharp/CefSharp/discussions/3719
            //Use your MIDI devices [MidiSysex]
            //Know when you're actively using this device [IdleDetection]
            //Send desktop notifications [Notifications]
            //Store files on this device [StorageAccess]

            using (callback)
            {
                string Permissions = "";
                string PermissionIcons = "";
                foreach (string _Permission in requestedPermissions.ToString().Replace(" ", "").Split(','))
                {
                    if (_Permission == "Geolocation")
                    {
                        Permissions += "Know your location";
                        PermissionIcons += "\xECAF";//E819
                    }
                    else if (_Permission == "IdleDetection")
                    {
                        Permissions += "Know when you're actively using this device";
                        PermissionIcons += "\xEA6C";
                    }
                    else if (_Permission == "StorageAccess")
                    {
                        Permissions += "Store files on this device";
                        PermissionIcons += "\xE8B7";
                    }
                    else if (_Permission == "Notifications")
                    {
                        Permissions += "Show desktop notifications";
                        PermissionIcons += "\xEA8f";
                    }
                    else if (_Permission == "MidiSysex")
                    {
                        Permissions += "Use your MIDI devices";
                        PermissionIcons += "\xEC4F";
                    }
                    else if (_Permission == "ArSession")
                    {
                        Permissions += "Use your camera to create a 3D map of your surroundings";
                        //PermissionIcons += "\xE81E";
                        PermissionIcons += "\xE809";
                    }
                    else if (_Permission == "VrSession")
                    {
                        Permissions += "Use your virtual reality devices";
                        PermissionIcons += "\xEC94";
                    }
                    else if (_Permission == "MultipleDownloads")
                    {
                        Permissions += "Download multiple files automatically";
                        PermissionIcons += "\xE896";
                    }
                    else if (_Permission == "Clipboard")
                    {
                        Permissions += "See text and images copied to the clipboard";
                        PermissionIcons += "\xE896";
                    }
                    //f158
                    Permissions += "\n";
                    PermissionIcons += "\n";
                }
                Permissions = Permissions.TrimEnd('\n');
                PermissionIcons = PermissionIcons.TrimEnd('\n');
                if (string.IsNullOrEmpty(Permissions))
                    Permissions = requestedPermissions.ToString();
                var infoWindow = new InformationDialogWindow("Permission", $"Allow {Utils.Host(requestingOrigin)} to", Permissions, "Allow", "Block", PermissionIcons);
                infoWindow.Topmost = true;

                if (infoWindow.ShowDialog() == true)
                {
                    callback.Continue(PermissionRequestResult.Accept);
                    return true;
                }
                return false;
                //System.Diagnostics.Debug.WriteLine($"{promptId}|{requestedPermissions} {requestingOrigin}");
            }
        }
    }
}
