using System.Windows;

namespace Flow.GeminiActions.Editor;

public partial class SpinnerOverlay : Window
{
    internal SpinnerOverlay(string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - Width - 24;
            Top = area.Bottom - Height - 24;
        };
    }
}
