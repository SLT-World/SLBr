/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using Microsoft.Win32;
using SLBr.Controls;
using SLBr.Pages;
using SLBr.WebView;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WinUI;

namespace SLBr
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string Name = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(Name));
        #endregion

        public int ID;

        private ObservableCollection<BrowserTabItem> _Tabs = [];
        public ObservableCollection<BrowserTabItem> Tabs
        {
            get => _Tabs;
            set
            {
                _Tabs = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<TabGroup> _TabGroup = [];
        public ObservableCollection<TabGroup> TabGroups
        {
            get => _TabGroup;
            set
            {
                _TabGroup = value;
                RaisePropertyChanged();
            }
        }

        public bool DimUnloadedIcon
        {
            get => _DimUnloadedIcon;
            set
            {
                _DimUnloadedIcon = value;
                RaisePropertyChanged();
            }
        }
        private bool _DimUnloadedIcon;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
        }

        //WARNING: DO NOT TOUCH OR REMOVE OnSourceInitialized.
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource Source = (HwndSource)PresentationSource.FromVisual(this);
            Source.AddHook(WndProc);
            Handle = Source.Handle;
            SetWindowDisplayAffinity();

            int DarkModeValue = App.Instance.CurrentTheme.DarkTitleBar ? 0x01 : 0x00;
            DllUtils.DwmSetWindowAttribute(Handle.Value, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref DarkModeValue, Marshal.SizeOf(typeof(int)));
            int NativeColor = Utils.NativeColor(App.Instance.CurrentTheme.SecondaryColor.ToDrawingColor());
            DllUtils.DwmSetWindowAttribute(Handle.Value, DwmWindowAttribute.DWMWA_CAPTION_COLOR, ref NativeColor, sizeof(int));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            HotKeyManager.HandleKeyDown(e);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case DllUtils.WM_NCHITTEST:
                    if (!IsFullscreen && Utils.IsSnapLayoutEnabled)
                    {
                        Point _Point = PointFromScreen(new Point((short)(lParam.ToInt32() & 0xFFFF), (short)((lParam.ToInt32() >> 16) & 0xFFFF)));
                        if (MaximizeRestoreButton.TransformToAncestor(this).TransformBounds(new Rect(MaximizeRestoreButton.RenderSize)).Contains(_Point))
                        {
                            MaximizeRestoreButton.Tag = "ForceMouseOver";
                            handled = true;
                            return new IntPtr(DllUtils.HTMAXBUTTON);
                        }
                        else
                            MaximizeRestoreButton.Tag = null;
                    }
                    break;
                case DllUtils.WM_NCLBUTTONDOWN:
                    if (!IsFullscreen)
                    {
                        if (wParam.ToInt32() == DllUtils.HTMAXBUTTON)
                        {
                            handled = true;
                            MaximizeRestoreButton.Tag = "ForcePressed";
                        }
                    }
                    break;
                case DllUtils.WM_NCLBUTTONUP:
                    if (!IsFullscreen)
                    {
                        if (wParam.ToInt32() == DllUtils.HTMAXBUTTON)
                        {
                            MaximizeRestoreButton.Tag = null;
                            MaximizeRestoreButton_Click(null, null);
                        }
                    }
                    break;
                case DllUtils.WM_COPYDATA:
                    COPYDATASTRUCT _dataStruct = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                    string[] Data = Marshal.PtrToStringUni(_dataStruct.lpData, _dataStruct.cbData / 2).Split("<|>");
                    if (Data[0] == "NewWindow")
                        App.Instance.NewWindow();
                    else
                    {
                        if (Data[0] == "Url")
                            NewTab(Data[1], true, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                        else if (Data[0] == "Start" && App.Instance.CurrentProfile.Name == Data[1])
                        {
                            if (App.Instance.Background)
                                App.Instance.ContinueBackgroundInitialization();
                            else
                                App.Instance.NewWindow();
                        }
                        if (Handle != null)
                            DllUtils.SetForegroundWindow(Handle.Value);
                    }
                    handled = true;
                    break;
                case DllUtils.WM_SETTINGCHANGE:
                    if (wParam == IntPtr.Zero && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet")
                    {
                        try
                        {
                            using (var Key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true))
                            {
                                Theme SystemTheme = App.Instance.Themes.FirstOrDefault(i => i.Name == "System");
                                if (SystemTheme != null)
                                {
                                    bool IsLight = Key.GetValue("SystemUsesLightTheme") as int? == 1;
                                    Theme TargetTheme = App.Instance.Themes.FirstOrDefault(i => i.Name == (IsLight ? "Light" : "Dark"));
                                    if (TargetTheme != null)
                                    {
                                        SystemTheme.PrimaryColor = TargetTheme.PrimaryColor;
                                        SystemTheme.SecondaryColor = TargetTheme.SecondaryColor;
                                        SystemTheme.BorderColor = TargetTheme.BorderColor;
                                        SystemTheme.GrayColor = TargetTheme.GrayColor;
                                        SystemTheme.FontColor = TargetTheme.FontColor;
                                        SystemTheme.IndicatorColor = TargetTheme.IndicatorColor;

                                        SystemTheme.DarkTitleBar = TargetTheme.DarkTitleBar;
                                        SystemTheme.DarkWebPage = TargetTheme.DarkWebPage;
                                        if (App.Instance.CurrentTheme.Name == SystemTheme.Name)
                                            App.Instance.SetAppearance(App.Instance.CurrentTheme, App.Instance.TabAlignment, App.Instance.VerticalTabWidth, App.Instance.AllowHomeButton, App.Instance.AllowTranslateButton, App.Instance.AllowReaderModeButton, App.Instance.ShowExtensionButton, App.Instance.ShowDownloadsButton, App.Instance.ShowFavouritesBar, App.Instance.AllowQRButton, App.Instance.AllowWebEngineButton);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    handled = true;
                    break;
                case DllUtils.WM_SYSCOMMAND:
                    if (IsFullscreen)
                    {
                        int Command = wParam.ToInt32() & 0xFFF0;
                        if (Command == DllUtils.SC_MOVE || Command == DllUtils.SC_SIZE || Command == DllUtils.SC_MINIMIZE || Command == DllUtils.SC_RESTORE || Command == DllUtils.SC_MAXIMIZE)
                            handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        BrowserTabItem NewTabTab = null;
        public nint? Handle;
        private void InitializeWindow()
        {
            ID = Utils.GenerateRandomId();
            App.Instance.AllWindows.Add(this);
            if (App.Instance.WindowsSaves.Count < App.Instance.AllWindows.Count)
                App.Instance.WindowsSaves.Add(new($"Window_{App.Instance.WindowsSaves.Count}.bin", App.Instance.UserApplicationWindowsPath));
            if (App.Instance.CurrentProfile.Type == ProfileType.User && !App.Instance.CurrentProfile.Default)
                TaskbarItemOverlay.SetProfile(TaskbarItem, App.Instance.CurrentProfile);
            NewTabTab = new(null)
            {
                Type = BrowserTabType.Add
            };
            Tabs.Add(NewTabTab);
            DimUnloadedIcon = bool.Parse(App.Instance.GlobalSave.Get("DimUnloadedIcon"));
            TabsUI.ItemsSource = Tabs;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ExecuteCloseEvent();
        }

        /*static readonly string[] TestUrls = {
            "https://ads.google.com/pagead.js",
            "https://example.com/js/ads.js",
            "https://googleads.g.doubleclick.net/pagead/js/ads.js",
            "https://safe.site.com/script.js",
            "https://cdn.doubleclick.net/script.js"
        };

        static readonly string[] AdIndicators = {
            "survey.min.js", "/survey.js", "/social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "usync.js", "moneybid.js", "miner.js", "prebid",
            "advertising.js", "adsense.js", "track", "plusone.js", "pagead.js", "gtag.js",
            "google.com/ads", "play.google.com/log"
        };

        static readonly FastHashSet<string> HasInLink = new FastHashSet<string> {
            "survey.min.js", "/survey.js", "/social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "usync.js", "moneybid.js", "miner.js", "prebid",
            "advertising.js", "adsense.js", "track", "plusone.js", "pagead.js", "gtag.js",
            "google.com/ads", "play.google.com/log"
        };

        static readonly HashSet<string> HasInLinkSlow = new HashSet<string> {
            "survey.min.js", "/survey.js", "/social-icons.js", "intergrator.js", "cookie.js", "analytics.js", "ads.js",
            "tracker.js", "tracker.ga.js", "tracker.min.js", "bugsnag.min.js", "async-ads.js", "displayad.js", "j.ad", "ads-beacon.js", "adframe.js", "ad-provider.js",
            "admanager.js", "usync.js", "moneybid.js", "miner.js", "prebid",
            "advertising.js", "adsense.js", "track", "plusone.js", "pagead.js", "gtag.js",
            "google.com/ads", "play.google.com/log"
        };


        static bool IndexOfOrdinalMethod(string url)
        {
            foreach (var pattern in AdIndicators)
                if (url.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        static bool ContainsOrdinalMethod(string url)
        {
            foreach (var pattern in AdIndicators)
                if (url.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        static bool IndexOfMethod(string url)
        {
            foreach (var pattern in AdIndicators)
                if (url.IndexOf(pattern) >= 0)
                    return true;
            return false;
        }

        static bool ContainsMethod(string url)
        {
            foreach (var pattern in AdIndicators)
                if (url.Contains(pattern))
                    return true;
            return false;
        }

        static bool LowercaseContains(string url)//Fastest in Release mode
        {
            string lower = url.ToLowerInvariant();
            foreach (var pattern in AdIndicators)
                if (lower.Contains(pattern))
                    return true;
            return false;
        }

        static bool HashSetContains(string url)
        {
            foreach (var pattern in HasInLinkSlow)
                if (url.Contains(pattern))
                    return true;
            return false;
        }

        static bool FastHashSetContains(string url)
        {
            foreach (var pattern in HasInLink)
                if (url.Contains(pattern))
                    return true;
            return false;
        }*/

        /*public static (string, string) ParseCertificateIssueSpan(string Certificate)
        {
            ReadOnlySpan<char> Span = Certificate.AsSpan().Trim();
            string CN = null;
            string O = null;
            while (!Span.IsEmpty)
            {
                int Comma = Span.IndexOf(",");
                ReadOnlySpan<char> Part = Comma >= 0 ? Span[..Comma] : Span;
                int Equal = Part.IndexOf("=");
                if (Equal > 0)
                {
                    ReadOnlySpan<char> Key = Part[..Equal].Trim();
                    ReadOnlySpan<char> Value = Part[(Equal + 1)..].Trim();
                    if (Key.Equals("CN", StringComparison.Ordinal))
                        CN = Value.ToString();
                    else if (Key.Equals("O", StringComparison.Ordinal))
                        O = Value.ToString();
                }
                Span = Comma >= 0 ? Span[(Comma + 1)..].TrimStart() : default;
            }
            return (CN ?? string.Empty, O ?? string.Empty);
        }

        public static (string, string) ParseCertificateIssueString(string Certificate)
        {
            if (string.IsNullOrWhiteSpace(Certificate))
                return (string.Empty, string.Empty);

            string CN = "";
            string O = "";

            string Remaining = Certificate.Trim();

            while (Remaining.Length > 0)
            {
                int Comma = Remaining.IndexOf(',');
                string Part = Comma >= 0 ? Remaining.Substring(0, Comma) : Remaining;

                int Equal = Part.IndexOf('=');
                if (Equal > 0)
                {
                    string Key = Part.Substring(0, Equal).Trim();
                    string Value = Part.Substring(Equal + 1).Trim();
                    if (Key == "CN")
                        CN = Value;
                    else if (Key == "O")
                        O = Value;
                }

                if (Comma >= 0)
                    Remaining = Remaining.Substring(Comma + 1).TrimStart();
                else
                    break;
            }

            return (CN ?? string.Empty, O ?? string.Empty);
        }*/


        /*const int Iterations = 5_0_000;
        private async void PerformBenchmark()
        {
            Benchmark.Clear();*/
        //byte[] LocalHash = [0x5B, 0x0B, 0x89, 0x75, 0x0C, 0x78, 0xF2, 0x33, 0xFE, 0xE2, 0x5C, 0x6B, 0xE3, 0x2D, 0x92, 0x8F, 0xCD, 0x80, 0x5A, 0x8C, 0x54, 0x55, 0xC2, 0x11, 0x0D, 0x29, 0x35, 0x3C, 0x2F, 0x51, 0x7F, 0xEE];
        //byte[] Bytes = [0x0A, 0x2D, 0x0A, 0x20, 0x5B, 0x0B, 0x89, 0x75, 0x0C, 0x78, 0xF2, 0x33, 0xFE, 0xE2, 0x5C, 0x6B, 0xE3, 0x2D, 0x92, 0x8F, 0xCD, 0x80, 0x5A, 0x8C, 0x54, 0x55, 0xC2, 0x11, 0x0D, 0x29, 0x35, 0x3C, 0x2F, 0x51, 0x7F, 0xEE, 0x12, 0x05, 0x08, 0x01, 0x12, 0x01, 0x02, 0x12, 0x02, 0x08, 0x04, 0x12, 0x03, 0x08, 0xAC, 0x02];
        //MessageBox.Show($"Run {Handlers.WebRiskHandler.SBv5GetThreatType(ProtoBuf.Serializer.Deserialize<SafeBrowsing.SearchHashesResponse>(Bytes), LocalHash).ToString()} {Protobuf.SafeBrowsingV5Parser.SBv5GetThreatType(Protobuf.SafeBrowsingV5Parser.ParseResponse(Bytes), LocalHash).ToString()}");
        /*Benchmark.Run("protobuf-net", Iterations, () =>
        {
            _ = Handlers.WebRiskHandler.SBv5GetThreatType(ProtoBuf.Serializer.Deserialize<SafeBrowsing.SearchHashesResponse>(Bytes), LocalHash);
        });
        Benchmark.Run("CSharp", Iterations, () =>
        {
            _ = Protobuf.SafeBrowsingV5Parser.SBv5GetThreatType(Protobuf.SafeBrowsingV5Parser.ParseResponse(Bytes), LocalHash);
        });*/
        /*Benchmark.Run("CSharpParse", Iterations, () =>
        {
            _ = Protobuf.SafeBrowsingV5Parser.ParseResponse(Bytes);
        });*/
        //byte[] Bytes = [0x08, 0x01, 0x1A, 0x80, 0x02, 0x64, 0x65, 0x32, 0x61, 0x39, 0x32, 0x34, 0x64, 0x64, 0x31, 0x61, 0x64, 0x38, 0x38, 0x39, 0x63, 0x35, 0x63, 0x36, 0x36, 0x66, 0x34, 0x31, 0x31, 0x33, 0x31, 0x33, 0x63, 0x64, 0x65, 0x62, 0x66, 0x61, 0x65, 0x35, 0x38, 0x66, 0x33, 0x64, 0x32, 0x37, 0x32, 0x35, 0x39, 0x32, 0x32, 0x33, 0x37, 0x30, 0x32, 0x31, 0x36, 0x39, 0x31, 0x30, 0x63, 0x61, 0x62, 0x63, 0x63, 0x32, 0x62, 0x66, 0x64, 0x39, 0x30, 0x30, 0x35, 0x63, 0x66, 0x66, 0x36, 0x35, 0x32, 0x30, 0x66, 0x31, 0x31, 0x61, 0x66, 0x35, 0x38, 0x65, 0x64, 0x33, 0x35, 0x33, 0x64, 0x61, 0x61, 0x63, 0x64, 0x31, 0x65, 0x63, 0x65, 0x35, 0x34, 0x63, 0x64, 0x66, 0x39, 0x39, 0x30, 0x37, 0x61, 0x32, 0x62, 0x65, 0x30, 0x64, 0x39, 0x66, 0x39, 0x36, 0x61, 0x38, 0x62, 0x62, 0x62, 0x33, 0x61, 0x65, 0x35, 0x64, 0x31, 0x38, 0x62, 0x62, 0x34, 0x33, 0x31, 0x64, 0x64, 0x34, 0x38, 0x64, 0x63, 0x63, 0x64, 0x66, 0x61, 0x63, 0x30, 0x31, 0x65, 0x33, 0x35, 0x35, 0x30, 0x62, 0x32, 0x34, 0x66, 0x36, 0x35, 0x32, 0x36, 0x30, 0x63, 0x35, 0x34, 0x31, 0x62, 0x33, 0x32, 0x32, 0x32, 0x66, 0x63, 0x36, 0x32, 0x33, 0x35, 0x32, 0x62, 0x39, 0x63, 0x64, 0x36, 0x64, 0x65, 0x38, 0x37, 0x32, 0x32, 0x64, 0x66, 0x34, 0x62, 0x34, 0x35, 0x66, 0x61, 0x63, 0x31, 0x64, 0x63, 0x32, 0x32, 0x33, 0x33, 0x39, 0x32, 0x33, 0x64, 0x66, 0x63, 0x32, 0x31, 0x61, 0x33, 0x30, 0x31, 0x36, 0x33, 0x62, 0x61, 0x38, 0x35, 0x33, 0x30, 0x36, 0x65, 0x63, 0x30, 0x36, 0x30, 0x61, 0x38, 0x37, 0x65, 0x33, 0x38, 0x30, 0x63, 0x37, 0x36, 0x64, 0x65, 0x63, 0x35, 0x39, 0x35, 0x30, 0x34, 0x66, 0x33, 0x39, 0x31, 0x32, 0x37, 0x36, 0x35, 0x65, 0x61];
        //MessageBox.Show($"Run {ProtoBuf.Serializer.Deserialize<SafeBrowsing.ClientDownloadResponse>(Bytes).ToVerdict().ToString()} {Protobuf.SafeBrowsingDownloadParser.ParseResponse(Bytes).ToVerdict().ToString()}");
        /*Benchmark.Run("protobuf-net", Iterations, () =>
        {
            _ = ProtoBuf.Serializer.Deserialize<SafeBrowsing.ClientDownloadResponse>(Bytes).ToVerdict();
        });*/
        /*Benchmark.Run("CSharp", Iterations, () =>
        {
            _ = Protobuf.SafeBrowsingDownloadParser.ParseResponse(Bytes);
        });*/
        /*MessageBox.Show($"Run");
        await Benchmark.RunAsync("protobuf-net", Iterations, async () =>
        {
            _ = DownloadRiskHandler.SBGetDownloadResponseBytesNet("C:/Path/Path/Path/File.path", "C:/Path/Path/Path/File.exe", "https://example.com/path/file.exe", "https://example.com");
        });
        await Benchmark.RunAsync("CSharp", Iterations, async () =>
        {
            _ = DownloadRiskHandler.SBGetDownloadResponseBytes("C:/Path/Path/Path/File.path", "C:/Path/Path/Path/File.exe", "https://example.com/path/file.exe", "https://example.com");
        });*/
        /*MessageBox.Show($"{Utils.GetFileExtension("https://example.com/example.pdf#id?query=123")} {System.IO.Path.GetExtension("https://example.com/example.pdf#id?query=123")}");
        Benchmark.Run("GetFileExtension", Iterations, () =>
        {
            _ = Utils.GetFileExtension("https://example.com/example.pdf#id?query=123");
        });
        Benchmark.Run("GetExtension", Iterations, () =>
        {
            _ = System.IO.Path.GetExtension("https://example.com/example.pdf#id?query=123");
        });*/
        //MessageBox.Show($"{Utils.FastHost("https://google.com/search?q=Hello+World")} {Utils.Host("https://google.com/search?q=Hello+World")}");
        /*Benchmark.Run("CapitalizeAllFirstCharacters", Iterations, () =>
        {
            _ = Utils.CapitalizeAllFirstCharacters("example.html foo bar");
        });
        Benchmark.Run("ToTitleCase", Iterations, () =>
        {
            _ = CultureInfo.InvariantCulture.TextInfo.ToTitleCase("example.html foo bar");
        });*/
        /*Benchmark.Run("FastHost", Iterations, () =>
        {
            _ = Utils.FastHost("https://google.com/search?q=Hello+World");
        });
        Benchmark.Run("Host", Iterations, () =>
        {
            _ = Utils.Host("https://google.com/search?q=Hello+World");
        });*/
        /*Benchmark.Run("Uri EscapeDataString JIT", 1, () =>
        {
            _ = Uri.EscapeDataString("foo=C# Example&bar=Foo Bar");
        });
        Benchmark.Run("Utils EscapeDataString Span JIT", 1, () =>
        {
            _ = Uri.EscapeDataString("foo=C# Example&bar=Foo Bar".AsSpan());
        });
        Benchmark.Run("Uri EscapeDataString", Iterations, () =>
        {
            _ = Uri.EscapeDataString("foo=C# Example&bar=Foo Bar");
        });
        Benchmark.Run("Utils EscapeDataString Span", Iterations, () =>
        {
            _ = Uri.EscapeDataString("foo=C# Example&bar=Foo Bar".AsSpan());
        });*/
        /*const string Domain = "adc3-launch.adcolony.com";
        SLBr.Handlers.AdBlockHandler _AdBlockHandler = new(App.Instance.AllowListSave);
        Benchmark.Run("ParseAdd", 1, () =>
        {
            _AdBlockHandler.ParseAdd(System.IO.File.ReadAllText(@"ads-ags.txt"));
        });
        Benchmark.Run("BlockRequest", Iterations, () =>
        {
            _ = _AdBlockHandler.ShouldBlockRequest(Domain, Domain, ResourceRequestType.Script);
        });*/
        /*MessageBox.Show(Benchmark.Report());
    }*/
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetAppearance(App.Instance.CurrentTheme);
            //PerformBenchmark();
            #region Benchmark
            /*Benchmark.Clear();
            const string Domain = "adc3-launch.adcolony.com";
            SLBr.Handlers.AdBlockHandler _AdBlockHandler = new(App.Instance.AllowListSave);
            //MessageBox.Show("0");
            Benchmark.RunAsync("ParseAdd", 1, async () =>
            {
                await _AdBlockHandler.ParseAdd(System.IO.File.ReadAllText(@"ads-ags.txt"));
            });
            //MessageBox.Show("1");
            Benchmark.Run("BlockRequest", Iterations, () =>
            {
                _ = _AdBlockHandler.ShouldBlockRequest(Domain, Domain, ResourceRequestType.Script);
            });*/
            /*string E = "1+1/54334235*(2131234)";
            Benchmark.Run("Generated Regex", Iterations, () =>
            {
                _ = Utils.OGMathRegex().IsMatch(E);
            });
            Benchmark.Run("Generated Regex (Compiled)", Iterations, () => //Best
            {
                _ = Utils.MathRegex().IsMatch(E);
            });
            Benchmark.Run("Regex", Iterations, () =>
            {*/
            //    _ = Regex.IsMatch(E, @"^[\d\s\.\+\-\*/%\(\)]+$");
            /*});
            Benchmark.Run("Regex (Compiled)", Iterations, () =>
            {*/
            //    _ = Regex.IsMatch(E, @"^[\d\s\.\+\-\*/%\(\)]+$", RegexOptions.Compiled);
            //});
            /*string E = "<h1>Hello</h1>";
            Benchmark.Run("Replace", Iterations, () =>
            {
                _ = E.Replace("<h1>", "").Replace("</h1>", "");
            });
            Benchmark.Run("Regex", Iterations, () =>
            {
                _ = Regex.Replace(E, "<.*?>", string.Empty);
            });
            Benchmark.Run("Substring", Iterations, () =>//Best
            {
                _ = E.Substring(4, E.Length - 9);
            });
            Benchmark.Run("Prefix", Iterations, () =>
            {
                _ = Utils.RemovePrefix(Utils.RemovePrefix(E, "<h1>"), "</h1>", false, true);
            });*/
            /*Benchmark.Run("Boolean", Iterations, () =>//Best
            {
                _ = true == false;
            });
            Benchmark.Run("Boolean (Integer based)", Iterations, () =>
            {
                _ = 1 == 0;
            });
            Benchmark.Run("Boolean String", Iterations, () =>
            {
                _ = "true" == "false";
            });
            Benchmark.Run("Boolean String (Integer based)", Iterations, () =>
            {
                _ = "1" == "0";
            });*/
            /*Benchmark.Run("Contains", Iterations, () =>//Fastest
            {
                _ = "https://googleads.youtube.com/watch?v=/pagead/js/ads.js".Contains("youtube.com/watch?v=");
            });
            Benchmark.Run("IndexOf", Iterations, () =>
            {
                _ = "https://googleads.youtube.com/watch?v=/pagead/js/ads.js".IndexOf("youtube.com/watch?v=") >= 0;
            });
            Benchmark.Run("IndexOf Span", Iterations, () =>
            {
                _ = "https://googleads.youtube.com/watch?v=/pagead/js/ads.js".AsSpan().IndexOf("youtube.com/watch?v=") >= 0;
            });*/
            /*Benchmark.Run("Span", Iterations, () =>
            {
                _ = ParseCertificateIssueSpan("CN:Hello,O:EEEE");
            });
            Benchmark.Run("String", Iterations, () =>//Faster, but uses more memory
            {
                _ = ParseCertificateIssueString("CN:Hello,O:EEEE");
            });*/
            /*Benchmark.Run("Normal", Iterations, () =>
            {
                _ = "O" == "O";
            });
            Benchmark.Run("Equals", Iterations, () =>
            {
                _ = "O".Equals("O", StringComparison.Ordinal);
            });
            Benchmark.Run("Normal Span", Iterations, () =>
            {
                ReadOnlySpan<char> Span = "O".AsSpan().Trim();
                _ = Span == "O";
            });
            Benchmark.Run("Equals Span", Iterations, () =>
            {
                ReadOnlySpan<char> Span = "O".AsSpan().Trim();
                Span.Equals("O", StringComparison.Ordinal);
            });*/
            /*//Normal contains is found to be significantly superior in Release build with less time taken & memory.
            Benchmark.Run("Normal", Iterations, () =>
            {
                _ = "https://googleads.g.doubleclick.net/pagead/js/ads.js".Contains("googleads.g.doubleclick.net/pagead/js/ads.js");
            });
            Benchmark.Run("Ordinal", Iterations, () =>
            {
                _ = "https://googleads.g.doubleclick.net/pagead/js/ads.js".Contains("googleads.g.doubleclick.net/pagead/js/ads.js");
            });
            Benchmark.Run("OrdinalIgnoreCase", Iterations, () =>
            {
                _ = "https://googleads.g.doubleclick.net/pagead/js/ads.js".Contains("googleads.g.doubleclick.net/pagead/js/ads.js", StringComparison.OrdinalIgnoreCase);
            });
            Benchmark.Run("Normal IndexOf", Iterations, () => //Worst performing
            {
                _ = "https://googleads.g.doubleclick.net/pagead/js/ads.js".IndexOf("googleads.g.doubleclick.net/pagead/js/ads.js");
            });
            Benchmark.Run("Ordinal IndexOf", Iterations, () =>
            {
                _ = "https://googleads.g.doubleclick.net/pagead/js/ads.js".IndexOf("googleads.g.doubleclick.net/pagead/js/ads.js");
            });
            Benchmark.Run("OrdinalIgnoreCase IndexOf", Iterations, () =>
            {
                _ = "https://googleads.g.doubleclick.net/pagead/js/ads.js".IndexOf("googleads.g.doubleclick.net/pagead/js/ads.js", StringComparison.OrdinalIgnoreCase);
            });*/
            /*Benchmark.Run("FastHost", Iterations, () =>
            {
                _ = Utils.FastHost("https://googleads.g.doubleclick.net/pagead/js/ads.js");
            });
            Benchmark.Run("Host", Iterations, () =>
            {
                _ = Utils.Host("https://googleads.g.doubleclick.net/pagead/js/ads.js");
            });*/
            /*Benchmark.Run("IndexOf OrdinalIgnoreCase", Iterations, () =>
            {
                _ = IndexOfOrdinalMethod(TestUrls[3]);
            });
            Benchmark.Run("Contains OrdinalIgnoreCase", Iterations, () =>
            {
                _ = ContainsOrdinalMethod(TestUrls[3]);
            });
            Benchmark.Run("IndexOf", Iterations, () =>
            {
                _ = IndexOfMethod(TestUrls[3]);
            });
            Benchmark.Run("Contains", Iterations, () =>
            {
                _ = ContainsMethod(TestUrls[3]);
            });
            Benchmark.Run("Lowercase + contains", Iterations, () =>
            {
                _ = LowercaseContains(TestUrls[3]);
            });
            Benchmark.Run("HashSet", Iterations, () =>
            {
                _ = HashSetContains(TestUrls[3]);
            });
            Benchmark.Run("FastHashSet", Iterations, () =>
            {
                _ = FastHashSetContains(TestUrls[3]);
            });*/
            //MessageBox.Show(Benchmark.Report());
            #endregion
        }

        bool VerticalTabs = false;

        public void SetAppearance(Theme _Theme)
        {
            Resources["PrimaryBrushColor"] = _Theme.PrimaryColor;
            Resources["SecondaryBrushColor"] = _Theme.SecondaryColor;
            Resources["BorderBrushColor"] = _Theme.BorderColor;
            Resources["GrayBrushColor"] = _Theme.GrayColor;
            Resources["FontBrushColor"] = _Theme.FontColor;
            Resources["IndicatorBrushColor"] = _Theme.IndicatorColor;

            if (Handle.HasValue)
            {
                int DarkModeValue = _Theme.DarkTitleBar ? 0x01 : 0x00;
                DllUtils.DwmSetWindowAttribute(Handle.Value, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref DarkModeValue, Marshal.SizeOf(typeof(int)));
                int NativeColor = Utils.NativeColor(_Theme.SecondaryColor.ToDrawingColor());
                DllUtils.DwmSetWindowAttribute(Handle.Value, DwmWindowAttribute.DWMWA_CAPTION_COLOR, ref NativeColor, sizeof(int));
            }

            SetTabAlignment();

            foreach (BrowserTabItem Tab in Tabs)
                Tab.Content?.SetAppearance(_Theme);
        }

        public void SetTabAlignment()
        {
            if (App.Instance.TabAlignment == 0)
            {
                CaptionIcon.Visibility = Visibility.Collapsed;
                CaptionTitle.Visibility = Visibility.Collapsed;
                VerticalTabs = false;
                CaptionHeight.Height = new GridLength(45);
                TabsUI.Style = FindResource(typeof(WinUITabControl)) as Style;
                Grid.SetRow(TabsUI, 0);
                Tabs.Remove(NewTabTab);
                Tabs.Add(NewTabTab);
                TabsUI.Padding = new Thickness(0);
                foreach (BrowserTabItem Tab in Tabs)
                {
                    Tab.Content?.InfoBarContainer.Margin = new Thickness(0);
                    Tab.Content?.WebContainer.Margin = new Thickness(0);
                    Tab.Content?.WebContainerBorder.BorderThickness = new Thickness(0);
                    Tab.Content?.NewTabButton.Visibility = Visibility.Collapsed;
                }
                TabPreviewPopup.Placement = PlacementMode.Bottom;
                TabPreviewPopup.VerticalOffset = -7.5;
                TabPreviewPopup.HorizontalOffset = -10;
            }
            else
            {
                CaptionIcon.Visibility = Visibility.Visible;
                CaptionTitle.Visibility = Visibility.Visible;
                VerticalTabs = true;
                CaptionHeight.Height = new GridLength(WindowState == WindowState.Maximized ? SystemParameters.CaptionHeight : 30);
                TabsUI.Style = Resources["VerticalTabControl"] as Style;
                Grid.SetRow(TabsUI, 1);
                TabsUI.ApplyTemplate();
                Tabs.Remove(NewTabTab);
                //Tabs.Insert(0, NewTabTab);
                TabsUI.LayoutUpdated -= TabsUI_LayoutUpdated;
                TabsUI.LayoutUpdated += TabsUI_LayoutUpdated;
                TabPreviewPopup.Placement = PlacementMode.Right;
                TabPreviewPopup.VerticalOffset = -10;
                TabPreviewPopup.HorizontalOffset = -7.5;
            }
            if (!IsFullscreen)
                MainWindowChrome.CaptionHeight = CaptionHeight.Height.Value;
            foreach (BrowserTabItem Tab in Tabs)
            {
                Tab.Content?.MaxHeight = TabsUI.ActualHeight;
                Tab.Content?.UpdateDevToolsPosition();
            }
        }
        private void TabsUI_LayoutUpdated(object? sender, EventArgs e)
        {
            TabsUI.LayoutUpdated -= TabsUI_LayoutUpdated;
            float ToolBarHeight = 41;
            if (App.Instance.ShowFavouritesBar == 0)
            {
                if (App.Instance.FavouriteManager.Favourites.Count == 0)
                    ToolBarHeight += 5;
                else
                    ToolBarHeight += 41.25f;
            }
            else if (App.Instance.ShowFavouritesBar == 1)
                ToolBarHeight += 41.25f;
            else if (App.Instance.ShowFavouritesBar == 2)
                ToolBarHeight += 5;
            TabsUI.Padding = new Thickness(0, ToolBarHeight, 0, 0);

            TabPanelScroll = TabsUI.Template.FindName("TabPanelScroll", TabsUI) as ScrollViewer;
            TabResizeThumb = TabsUI.Template.FindName("TabResizeThumb", TabsUI) as Thumb;
            if (TabPanelScroll == null || TabResizeThumb == null)
                return;
            TabPanelScroll.Width = App.Instance.VerticalTabWidth;
            TabResizeThumb.Margin = new Thickness(App.Instance.VerticalTabWidth - 5, TabsUI.Padding.Top, 0, 0);
            foreach (BrowserTabItem Tab in Tabs)
            {
                Tab.Content?.InfoBarContainer.Margin = new Thickness(App.Instance.VerticalTabWidth, 0, 0, 0);
                Tab.Content?.WebContainer.Margin = new Thickness(App.Instance.VerticalTabWidth, 0, 0, 0);
                Tab.Content?.WebContainerBorder.BorderThickness = new Thickness(1, 0, 0, 0);
                Tab.Content?.NewTabButton.Visibility = Visibility.Visible;
            }
            TabResizeThumb.DragDelta -= TabResizeThumb_DragDelta;
            TabResizeThumb.DragDelta += TabResizeThumb_DragDelta;
        }

        private void TabResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Instance.VerticalTabWidth = Math.Max(45, Math.Min(250, App.Instance.VerticalTabWidth + e.HorizontalChange));
            App.Instance.GlobalSave.Set("VerticalTabWidth", App.Instance.VerticalTabWidth);
            TabPanelScroll.Width = App.Instance.VerticalTabWidth;
            TabResizeThumb.Margin = new Thickness(App.Instance.VerticalTabWidth - 5, TabsUI.Padding.Top, 0, 0);
            foreach (BrowserTabItem Tab in Tabs)
            {
                Tab.Content?.InfoBarContainer.Margin = new Thickness(App.Instance.VerticalTabWidth, 0, 0, 0);
                Tab.Content?.WebContainer.Margin = new Thickness(App.Instance.VerticalTabWidth, 0, 0, 0);
                Tab.Content?.WebContainerBorder.BorderThickness = new Thickness(1, 0, 0, 0);
                Tab.Content?.NewTabButton.Visibility = Visibility.Visible;
            }
        }

        public ScrollViewer TabPanelScroll = null;
        Thumb TabResizeThumb = null;

        public void ButtonAction(object sender, RoutedEventArgs e)
        {
            FrameworkElement Element = (FrameworkElement)sender;
            string[] Values = Element.Tag.ToString().Split("<,>", StringSplitOptions.None);
            if (Element.DataContext is BrowserTabItem Tab)
            {
                if (Tab.Type == BrowserTabType.Group)
                {
                    Array.Resize(ref Values, Values.Length + 3);
                    if (Values[0] == "5")
                        Values[3] = Tab.ID.ToString();
                    else if (Values[0] == "6")
                    {
                        Values[1] = Tab.ID.ToString();
                        Values[2] = Tab.ParentWindow.ID.ToString();
                    }
                    else
                        Values[1] = Tab.ID.ToString();
                }
                else if (Tab.Type == BrowserTabType.Navigation && Values.Length == 1)
                {
                    Array.Resize(ref Values, Values.Length + 2);
                    Values[1] = Tab.ID.ToString();
                    Values[2] = Values[0] == "5" ? "Tab" : Tab.ParentWindow.ID.ToString();
                }
            }
            Action((Actions)int.Parse(Values[0]), (Values.Length > 1) ? Values[1] : string.Empty, (Values.Length > 2) ? Values[2] : string.Empty, (Values.Length > 3) ? Values[3] : string.Empty);
        }

        private void Action(Actions _Action, string V1 = "", string V2 = "", string V3 = "")
        {
            Browser _BrowserView = GetTab().Content;
            if (_BrowserView != null)
                V1 = V1.Replace("{CurrentUrl}", _BrowserView.Address);
            V1 = V1.Replace("{Homepage}", App.Instance.GlobalSave.Get("Homepage"));

            switch (_Action)
            {
                case Actions.Exit:
                    App.Instance.CloseSLBr(true);
                    break;

                case Actions.Undo:
                    Undo(V1);
                    break;
                case Actions.Redo:
                    Redo(V1);
                    break;
                case Actions.Refresh:
                    Refresh(V1);
                    break;
                case Actions.Navigate:
                    Navigate(V1);
                    break;

                case Actions.CreateTab:
                    if (V2 == "Tab")
                    {
                        BrowserTabItem _Tab = GetBrowserTabWithId(int.Parse(V1));
                        NewTab(_Tab.Content.Address, true, Tabs.IndexOf(_Tab) + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")), _Tab.TabGroup);
                    }
                    else if (V2 == "Private")
                        NewTab(V1, true, -1, true);
                    else if (V2 == "Group")
                    {
                        BrowserTabItem _Tab = GetBrowserTabWithId(int.Parse(V3));
                        NewTab(V1, true, Tabs.IndexOf(_Tab) + 1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")), _Tab.TabGroup);
                    }
                    else
                        NewTab(V1, true, -1, bool.Parse(App.Instance.GlobalSave.Get("PrivateTabs")));
                    break;
                case Actions.CloseTab:
                    CloseTab(int.Parse(V1), int.Parse(V2));
                    break;
                case Actions.NewWindow:
                    App.Instance.NewWindow();
                    break;
                case Actions.UnloadTab:
                    ForceUnloadTab(int.Parse(V1));
                    break;

                case Actions.DevTools:
                    DevTools(V1);
                    break;
                case Actions.Favourite:
                    Favourite(V1);
                    break;
                case Actions.CreateGroup:
                    CreateGroup();
                    break;
                case Actions.Ungroup:
                    Ungroup(V1);
                    break;
                case Actions.ModifyGroup:
                    ModifyGroup(V1);
                    break;
                case Actions.Mute:
                    ToggleMute(V1);
                    break;
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            TabPreviewPopup.IsOpen = false;
            MaximizeRestoreButton.Tag = null;
            foreach (BrowserTabItem Tab in Tabs)
                Tab.Content?.MaxHeight = TabsUI.ActualHeight;
            WindowBackground.Background = (SolidColorBrush)FindResource("SecondaryBrush");
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            TabPreviewPopup.IsOpen = false;
            MaximizeRestoreButton.Tag = null;
            WindowBackground.Background = (SolidColorBrush)FindResource("BorderBrush");
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            TabPreviewPopup.IsOpen = false;
            MaximizeRestoreButton.Tag = null;
            if (WindowState != WindowState.Minimized)
            {
                if (WindowState == WindowState.Maximized)
                {
                    if (App.Instance.TabAlignment != 0)
                        CaptionHeight.Height = new GridLength(SystemParameters.CaptionHeight);
                    MaximizeRestoreButton.Content = "\xe923";
                }
                else
                {
                    if (App.Instance.TabAlignment != 0)
                        CaptionHeight.Height = new GridLength(30);
                    MaximizeRestoreButton.Content = "\xe922";
                }
                if (!IsFullscreen)
                    MainWindowChrome.CaptionHeight = CaptionHeight.Height.Value;
                if (Tabs.Count != 0 && TabsUI.SelectedIndex != -1)
                    Tabs[TabsUI.SelectedIndex].Content?.ReFocus();
            }
            else
            {
                if (Tabs.Count != 0 && TabsUI.SelectedIndex != -1)
                    Tabs[TabsUI.SelectedIndex].Content?.UnFocus();
            }
            foreach (BrowserTabItem Tab in Tabs)
                Tab.Content?.MaxHeight = TabsUI.ActualHeight;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            TabPreviewPopup.IsOpen = false;
            MaximizeRestoreButton.Tag = null;
            GetTab()?.Content?.TriggerLocationChanged();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TabPreviewPopup.IsOpen = false;
            MaximizeRestoreButton.Tag = null;
            foreach (BrowserTabItem Tab in Tabs)
            {
                if (e.HeightChanged)
                    Tab.Content?.MaxHeight = TabsUI.ActualHeight;
            }
            GetTab()?.Content?.TriggerSizeChanged();
        }

        public void UnloadTabs()
        {
            if (WindowState == WindowState.Minimized)
            {
                foreach (BrowserTabItem Tab in Tabs)
                {
                    if (Tab.Content != null)
                        UnloadTab(Tab.Content);
                }
            }
            else
            {
                for (int i = 0; i < Tabs.Count; i++)
                {
                    if (i != TabsUI.SelectedIndex)
                    {
                        BrowserTabItem Tab = Tabs[i];
                        if (Tab.Content != null)
                            UnloadTab(Tab.Content);
                    }
                }
            }
        }
        public void ForceUnloadTab(int Id)
        {
            BrowserTabItem _Tab = GetBrowserTabWithId(Id);
            if (_Tab?.Content != null)
                UnloadTab(_Tab.Content, true);
        }
        public void UnloadTab(Browser BrowserView, bool Bypass = false)
        {
            if (Bypass || BrowserView.CanUnload())
                BrowserView.Unload();
        }
        public void Favourite(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            _Tab.Content?.FavouriteAction();
        }
        public void ToggleMute(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            _Tab.Content?.ToggleMute();
        }
        public void Undo(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            if (_Tab.Content == null)
                return;
            if (_Tab.Content.CanGoBack)
                _Tab.Content.Back();
        }
        public void Redo(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            if (_Tab.Content == null)
                return;
            if (_Tab.Content.CanGoForward)
                _Tab.Content.Forward();
        }
        public void Refresh(string Id = "", bool IgnoreCache = false)
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            if (_Tab.Content == null)
                return;
            if (!_Tab.Content.IsLoading)
                _Tab.Content.Refresh(IgnoreCache);
            else
                _Tab.Content.Stop();
        }
        public void CreateGroup()
        {
            DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Create Group", [new() { Name = "Name", IsRequired = true, Type = DialogInputType.Text, Value = "" }, new() { Name = "Color", IsRequired = true, Type = DialogInputType.Color, Value = Utils.ColorToHex(Colors.White) }], "\xF5ED")
            {
                Topmost = true
            };
            if (_DynamicDialogWindow.ShowDialog() == true)
                NewTabGroup(_DynamicDialogWindow.InputFields[0].Value.Trim(), Utils.HexToColor(_DynamicDialogWindow.InputFields[1].Value.Trim()));
        }
        public void Ungroup(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            List<BrowserTabItem> ProcessTabs = Tabs.Where(i => i.TabGroup == _Tab.TabGroup).ToList();
            Tabs.Remove(_Tab);
            foreach (BrowserTabItem _FTab in ProcessTabs)
                _FTab.TabGroup = null;
            TabGroups.Remove(_Tab.TabGroup);
        }
        public void ModifyGroup(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            TabGroup Group = _Tab.TabGroup;
            DynamicDialogWindow _DynamicDialogWindow = new("Prompt", "Edit Group", [new() { Name = "Name", IsRequired = true, Type = DialogInputType.Text, Value = Group.Header }, new() { Name = "Color", IsRequired = true, Type = DialogInputType.Color, Value = Utils.ColorToHex(Group.Background.Color) }], "\xe70f")
            {
                Topmost = true
            };
            if (_DynamicDialogWindow.ShowDialog() == true)
            {
                Group.Header = _DynamicDialogWindow.InputFields[0].Value.Trim();
                Group.Background = new SolidColorBrush(Utils.HexToColor(_DynamicDialogWindow.InputFields[1].Value.Trim()));
            }
        }
        public void Navigate(string Url)
        {
            GetTab().Content?.Navigate(Url);
        }
        public bool IsFullscreen;
        private DispatcherTimer FullscreenPopupTimer;
        public void Fullscreen(bool Fullscreen, Browser BrowserView = null)
        {
            MaximizeRestoreButton.Tag = null;
            bool Changed = IsFullscreen != Fullscreen;
            IsFullscreen = Fullscreen;
            if (Changed)
            {
                BrowserView ??= GetTab().Content;
                if (BrowserView != null)
                {
                    if (Fullscreen)
                    {
                        BrowserView.ToggleSideBar(true);
                        BrowserView.CoreContainerSizeEmulator.Children.Remove(BrowserView.CoreContainer);
                        FullscreenContainer.Children.Add(BrowserView.CoreContainer);
                        MainWindowChrome.CaptionHeight = 0;
                        ResizeMode = ResizeMode.NoResize;
                        //WARNING: The removal of the following WindowState will result in the taskbar being visible.
                        WindowState = WindowState.Normal;
                        WindowStyle = WindowStyle.ToolWindow;
                        WindowState = WindowState.Maximized;
                        if (bool.Parse(App.Instance.GlobalSave.Get("FullscreenPopup")))
                        {
                            FullscreenPopup.IsOpen = true;
                            FullscreenPopupTimer = new() { Interval = TimeSpan.FromSeconds(3) };
                            FullscreenPopupTimer.Tick += FullscreenPopupTimer_Tick;
                            FullscreenPopupTimer.Start();
                        }
                    }
                    else
                    {
                        FullscreenContainer.Children.Remove(BrowserView.CoreContainer);
                        BrowserView.CoreContainerSizeEmulator.Children.Add(BrowserView.CoreContainer);
                        MainWindowChrome.CaptionHeight = CaptionHeight.Height.Value;
                        ResizeMode = ResizeMode.CanResize;
                        BrowserView.ToolBar.Visibility = Visibility.Visible;
                        WindowStyle = WindowStyle.SingleBorderWindow;
                        FullscreenPopupTimer_Tick(null, null);
                    }
                    Keyboard.Focus(BrowserView.WebView?.Control);
                }
            }
        }
        private void FullscreenPopupTimer_Tick(object? sender, EventArgs e)
        {
            FullscreenPopup.IsOpen = false;
            FullscreenPopupTimer?.Stop();
            FullscreenPopupTimer = null;
        }

        public void DevTools(string Id = "")
        {
            BrowserTabItem _Tab = string.IsNullOrEmpty(Id) ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(int.Parse(Id));
            _Tab.Content?.DevTools();
        }
        public TabGroup NewTabGroup(string Header, Color Background, int Index = -1, bool IsCollapsed = false)
        {
            TabGroup NewTabGroup = new(this) { Background = new SolidColorBrush(Background), Header = Header, IsCollapsed = IsCollapsed };
            TabGroups.Add(NewTabGroup);
            BrowserTabItem _Tab = new(this) { TabGroup = NewTabGroup, Type = BrowserTabType.Group };
            if (VerticalTabs)
                Tabs.Insert(Index != -1 ? Index : Tabs.Count, _Tab);
            else
                Tabs.Insert(Index != -1 ? Index : Tabs.Count - 1, _Tab);
            return NewTabGroup;
        }
        public IWebView NewTab(string Url, bool IsSelected = false, int Index = -1, bool IsPrivate = false, TabGroup? Group = null)
        {
            if (!App.Instance.Background && WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                Activate();
            }
            if (Index == -1)
                Index = VerticalTabs ? Tabs.Count : Tabs.Count - 1;
            if (Group == null)
            {
                BrowserTabItem LeftTab = null;
                BrowserTabItem RightTab = null;
                if (Index > 0)
                    LeftTab = Index > Tabs.Count ? Tabs.Last() : Tabs[Index - 1];
                if (Index < Tabs.Count - 1)
                    RightTab = Tabs[Index];

                if (LeftTab != null && RightTab != null && LeftTab.TabGroup != null && (/*LeftTab.Type == BrowserTabType.Group || */LeftTab.TabGroup == RightTab.TabGroup))
                    Group = LeftTab.TabGroup;
            }
            BrowserTabItem _Tab = new(this) { Header = Utils.CleanUrl(Url, true, true, true, true), TabGroup = Group };
            _Tab.Content = new Browser(Url, _Tab, IsPrivate);
            Tabs.Insert(Index, _Tab);
            if (IsSelected)
                TabsUI.SelectedIndex = Tabs.IndexOf(_Tab);
            return _Tab.Content.WebView;
        }

        public BrowserTabItem GetBrowserTabWithId(int Id)
        {
            foreach (BrowserTabItem _Tab in Tabs)
            {
                if (_Tab.ID == Id)
                    return _Tab;
            }
            return null;
        }
        public void CloseTab(int Id, int WindowId)
        {
            ShowPreview(null);
            if (WindowId != ID)
            {
                foreach (MainWindow _Window in App.Instance.AllWindows)
                {
                    if (_Window.ID == WindowId)
                    {
                        _Window.CloseTab(Id, WindowId);
                        return;
                    }
                }
                return;
            }
            BrowserTabItem _Tab = Id == -1 ? Tabs[TabsUI.SelectedIndex] : GetBrowserTabWithId(Id);
            if (_Tab.Type == BrowserTabType.Navigation)
            {
                bool IsSelected = Id == -1 || _Tab == Tabs[TabsUI.SelectedIndex];
                _Tab.Content?.Dispose();
                if (IsSelected && Tabs.Any(i => i != _Tab && i.Type == BrowserTabType.Navigation))
                {
                    int SelectedIndex = TabsUI.SelectedIndex;
                    if (SelectedIndex > 0)
                        SelectedIndex--;
                    else
                        SelectedIndex++;
                    if (SelectedIndex > Tabs.Count - 1)
                        SelectedIndex = Tabs.Count - 1;
                    if (Tabs[SelectedIndex].Type != BrowserTabType.Navigation)
                    {
                        BrowserTabItem? TargetTab = Tabs.Where(i => i != _Tab && i.Type == BrowserTabType.Navigation).OrderBy(i => SelectedIndex - Tabs.IndexOf(i)).FirstOrDefault();
                        if (TargetTab != null)
                            SelectedIndex = Tabs.IndexOf(TargetTab);
                        else
                            SelectedIndex = -1;
                    }
                    if (SelectedIndex == -1)
                        TabsUI.SelectedItem = null;
                    else
                        TabsUI.SelectedIndex = SelectedIndex;
                }
                Tabs.Remove(_Tab);
            }
            else if (_Tab.Type == BrowserTabType.Group)
            {
                List<BrowserTabItem> ProcessTabs = Tabs.Where(i => i.TabGroup == _Tab.TabGroup).ToList();
                for (int i = 0; i < ProcessTabs.Count; i++)
                {
                    var _FTab = ProcessTabs[i];
                    bool IsSelected = Id == -1 || _FTab == Tabs[TabsUI.SelectedIndex];
                    _Tab.Content?.Dispose();
                    Tabs.Remove(_FTab);
                    if (IsSelected && Tabs.Any(i => i.Type == BrowserTabType.Navigation))
                    {
                        int SelectedIndex = TabsUI.SelectedIndex;
                        if (SelectedIndex > 0)
                            SelectedIndex--;
                        else
                            SelectedIndex++;
                        if (SelectedIndex > Tabs.Count - 1)
                            SelectedIndex = Tabs.Count - 1;
                        if (Tabs[SelectedIndex].Type != BrowserTabType.Navigation)
                        {
                            BrowserTabItem? TargetTab = Tabs.Where(i => i != _Tab && i.Type == BrowserTabType.Navigation).OrderBy(i => SelectedIndex - Tabs.IndexOf(i)).FirstOrDefault();
                            if (TargetTab != null)
                                SelectedIndex = Tabs.IndexOf(TargetTab);
                            else
                                SelectedIndex = -1;
                        }
                        if (SelectedIndex == -1)
                            TabsUI.SelectedItem = null;
                        else
                            TabsUI.SelectedIndex = SelectedIndex;
                    }
                }
                Tabs.Remove(_Tab);
            }
            if (!Tabs.Any(i => i.Type == BrowserTabType.Navigation))
            {
                TabsUI.Visibility = Visibility.Collapsed;
                Close();
            }
        }

        public void Find(string Text = "")
        {
            GetTab().Content?.Find(Text);
        }
        public void StopFind()
        {
            GetTab().Content?.StopFind();
        }
        public void Screenshot()
        {
            GetTab().Content?.Screenshot();
        }

        /*public void Zoom(int Delta)
        {
            GetTab().Content?.Zoom(Delta);
        }*/

        public BrowserTabItem GetTab(Browser _Control = null)
        {
            if (_Control != null)
            {
                foreach (BrowserTabItem _Tab in Tabs)
                {
                    if (_Tab.Content == _Control)
                        return _Tab;
                }
            }
            else if (TabsUI != null && Tabs.Count != 0 && TabsUI.SelectedIndex != -1)
            {
                try
                {
                    return Tabs[TabsUI.SelectedIndex];
                }
                catch { }
            }
            return null;
        }

        bool WindowExcludedFromCapture = false;

        public void SetWindowDisplayAffinity()
        {
            if (Handle == null || Tabs.Count == 0 || TabsUI.SelectedIndex == -1)
                return;
            BrowserTabItem CurrentTab = Tabs[TabsUI.SelectedIndex];
            bool ExcludeFromCapture = App.Instance.BlockScreenCapture == 2 || (App.Instance.BlockScreenCapture == 1 && (CurrentTab?.Content?.Private ?? false));
            if (WindowExcludedFromCapture != ExcludeFromCapture)
            {
                DllUtils.SetWindowDisplayAffinity(Handle.Value, ExcludeFromCapture ? DllUtils.WindowDisplayAffinity.WDA_EXCLUDEFROMCAPTURE : DllUtils.WindowDisplayAffinity.WDA_NONE);
                WindowExcludedFromCapture = ExcludeFromCapture;
            }
        }

        BrowserTabItem? PreviousTab;
        private void TabsUI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                BrowserTabItem _CurrentTab = Tabs[TabsUI.SelectedIndex];
                if (PreviousTab != null && PreviousTab != _CurrentTab)
                {
                    Fullscreen(false, PreviousTab.Content);
                    PreviousTab.Content?.UnFocus();
                }
                if (_CurrentTab.Type != BrowserTabType.Add)
                {
                    if (_CurrentTab.Content != null)
                    {
                        _CurrentTab.Content.ReFocus();
                        Keyboard.Focus(_CurrentTab.Content.WebView?.Control);
                    }
                    SetWindowDisplayAffinity();
                    Title = _CurrentTab.Header + " - SLBr";
                }
                PreviousTab = _CurrentTab;
            }
            catch
            {
                Title = "SLBr";
            }
        }

        public void ExecuteCloseEvent()
        {
            foreach (BrowserTabItem Tab in Tabs)
                Tab.Content?.ToggleSideBar(true);
            if (App.Instance.AllWindows.Count == 1)
                App.Instance.CloseSLBr(false);
            else if (App.Instance.WindowsSaves.Count == App.Instance.AllWindows.Count)
                App.Instance.WindowsSaves.RemoveAt(App.Instance.WindowsSaves.Count - 1);
            App.Instance.AllWindows.Remove(this);
        }

        public async void ShowPreview(BrowserTabItem? Tab, FrameworkElement Anchor = null)
        {
            MaximizeRestoreButton.Tag = null;
            if (Tab == null)
                TabPreviewPopup.IsOpen = false;
            else
            {
                TabPreviewHeader.Text = Tab.Header.ReplaceLineEndings("").Trim().Cut(50, true);
                TabPreviewState.Text = Tab.IsUnloaded ? "Unloaded" : "Loaded";
                //TabPreviewImage.Source = null;
                try
                {
                    if (Tab?.Content != null)
                    {
                        TabPreviewHost.Text = Utils.HostOnlyHTTP(Tab?.Content?.Address).ReplaceLineEndings("").Trim().Cut(50, true);
                        if (App.Instance.TabMemory && !Tab.IsUnloaded && Tab?.Content?.WebView != null && Tab?.Content?.WebView?.Engine != WebEngineType.Trident)
                            TabPreviewState.Text = $"Memory usage: {await Tab?.Content?.WebView?.EvaluateScriptAsync(Scripts.EstimatedMemoryUsageScript)} MB";
                        //Rich Tab Tooltips OGP proof of concept.
                        //TODO
                        /*if (!Tab.IsUnloaded && Tab?.Content?.WebView != null && Tab?.Content?.WebView?.Engine != WebEngineType.Trident)
                        {
                            string? Response = (await Tab?.Content?.WebView.EvaluateScriptAsync(Scripts.FetchOpenGraphProtocolScript)).ToString();
                            if (!string.IsNullOrEmpty(Response))
                            {
                                using var Document = JsonDocument.Parse(Response);
                                if (Document.RootElement.TryGetProperty("title", out JsonElement TitleElement) && TitleElement.ValueKind != JsonValueKind.Null)
                                {
                                    string Header = TitleElement.GetString();
                                    if (!string.IsNullOrEmpty(Header))
                                        TabPreviewHeader.Text = TitleElement.GetString().ReplaceLineEndings("").Trim().Cut(50, true);
                                }
                                if (Document.RootElement.TryGetProperty("description", out JsonElement DescriptionElement) && DescriptionElement.ValueKind != JsonValueKind.Null)
                                    TabPreviewHost.Text = DescriptionElement.GetString().ReplaceLineEndings("").Trim().Cut(50, true);
                                if (Document.RootElement.TryGetProperty("image", out JsonElement ImageElement) && ImageElement.ValueKind != JsonValueKind.Null)
                                {
                                    BitmapImage _BitmapImage = new();
                                    _BitmapImage.BeginInit();
                                    _BitmapImage.DecodePixelWidth = 275;
                                    _BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                    _BitmapImage.UriSource = new Uri(Utils.ResolveUrl(Tab?.Content?.Address, ImageElement.GetString()), UriKind.Absolute);
                                    _BitmapImage.EndInit();
                                    TabPreviewImage.Source = _BitmapImage;
                                }
                                else
                                    TabPreviewImage.Source = null;
                            }
                        }*/
                    }
                }
                catch
                {
                    TabPreviewHost.Text = Tab.Header.ReplaceLineEndings("").Trim().Cut(50, true);
                }
                if (Tab.IsUnloaded)
                {
                    TabPreviewStateIcon.Text = "\xf1e8";
                    TabPreviewStateIcon.Foreground = App.Instance.GreenColor;
                }
                else
                {
                    //TODO: Replace with audio indicator.
                    TabPreviewStateIcon.Text = "\xf42f";
                    TabPreviewStateIcon.Foreground = (SolidColorBrush)FindResource("GrayBrush");
                }
                TabPreviewImage.Source = Tab.Preview;
                TabPreviewImage.UpdateLayout();
                TabPreviewPopup.PlacementTarget = Anchor;
                TabPreviewPopup.IsOpen = true;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            ExecuteCloseEvent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
            SystemCommands.MinimizeWindow(this);

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            SystemCommands.CloseWindow(this);

        private void ToastPopup_MouseDown(object sender, MouseButtonEventArgs e) =>
            ToastPopup.IsOpen = false;

        public void OpenToast(string Text, string Icon)
        {
            if (!bool.Parse(App.Instance.GlobalSave.Get("Toast")))
                return;
            ToastPopup.IsOpen = true;
            ToastHeader.Text = Text;
            ToastIcon.Text = Icon;
        }
    }
}