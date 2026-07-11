using System.ComponentModel;
using System.Windows;
using NamePlateStudio.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace NamePlateStudio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "종료하시겠습니까?",
            "NamePlateStudio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
