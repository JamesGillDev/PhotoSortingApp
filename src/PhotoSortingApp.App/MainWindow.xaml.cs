using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
}
