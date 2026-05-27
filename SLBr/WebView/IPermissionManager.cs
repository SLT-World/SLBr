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
        Task<(WebPermissionState, bool)> GetSetting(string Url, WebPermissionType Type);
        void SetSetting(string Url, WebPermissionType Type, WebPermissionState Value);
    }

    public class ChromiumPermissionManager(IRequestContext _Context) : IPermissionManager
    {
        IRequestContext Context = _Context;

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
                IsSupported = false;
                Value = WebPermissionState.Default;
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
