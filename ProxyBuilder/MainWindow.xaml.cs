using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ProxyBuilder
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ProxyItem> ProxyItems { get; } = new ObservableCollection<ProxyItem>();

        public MainWindow()
        {
            InitializeComponent();
            listItems.ItemsSource = ProxyItems;
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            ToggleLoading(true);
            foreach (var item in await App.CardFetcher.ParseList(tbSource.Text))
            {
                ProxyItems.Add(item);
            }
            ToggleLoading(false);
        }

        private void ToggleLoading(bool isLoading)
        {
            contentGrid.IsEnabled = !isLoading;
            loaderGrid.Visibility = isLoading ? Visibility.Visible : Visibility.Hidden;
        }

        private void BtnBuild_Click(object sender, RoutedEventArgs e)
        {
            ToggleLoading(true);
            App.Printer.Print(ProxyItems);
            ToggleLoading(false);
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is ProxyItem item)
            {
                ProxyItems.Remove(item);
            }
        }
    }
}
