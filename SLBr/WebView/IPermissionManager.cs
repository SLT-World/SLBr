/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using CefSharp;
using Microsoft.Web.WebView2.Core;

namespace SLBr.WebView
{
    //WARNING: Do not mix with WebPermissionKind. Certain types are not unsupported in WebPermissionKind.
    public enum WebPermissionType
    {
        Cookies,
        Images,
        JavaScript,
        Popups,
        Geolocation,
        Notifications,
        MixedScript,
        MediaStreamMic,
        MediaStreamCamera,
        ProtocolHandlers,
        AutomaticDownloads,
        MidiSysex,
        ProtectedMediaIdentifier,
        DurableStorage,
        BluetoothGuard,
        BackgroundSync,
        Autoplay,
        Ads,
        Sound,
        Sensors,
        PaymentHandler,
        UsbGuard,
        IdleDetection,
        SerialGuard,
        BluetoothScanning,
        HidGuard,
        LegacyCookieAccess,
        FileSystemWriteGuard,
        Nfc,
        ClipboardReadWrite,
        Vr,
        Ar,
        FileSystemReadGuard,
        StorageAccess,
        CameraPanTiltZoom,
        WindowManagement,
        InsecurePrivateNetwork,
        LocalFonts,
        JavascriptJit,
        FederatedIdentityApi,
        PrivateNetworkGuard,
        TopLevelStorageAccess,
        FederatedIdentityAutoReauthnPermission,
        AntiAbuse,
        ThirdPartyStoragePartitioning,
        AutoPictureInPicture,
        FileSystemAccessExtendedPermission,
        FileSystemAccessRestorePermission,
        CapturedSurfaceControl,
        DirectSockets,
        KeyboardLock,
        PointerLock,
        JavascriptOptimizer,
        HandTracking,
        WebAppInstallation,
        DirectSocketsPrivateNetworkAccess,
    }

    public interface IPermissionManager
    {
        //Task<List<string>> GetAllSites();
        Task<(WebPermissionState, bool)> GetSetting(string Url, WebPermissionType Type);
        void SetSetting(string Url, WebPermissionType Type, WebPermissionState Value);
    }

    public class ChromiumPermissionManager(IRequestContext _Context) : IPermissionManager
    {
        IRequestContext Context = _Context;

        /*public async Task<List<string>> GetAllSites()
        {
            return await Cef.UIThreadTaskFactory.StartNew(delegate
            {
                HashSet<string> UniqueSites = [];
                object RawPreference = Context.GetPreference("profile.content_settings.exceptions");
                if (RawPreference is System.Dynamic.ExpandoObject Root)
                {
                    IDictionary<string, object> Exceptions = (IDictionary<string, object>)Root;
                    foreach (KeyValuePair<string, object> FeaturePair in Exceptions)
                    {
                        if (FeaturePair.Value is System.Dynamic.ExpandoObject FeatureExpando)
                        {
                            IDictionary<string, object> FeatureSites = (IDictionary<string, object>)FeatureExpando;
                            foreach (var SitePair in FeatureSites)
                            {
                                string SitePattern = SitePair.Key;
                                if (SitePattern == "*,*")
                                    continue;
                                UniqueSites.Add(CleanChromiumPattern(SitePattern));
                            }
                        }
                    }
                }
                return UniqueSites.ToList();
            });
        }

        private string CleanChromiumPattern(string Pattern)
        {
            if (string.IsNullOrEmpty(Pattern))
                return Pattern;
            string Primary = Pattern.Split(',')[0];
            Primary = Primary.Replace("[*.]", "");
            if (Primary.EndsWith(":443"))
                Primary = Primary[..^4];
            else if (Primary.EndsWith(":80"))
                Primary = Primary[..^3];
            return Primary;
        }*/

        public async Task<(WebPermissionState, bool)> GetSetting(string TopLevelUrl, WebPermissionType Type)
        {
            return await Cef.UIThreadTaskFactory.StartNew(delegate
            {
                return (Context.GetContentSetting(TopLevelUrl, TopLevelUrl, Type.ToContentSettingType()).ToWebPermissionValue(), true);
            });
        }

        public void SetSetting(string TopLevelUrl, WebPermissionType Type, WebPermissionState Value)
        {
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                Context.SetContentSetting(TopLevelUrl, TopLevelUrl, Type.ToContentSettingType(), Value.ToContentSettingValue());
            });
        }
    }

    public class ChromiumEdgePermissionManager(CoreWebView2 _Context) : IPermissionManager
    {
        CoreWebView2 Context = _Context;

        public async Task<(WebPermissionState, bool)> GetSetting(string TopLevelUrl, WebPermissionType Type)
        {
            bool IsSupported = true;
            WebPermissionState Value;
            CoreWebView2PermissionKind? CorePermissionKind = Type.ToWebView2PermissionKind();
            if (CorePermissionKind == null)
            {
                //NOTE: Unapplicable, global variable.
                /*if (Type == WebPermissionType.JavaScript)
                    Value = _Context.Settings.IsScriptEnabled ? WebPermissionState.Allow : WebPermissionState.Deny;
                else
                {*/
                IsSupported = false;
                Value = WebPermissionState.Default;
                //}
            }
            else
            {
                //NOTE: PermissionOrigin example: https://www.google.com/
                IReadOnlyList<CoreWebView2PermissionSetting> CorePermissionList = await Context.Profile.GetNonDefaultPermissionSettingsAsync();
                CoreWebView2PermissionSetting? CoreSetting = CorePermissionList.FirstOrDefault(i => i.PermissionOrigin == TopLevelUrl && i.PermissionKind == CorePermissionKind);
                if (CoreSetting == null)
                    Value = WebPermissionState.Default;
                else
                    Value = CoreSetting.PermissionState.ToWebPermissionState();
            }
            return (Value, IsSupported);
        }

        public void SetSetting(string TopLevelUrl, WebPermissionType Type, WebPermissionState Value)
        {
            CoreWebView2PermissionKind? CorePermissionKind = Type.ToWebView2PermissionKind();
            if (CorePermissionKind != null)
                Context.Profile.SetPermissionStateAsync(CorePermissionKind.Value, TopLevelUrl, Value.ToWebView2PermissionState());
            /*else
            {
                if (Type == WebPermissionType.JavaScript)
                    _Context.Settings.IsScriptEnabled = Value == WebPermissionState.Deny ? false : true;
            }*/
        }
    }

    public class TridentPermissionManager : IPermissionManager
    {
        public async Task<(WebPermissionState, bool)> GetSetting(string TopLevelUrl, WebPermissionType Type)
        {
            return (WebPermissionState.Default, false);
        }

        public void SetSetting(string TopLevelUrl, WebPermissionType Type, WebPermissionState Value)
        {
        }
    }
}
