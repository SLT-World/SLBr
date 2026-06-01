/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

namespace SLBr.Tests.Helpers
{
    public class StringTests
    {
        [Theory]
        [InlineData("hello world", "Hello World")]
        [InlineData("a b c", "A B C")]
        [InlineData("already Capitalized", "Already Capitalized")]
        [InlineData("  multiple   spaces  ", "  Multiple   Spaces  ")]
        [InlineData("123 hello", "123 Hello")]
        [InlineData("file.html", "File.html")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void CapitalizeAllFirstCharacters_ReturnsExpected(string? Input, string? Expected)
        {
            string? Actual = Input!.ToTitleCase();
            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData("hello world", false)]
        [InlineData("   ", true)]
        [InlineData("", true)]
        [InlineData(null, true)]
        public void IsEmptyOrWhiteSpace_ReturnsExpected(string? Input, bool Expected)
        {
            bool Actual = Utils.IsEmptyOrWhiteSpace(Input);
            Assert.Equal(Expected, Actual);
        }
    }
}
