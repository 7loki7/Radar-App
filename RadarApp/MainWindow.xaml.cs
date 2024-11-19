using RadarLibrary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static RadarLibrary.Models;

namespace RadarApp
{
    public partial class MainWindow : Window
    {
        private RadarCommunicator _radar;
        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();
        private const int MaxDistance = 100; 

        public MainWindow()
        {
            InitializeComponent();
            DrawRadarBackground();
        }
        private void SetIpAddress_Click(object sender, EventArgs e)
        {
            if (int.TryParse(IpInputTextBox.Text, out int ipNumber))
            {
                string ipAddress = ConvertNumberToIpAddress(ipNumber);
                string url = $"http://{ipAddress}:8001/scan_radars";
                _radar = new RadarCommunicator(url);

                _refreshTimer.Interval = TimeSpan.FromSeconds(1);
                _refreshTimer.Tick += RefreshRadar_Click;
                _refreshTimer.Start();
            }
            else
            {
                MessageBox.Show("Please enter a valid number.");
            }
        }

        private static string ConvertNumberToIpAddress(int number)
        {
            return $"{(number >> 24) & 0xFF}.{(number >> 16) & 0xFF}.{(number >> 8) & 0xFF}.{number & 0xFF}";
        }

        private async void RefreshRadar_Click(object sender, EventArgs e)
        {
            var status = await _radar.GetRadarStatusAsync();
            RadarStatusTextBlock.Text = status.IsOnline ? "Radar Status: Online" : $"Radar Status: Offline, last detected targets are displayed";
            if (status.LastError is not null) RadarStatusTextBlock.Text = $"Radar Status: Offline - Error: {status.LastError}";
            RadarStatusTextBlock.Foreground = status.IsOnline ? Brushes.Green : Brushes.Red;

            if (status.IsOnline)
            {
                try
                {
                    var targets = await _radar.GetRadarDataAsync();
                    if (targets is not null) DrawRadarTargets(targets);
                }
                catch (Exception ex)
                {
                    RadarStatusTextBlock.Text = $"Radar Status: Offline, Error occured while drawing targets";
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void DrawRadarTargets(List<RadarTarget> targets)
        {
            RadarCanvas.Children.Clear();
            DrawRadarBackground();

            foreach (var target in targets)
            {
                if (target.Distance <= MaxDistance && (target.Angle >= -45 && target.Angle <= 45)
 )
                {
                    double angleInRadians = ((target.Angle * Math.PI) / 180);
                    double normalizedDistance = Math.Min(target.Distance, 100) / 100;
                    double x = 200 + (normalizedDistance * 200 * Math.Cos(angleInRadians));
                    double y = 200 - (normalizedDistance * 200 * Math.Sin(angleInRadians));

                    Ellipse targetDot = new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = Brushes.Red
                    };

                    Canvas.SetLeft(targetDot, x - 4);
                    Canvas.SetTop(targetDot, y - 4);
                    RadarCanvas.Children.Add(targetDot);
                }
            }
        }
        
        private void DrawRadarBackground()
        {
            RadarCanvas.Children.Add(CreateArc(200));
            RadarCanvas.Children.Add(CreateArc(150));
            RadarCanvas.Children.Add(CreateArc(100));
            RadarCanvas.Children.Add(CreateArc(50));

            RadarCanvas.Children.Add(CreateLine(200, 200, 200 + 200 * Math.Cos(-45 * Math.PI / 180), 200 - 200 * Math.Sin(-45 * Math.PI / 180)));
            RadarCanvas.Children.Add(CreateLine(200, 200, 200 + 200 * Math.Cos(45 * Math.PI / 180), 200 - 200 * Math.Sin(45 * Math.PI / 180)));
        }

        private static Path CreateArc(double radius)
        {
            PathFigure figure = new PathFigure();
            figure.StartPoint = new Point(200 + radius * Math.Cos(-45 * Math.PI / 180), 200 - radius * Math.Sin(-45 * Math.PI / 180));
            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(200 + radius * Math.Cos(45 * Math.PI / 180), 200 - radius * Math.Sin(45 * Math.PI / 180)),
                Size = new Size(radius, radius),
                IsLargeArc = false,
                SweepDirection = SweepDirection.Counterclockwise
            });

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            Path path = new Path
            {
                Data = geometry,
                Stroke = Brushes.Green,
                StrokeThickness = 1
            };

            return path;
        }

        private Line CreateLine(double x1, double y1, double x2, double y2)
        {
            return new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = Brushes.Green,
                StrokeThickness = 1
            };
        }
    }
}