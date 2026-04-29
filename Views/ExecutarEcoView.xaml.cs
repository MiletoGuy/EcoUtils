using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EcoUtils.ViewModels;

namespace EcoUtils.Views;

public partial class ExecutarEcoView : UserControl
{
    private bool _syncingHeader = false;

    public ExecutarEcoView()
    {
        InitializeComponent();
        Loaded             += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private ExecutarEcoViewModel? Vm => DataContext as ExecutarEcoViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ExecutarEcoViewModel old)
            old.PropertyChanged -= OnVmPropertyChanged;
        if (e.NewValue is ExecutarEcoViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            SyncHeaderFromVm();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SyncHeaderFromVm();

        var dpd = DependencyPropertyDescriptor.FromProperty(
            ColumnDefinition.WidthProperty, typeof(ColumnDefinition));
        dpd.AddValueChanged(HeaderColApelido,    OnHeaderColChanged);
        dpd.AddValueChanged(HeaderColExecutavel, OnHeaderColChanged);
        dpd.AddValueChanged(HeaderColBanco,      OnHeaderColChanged);
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is
            nameof(ExecutarEcoViewModel.ColWidthApelido)    or
            nameof(ExecutarEcoViewModel.ColWidthExecutavel) or
            nameof(ExecutarEcoViewModel.ColWidthBanco))
        {
            _syncingHeader = true;
            SyncHeaderFromVm();
            _syncingHeader = false;
        }
    }

    private void SyncHeaderFromVm()
    {
        if (Vm is null) return;
        HeaderColApelido.Width    = Vm.ColWidthApelido;
        HeaderColExecutavel.Width = Vm.ColWidthExecutavel;
        HeaderColBanco.Width      = Vm.ColWidthBanco;
    }

    private void OnHeaderColChanged(object? sender, EventArgs e)
    {
        if (_syncingHeader || Vm is null) return;
        if (sender == HeaderColApelido)    Vm.ColWidthApelido    = HeaderColApelido.Width;
        if (sender == HeaderColExecutavel) Vm.ColWidthExecutavel = HeaderColExecutavel.Width;
        if (sender == HeaderColBanco)      Vm.ColWidthBanco      = HeaderColBanco.Width;
    }
}
