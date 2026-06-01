/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

namespace SLBr.Tests.Helpers
{
    public class UrlValidationTests
    {
        [Theory]
        [InlineData("example.com", true)]
        [InlineData("https://example.com", true)]
        [InlineData("る.com", true)]
        [InlineData("http://www.🌾.com/", true)]
        [InlineData("https://xn--j1ay.xn--p1ai/", true)]
        [InlineData("https://кц.рф/", true)]
        [InlineData("file:///Z:/A B", true)]
        [InlineData("invalid://a b", true)]
        [InlineData("file:///C:/Example/Example/Example", true)]
        [InlineData("file:///C:/Foo Bar/", true)]
        [InlineData("custom://foo/bar", true)]
        [InlineData("a_b.com", true)]

        [InlineData("101", false)]
        [InlineData("foo bar", false)]
        [InlineData("http://www.a&b.com", false)]
        [InlineData("http://www.a b.com", false)]
        [InlineData("http://www.a\\b.com", false)]
        public void IsUrl_ReturnsExpectedValidity(string Input, bool Expected)
        {
            bool Actual = Utils.IsUrl(Input);
            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData("example.com", true)]
        [InlineData("る.com", true)]
        [InlineData("🌾.com", true)]
        [InlineData("ñ.com", true)]

        [InlineData("101", false)]
        [InlineData("a b", false)]
        [InlineData("www.a&b.com", false)]
        public void IsDomain_ReturnsExpectedValidity(string Input, bool Expected)
        {
            bool Actual = Utils.IsDomain(Input);
            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData("127.0.0.1", true)]
        //[InlineData("127.0.0.1:8080", true)]
        [InlineData("2001:db8::1", true)]
        [InlineData("[2001:db8::1]:3000", true)]
        [InlineData("2001:db8:0:0:0:0:2:1", true)]

        [InlineData("る.com", false)]
        [InlineData("🌾.com", false)]
        [InlineData("ñ.com", false)]
        [InlineData("101", false)]
        [InlineData("a b", false)]
        [InlineData("www.a&b.com", false)]
        public void IsIPAddress_ReturnsExpectedValidity(string Input, bool Expected)
        {
            bool Actual = Utils.IsIPAddress(Input);
            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData("https://example.com", "https")]
        [InlineData("http://example.com", "http")]
        [InlineData("file:///C:/", "file")]
        [InlineData("custom://foo/bar", "custom")]
        public void GetScheme_ReturnsExpected(string Input, string Expected)
        {
            string Actual = Utils.GetScheme(Input);
            Assert.Equal(Expected, Actual);
        }
    }
}
