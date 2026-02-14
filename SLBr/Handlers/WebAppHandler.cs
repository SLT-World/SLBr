/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace SLBr.Handlers
{
    public static class WebAppHandler
    {
        private static string AppsFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SLBr", "Apps");
        private static string StartMenuFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "SLBr Apps");

        static WebAppHandler()
        {
            Directory.CreateDirectory(AppsFolder);
            Directory.CreateDirectory(StartMenuFolder);
        }

        public static ManifestIcon PickBestIcon(WebAppManifest Manifest)
        {
            if (Manifest?.Icons == null || Manifest.Icons.Count == 0)
                return null;
            ManifestIcon TryFind(int Size, string Purpose = null)
            {
                return Manifest.Icons.FirstOrDefault(i => (i.Sizes ?? string.Empty).Split(' ').Any(s => s.Trim().Equals($"{Size}x{Size}")) && (Purpose == null || (i.Purpose ?? "any").Contains(Purpose)));
            }
            return TryFind(512, "maskable") ?? TryFind(512) ?? TryFind(256, "maskable") ?? TryFind(256) ?? Manifest.Icons.LastOrDefault();
        }

        //TODO: Figure out a way to generate an ico that can be used as an icon for shortcuts. Tried and failed without utilizing external libraries.
        public static void SaveAsIcon(BitmapSource Image, string FilePath, int Size = 256)
        {
            TransformedBitmap Scaled = new TransformedBitmap(Image, new System.Windows.Media.ScaleTransform((double)Size / Image.PixelWidth, (double)Size / Image.PixelHeight));
            PngBitmapEncoder Encoder = new PngBitmapEncoder();
            Encoder.Frames.Add(BitmapFrame.Create(Scaled));

            using (var Stream = new MemoryStream())
            using (var _FileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
            {
                Encoder.Save(Stream);
                byte[] Bytes = Stream.ToArray();
                using (var Writer = new BinaryWriter(_FileStream))
                {
                    Writer.Write((short)0);
                    Writer.Write((short)1);
                    Writer.Write((short)1);

                    Writer.Write((byte)Size);
                    Writer.Write((byte)Size);
                    Writer.Write((byte)0);
                    Writer.Write((byte)0);
                    Writer.Write((short)1);
                    Writer.Write((short)32);
                    Writer.Write(Bytes.Length);
                    Writer.Write(6 + 16);
                    Writer.Write(Bytes);
                }
            }
        }

        public static async Task Install(WebAppManifest Manifest)
        {
            ManifestIcon Best = PickBestIcon(Manifest);
            string ID = Utils.SanitizeFileName(Manifest.StartUrl);
            string ManifestPath = Path.Combine(AppsFolder, $"{ID}.json");
            File.WriteAllText(ManifestPath, JsonSerializer.Serialize(Manifest, new JsonSerializerOptions { WriteIndented = false }));
            string ImagePath = Path.Combine(AppsFolder, $"{ID}.ico");
            if (Best != null)
            {
                try
                {
                    using (HttpClient Client = new HttpClient())
                    {
                        using MemoryStream Stream = new MemoryStream(await Client.GetByteArrayAsync(Best.Source));
                        var Decoder = BitmapDecoder.Create(Stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        var Frame = Decoder.Frames[0];
                        if (Frame.CanFreeze)
                            Frame.Freeze();
                        try
                        {
                            using (FileStream ImageStream = new FileStream(ImagePath, FileMode.Create))
                            {
                                BitmapEncoder Encoder = new PngBitmapEncoder();
                                Encoder.Frames.Add(Frame);
                                Encoder.Save(ImageStream);
                                SaveAsIcon(Frame, ImagePath);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            string AppName = !string.IsNullOrWhiteSpace(Manifest.Name) ? Manifest.Name : new Uri(Manifest.StartUrl).Host;
            string ShortcutPath = Path.Combine(StartMenuFolder, $"{Utils.SanitizeFileName(AppName)}.lnk");
            CreateShortcut(ShortcutPath, AppName, ID);

            string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CreateShortcut(Path.Combine(Desktop, $"{Utils.SanitizeFileName(AppName)}.lnk"), AppName, ID);
        }

        private static void CreateShortcut(string ShortcutPath, string Name, string ID)
        {
            ShortcutCreator.CreateShortcut(ShortcutPath, App.Instance.ExecutablePath, $"--app=\"{ID}\"", $"{Name} App", App.Instance.ExecutablePath);
        }

        public static async Task<WebAppManifest?> FetchManifestAsync(string PageUrl, string ManifestUrl)
        {
            string ResolvedUrl = Utils.ResolveUrl(PageUrl, ManifestUrl);
            using (HttpClient Client = new HttpClient())
            {
                string Json = await Client.GetStringAsync(ResolvedUrl);
                WebAppManifest? Manifest = JsonSerializer.Deserialize<WebAppManifest>(Json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (Manifest != null)
                {
                    Manifest.StartUrl = Utils.ResolveUrl(PageUrl, string.IsNullOrWhiteSpace(Manifest.StartUrl) ? "/" : Manifest.StartUrl);
                    foreach (ManifestIcon _ManifestIcon in Manifest.Icons ?? new())
                        _ManifestIcon.Source = Utils.ResolveUrl(ResolvedUrl, string.IsNullOrWhiteSpace(_ManifestIcon.Source) ? "/" : _ManifestIcon.Source);
                }
                return Manifest;
            }
        }

        public static WebAppManifest? LoadManifest(string Json)
        {
            return JsonSerializer.Deserialize<WebAppManifest>(Json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    public static class ShortcutCreator
    {
        public static void CreateShortcut(string ShortcutPath, string TargetPath, string Arguments, string Description, string IconPath = null)
        {
            IShellLinkW Link = (IShellLinkW)new ShellLink();
            Link.SetDescription(Description);
            Link.SetPath(TargetPath);
            if (!string.IsNullOrEmpty(Arguments))
                Link.SetArguments(Arguments);
            if (!string.IsNullOrEmpty(IconPath))
                Link.SetIconLocation(IconPath, 0);

            IPersistFile File = (IPersistFile)Link;
            File.Save(ShortcutPath, false);
        }
    }
}
