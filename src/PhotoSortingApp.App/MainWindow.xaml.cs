using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PhotoSortingApp.App.ViewModels;

namespace PhotoSortingApp.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void PhotoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || sender is not ListBox listBox)
        {
            return;
        }

        var selectedItems = listBox.SelectedItems.OfType<PhotoItemViewModel>().ToList();
        viewModel.SetSelectedPhotos(selectedItems);

        if (listBox.SelectedItem is PhotoItemViewModel selected &&
            viewModel.SelectedPhoto?.Id != selected.Id)
        {
            viewModel.SelectedPhoto = selected;
        }
    }

    private void PhotoList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel ||
            sender is not ListBox listBox ||
            listBox.SelectedItem is not PhotoItemViewModel)
        {
            return;
        }

        if (viewModel.OpenPhotoCommand.CanExecute(null))
        {
            viewModel.OpenPhotoCommand.Execute(null);
        }
    }

    private void PhotoList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var listBoxItem = ItemsControl.ContainerFromElement(listBox, source) as ListBoxItem;
        if (listBoxItem?.DataContext is not PhotoItemViewModel item)
        {
            return;
        }

        listBox.SelectedItem = item;
        if (DataContext is MainViewModel viewModel && viewModel.SelectedPhoto?.Id != item.Id)
        {
            viewModel.SelectedPhoto = item;
        }
    }

    private void OnContextOpenFileClick(object sender, RoutedEventArgs e)
    {
        ExecuteFromContext(vm => vm.OpenPhotoCommand);
    }

    private void OnContextOpenFileLocationClick(object sender, RoutedEventArgs e)
    {
        ExecuteFromContext(vm => vm.OpenFileLocationCommand);
    }

    private void OnContextSaveMediaClick(object sender, RoutedEventArgs e)
    {
        ExecuteFromContext(vm => vm.SaveImageCommand);
    }

    private void OnContextMoveClick(object sender, RoutedEventArgs e)
    {
        ExecuteFromContext(vm => vm.MovePhotoCommand);
    }

    private void OnContextCopyClick(object sender, RoutedEventArgs e)
    {
        ExecuteFromContext(vm => vm.CopyPhotoCommand);
    }

    private void OnContextDuplicateClick(object sender, RoutedEventArgs e)
    {
        ExecuteFromContext(vm => vm.DuplicatePhotoCommand);
    }

    private void OnContextFixLocationClick(object sender, RoutedEventArgs e)
    {
        ExecuteFromContext(vm => vm.RepairPhotoLocationCommand);
    }

    private void ExecuteFromContext(Func<MainViewModel, System.Windows.Input.ICommand> commandSelector)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var command = commandSelector(viewModel);
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private void YearAlbumsFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not SmartAlbumItemViewModel album)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = album.Key.StartsWith("year:", StringComparison.OrdinalIgnoreCase);
    }
}
