using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;

namespace CoffeeShopPOS
{
    public partial class MainWindow : Window
    {
        private SupabaseService _supabase = SupabaseService.Instance;
        private ObservableCollection<CartItem> _cart = new ObservableCollection<CartItem>();
        private bool _isClosingShift = false;

        public MainWindow()
        {
            InitializeComponent();
            CartListBox.ItemsSource = _cart;
            InitializeSupabase();
        }

        private async void InitializeSupabase()
        {
            try {
                await _supabase.InitializeAsync();
                _supabase.OnOrderReceived += (order) => {
                    Dispatcher.Invoke(() => {
                        MessageBox.Show($"New Order from Client App! Total: {order.Total:C}", "Incoming Order");
                    });
                };
            } catch (Exception ex) {
                MessageBox.Show("Failed to initialize Supabase: " + ex.Message);
            }
        }

        private void PinKey_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            PinInput.Password += btn.Content.ToString();
        }

        private void PinClear_Click(object sender, RoutedEventArgs e)
        {
            PinInput.Password = "";
        }

        private async void PinOk_Click(object sender, RoutedEventArgs e)
        {
            var pin = PinInput.Password;
            bool success = await _supabase.LoginWithPinAsync(pin);

            if (success)
            {
                PinInput.Password = "";
                LoginGrid.Visibility = Visibility.Collapsed;
                EmployeeInfo.Text = $"Logged in as: {_supabase.CurrentEmployee.Name}";

                // Show Open Shift dialog
                _isClosingShift = false;
                ShiftDialogTitle.Text = "Opening Float";
                ShiftCashInput.Text = "50.00"; // Mock default
                ShiftDialogGrid.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Invalid PIN");
                PinInput.Password = "";
            }
        }

        private async void ShiftConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(ShiftCashInput.Text, out decimal cash))
            {
                if (_isClosingShift)
                {
                    await _supabase.CloseShiftAsync(cash);
                    MainPosGrid.Visibility = Visibility.Collapsed;
                    LoginGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    await _supabase.OpenShiftAsync(cash);
                    LoadArticles();
                    MainPosGrid.Visibility = Visibility.Visible;
                }
                ShiftDialogGrid.Visibility = Visibility.Collapsed;
            }
        }

        private async void LoadArticles()
        {
            var articles = await _supabase.GetArticlesAsync();
            ArticleItemsControl.ItemsSource = articles;
        }

        private void ArticleButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var article = btn.Tag as Article;

            var existing = _cart.FirstOrDefault(i => i.ArticleId == article.Id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _cart.Add(new CartItem
                {
                    ArticleId = article.Id,
                    Name = article.Name,
                    UnitPrice = article.Price,
                    Quantity = 1,
                    RequiresCoffee = article.RequiresCoffee
                });
            }
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            TotalText.Text = _cart.Sum(i => i.Subtotal).ToString("C");
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            _cart.Clear();
            UpdateTotal();
        }

        private async void Pay_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0) return;

            var order = new Order { Total = _cart.Sum(i => i.Subtotal) };
            var items = _cart.Select(i => new OrderItem
            {
                ArticleId = i.ArticleId,
                ArticleName = i.Name,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList();

            await _supabase.CreateOrderAsync(order, items);

            _cart.Clear();
            UpdateTotal();
            MessageBox.Show("Order Completed!");
        }

        private void CloseShift_Click(object sender, RoutedEventArgs e)
        {
            _isClosingShift = true;
            ShiftDialogTitle.Text = "Closing Cash";
            ShiftCashInput.Text = "";
            ShiftDialogGrid.Visibility = Visibility.Visible;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainPosGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
        }

        private async void BeansButton_Click(object sender, RoutedEventArgs e)
        {
            await _supabase.StartNewBeanBagAsync();
            MessageBox.Show("New 1kg Bean Bag Started!");
        }
    }

    public class CartItem : System.ComponentModel.INotifyPropertyChanged
    {
        public Guid ArticleId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public bool RequiresCoffee { get; set; }

        private int _quantity;
        public int Quantity {
            get => _quantity;
            set {
                _quantity = value;
                OnPropertyChanged("Quantity");
                OnPropertyChanged("Subtotal");
            }
        }

        public decimal Subtotal => UnitPrice * Quantity;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
