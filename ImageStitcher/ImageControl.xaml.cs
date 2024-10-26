using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageStitcher;

/// <summary>
/// Interaction logic for ImageControl.xaml
/// </summary>
public partial class ImageControl : UserControl
{
    public ImageControl()
    {
        InitializeComponent();
    }

    public ImageContainer Image { get; set; }

    public void LoadImage() => ImageView.Source = Image?.Image;

    public bool IsRoiEnabled { get; set; }

    public Rect? ImageRoi { get; private set; }

    Point? m_StartPoint;
    Point? m_EndPoint;

    private void ImageView_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Image != null && IsRoiEnabled)
        {
            m_StartPoint = e.GetPosition(ImageView);
            UpdateRoi();

            ImageView.CaptureMouse();
        }
    }

    private void ImageView_MouseMove(object sender, MouseEventArgs e)
    {
        if (Image != null && m_StartPoint != null)
        {
            var point = e.GetPosition(ImageView);
            if (point.X >= 0 && point.Y >= 0 && point.X < ImageView.ActualWidth && point.Y < ImageView.ActualHeight)
            {
                m_EndPoint = point;
                UpdateRoi();
            }
        }
    }

    private void ImageView_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (Image != null && m_StartPoint != null)
        {
            if (m_EndPoint != null)
            {
                var point1 = ClientToImage(m_StartPoint.Value);
                var point2 = ClientToImage(m_EndPoint.Value);
                ImageRoi = new(Math.Min(point1.X, point2.X), Math.Min(point1.Y, point2.Y),
                    Math.Abs(point1.X - point2.X), Math.Abs(point1.Y - point2.Y));
            }
            m_StartPoint = null;
            m_EndPoint = null;
            UpdateRoi();

            ImageView.ReleaseMouseCapture();
        }
    }

    private void UpdateRoi()
    {
        if (m_StartPoint != null && m_EndPoint != null)
        {
            Canvas.SetLeft(RoiRectangle, Math.Min(m_StartPoint.Value.X, m_EndPoint.Value.X));
            Canvas.SetTop(RoiRectangle, Math.Min(m_StartPoint.Value.Y, m_EndPoint.Value.Y));
            RoiRectangle.Width = Math.Abs(m_StartPoint.Value.X - m_EndPoint.Value.X);
            RoiRectangle.Height = Math.Abs(m_StartPoint.Value.Y - m_EndPoint.Value.Y);
            RoiRectangle.Visibility = Visibility.Visible;
        }
        else
        {
            RoiRectangle.Visibility = Visibility.Hidden;
        }
    }

    private Point ClientToImage(Point point)
    {
        point.X *= Image.Width / ImageView.ActualWidth;
        point.Y *= Image.Height / ImageView.ActualHeight;
        return point;
    }
}
