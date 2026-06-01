/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

namespace SLBr.Tests.Helpers
{
    public class UrlNormalizationTests
    {
        [Theory]
        [InlineData("192.168.1.1", "192.168.1.1")]
        [InlineData("2001:db8::1", "[2001:db8::1]")]
        [InlineData("[2001:db8::1]", "[2001:db8::1]")]
        [InlineData("example.com", "example.com")]
        [InlineData("localhost", "localhost")]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        public void NormalizeIP_ReturnsExpected(string Input, string Expected)
        {
            string Actual = Utils.NormalizeIP(Input);
            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData("example.com", "https://example.com")]
        [InlineData("http://example.com", "http://example.com")]
        [InlineData("https://example.com", "https://example.com")]
        [InlineData("localhost:8080/api/v1", "http://localhost:8080/api/v1")]
        //[InlineData("127.0.0.1", "http://127.0.0.1")]
        //[InlineData("2001:db8::1", "http://2001:db8::1")]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        public void FixUrl_ReturnsExpected(string Input, string Expected)
        {
            string Actual = Utils.FixUrl(Input);
            Assert.Equal(Expected, Actual);
        }

        [Fact]
        public void FixUrl_WithMalformedBracketedHost_ReturnsOriginal()
        {
            var MalformedUrl = "https://[2001:db8::1";
            string Actual = Utils.FixUrl(MalformedUrl);

            Assert.Equal(MalformedUrl, Actual);
        }

        [Fact]
        public void FixUrl_WithValidIPNormalizationTrigger_RebuildsCorrectlyWithStringBuilder()
        {
            string Actual = Utils.FixUrl("http://127.0.0.1");
            Assert.Equal("http://127.0.0.1", Actual);
        }
    }
}
