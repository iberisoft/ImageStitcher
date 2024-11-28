using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ByteImage = Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>;

namespace ImageStitcher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Engine.PreviewRoiImages += Engine_PreviewRoiImages;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Settings.Default.Save();
    }

    private async void OpenImages(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "*.tif|*.tif",
            Multiselect = true
        };
        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 1)
        {
            await DoJob(async () =>
            {
                SourceImageControl1.Image = null;
                SourceImageControl1.LoadImage();
                SourceImageControl2.Image = null;
                SourceImageControl2.LoadImage();
                ResultImageControl.Image = null;
                ResultImageControl.LoadImage();
                SourceRoiImageView1.Source = null;
                SourceRoiImageView2.Source = null;
                await Task.Run(() =>
                {
                    SourceImageControl1.Image = new ImageContainer(dialog.FileNames[0]);
                    SourceImageControl2.Image = new ImageContainer(dialog.FileNames[1]);
                });
                SourceImageControl1.LoadImage();
                SourceImageControl2.LoadImage();
            });
        }
    }

    private async void StitchImages(object sender, RoutedEventArgs e)
    {
        if (SourceImageControl1.ImageRoi == null || SourceImageControl2.ImageRoi == null)
        {
            MessageBox.Show("Select ROIs on both images.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "*.tif|*.tif"
        };
        if (dialog.ShowDialog() == true)
        {
            var errorText = "";

            await DoJob(async () =>
            {
                ResultImageControl.Image = null;
                ResultImageControl.LoadImage();
                SourceRoiImageView1.Source = null;
                SourceRoiImageView2.Source = null;
                await Task.Run(() =>
                {
                    try
                    {
                        StitchImages(dialog.FileName);
                        ResultImageControl.Image = new ImageContainer(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        errorText = ex.Message;
                    }
                });
                ResultImageControl.LoadImage();
            });

            if (errorText != "")
            {
                MessageBox.Show(errorText, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SwapImages(object sender, RoutedEventArgs e)
    {
        var image = SourceImageControl1.Image;
        SourceImageControl1.Image = SourceImageControl2.Image;
        SourceImageControl2.Image = image;
        SourceImageControl1.LoadImage();
        SourceImageControl2.LoadImage();
        ResultImageControl.Image = null;
        ResultImageControl.LoadImage();

        var imageRoi = SourceImageControl1.ImageRoi;
        SourceImageControl1.ImageRoi = SourceImageControl2.ImageRoi;
        SourceImageControl2.ImageRoi = imageRoi;
    }

    string m_DefaultTitle;

    private async Task DoJob(Func<Task> job)
    {
        try
        {
            IsEnabled = false;
            m_DefaultTitle ??= Title;
            Title = m_DefaultTitle + " (Processing...)";

            await job();
        }
        finally
        {
            IsEnabled = true;
            Title = m_DefaultTitle;
        }
    }

    private void StitchImages(string resultImagePath) => Engine.StitchImages(SourceImageControl1.Image.FilePath, SourceImageControl1.ImageRoi.Value,
        SourceImageControl2.Image.FilePath, SourceImageControl2.ImageRoi.Value, resultImagePath);

    private void Engine_PreviewRoiImages(ByteImage image1, ByteImage image2)
    {
        var filePath1 = Path.Combine(Path.GetTempPath(), nameof(ImageStitcher) + "1.tif");
        image1.Save(filePath1);
        var filePath2 = Path.Combine(Path.GetTempPath(), nameof(ImageStitcher) + "2.tif");
        image2.Save(filePath2);

        Dispatcher.Invoke(() =>
        {
            SourceRoiImageView1.Source = new ImageContainer(filePath1).Image;
            SourceRoiImageView2.Source = new ImageContainer(filePath2).Image;
        });
    }
}
