using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SLBr.Controls
{
    /// <summary>
    /// Interaction logic for RadialProgressBar.xaml
    /// </summary>
    public partial class RadialProgressBar : UserControl
    {
        public RadialProgressBar()
        {
            InitializeComponent();
            Angle = (Percentage * 360) / 100;
            RenderArc();
        }

        public int Radius
        {
            get { return (int)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }
        public Brush SegmentColor
        {
            get { return (Brush)GetValue(SegmentColorProperty); }
            set { SetValue(SegmentColorProperty, value); }
        }
        public int StrokeThickness
        {
            get { return (int)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }
        public double Percentage
        {
            get { return (double)GetValue(PercentageProperty); }
            set { SetValue(PercentageProperty, value); }
        }
        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public static readonly DependencyProperty PercentageProperty =
            DependencyProperty.Register("Percentage", typeof(double), typeof(RadialProgressBar), new PropertyMetadata(65d, new PropertyChangedCallback(OnPercentageChanged)));
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(int), typeof(RadialProgressBar), new PropertyMetadata(5, new PropertyChangedCallback(OnThicknessChanged)));
        public static readonly DependencyProperty SegmentColorProperty =
            DependencyProperty.Register("SegmentColor", typeof(Brush), typeof(RadialProgressBar), new PropertyMetadata(new SolidColorBrush(Colors.Red), new PropertyChangedCallback(OnColorChanged)));
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(int), typeof(RadialProgressBar), new PropertyMetadata(25, new PropertyChangedCallback(OnPropertyChanged)));
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(RadialProgressBar), new PropertyMetadata(120d, new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((RadialProgressBar)sender).set_Color((SolidColorBrush)args.NewValue);
        }

        private static void OnThicknessChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((RadialProgressBar)sender).set_tick((int)args.NewValue);
        }

        private static void OnPercentageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            RadialProgressBar circle = sender as RadialProgressBar;
            if (circle.Percentage > 100) circle.Percentage = 100;
            circle.Angle = (circle.Percentage * 360) / 100;
        }

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((RadialProgressBar)sender).RenderArc();
        }

        public void set_tick(int n)
        {
            pathRoot.StrokeThickness = n;
        }

        public void set_Color(SolidColorBrush n)
        {
            pathRoot.Stroke = n;
        }

        public void RenderArc()
        {
            Point StartPoint = new Point(Radius, 0);
            Point EndPoint = ComputeCartesianCoordinate(Angle, Radius);
            EndPoint.X += Radius;
            EndPoint.Y += Radius;

            pathRoot.Width = Radius * 2 + StrokeThickness;
            pathRoot.Height = Radius * 2 + StrokeThickness;
            pathRoot.Margin = new Thickness(StrokeThickness, StrokeThickness, 0, 0);

            pathFigure.StartPoint = StartPoint;

            if (StartPoint.X == Math.Round(EndPoint.X) && StartPoint.Y == Math.Round(EndPoint.Y))
                EndPoint.X -= 0.01;

            arcSegment.Point = EndPoint;
            arcSegment.Size = new Size(Radius, Radius);//OuterArcSize
            arcSegment.IsLargeArc = Angle > 180.0;//LargeArc
        }

        private Point ComputeCartesianCoordinate(double angle, double radius)
        {
            double angleRad = (Math.PI / 180.0) * (angle - 90);

            double x = radius * Math.Cos(angleRad);
            double y = radius * Math.Sin(angleRad);

            return new Point(x, y);
        }
    }
}
