using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;
using CoffeeShopPOS.Data;

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
            try
            {
                // Hook events before initializing so that cached values trigger immediately
                _supabase.OnArticlesChanged += () => Dispatcher.Invoke(LoadArticles);
                _supabase.OnBrandingChanged += () => Dispatcher.Invoke(UpdateBranding);

                await _supabase.InitializeAsync();

                _supabase.OnOrderReceived += (order) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"New Order from Client App! Total: {order.Total:C}", "Incoming Order");
                    });
                };

                // Check if session was restored automatically
                if (_supabase.CurrentEmployee != null)
                {
                    OnLoginSuccess();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize Supabase: " + ex.Message);
            }
        }

        private void PinKey_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                PinInput.Password += btn.Content.ToString();
            }
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
                OnLoginSuccess();
            }
            else
            {
                MessageBox.Show("Invalid PIN");
                PinInput.Password = "";
            }
        }

        private void OnLoginSuccess()
        {
            LoginGrid.Visibility = Visibility.Collapsed;
            EmployeeInfo.Text = $"Logged in as: {_supabase.CurrentEmployee.Name} ({_supabase.CurrentEmployee.Role})";

            // If Admin, show Admin Panel button
            if (_supabase.CurrentEmployee.Role == "admin")
            {
                AdminPanelButton.Visibility = Visibility.Visible;
            }
            else
            {
                AdminPanelButton.Visibility = Visibility.Collapsed;
                AdminPanelGrid.Visibility = Visibility.Collapsed;
            }

            // Show Open Shift dialog
            _isClosingShift = false;
            ShiftDialogTitle.Text = "Opening Float";
            ShiftCashInput.Text = "50.00"; // Mock default
            ShiftDialogGrid.Visibility = Visibility.Visible;

            // Trigger sync of articles and branding
            LoadArticles();
            UpdateBranding();
        }

        private async void ShiftConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(ShiftCashInput.Text, out decimal cash))
            {
                if (_isClosingShift)
                {
                    await _supabase.CloseShiftAsync(cash);
                    MainPosGrid.Visibility = Visibility.Collapsed;
                    AdminPanelButton.Visibility = Visibility.Collapsed;
                    AdminPanelGrid.Visibility = Visibility.Collapsed;
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
            else
            {
                MessageBox.Show("Please enter a valid amount.");
            }
        }

        private void LoadArticles()
        {
            try
            {
                using var db = new LocalDbContext();
                var activeArticles = db.Articles.Where(a => a.Active).ToList();

                // Group by category for Category Flow Layout
                var grouped = activeArticles
                    .GroupBy(a => string.IsNullOrEmpty(a.Category) ? "Uncategorized" : a.Category)
                    .Select(g => new CategoryGroup
                    {
                        CategoryName = g.Key,
                        Articles = g.ToList()
                    })
                    .ToList();

                CategoriesItemsControl.ItemsSource = grouped;

                // Also reload admin margins if panel is open
                if (AdminPanelGrid.Visibility == Visibility.Visible)
                {
                    LoadAdminMargins();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading articles to UI: " + ex.Message);
            }
        }

        private void UpdateBranding()
        {
            // Apply Dynamic Resource Colors
            _supabase.ApplyCachedBranding();

            // Load header Logo
            var localLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
            if (File.Exists(localLogoPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(localLogoPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Crucial so file is not locked on disk!
                    bitmap.EndInit();
                    LogoImage.Source = bitmap;
                    LogoImage.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to render brand logo: " + ex.Message);
                }
            }
            else
            {
                LogoImage.Visibility = Visibility.Collapsed;
            }
        }

        private void ArticleButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var article = btn.Tag as LocalArticle;
            if (article == null) return;

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

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            await _supabase.LogoutAsync();

            MainPosGrid.Visibility = Visibility.Collapsed;
            AdminPanelButton.Visibility = Visibility.Collapsed;
            AdminPanelGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
            EmployeeInfo.Text = "";
        }

        private async void BeansButton_Click(object sender, RoutedEventArgs e)
        {
            await _supabase.StartNewBeanBagAsync();
            MessageBox.Show("New 1kg Bean Bag Started!");
        }

        // Admin panel logic
        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_supabase.CurrentEmployee?.Role != "admin") return;

            LoadAdminMargins();
            AdminPanelGrid.Visibility = Visibility.Visible;
        }

        private void CloseAdminPanel_Click(object sender, RoutedEventArgs e)
        {
            AdminPanelGrid.Visibility = Visibility.Collapsed;
        }

        private void LoadAdminMargins()
        {
            try
            {
                using var db = new LocalDbContext();
                var articles = db.Articles.Where(a => a.Active).ToList();
                var costs = db.ArticleCosts.ToList();

                var marginList = articles.Select(a =>
                {
                    var cost = costs.FirstOrDefault(c => c.ArticleId == a.Id)?.UnitCost ?? 0m;
                    return new AdminArticleMargin
                    {
                        Name = a.Name,
                        Category = a.Category ?? "Uncategorized",
                        Price = a.Price,
                        UnitCost = cost
                    };
                }).ToList();

                AdminMarginsGrid.ItemsSource = marginList;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading admin margins: " + ex.Message);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Insert system closed notification (fire-and-forget)
            if (_supabase.CurrentEmployee != null)
            {
                _supabase.SendSystemClosedNotification(_supabase.CurrentEmployee.Id);
            }
            base.OnClosing(e);
        }
    }

    public class CategoryGroup
    {
        public string CategoryName { get; set; }
        public List<LocalArticle> Articles { get; set; }
    }

    public class AdminArticleMargin
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Profit => Price - UnitCost;
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
