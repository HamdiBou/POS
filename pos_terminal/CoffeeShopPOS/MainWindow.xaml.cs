using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CoffeeShopPOS.Services;
using CoffeeShopPOS.ViewModels;

namespace CoffeeShopPOS
{
    public partial class MainWindow : Window, IPinInputProvider
    {
        private readonly SupabaseService _supabase = SupabaseService.Instance;
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            _viewModel.PinInputProvider = this;
            this.DataContext = _viewModel;

            InitializeSupabase();
        }

        private async void InitializeSupabase()
        {
            try
            {
                // Hook events before initializing so that cached values trigger immediately
                _supabase.OnBrandingChanged += () => Dispatcher.Invoke(UpdateBranding);

                await _supabase.InitializeAsync();
                await _supabase.SyncArticlesInitialAsync();
                _viewModel.LoadArticles();

                _supabase.OnOrderReceived += (order) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"New Order from Client App! Total: {order.Total:C}", "Incoming Order");
                    });
                };

                // Trigger branding update instantly on load
                UpdateBranding();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize Supabase: " + ex.Message);
            }
        }

        // IPinInputProvider implementation
        public void AppendPin(string digit)
        {
            PinInput.Password += digit;
        }

        public void ClearPin()
        {
            PinInput.Password = string.Empty;
        }

        public string GetPin()
        {
            return PinInput.Password;
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
}
