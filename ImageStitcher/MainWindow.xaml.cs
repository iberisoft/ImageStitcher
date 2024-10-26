using Microsoft.Win32;
using System.Windows;

namespace ImageStitcher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Engine.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Engine.Shutdown();
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
        var dialog = new SaveFileDialog
        {
            Filter = "*.tif|*.tif"
        };
        if (dialog.ShowDialog() == true)
        {
            var result = false;
            var resultText = "";

            await DoJob(async () =>
            {
                ResultImageControl.Image = null;
                ResultImageControl.LoadImage();
                await Task.Run(() =>
                {
                    result = StitchImages(dialog.FileName, out resultText);
                    if (result)
                    {
                        ResultImageControl.Image = new ImageContainer(dialog.FileName);
                    }
                });
                ResultImageControl.LoadImage();
            });

            MessageBox.Show(resultText, Title, MessageBoxButton.OK, result ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
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

    private bool StitchImages(string resultImagePath, out string resultText)
    {
        try
        {
            resultText = Engine.StitchImages(SourceImageControl1.Image.FilePath, SourceImageControl1.ImageRoi,
                SourceImageControl2.Image.FilePath, SourceImageControl2.ImageRoi, resultImagePath);
            return true;
        }
        catch (Exception ex)
        {
            resultText = ex.Message;
            return false;
        }
    }
}
