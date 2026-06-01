/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

namespace SLBr.Tests.Helpers
{
    public class UrlResolverTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ResolveUrl_WhenRelativePathIsNullOrEmpty_ReturnsBaseUrl(string RelativePath)
        {
            string BaseUrl = "https://example.com";
            string Actual = Utils.ResolveUrl(BaseUrl, RelativePath);
            Assert.Equal(BaseUrl, Actual);
        }

        [Fact]
        public void ResolveUrl_WhenRelativePathIsAbsolute_ReturnsRelativePathAsIs()
        {
            string BaseUrl = "https://example.com";
            string RelativePath = "https://example.com/api/";
            string Actual = Utils.ResolveUrl(BaseUrl, RelativePath);
            Assert.Equal(RelativePath, Actual);
        }

        [Fact]
        public void ResolveUrl_WhenRelativePathIsRelative_CombinesBaseAndRelative()
        {
            string BaseUrl = "https://example.com/api/";
            string RelativePath = "users/123";
            string Actual = Utils.ResolveUrl(BaseUrl, RelativePath);
            Assert.Equal("https://example.com/api/users/123", Actual);
        }

        [Fact]
        public void ResolveUrl_WhenRelativePathIsRootRelative_ResolvesFromHostRoot()
        {
            string BaseUrl = "https://example.com";
            string relativePath = "/status";
            string Actual = Utils.ResolveUrl(BaseUrl, relativePath);
            Assert.Equal("https://example.com/status", Actual);
        }
    }
}
