using CefSharp;
using CefSharp.BrowserSubprocess;
using CefSharp.Wpf.HwndHost;
using SLBr.Controls;
using SLBr.Handlers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Windows;

namespace SLBr
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            //https://github.com/dotnet/runtime/issues/93914
            //https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector
            /*Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
            Environment.SetEnvironmentVariable("DOTNET_GCHeapCount", "16");
            Environment.SetEnvironmentVariable("DOTNET_GCConserveMemory", "5");*/

            /*string Username = "Default";
            IEnumerable<string> Args = Environment.GetCommandLineArgs().Skip(1);
            foreach (string Flag in Args)
            {
                if (Flag.StartsWith("--user=", StringComparison.Ordinal))
                {
                    Username = Flag.Replace("--user=", string.Empty).Replace(" ", "-");
                    System.Windows.MessageBox.Show(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", Username, "Save.bin")).Contains("Performance<:>2").ToString());
                }
            }
            */
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
            MinimizeMemory();
            if (args.Length > 0 && args[0].StartsWith("--type=", StringComparison.Ordinal))
                return SelfHost.Main(args);
            else if (args.Length > 0 && args[0].StartsWith("--app=", StringComparison.Ordinal))
            {
                BackgroundWorker Worker = new BackgroundWorker();
                Worker.DoWork += Worker_DoWork;
                Worker.RunWorkerAsync();

                CefSettings Settings = new CefSettings();
                Settings.BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName;

                string UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", "Default");

                string UserDataPath = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "User Data"));
                Settings.LogFile = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "Errors.log"));
                Settings.LogSeverity = LogSeverity.Error;
                Settings.CachePath = Path.GetFullPath(Path.Combine(UserDataPath, "Cache"));
                Settings.RootCachePath = UserDataPath;

                Settings.AddNoErrorFlag("enable-tls13-early-data");

                Settings.AddNoErrorFlag("reduce-accept-language");

                Settings.AddNoErrorFlag("enable-quic");
                Settings.AddNoErrorFlag("enable-spdy4");
                Settings.AddNoErrorFlag("enable-ipv6");

                Settings.AddNoErrorFlag("no-proxy-server");
                Settings.AddNoErrorFlag("no-pings");

                Settings.AddNoErrorFlag("disable-background-networking");
                Settings.AddNoErrorFlag("disable-component-extensions-with-background-pages");

                Settings.AddNoErrorFlag("disable-translate");
                Settings.AddNoErrorFlag("disable-variations-seed-fetch");

                Settings.AddNoErrorFlag("no-default-browser-check");
                Settings.AddNoErrorFlag("no-first-run");
                Settings.AddNoErrorFlag("disable-domain-reliability");


                Settings.AddNoErrorFlag("disable-chrome-tracing-computation");
                Settings.AddNoErrorFlag("disable-default-apps");

                Settings.AddNoErrorFlag("disable-modal-animations");

                Settings.AddNoErrorFlag("no-network-profile-warning");

                Settings.AddNoErrorFlag("disable-login-animations");
                Settings.AddNoErrorFlag("disable-stack-profiler");
                Settings.AddNoErrorFlag("disable-system-font-check");
                Settings.AddNoErrorFlag("disable-breakpad");
                Settings.AddNoErrorFlag("disable-crash-reporter");
                Settings.AddNoErrorFlag("disable-crashpad-forwarding");

                Settings.AddNoErrorFlag("disable-top-sites");
                Settings.AddNoErrorFlag("no-service-autorun");
                Settings.AddNoErrorFlag("disable-auto-reload");
                Settings.AddNoErrorFlag("disable-dinosaur-easter-egg");

                Settings.AddNoErrorFlag("wm-window-animations-disabled");
                Settings.AddNoErrorFlag("animation-duration-scale", "0");
                Settings.AddNoErrorFlag("disable-histogram-customizer");

                Settings.AddNoErrorFlag("suppress-message-center-popups");
                Settings.AddNoErrorFlag("disable-prompt-on-repost");
                Settings.AddNoErrorFlag("propagate-iph-for-testing");
                Settings.AddNoErrorFlag("disable-search-engine-choice-screen");
                Settings.AddNoErrorFlag("ash-no-nudges");
                Settings.AddNoErrorFlag("noerrdialogs");
                Settings.AddNoErrorFlag("disable-notifications");

                Settings.AddNoErrorFlag("connectivity-check-url", "https://cp.cloudflare.com/generate_204");
                Settings.AddNoErrorFlag("sync-url", "dummy.invalid");
                Settings.AddNoErrorFlag("gaia-url", "dummy.invalid");
                Settings.AddNoErrorFlag("gcm-checkin-url", "dummy.invalid");
                Settings.AddNoErrorFlag("gcm-mcs-endpoint", "dummy.invalid");
                Settings.AddNoErrorFlag("gcm-registration-url", "dummy.invalid");
                Settings.AddNoErrorFlag("google-url", "dummy.invalid");
                Settings.AddNoErrorFlag("google-apis-url", "dummy.invalid");
                Settings.AddNoErrorFlag("google-base-url", "dummy.invalid");
                Settings.AddNoErrorFlag("lso-url", "dummy.invalid");
                Settings.AddNoErrorFlag("model-quality-service-url", "dummy.invalid");
                Settings.AddNoErrorFlag("oauth-account-manager-url", "dummy.invalid");
                Settings.AddNoErrorFlag("secure-connect-api-url", "dummy.invalid");
                Settings.AddNoErrorFlag("variations-server-url", "dummy.invalid");
                Settings.AddNoErrorFlag("variations-insecure-server-url", "dummy.invalid");
                Settings.AddNoErrorFlag("device-management-url", "dummy.invalid");
                Settings.AddNoErrorFlag("realtime-reporting-url", "dummy.invalid");
                Settings.AddNoErrorFlag("encrypted-reporting-url", "dummy.invalid");
                Settings.AddNoErrorFlag("file-storage-server-upload-url", "dummy.invalid");
                Settings.AddNoErrorFlag("google-doodle-url", "dummy.invalid");
                Settings.AddNoErrorFlag("third-party-doodle-url", "dummy.invalid");
                Settings.AddNoErrorFlag("search-provider-logo-url", "dummy.invalid");
                Settings.AddNoErrorFlag("translate-script-url", "dummy.invalid");
                Settings.AddNoErrorFlag("autofill-server-url", "dummy.invalid");
                Settings.AddNoErrorFlag("override-metrics-upload-url", "dummy.invalid");
                Settings.AddNoErrorFlag("crash-server-url", "dummy.invalid");

                Settings.AddNoErrorFlag("dark-mode-settings", "ImagePolicy=1");
                Settings.AddNoErrorFlag("process-per-site");
                Settings.AddNoErrorFlag("password-store", "basic");
                Settings.AddNoErrorFlag("animated-image-resume");
                Settings.AddNoErrorFlag("disable-image-animation-resync");
                Settings.AddNoErrorFlag("disable-checker-imaging");
                Settings.AddNoErrorFlag("enable-speech-input");
                Settings.AddNoErrorFlag("enable-usermedia-screen-capturing");
                Settings.AddNoErrorFlag("auto-select-desktop-capture-source", "Entire screen");
                Settings.AddNoErrorFlag("disable-fetching-hints-at-navigation-start");
                Settings.AddNoErrorFlag("disable-model-download-verification");
                Settings.AddNoErrorFlag("disable-component-update");
                Settings.AddNoErrorFlag("component-updater", "disable-background-downloads,disable-delta-updates");
                Settings.AddNoErrorFlag("enable-parallel-downloading");

                string EnableFeatures = "HeapProfilerReporting,ReducedReferrerGranularity,ThirdPartyStoragePartitioning,PrecompileInlineScripts,OptimizeHTMLElementUrls,UseEcoQoSForBackgroundProcess,EnableLazyLoadImageForInvisiblePage,ParallelDownloading,TrackingProtection3pcd,LazyBindJsInjection,SkipUnnecessaryThreadHopsForParseHeaders,SimplifyLoadingTransparentPlaceholderImage,OptimizeLoadingDataUrls,ThrottleUnimportantFrameTimers,Prerender2MemoryControls,PrefetchPrivacyChanges,DIPS,LightweightNoStatePrefetch,BackForwardCacheMemoryControls,ClearCanvasResourcesInBackground,Canvas2DReclaimUnusedResources,EvictionUnlocksResources,SpareRendererForSitePerProcess,ReduceSubresourceResponseStartedIPC";
                string DisableFeatures = "LensOverlay,KAnonymityService,NetworkTimeServiceQuerying,LiveCaption,DefaultWebAppInstallation,PersistentHistograms,Translate,InterestFeedContentSuggestions,CertificateTransparencyComponentUpdater,AutofillServerCommunication,AcceptCHFrame,PrivacySandboxSettings4,ImprovedCookieControls,GlobalMediaControls,HardwareMediaKeyHandling,PrivateAggregationApi,PrintCompositorLPAC,CrashReporting,SegmentationPlatform,InstalledApp,BrowsingTopics,Fledge,FledgeBiddingAndAuctionServer,InterestFeedContentSuggestions,OptimizationHintsFetchingSRP,OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints";
                string EnableBlinkFeatures = "UnownedAnimationsSkipCSSEvents,StaticAnimationOptimization,PageFreezeOptIn,FreezeFramesOnVisibility";
                string DisableBlinkFeatures = "DocumentWrite,LanguageDetectionAPI,DocumentPictureInPictureAPI";

                Settings.AddNoErrorFlag("disable-features", DisableFeatures);
                Settings.AddNoErrorFlag("enable-features", EnableFeatures);
                Settings.AddNoErrorFlag("enable-blink-features", EnableBlinkFeatures);
                Settings.AddNoErrorFlag("disable-blink-features", DisableBlinkFeatures);

                CefSharpSettings.RuntimeStyle = CefRuntimeStyle.Alloy;
                Cef.Initialize(Settings);

                Application CleanApp = new Application();
                string AppsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SLBr", "Apps");
                string ID = args[0].Substring("--app=".Length).Trim('"');
                string ManifestPath = Path.Combine(AppsFolder, $"{ID}.json");

                WebAppManifest? Manifest = WebAppHandler.LoadManifest(File.ReadAllText(ManifestPath));
                WebAppWindow Window = new WebAppWindow(Manifest);
                CleanApp.Run(Window);

                Cef.Shutdown();
                return Environment.ExitCode;
            }
            else
            {
                //DispatcherTimer FlushTimer = new DispatcherTimer();
                //FlushTimer.Interval = new TimeSpan(500);
                //FlushTimer.Tick += FlushTimer_Tick;
                BackgroundWorker Worker = new BackgroundWorker();
                Worker.DoWork += Worker_DoWork;
                Worker.RunWorkerAsync();
                App.Main();

                Cef.Shutdown();
                return Environment.ExitCode;
            }

            /*BackgroundWorker Worker = new BackgroundWorker();
            Worker.DoWork += Worker_DoWork;
            Worker.RunWorkerAsync();

            WebViewSettings Settings = new WebViewSettings();
            Settings.RegisterProtocol("gemini", WebViewManager.GeminiHandler);
            Settings.RegisterProtocol("gopher", WebViewManager.GopherHandler);
            Settings.RegisterProtocol("slbr", WebViewManager.SLBrHandler);

            string EnableFeatures = "HeapProfilerReporting,ReducedReferrerGranularity,ThirdPartyStoragePartitioning,PrecompileInlineScripts,OptimizeHTMLElementUrls,UseEcoQoSForBackgroundProcess,EnableLazyLoadImageForInvisiblePage,ParallelDownloading,TrackingProtection3pcd,LazyBindJsInjection,SkipUnnecessaryThreadHopsForParseHeaders,SimplifyLoadingTransparentPlaceholderImage,OptimizeLoadingDataUrls,ThrottleUnimportantFrameTimers,Prerender2MemoryControls,PrefetchPrivacyChanges,DIPS,LightweightNoStatePrefetch,BackForwardCacheMemoryControls,ClearCanvasResourcesInBackground,Canvas2DReclaimUnusedResources,EvictionUnlocksResources,SpareRendererForSitePerProcess,ReduceSubresourceResponseStartedIPC";
            string DisableFeatures = "LensOverlay,KAnonymityService,NetworkTimeServiceQuerying,LiveCaption,DefaultWebAppInstallation,PersistentHistograms,Translate,InterestFeedContentSuggestions,CertificateTransparencyComponentUpdater,AutofillServerCommunication,AcceptCHFrame,PrivacySandboxSettings4,ImprovedCookieControls,GlobalMediaControls,HardwareMediaKeyHandling,PrivateAggregationApi,PrintCompositorLPAC,CrashReporting,SegmentationPlatform,InstalledApp,BrowsingTopics,Fledge,FledgeBiddingAndAuctionServer,InterestFeedContentSuggestions,OptimizationHintsFetchingSRP,OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints";
            string EnableBlinkFeatures = "UnownedAnimationsSkipCSSEvents,StaticAnimationOptimization,PageFreezeOptIn,FreezeFramesOnVisibility";
            string DisableBlinkFeatures = "DocumentWrite,LanguageDetectionAPI,DocumentPictureInPictureAPI";

            Settings.AddNoErrorFlag("disable-features", DisableFeatures);
            Settings.AddNoErrorFlag("enable-features", EnableFeatures);
            Settings.AddNoErrorFlag("enable-blink-features", EnableBlinkFeatures);
            Settings.AddNoErrorFlag("disable-blink-features", DisableBlinkFeatures);
            Settings.CefRuntimeStyle = CefRuntimeStyle.Chrome;

            Settings.UserDataPath = "";
            string UserApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLBr", "Default");
            string UserDataPath = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "User Data"));
            Settings.LogFile = Path.GetFullPath(Path.Combine(UserApplicationDataPath, "Errors.log"));

            WebViewManager.Settings = Settings;
            WebViewManager.RuntimeSettings.PDFViewer = false;

            Application CleanApp = new Application();

            WebViewDemo Window = new WebViewDemo();

            CleanApp.Run(Window);

            Cef.Shutdown();
            return Environment.ExitCode;*/
        }
        private static void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            OptimizeMemoryUsage();
        }
        private static void OptimizeMemoryUsage()
        {
            while (true)
            {
                try
                {
                    FlushMemory();
                    MinimizeFootprint();
                }
                finally
                {
                    Thread.Sleep(60000);
                }
            }
        }

        private static void FlushMemory()
        {
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            /*if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (IntPtr.Size == 8)
                    SetProcessWorkingSetSize64(Process.GetCurrentProcess().Handle, -1, -1);
                else
                    SetProcessWorkingSetSize32(Process.GetCurrentProcess().Handle, -1, -1);
            }*/
        }

        private static void MinimizeFootprint()
        {
            DllUtils.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }

        /*private static void FlushTimer_Tick(object? sender, EventArgs e)
        {
            Process[] SubProcesses = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            foreach (Process SubProcess in SubProcesses)
            {
                int CurrentMemoryUsage = (int)SubProcess.WorkingSet64;
                int CurrentMemoryMB = (CurrentMemoryUsage / 1024 / 1024);
                int MaxMemoryUsage = (int)Math.Round(CurrentMemoryMB * 0.3f);
                Utils.LimitMemoryUsage(SubProcess.Handle, MaxMemoryUsage);
            }
            Utils.LimitMemoryUsage(Process.GetCurrentProcess().Handle, 10);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Chromium.ExecuteScriptAsync("window.gc();");
        }*/

        private static void MinimizeMemory()
        {
            Process CurrentProcess = Process.GetCurrentProcess();
            //SetCpuAffinity(0x0001);
            /*CurrentProcess.PriorityBoostEnabled = false;
            CurrentProcess.PriorityClass = ProcessPriorityClass.Idle;
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;*/

            //GC.Collect(GC.MaxGeneration);
            //GC.WaitForPendingFinalizers();
            //if (Environment.OSVersion.Platform == PlatformID.Win32NT) //It will only run on Windows regardless
            DllUtils.SetProcessWorkingSetSize(CurrentProcess.Handle, -1, -1);
            //EmptyWorkingSet(CurrentProcess.Handle);
        }

        /*[DllImport("kernel32.dll")]
        private static extern bool SetProcessAffinityMask(IntPtr handle, IntPtr affinity);

        public static void SetCpuAffinity(int affinityMask)
        {
            Process process = Process.GetCurrentProcess();
            SetProcessAffinityMask(process.Handle, (IntPtr)affinityMask);
        }*/
    }
}
