/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.WebView;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SLBr.Managers
{
    public class CdnManager
    {
        private List<CdnEntry> Entries = [];

        public readonly FastHashSet<string> Domains = [];
        private readonly ConcurrentDictionary<string, (string Resolved, string? Latest)> UrlCache = new();

        public CdnManager(string CacheDirectory)
        {
            Domains.Add("ajax.googleapis.com");
            Domains.Add("fonts.googleapis.com");
            Domains.Add("fonts.gstatic.com");
            Domains.Add("cdnjs.cloudflare.com");
            Domains.Add("cdn.jsdelivr.net");
            Domains.Add("unpkg.com");
            Domains.Add("ajax.aspnetcdn.com");
            Domains.Add("ajax.microsoft.com");
            Domains.Add("maxcdn.bootstrapcdn.com");
            Domains.Add("stackpath.bootstrapcdn.com");

            Entries.Add(new CdnEntry(
                "Google Hosted Libraries",
                @"^https://ajax\.googleapis\.com/ajax/libs/(?<l>[^/]+)/(?<v>\d+(?:\.\d+)*[-a-zA-Z0-9.]*)/(?<p>[^?#]+)(?:\?.*|#.*)?$",
                Path.Combine(CacheDirectory, "google", "{l}", "{v}", "{p}")
            ));

            Entries.Add(new CdnEntry(
                "Cloudflare cdnjs",
                @"^https://cdnjs\.cloudflare\.com/ajax/libs/(?<l>[^/]+)/(?<v>\d+(?:\.\d+)*[-a-zA-Z0-9.]*)/(?<p>[^?#]+)(?:\?.*|#.*)?$",
                Path.Combine(CacheDirectory, "cloudflare", "{l}", "{v}", "{p}")
            ));

            Entries.Add(new CdnEntry(
                "jsDelivr NPM",
                @"^https://cdn\.jsdelivr\.net/npm/(?<l>[^@/]+)@(?<v>\d+(?:\.\d+)*[-a-zA-Z0-9.]*)/(?<p>[^?#]+)(?:\?.*|#.*)?$",
                Path.Combine(CacheDirectory, "jsdelivr-npm", "{l}", "{v}", "{p}")
            ));

            Entries.Add(new CdnEntry(
                "UNPKG",
                @"^https://unpkg\.com/(?<l>[^@/]+)@(?<v>\d+(?:\.\d+)*[-a-zA-Z0-9.]*)/(?<p>[^?#]+)(?:\?.*|#.*)?$",
                Path.Combine(CacheDirectory, "unpkg", "{l}", "{v}", "{p}")
            ));

            Entries.Add(new CdnEntry(
                "Microsoft Ajax",
                @"^https://ajax\.(?:aspnetcdn|microsoft)\.com/ajax/(?<l>[^/]+)/(?<n>[^/?#]+\-(?<v>\d+(?:\.\d+)*)\.[^/?#]+)(?:\?.*|#.*)?$",
                Path.Combine(CacheDirectory, "microsoft", "{l}", "{v}", "{n}")
            ));

            Entries.Add(new CdnEntry(
                "Microsoft Ajax Nested",
                @"^https://ajax\.(?:aspnetcdn|microsoft)\.com/ajax/(?<l>[^/]+)/(?<v>\d+(?:\.\d+)*)/(?<p>[^?#]+)(?:\?.*|#.*)?$",
                Path.Combine(CacheDirectory, "microsoft", "{l}", "{v}", "{p}")
            ));

            Entries.Add(new CdnEntry(
                "Google Fonts",
                @"^https://fonts\.googleapis\.com/(?<a>css|css2)\?(?<q>[^#]+)(?:#.*)?$",
                Path.Combine(CacheDirectory, "google-fonts", "{a}", "{q}.css")
            ));

            Entries.Add(new CdnEntry(
                "Google Fonts Assets",
                @"^https://fonts\.gstatic\.com/s/(?<f>[^/]+)/(?<v>v?\d+(?:\.\d+)*[-a-zA-Z0-9.]*)/(?<n>[^/?#]+\.(?:woff2|woff|ttf|otf))((?:\?.*|#.*)?)$",
                Path.Combine(CacheDirectory, "google-fonts-assets", "{f}", "{v}", "{n}")
            ));

            Entries.Add(new CdnEntry(
                "BootstrapCDN",
                @"^https://(?:maxcdn|stackpath)\.bootstrapcdn\.com/(?<l>[^/]+)/(?<v>\d+(?:\.\d+)*[-a-zA-Z0-9.]*)/(?<p>[^?#]+)(?:\?.*|#.*)?$",
                Path.Combine(CacheDirectory, "bootstrap", "{l}", "{v}", "{p}")
            ));
        }

        public bool TryMatch(string Url, out string? ResolvedPath)
        {
            if (UrlCache.TryGetValue(Url, out var Cache))
            {
                ResolvedPath = Cache.Resolved;
                return ResolvedPath != null;
            }
            foreach (CdnEntry Entry in Entries)
            {
                if (Entry.TryMatch(Url, out ResolvedPath) && ResolvedPath != null)
                {
                    UrlCache[Url] = (ResolvedPath, null);
                    return true;
                }
            }
            UrlCache[Url] = (null, null);
            ResolvedPath = null;
            return false;
        }

        public bool TryMatchLatest(string Url, out string? LatestPath)
        {
            if (UrlCache.TryGetValue(Url, out var Cache))
            {
                LatestPath = string.IsNullOrEmpty(Cache.Latest) ? Cache.Resolved : Cache.Latest;
                return LatestPath != null;
            }
            foreach (CdnEntry Entry in Entries)
            {
                if (Entry.TryMatchLatest(Url, out string? ResolvedPath, out string? LatestLocalPath) && ResolvedPath != null)
                {
                    UrlCache[Url] = (ResolvedPath, LatestLocalPath);
                    LatestPath = string.IsNullOrEmpty(LatestLocalPath) ? ResolvedPath : LatestLocalPath;
                    return true;
                }
            }
            UrlCache[Url] = (null, null);
            LatestPath = null;
            return false;
        }

        public bool IsValidCdn(string Url, ResourceRequestType ResourceType) =>
            !(ResourceType is ResourceRequestType.MainFrame or ResourceRequestType.SubFrame or ResourceRequestType.NavigationPreLoadMainFrame or ResourceRequestType.NavigationPreLoadSubFrame or ResourceRequestType.CSPReport or ResourceRequestType.Ping or ResourceRequestType.Prefetch or ResourceRequestType.PluginResource) && Domains.Contains(Utils.FastHost(Url, false));
            //Entries.Any(i => i.Matches(Url));
    }

    public class CdnEntry(string _Name, string UrlPattern, string _LocalPathTemplate)
    {
        public string Name { get; } = _Name;
        private Regex UrlRegex = new(UrlPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private string LocalPathTemplate = _LocalPathTemplate;

        public bool Matches(string Url)
        {
            return UrlRegex.IsMatch(Url);
        }

        public bool TryMatch(string Url, out string? ResolvedPath)
        {
            ResolvedPath = null;
            //Url = Url.ToLowerInvariant();
            Match _Match = UrlRegex.Match(Url);
            if (!_Match.Success)
                return false;
            string LocalPath = LocalPathTemplate;
            foreach (Group _Group in _Match.Groups.Cast<Group>())
            {
                if (_Group.Success && !int.TryParse(_Group.Name, out _))
                {
                    string Value = _Group.Value;
                    if (_Group.Name == "q")
                        Value = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Value)));//Note: Google Fonts CSS configuration.
                    LocalPath = LocalPath.Replace($"{{{_Group.Name}}}", Value);
                }
            }
            ResolvedPath = Path.GetFullPath(LocalPath);
            return true;
        }

        public bool TryMatchLatest(string Url, out string? ResolvedPath, out string? LatestLocalPath)
        {
            ResolvedPath = null;
            LatestLocalPath = null;
            //Url = Url.ToLowerInvariant();
            Match _Match = UrlRegex.Match(Url);
            if (!_Match.Success)
                return false;
            string LocalPath = LocalPathTemplate;
            string? _Version = null;
            foreach (Group _Group in _Match.Groups.Cast<Group>())
            {
                if (_Group.Success && !int.TryParse(_Group.Name, out _))
                {
                    string Value = _Group.Value;
                    if (_Group.Name == "q")
                        Value = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Value)));//Note: Google Fonts CSS configuration.
                    else if (_Group.Name == "v")
                        _Version = Value;
                    LocalPath = LocalPath.Replace($"{{{_Group.Name}}}", Value);
                }
            }
            ResolvedPath = Path.GetFullPath(LocalPath);
            if (!string.IsNullOrEmpty(_Version) && Version.TryParse(CleanVersionString(_Version), out Version? RequestedVersion))
                LatestLocalPath = FindLatestLocalVersionPath(ResolvedPath, _Version, RequestedVersion);
            return true;
        }

        private string CleanVersionString(string Version)
        {
            return new string(Version.Where(i => char.IsDigit(i) || i == '.').ToArray()).Trim('.');
        }

        private string? FindLatestLocalVersionPath(string OriginalPath, string OriginalVersionToken, Version RequestedVersion)
        {
            try
            {
                string VersionPlaceholder = $"\\{Path.DirectorySeparatorChar}{OriginalVersionToken}\\{Path.DirectorySeparatorChar}";
                int Index = OriginalPath.IndexOf(Path.DirectorySeparatorChar + OriginalVersionToken + Path.DirectorySeparatorChar);
                if (Index < 0)
                    return null;
                string LibraryRootFolder = OriginalPath[..Index];
                if (!Directory.Exists(LibraryRootFolder))
                    return null;

                string[] Directories = Directory.GetDirectories(LibraryRootFolder);
                Version HighestCompatibleVersion = RequestedVersion;
                string HighestVersionToken = OriginalVersionToken;

                foreach (string DirectoryPath in Directories)
                {
                    string DirectoryName = Path.GetFileName(DirectoryPath);
                    if (Version.TryParse(CleanVersionString(DirectoryName), out Version? LocalVersion))
                    {
                        if (LocalVersion.Major == RequestedVersion.Major && LocalVersion > HighestCompatibleVersion)
                        {
                            HighestCompatibleVersion = LocalVersion;
                            HighestVersionToken = DirectoryName;
                        }
                    }
                }

                if (HighestVersionToken != OriginalVersionToken)
                {
                    string UpdatedPath = OriginalPath.Replace(Path.DirectorySeparatorChar + OriginalVersionToken + Path.DirectorySeparatorChar, Path.DirectorySeparatorChar + HighestVersionToken + Path.DirectorySeparatorChar);
                    if (File.Exists(UpdatedPath))
                        return UpdatedPath;
                }
            }
            catch { }
            return null;
        }
    }
}
