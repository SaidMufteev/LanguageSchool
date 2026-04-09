using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MufteevLanguage
{
    /// <summary>
    /// Логика взаимодействия для ClientPage.xaml
    /// </summary>
    public partial class ClientPage : Page
    {
        private List<Client> _filteredClients;
        private int _currentPage = 1;
        private int _pageSize = 10;

        public ClientPage()
        {
            InitializeComponent();

            var currentClient = MufteevLanguageEntities.GetContext().Client.ToList();

            ClientListView.ItemsSource = currentClient;

            ComboPageSize.Items.Add("10");
            ComboPageSize.Items.Add("50");
            ComboPageSize.Items.Add("200");
            ComboPageSize.Items.Add("Все");
            ComboPageSize.SelectedIndex = 0;

            UpdateClients();
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPage = 1;
            UpdateClients();
        }

        private void ComboType2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPage = 1;
            UpdateClients();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPage = 1;
            UpdateClients();
        }

        private void UpdateClients()
        {
            var currentClient = MufteevLanguageEntities.GetContext().Client.ToList();

            // ФИЛЬТРАЦИЯ ПО ПОЛУ
            if (ComboType2.SelectedIndex == 0) currentClient = currentClient.Where(p => (p.GenderCode == "м" || p.GenderCode == "ж")).ToList();
            if (ComboType2.SelectedIndex == 1) currentClient = currentClient.Where(p => (p.GenderCode == "ж")).ToList();
            if (ComboType2.SelectedIndex == 2) currentClient = currentClient.Where(p => (p.GenderCode == "м")).ToList();

            // ПОИСК
            currentClient = currentClient.Where(p =>
                p.FirstName.ToLower().Contains(TBoxSearch.Text.ToLower()) ||
                p.LastName.ToLower().Contains(TBoxSearch.Text.ToLower()) ||
                p.Patronymic.ToLower().Contains(TBoxSearch.Text.ToLower()) ||
                p.Email.ToLower().Contains(TBoxSearch.Text.ToLower()) ||
                p.Phone.Replace("+7", "").Replace("8", "").Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "")
                .Contains(TBoxSearch.Text.Replace("+7", "").Replace("8", "").Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""))).ToList();
            ClientListView.ItemsSource = currentClient;
            _filteredClients = currentClient;

            // СОРТИРОВКА
            if (ComboType.SelectedIndex == 1) currentClient = currentClient.OrderBy(p => p.FirstName).ToList();
            if (ComboType.SelectedIndex == 2) currentClient = currentClient.OrderByDescending(p => p.LastVisitDate).ToList();
            if (ComboType.SelectedIndex == 3) currentClient = currentClient.OrderByDescending(p => p.VisitsCount).ToList();

            _filteredClients = currentClient;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            if (_filteredClients == null) return;

            List<Client> clientsToShow;

            if (_pageSize == 0)
            {
                clientsToShow = _filteredClients.ToList();
            }
            else
            {
                clientsToShow = _filteredClients
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();
            }

            ClientListView.ItemsSource = clientsToShow;

            int totalCount = _filteredClients.Count;
            int displayedCount = clientsToShow.Count;
            TBCount.Text = $"{displayedCount} из {totalCount}";

            int totalPages = _pageSize == 0 ? 1 : (int)Math.Ceiling((double)totalCount / _pageSize);
            PageNumberText.Text = $"{_currentPage}/{totalPages}";

            LeftDirButton.IsEnabled = _currentPage > 1;
            RightDirButton.IsEnabled = _currentPage < totalPages && totalPages > 0;

            // ОБНОВЛЕНИЕ PageListBox
            UpdatePageListBox(totalPages);
        }

        // НОВЫЙ МЕТОД: заполнение списка страниц
        private void UpdatePageListBox(int totalPages)
        {
            PageListBox.Items.Clear();
            for (int i = 1; i <= totalPages; i++)
            {
                PageListBox.Items.Add(i);
            }

            // Подсветка текущей страницы
            PageListBox.SelectedItem = _currentPage;

            // Показываем PageListBox только если страниц больше 1
            PageListBox.Visibility = totalPages > 1 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            Client selectedClient = btn.Tag as Client;

            if (selectedClient == null) return;

            if (selectedClient.ClientService != null && selectedClient.ClientService.Any())
            {
                MessageBox.Show("Нельзя удалить клиента, у которого есть посещения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Удалить клиента {selectedClient.FirstName} {selectedClient.LastName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var db = MufteevLanguageEntities.GetContext();
                    db.Client.Remove(selectedClient);
                    db.SaveChanges();
                    UpdateClients();
                    MessageBox.Show("Клиент успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ComboPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selected = ComboPageSize.SelectedItem?.ToString();
            if (selected == "Все")
                _pageSize = 0;
            else if (int.TryParse(selected, out int size))
                _pageSize = size;

            _currentPage = 1;
            UpdatePagination();
        }

        private void LeftDirButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagination();
            }
        }

        private void RightDirButton_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = _pageSize == 0 ? 1 : (int)Math.Ceiling((double)_filteredClients.Count / _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdatePagination();
            }
        }

        private void PageListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (PageListBox.SelectedItem != null)
            {
                _currentPage = (int)PageListBox.SelectedItem;
                UpdatePagination();
            }
        }
    }
}
