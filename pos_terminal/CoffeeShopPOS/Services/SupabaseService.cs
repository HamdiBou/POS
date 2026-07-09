using System;
using System.Threading.Tasks;
using Supabase;
using CoffeeShopPOS.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace CoffeeShopPOS.Services
{
    public class SupabaseService
    {
        private static SupabaseService _instance;
        public static SupabaseService Instance => _instance ??= new SupabaseService();

        private Client _client;
        public Client Client => _client;

        private string _supabaseUrl = "YOUR_SUPABASE_URL";
        private string _supabaseKey = "YOUR_SUPABASE_ANON_KEY";
        private string _edgeFunctionUrl = "YOUR_SUPABASE_URL/functions/v1/pin-login";

        public Employee CurrentEmployee { get; private set; }
        public Shift CurrentShift { get; private set; }
        public BeanBag ActiveBeanBag { get; private set; }

        public async Task InitializeAsync()
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            _client = new Client(_supabaseUrl, _supabaseKey, options);
            await _client.InitializeAsync();

            // Realtime subscription for client_app orders
            await _client.Realtime.ConnectAsync();
            var channel = _client.Realtime.Channel("public:orders");
            channel.AddPostgresChangeHandler(Supabase.Realtime.PostgresChanges.PostgresChangesOptions.ListenType.Inserts, (sender, args) => {
                if (args.Payload?.Data != null)
                {
                    var order = JsonConvert.DeserializeObject<Order>(args.Payload.Data.ToString());
                    if (order != null && order.Source == "client_app")
                    {
                        OnOrderReceived?.Invoke(order);
                    }
                }
            });
            await channel.Subscribe();
        }

        public Action<Order> OnOrderReceived;

        public async Task<bool> LoginWithPinAsync(string pin)
        {
            using var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(new { pin }), Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(_edgeFunctionUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<LoginResponse>(resultJson);

                    // Set the session token to enable RLS
                    _client.Auth.SetSession(result.Token, "", true);

                    CurrentEmployee = new Employee {
                        Id = result.User.Id,
                        Name = result.User.Name,
                        Role = result.User.Role
                    };

                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        public async Task<Shift> OpenShiftAsync(decimal openingCash)
        {
            var shift = new Shift
            {
                EmployeeId = CurrentEmployee.Id,
                OpeningCash = openingCash,
                OpenedAt = DateTime.UtcNow
            };

            var response = await _client.From<Shift>().Insert(shift);
            CurrentShift = response.Model;
            return CurrentShift;
        }

        public async Task CloseShiftAsync(decimal closingCash)
        {
            if (CurrentShift == null) return;

            CurrentShift.ClosingCash = closingCash;
            CurrentShift.ClosedAt = DateTime.UtcNow;

            await _client.From<Shift>().Update(CurrentShift);
            CurrentShift = null;
            ActiveBeanBag = null;
        }

        public async Task<List<Article>> GetArticlesAsync()
        {
            var response = await _client.From<Article>().Where(x => x.Active).Get();
            return response.Models;
        }

        public async Task CreateOrderAsync(Order order, List<OrderItem> items)
        {
            order.ShiftId = CurrentShift?.Id;
            order.EmployeeId = CurrentEmployee.Id;
            order.CreatedAt = DateTime.UtcNow;

            var orderResponse = await _client.From<Order>().Insert(order);
            var savedOrder = orderResponse.Model;

            foreach (var item in items)
            {
                item.OrderId = savedOrder.Id;
                await _client.From<OrderItem>().Insert(item);

                // Update bean bag count if article requires coffee
                if (ActiveBeanBag != null)
                {
                    // In a real app, we'd fetch the article to check requires_coffee if not already in memory
                    // For this demo, assume we know if it requires coffee
                }
            }
        }

        public async Task StartNewBeanBagAsync()
        {
            if (CurrentShift == null) return;

            // Close previous bag if any
            if (ActiveBeanBag != null)
            {
                ActiveBeanBag.EndedAt = DateTime.UtcNow;
                await _client.From<BeanBag>().Update(ActiveBeanBag);
            }

            var newBag = new BeanBag
            {
                EmployeeId = CurrentEmployee.Id,
                ShiftId = CurrentShift.Id,
                StartedAt = DateTime.UtcNow,
                ExpectedYield = 50, // Default or fetch from settings
                CoffeeCount = 0
            };

            var response = await _client.From<BeanBag>().Insert(newBag);
            ActiveBeanBag = response.Model;
        }

        private class LoginResponse
        {
            public UserInfo User { get; set; }
            public string Token { get; set; }
        }

        private class UserInfo
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Role { get; set; }
        }
    }
}
