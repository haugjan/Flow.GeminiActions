using System.Windows;
using System.Windows.Threading;

namespace Flow.GeminiActions.Editor;

public partial class SpinnerOverlay : Window
{
    internal SpinnerOverlay(string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        Loaded += (_, _) => CenterOnWorkArea();
    }

    private void CenterOnWorkArea()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;
    }

    internal void SetTitle(string title) => TitleText.Text = title;

    internal void ShowResult(string message, bool success)
    {
        Spinner.Visibility = Visibility.Collapsed;
        SuccessIcon.Visibility = success ? Visibility.Visible : Visibility.Collapsed;
        ErrorIcon.Visibility = success ? Visibility.Collapsed : Visibility.Visible;
        TitleText.Text = message;

        var timeout = success ? TimeSpan.FromMilliseconds(1500) : TimeSpan.FromSeconds(5);
        var timer = new DispatcherTimer { Interval = timeout };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}
