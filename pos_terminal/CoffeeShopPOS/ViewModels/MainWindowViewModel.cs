using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CoffeeShopPOS.Data;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;

namespace CoffeeShopPOS.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly SupabaseService _supabase = SupabaseService.Instance;
        private bool _isClosingShift;

        // Observable Collections
        public ObservableCollection<CategoryGroupViewModel> Categories { get; } = new();
        public ObservableCollection<CartItem> Cart { get; } = new();
        public ObservableCollection<AdminArticleMargin> AdminMargins { get; } = new();

        // UI State properties
        private string _employeeInfoText = string.Empty;
        public string EmployeeInfoText
        {
            get => _employeeInfoText;
            set => SetProperty(ref _employeeInfoText, value);
        }

        private string _totalText = "$0.00";
        public string TotalText
        {
            get => _totalText;
            set => SetProperty(ref _totalText, value);
        }

        private string _shiftDialogTitle = "Opening Float";
        public string ShiftDialogTitle
        {
            get => _shiftDialogTitle;
            set => SetProperty(ref _shiftDialogTitle, value);
        }

        private string _shiftCashInput = "50.00";
        public string ShiftCashInput
        {
            get => _shiftCashInput;
            set => SetProperty(ref _shiftCashInput, value);
        }

        private Visibility _loginGridVisibility = Visibility.Visible;
        public Visibility LoginGridVisibility
        {
            get => _loginGridVisibility;
            set => SetProperty(ref _loginGridVisibility, value);
        }

        private Visibility _adminPanelGridVisibility = Visibility.Collapsed;
        public Visibility AdminPanelGridVisibility
        {
            get => _adminPanelGridVisibility;
            set => SetProperty(ref _adminPanelGridVisibility, value);
        }

        private Visibility _mainPosGridVisibility = Visibility.Collapsed;
        public Visibility MainPosGridVisibility
        {
            get => _mainPosGridVisibility;
            set => SetProperty(ref _mainPosGridVisibility, value);
        }

        private Visibility _adminPanelButtonVisibility = Visibility.Collapsed;
        public Visibility AdminPanelButtonVisibility
        {
            get => _adminPanelButtonVisibility;
            set => SetProperty(ref _adminPanelButtonVisibility, value);
        }

        private Visibility _shiftDialogGridVisibility = Visibility.Collapsed;
        public Visibility ShiftDialogGridVisibility
        {
            get => _shiftDialogGridVisibility;
            set => SetProperty(ref _shiftDialogGridVisibility, value);
        }

        private bool _showNonSellableArticles;
        public bool ShowNonSellableArticles
        {
            get => _showNonSellableArticles;
            set
            {
                if (SetProperty(ref _showNonSellableArticles, value))
                {
                    LoadArticles();
                }
            }
        }

        public string ArticleViewButtonText => ShowNonSellableArticles ? "Afficher les articles vendables" : "Afficher les matières premières";

        // Commands
        public ICommand PinKeyCommand { get; }
        public ICommand PinClearCommand { get; }
        public ICommand PinOkCommand { get; }
        public ICommand AddArticleToCartCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand PayCommand { get; }
        public ICommand CloseShiftCommand { get; }
        public ICommand ShiftConfirmCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand BeansButtonCommand { get; }
        public ICommand AdminPanelCommand { get; }
        public ICommand CloseAdminPanelCommand { get; }
        public ICommand ToggleArticleViewCommand { get; }

        public MainWindowViewModel()
        {
            // Initialize Commands
            PinKeyCommand = new RelayCommand(ExecutePinKey);
            PinClearCommand = new RelayCommand(ExecutePinClear);
            PinOkCommand = new RelayCommand(async (p) => await ExecutePinOkAsync(p));
            AddArticleToCartCommand = new RelayCommand(ExecuteAddArticleToCart);
            ClearCartCommand = new RelayCommand(ExecuteClearCart);
            PayCommand = new RelayCommand(async (_) => await ExecutePayAsync());
            CloseShiftCommand = new RelayCommand(ExecuteCloseShift);
            ShiftConfirmCommand = new RelayCommand(async (_) => await ExecuteShiftConfirmAsync());
            LogoutCommand = new RelayCommand(async (_) => await ExecuteLogoutAsync());
            BeansButtonCommand = new RelayCommand(async (_) => await ExecuteBeansButtonAsync());
            AdminPanelCommand = new RelayCommand(ExecuteAdminPanel);
            CloseAdminPanelCommand = new RelayCommand(ExecuteCloseAdminPanel);
            ToggleArticleViewCommand = new RelayCommand(ExecuteToggleArticleView);

            // Hook Event Listeners
            _supabase.OnArticlesChanged += HandleArticlesChanged;
            _supabase.OnBrandingChanged += HandleBrandingChanged;

            // Check if session was restored automatically
            if (_supabase.CurrentEmployee != null)
            {
                OnLoginSuccess();
            }
        }

        // Event Handlers (Thread-Safe via Dispatcher)
        private void HandleArticlesChanged()
        {
            RunOnUIThread(LoadArticles);
        }

        private void HandleBrandingChanged()
        {
            RunOnUIThread(() =>
            {
                _supabase.ApplyCachedBranding();
            });
        }

        private void RunOnUIThread(Action action)
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(action);
                }
            }
            else
            {
                action();
            }
        }

        // Command Implementations
        private void ExecutePinKey(object? parameter)
        {
            if (parameter is string digit && PinInputProvider != null)
            {
                PinInputProvider.AppendPin(digit);
            }
        }

        private void ExecutePinClear(object? parameter)
        {
            PinInputProvider?.ClearPin();
        }

        private async Task ExecutePinOkAsync(object? parameter)
        {
            if (PinInputProvider == null) return;

            string pin = PinInputProvider.GetPin();
            bool success = await _supabase.LoginWithPinAsync(pin);

            if (success)
            {
                PinInputProvider.ClearPin();
                await _supabase.SyncArticlesInitialAsync();
                OnLoginSuccess();
            }
            else
            {
                MessageBox.Show("Code PIN invalide");
                PinInputProvider.ClearPin();
            }
        }

        private void OnLoginSuccess()
        {
            LoginGridVisibility = Visibility.Collapsed;
            EmployeeInfoText = $"Connecté en tant que : {_supabase.CurrentEmployee.Name} ({_supabase.CurrentEmployee.Role})";

            if (_supabase.CurrentEmployee.Role == "admin")
            {
                AdminPanelButtonVisibility = Visibility.Visible;
            }
            else
            {
                AdminPanelButtonVisibility = Visibility.Collapsed;
                AdminPanelGridVisibility = Visibility.Collapsed;
            }

            _isClosingShift = false;
            ShiftDialogTitle = "Opening Float";
            ShiftCashInput = "50.00";
            ShiftDialogGridVisibility = Visibility.Visible;

            LoadArticles();
        }

        private void ExecuteAddArticleToCart(object? parameter)
        {
            if (parameter is LocalArticle article)
            {
                if (!article.IsSellable)
                {
                    return;
                }

                var existing = Cart.FirstOrDefault(i => i.ArticleId == article.Id);
                if (existing != null)
                {
                    existing.Quantity++;
                }
                else
                {
                    Cart.Add(new CartItem
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
        }

        private void ExecuteClearCart(object? parameter)
        {
            Cart.Clear();
            UpdateTotal();
        }

        private async Task ExecutePayAsync()
        {
            if (Cart.Count == 0) return;

            var order = new Order { Total = Cart.Sum(i => i.Subtotal) };
            var items = Cart.Select(i => new OrderItem
            {
                ArticleId = i.ArticleId,
                ArticleName = i.Name,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList();

            await _supabase.CreateOrderAsync(order, items);

            Cart.Clear();
            UpdateTotal();
            MessageBox.Show("Commande enregistrée !");
        }

        private void ExecuteCloseShift(object? parameter)
        {
            _isClosingShift = true;
            ShiftDialogTitle = "Closing Cash";
            ShiftCashInput = "";
            ShiftDialogGridVisibility = Visibility.Visible;
        }

        private async Task ExecuteShiftConfirmAsync()
        {
            if (decimal.TryParse(ShiftCashInput, out decimal cash))
            {
                if (_isClosingShift)
                {
                    await _supabase.CloseShiftAsync(cash);
                    MainPosGridVisibility = Visibility.Collapsed;
                    AdminPanelButtonVisibility = Visibility.Collapsed;
                    AdminPanelGridVisibility = Visibility.Collapsed;
                    LoginGridVisibility = Visibility.Visible;
                }
                else
                {
                    await _supabase.OpenShiftAsync(cash);
                    LoadArticles();
                    MainPosGridVisibility = Visibility.Visible;
                }
                ShiftDialogGridVisibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("Veuillez saisir un montant valide.");
            }
        }

        private async Task ExecuteLogoutAsync()
        {
            await _supabase.LogoutAsync();

            MainPosGridVisibility = Visibility.Collapsed;
            AdminPanelButtonVisibility = Visibility.Collapsed;
            AdminPanelGridVisibility = Visibility.Collapsed;
            LoginGridVisibility = Visibility.Visible;
            EmployeeInfoText = string.Empty;
        }

        private async Task ExecuteBeansButtonAsync()
        {
            await _supabase.StartNewBeanBagAsync();
            MessageBox.Show("Nouveau sac de 1 kg de grains démarré !");
        }

        private void ExecuteAdminPanel(object? parameter)
        {
            if (_supabase.CurrentEmployee?.Role != "admin") return;

            LoadAdminMargins();
            AdminPanelGridVisibility = Visibility.Visible;
        }

        private void ExecuteCloseAdminPanel(object? parameter)
        {
            AdminPanelGridVisibility = Visibility.Collapsed;
        }

        private void ExecuteToggleArticleView(object? parameter)
        {
            ShowNonSellableArticles = !ShowNonSellableArticles;
            OnPropertyChanged(nameof(ArticleViewButtonText));
        }

        // Logic Helpers
        public void LoadArticles()
        {
            try
            {
                using var db = new LocalDbContext();
                var activeArticles = db.Articles
                    .Where(a => a.Active && (ShowNonSellableArticles ? !a.IsSellable : a.IsSellable))
                    .ToList();

                // Group by category for Category Flow Layout
                var grouped = activeArticles
                    .GroupBy(a => string.IsNullOrEmpty(a.Category) ? "Uncategorized" : a.Category)
                    .Select(g => new CategoryGroupViewModel
                    {
                        CategoryName = g.Key,
                        Articles = new ObservableCollection<LocalArticle>(g.ToList())
                    })
                    .ToList();

                Categories.Clear();
                foreach (var group in grouped)
                {
                    Categories.Add(group);
                }

                if (AdminPanelGridVisibility == Visibility.Visible)
                {
                    LoadAdminMargins();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading articles to ViewModel: " + ex.Message);
            }
        }

        public void LoadAdminMargins()
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

                AdminMargins.Clear();
                foreach (var margin in marginList)
                {
                    AdminMargins.Add(margin);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading admin margins to ViewModel: " + ex.Message);
            }
        }

        private void UpdateTotal()
        {
            TotalText = Cart.Sum(i => i.Subtotal).ToString("C");
        }

        // Interface for accessing PasswordBox PIN securely
        public IPinInputProvider? PinInputProvider { get; set; }

        // INotifyPropertyChanged support
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class CategoryGroupViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public ObservableCollection<LocalArticle> Articles { get; set; } = new();
    }

    public class AdminArticleMargin
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Profit => Price - UnitCost;
    }

    public class CartItem : INotifyPropertyChanged
    {
        public Guid ArticleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public bool RequiresCoffee { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal Subtotal => UnitPrice * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public interface IPinInputProvider
    {
        void AppendPin(string digit);
        void ClearPin();
        string GetPin();
    }
}
