/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

namespace SLBr.Tests.Helpers
{
    public class UrlManipulationTests
    {
        [Theory]
        [InlineData("https://example.com", "example.com")]
        [InlineData("http://example.com", "example.com")]
        [InlineData("https://example.com/", "example.com")]
        [InlineData("https://example.com#fragment", "example.com")]
        [InlineData("file:///C:/Folder/File.txt", "C:/Folder/File.txt")]
        [InlineData("   ", "   ")]
        [InlineData(null, null)]
        public void CleanUrl_ReturnsExpected(string? Input, string? Expected)
        {
            string Actual = Utils.CleanUrl(Input!);
            Assert.Equal(Expected, Actual);
        }

        [Fact]
        public void CleanUrl_StripsParameters()
        {
            string Input = "https://example.com/?q=hello+world";
            string Actual = Utils.CleanUrl(Input, RemoveParameters: true);
            Assert.Equal("example.com", Actual);
        }

        [Fact]
        public void CleanUrl_StripsTrivialSubdomains()
        {
            string InputWWW = "https://www.example.com";
            string InputM = "https://m.example.com";

            string ActualWWW = Utils.CleanUrl(InputWWW, RemoveTrivialSubdomain: true);
            string ActualM = Utils.CleanUrl(InputM, RemoveTrivialSubdomain: true);

            Assert.Equal("example.com", ActualWWW);
            Assert.Equal("example.com", ActualM);
        }

        [Fact]
        public void CleanUrl_PreservesProtocol()
        {
            string Input = "https://example.com";
            string Actual = Utils.CleanUrl(Input, RemoveProtocol: false);
            Assert.Equal("https://example.com", Actual);
        }

        [Theory]
        [InlineData("https://example.com/page.html", "example.com")]
        [InlineData("http://example.com", "example.com")]
        [InlineData("custom://foo/bar", "foo")]
        [InlineData("", "")]
        public void FastHost_ExtractsHost(string Input, string Expected)
        {
            string Actual = Utils.FastHost(Input);
            Assert.Equal(Expected, Actual);
        }

        [Fact]
        public void FastHost_PreservesProtocol()
        {
            string Input = "https://example.com";
            string Actual = Utils.FastHost(Input, KeepProtocol: true);
            Assert.Equal("https://example.com", Actual);
        }

        [Fact]
        public void FastHost_RetainsTrivialSubdomains()
        {
            string Input = "https://www.example.com";
            string Actual = Utils.FastHost(Input, RemoveTrivialSubdomain: false);
            Assert.Equal("www.example.com", Actual);
        }

        [Theory]
        [InlineData("https://example.com/path", "example.com")]
        [InlineData("https://www.example.com/", "example.com")]
        [InlineData("http://example.com?q=1", "example.com")]
        [InlineData("file:///C:/Folder/File.txt", "C:")]
        public void Host_ExtractsBaseHost(string Input, string Expected)
        {
            string Actual = Utils.Host(Input);
            Assert.Equal(Expected, Actual);
        }

        [Fact]
        public void Host_RetainsSubdomain()
        {
            string Input = "https://www.example.com";
            string Actual = Utils.Host(Input, RemoveTrivialSubdomain: false);
            Assert.Equal("www.example.com", Actual);
        }

        //TODO: NormalizeIP, FixUrl
    }
}
