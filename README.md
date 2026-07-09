# Coffee Shop POS Terminal

This is the POS terminal for a single coffee shop, built with .NET/WPF and Supabase.

## Environment Requirements

- **OS**: Windows (required to run the WPF application).
- **SDK**: .NET 8.0 or later.
- **Backend**: Supabase project.

## Setup Instructions

### 1. Supabase Backend
1. Create a new project on [Supabase](https://supabase.com).
2. Run the SQL scripts in `backend/supabase/migrations/` in the Supabase SQL Editor to set up the schema, RLS policies, and triggers.
3. Deploy the Edge Function:
   - Navigate to `backend/supabase/functions/pin-login`.
   - Run `supabase functions deploy pin-login`.
   - Set the `JWT_SECRET` environment variable in Supabase (found in Project Settings -> API).

### 2. Configure POS Terminal
1. Open `pos_terminal/CoffeeShopPOS/Services/SupabaseService.cs`.
2. Replace the following placeholders with your Supabase project details:
   - `YOUR_SUPABASE_URL`
   - `YOUR_SUPABASE_ANON_KEY`

### 3. Seed Data
You'll need at least one employee and some articles to use the app. You can use the Supabase dashboard or SQL:

```sql
-- PIN is '1234' (hashed with bcrypt)
INSERT INTO employees (name, role, pin_hash)
VALUES ('John Doe', 'worker', '$2a$10$3e87Bf5YI6N9eO7q.L6.ueK3y.j8S9w8G0e0e0e0e0e0e0e0e0e0e');

INSERT INTO articles (name, price, category, requires_coffee)
VALUES
('Espresso', 2.50, 'Coffee', true),
('Latte', 4.00, 'Coffee', true),
('Croissant', 3.00, 'Food', false);
```

### 4. Build and Run
```bash
cd pos_terminal/CoffeeShopPOS
dotnet build
dotnet run
```

## Testing the "1kg of beans" Logic
1. Log in with a worker PIN.
2. Open a shift with a float (e.g., 50.00).
3. Press the **"1kg Beans"** button to start a new bag.
4. Take several "Espresso" orders (which have `requires_coffee = true`).
5. Close the shift.
6. In the Supabase `bean_bags` table, you will see the `coffee_count` incremented automatically by the Postgres trigger.
7. If the final count deviates significantly from the `expected_yield`, the `flagged` bit will be set upon closing the bag.

## Realtime Notifications
To test incoming orders from the future client app:
- Insert a record into the `orders` table with `source = 'client_app'`.
- The POS terminal will show a popup notification immediately.
