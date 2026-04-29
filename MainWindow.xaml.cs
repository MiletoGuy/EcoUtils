using System.Windows;
using EcoUtils.ViewModels;

namespace EcoUtils;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}