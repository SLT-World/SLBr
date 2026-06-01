/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Runtime.InteropServices;

namespace SLBr.Tests.Helpers
{
    public class UserAgentTests
    {
        [Theory]
        [InlineData("10.0", "Win64; x64", "Windows NT 10.0; Win64; x64")]
        [InlineData("10.0", "", "Windows NT 10.0")]
        [InlineData("6.1", "WOW64", "Windows NT 6.1; WOW64")]
        public void BuildOSCpuInfoFromOSVersionAndCpuType_FormatsCorrectly(string OSVersion, string CPUType, string Expected)
        {
            string Actual = UserAgentGenerator.BuildOSCpuInfoFromOSVersionAndCpuType(OSVersion, CPUType);
            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData("Windows NT 10.0; Win64; x64", "SLBr/1.0", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) SLBr/1.0 Safari/537.36")]
        [InlineData("Linux; Android 10; K", "Chrome/148.0.0.0 Mobile", "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Mobile Safari/537.36")]
        public void BuildUserAgentFromOSAndProduct_ConstructsValidUserAgent(string OSInfo, string Product, string Expected)
        {
            string Actual = UserAgentGenerator.BuildUserAgentFromOSAndProduct(OSInfo, Product);
            Assert.Equal(Expected, Actual);
        }

        [Fact]
        public void BuildMobileUserAgentFromProduct_ContainsMobileSuffixAndAndroidOs()
        {
            string Product = "SLBr";
            string Actual = UserAgentGenerator.BuildMobileUserAgentFromProduct(Product);

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

        [Fact]
        public void BuildCPUInfo_ReturnsExpectedStringBasedOnProcessBitness()
        {
            string Actual = UserAgentGenerator.BuildCPUInfo();
            if (!Environment.Is64BitOperatingSystem)
                Assert.Equal(string.Empty, Actual);
            else if (!Environment.Is64BitProcess)
                Assert.Equal("WOW64", Actual);
            else
                Assert.StartsWith("Win64; ", Actual);
        }
    }
}