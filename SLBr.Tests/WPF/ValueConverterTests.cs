/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using SLBr.Controls;
using System.Globalization;
using System.Windows;

namespace SLBr.Tests.WPF
{
    public class ValueConverterTests
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        [Theory]
        [InlineData(59.9, 60.0, true)]
        [InlineData(60.0, 60.0, false)]
        [InlineData(60.1, 60.0, false)]
        [InlineData("test", 60.0, false)]
        [InlineData(95.0, 100, true)]
        [InlineData(105.0, 100, false)]
        public void LessThanConverter_ReturnsExpected(object Input, double Threshold, bool Expected)
        {
            var Converter = new LessThanConverter { Threshold = Threshold };
            var Actual = Converter.Convert(Input, typeof(bool), null!, Culture);

            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void InvertBooleanConverter_ReturnsExpected(bool Input, bool Expected)
        {
            var Converter = new InvertBooleanConverter();
            var Actual = Converter.Convert(Input, typeof(bool), null!, Culture);

            Assert.Equal(Expected, Actual);
        }

        [Theory]
        [InlineData(true, false, Visibility.Visible)]
        [InlineData(false, false, Visibility.Collapsed)]
        [InlineData(true, true, Visibility.Collapsed)]
        [InlineData(false, true, Visibility.Visible)]
        public void BooleanToVisibilityConverter_ReturnsExpectedVisibility(bool Input, bool Invert, Visibility Expected)
        {
            var Converter = new BooleanToVisibilityConverter { Invert = Invert };
            var Actual = Converter.Convert(Input, typeof(Visibility), null!, Culture);

            Assert.Equal(Expected, Actual);
        }

        [Fact]
        public void NullToVisibilityConverter_WithNonNull_ReturnsVisible()
        {
            var Converter = new NullToVisibilityConverter();
            var Actual = Converter.Convert(new object(), typeof(Visibility), null!, Culture);

            Assert.Equal(Visibility.Visible, Actual);
        }

        [Fact]
        public void NullToVisibilityConverter_WithNull_ReturnsCollapsed()
        {
            var Converter = new NullToVisibilityConverter();
            var Actual = Converter.Convert(null!, typeof(Visibility), null!, Culture);

            Assert.Equal(Visibility.Collapsed, Actual);
        }

        [Theory]
        [InlineData(100.0, "40.0", 60.0)]
        [InlineData(50.5, "10.5", 40.0)]
        [InlineData(0.0, "25", -25.0)]
        public void SubtractConverter_SubtractsParameterFromValue(double Input, string Parameter, double Expected)
        {
            var Converter = new SubtractConverter();
            var Actual = Converter.Convert(Input, typeof(double), Parameter, Culture);

            Assert.Equal(Expected, (double)Actual);
        }
    }
}
