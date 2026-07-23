using System;
using System.Windows;
using System.Windows.Media;

namespace CoffeeShopPOS
{
    public static class ThemeHelper
    {
        public static void ApplyDefaultTheme()
        {
            try
            {
                var defaultTheme = new ResourceDictionary
                {
                    Source = new Uri("Themes/DefaultTheme.xaml", UriKind.Relative)
                };
                var appResources = Application.Current.Resources;
                appResources.MergedDictionaries.Clear();
                appResources.MergedDictionaries.Add(defaultTheme);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load default theme: " + ex.Message);
            }
        }

        public static void ApplyBrandColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
            {
                ApplyDefaultTheme();
                return;
            }

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                var brush = new SolidColorBrush(color);

                var customTheme = new ResourceDictionary();
                customTheme["HeaderBackgroundBrush"] = brush;
                customTheme["AccentBrush"] = brush;

                var appResources = Application.Current.Resources;
                appResources.MergedDictionaries.Clear();
                appResources.MergedDictionaries.Add(customTheme);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to apply brand color: " + ex.Message);
                ApplyDefaultTheme();
            }
        }
    }
}
