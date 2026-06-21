/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.Managers;

namespace SLBr.Tests.Managers
{
    public class CdnManagerTests
    {
        private string CdnPath;
        private CdnManager CdnManager;

        public CdnManagerTests()
        {
            CdnPath = Path.Combine(Path.GetTempPath(), "CdnManagerTests");
            CdnManager = new CdnManager(CdnPath);
        }

        [Theory]
        [InlineData("https://cdnjs.cloudflare.com/ajax/libs/react/19.1.1/cjs/react.production.min.js?version=19.1.1",
            "cloudflare/react/19.1.1/cjs/react.production.min.js",
            true)]
        [InlineData("https://cdnjs.cloudflare.com/ajax/libs/react/19.1.1/cjs/react.production.min.js",
            "cloudflare/react/19.1.1/cjs/react.production.min.js",
            true)]
        [InlineData("https://ajax.googleapis.com/ajax/libs/bootstrap/5.3.3/js/bootstrap.min.js",
            "google/bootstrap/5.3.3/js/bootstrap.min.js",
            true)]
        [InlineData("https://ajax.googleapis.com/ajax/libs/jquery/1/jquery.js",
            "google/jquery/1/jquery.js",
            true)]
        [InlineData("https://cdn.jsdelivr.net/npm/jquery@3.6.4/dist/jquery.min.js",
            "jsdelivr-npm/jquery/3.6.4/dist/jquery.min.js",
            true)]
        [InlineData("https://ajax.microsoft.com/ajax/jquery/jquery-1.8.0.js",
            "microsoft/jquery/1.8.0/jquery-1.8.0.js",
            true)]
        [InlineData("https://ajax.aspnetcdn.com/ajax/jquery/jquery-1.9.0.min.js",
            "microsoft/jquery/1.9.0/jquery-1.9.0.min.js",
            true)]
        [InlineData("https://ajax.aspnetcdn.com/ajax/bootstrap/4.6.0/js/bootstrap.min.js",
            "microsoft/bootstrap/4.6.0/js/bootstrap.min.js",
            true)]
        [InlineData("https://fonts.gstatic.com/s/materialsymbolsrounded/v352/syl0-zNym6YjUruM-QrEh7-nyTnjDwKNJ_190FjpZIvDmUSVOK7BDB_Qb9vUSzq3wzLK-P0J-V_Zs-QtQth3-jOcbTCVpeRL2w5rwZu2rIelXxc.woff2",
            "google-fonts-assets/materialsymbolsrounded/v352/syl0-zNym6YjUruM-QrEh7-nyTnjDwKNJ_190FjpZIvDmUSVOK7BDB_Qb9vUSzq3wzLK-P0J-V_Zs-QtQth3-jOcbTCVpeRL2w5rwZu2rIelXxc.woff2",
            true)]
        [InlineData("https://maxcdn.bootstrapcdn.com/font-awesome/4.3.0/css/font-awesome.min.css",
            "bootstrap/font-awesome/4.3.0/css/font-awesome.min.css",
            true)]
        [InlineData("https://unpkg.com/preact@10.26.4/dist/preact.min.js",
            "unpkg/preact/10.26.4/dist/preact.min.js",
            true)]
        [InlineData("https://unpkg.com/preact@latest/dist/preact.min.js", null, false)]
        [InlineData("https://example.com", null, false)]
        public void TryMatch_ReturnsExpected(string? Url, string? ExpectedRelativePath, bool ExpectedMatch)
        {
            bool MatchResult = CdnManager.TryMatch(Url, out string? ResolvedPath);
            Assert.Equal(ExpectedMatch, MatchResult);
            if (ExpectedMatch && ExpectedRelativePath != null)
            {
                string ExpectedAbsolutePath = Path.GetFullPath(Path.Combine(CdnPath, ExpectedRelativePath));
                Assert.Equal(ExpectedAbsolutePath, ResolvedPath);
            }
            else
                Assert.Null(ResolvedPath);
        }

        [Theory]
        [InlineData("10.5.0", "10.22.0", true)]
        [InlineData("10.5.0", "11.0.0", false)]
        public void TryMatchLatest_ReturnsExpected(string RequestedVersion, string DiskVersion, bool ShouldUpgrade)
        {
            //TODO: Handle version within filename.
            /*string Url = $"https://ajax.microsoft.com/ajax/jquery/jquery-{RequestedVersion}.js";
            string TemporaryLibraryDirectory = Path.Combine(CdnPath, "microsoft", "jquery", DiskVersion, "dist");
            Directory.CreateDirectory(TemporaryLibraryDirectory);
            File.WriteAllText(Path.Combine(TemporaryLibraryDirectory, $"jquery-{RequestedVersion}.js"), "");*/

            string Url = $"https://maxcdn.bootstrapcdn.com/font-awesome/{RequestedVersion}/css/font-awesome.min.css";
            string TemporaryLibraryDirectory = Path.Combine(CdnPath, "bootstrap", "font-awesome", DiskVersion, "css");
            Directory.CreateDirectory(TemporaryLibraryDirectory);
            File.WriteAllText(Path.Combine(TemporaryLibraryDirectory, "font-awesome.min.css"), "");

            CdnManager.TryMatchLatest(Url, out string? LatestPath);

            string TargetFolderToken = ShouldUpgrade ? DiskVersion : RequestedVersion;
            Assert.Contains($"{Path.DirectorySeparatorChar}{TargetFolderToken}{Path.DirectorySeparatorChar}", LatestPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(CdnPath))
            {
                try
                {
                    Directory.Delete(CdnPath, true);
                }
                catch { }
            }
        }
    }
}
