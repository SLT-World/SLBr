/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.WebView;
using System.Runtime.InteropServices;

namespace SLBr.Tests.Helpers
{
    public class UserAgentTests
    {
        [Theory]
        [InlineData("Windows NT 10.0; Win64; x64", "SLBr/1.0", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) SLBr/1.0 Safari/537.36")]
        [InlineData("Linux; Android 10; K", "Chrome/148.0.0.0 Mobile", "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Mobile Safari/537.36")]
        public void BuildUserAgentFromOSAndProduct_ConstructsValidUserAgent(string OSInfo, string Product, string Expected)
        {
            string Actual = UserAgentGenerator.BuildUserAgentFromOSAndProduct(OSInfo, Product);
            Assert.Equal(Expected, Actual);
        }

        [Fact]
        public void BuildUserAgentFromProduct_ContainsMobileSuffixAndAndroidOs()
        {
            string Product = "SLBr";
            string Actual = UserAgentGenerator.BuildUserAgentFromProduct(Product, PlatformID.Unix);

            Assert.Equal("Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) SLBr Mobile Safari/537.36", Actual);
        }

        [Fact]
        public void GetOSVersion_MatchesEnvironmentMajorMinor()
        {
            string Actual = UserAgentGenerator.GetOSVersion();
            Version SystemVersion = Environment.OSVersion.Version;
            string Expected = $"{SystemVersion.Major}.{SystemVersion.Minor}";

            Assert.Equal(Expected, Actual);
        }

        [Fact]
        public void GetCPUArchitecture_ReturnsExpectedValueForCurrentSystem()
        {
            string Actual = UserAgentGenerator.GetCPUArchitecture();
            Architecture CurrentArch = RuntimeInformation.ProcessArchitecture;
            if (CurrentArch == Architecture.Arm || CurrentArch == Architecture.Arm64)
                Assert.Equal("arm", Actual);
            else
                Assert.Equal("x86", Actual);
        }

        [Fact]
        public void IsWindows11OrGreater_ReturnsExpected()
        {
            bool Expected = Environment.OSVersion.Version >= new Version(10, 0, 22000);
            bool Actual = UserAgentGenerator.IsWindows11OrGreater;
            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData(84, "Not;A=Brand", "8")]
        [InlineData(86, "Not?A_Brand", "24")]
        public void GetGreasedUserAgentBrandVersion_ReturnsExpectedBrandVersion(int Seed, string ExpectedBrand, string ExpectedVersion)
        {
            WebUserAgentBrand Actual = UserAgentGenerator.GetGreasedUserAgentBrandVersion(Seed);
            Assert.Equal(ExpectedBrand, Actual.Brand);
            Assert.Equal(ExpectedVersion, Actual.Version);
        }
    }
}