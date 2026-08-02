using System;
using System.Collections.Generic;
using System.IO;

namespace CoffeeShopPOS.Services
{
    public static class EnvLoader
    {
        private static readonly Dictionary<string, string> _envVars = new();
        private static bool _isLoaded;

        public static void Load()
        {
            if (_isLoaded) return;

            string envPath = FindEnvFile();
            if (string.IsNullOrEmpty(envPath))
            {
                Console.WriteLine(".env file not found.");
                _isLoaded = true;
                return;
            }

            try
            {
                var lines = File.ReadAllLines(envPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("#")) continue;

                    int separatorIndex = trimmed.IndexOf('=');
                    if (separatorIndex <= 0) continue;

                    string key = trimmed.Substring(0, separatorIndex).Trim();
                    string value = trimmed.Substring(separatorIndex + 1).Trim();

                    // Remove quotes if present
                    if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    else if (value.StartsWith("'") && value.EndsWith("'") && value.Length >= 2)
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    _envVars[key] = value;
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing .env file: " + ex.Message);
            }

            _isLoaded = true;
        }

        public static string Get(string key, string defaultValue = "")
        {
            Load();
            if (_envVars.TryGetValue(key, out string? value))
            {
                return value;
            }
            // Fall back to actual environment variables
            string? envVal = Environment.GetEnvironmentVariable(key);
            return envVal ?? defaultValue;
        }

        private static string FindEnvFile()
        {
            // Start from executable base directory
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;

            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(currentDir, ".env");
                if (File.Exists(path))
                {
                    return path;
                }

                var parent = Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;
            }

            // Also check current working directory
            string cwd = Directory.GetCurrentDirectory();
            string cwdPath = Path.Combine(cwd, ".env");
            if (File.Exists(cwdPath))
            {
                return cwdPath;
            }

            return string.Empty;
        }
    }
}
