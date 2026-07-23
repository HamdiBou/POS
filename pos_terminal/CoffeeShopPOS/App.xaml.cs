using System;
using System.Configuration;
using System.Data;
using System.Windows;
using CoffeeShopPOS.Data;

namespace CoffeeShopPOS;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            using (var db = new LocalDbContext())
            {
                db.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to initialize local database: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
