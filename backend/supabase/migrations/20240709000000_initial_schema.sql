-- Create enum for roles
CREATE TYPE employee_role AS ENUM ('admin', 'worker');

-- Create enum for order source
CREATE TYPE order_source AS ENUM ('pos', 'client_app');

-- Create employees table
CREATE TABLE employees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    role employee_role NOT NULL DEFAULT 'worker',
    pin_hash TEXT NOT NULL,
    active BOOLEAN NOT NULL DEFAULT true
);

-- Create shifts table
CREATE TABLE shifts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id),
    opened_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    closed_at TIMESTAMPTZ,
    opening_cash DECIMAL(12, 2) NOT NULL,
    closing_cash DECIMAL(12, 2),
    CONSTRAINT valid_closing_at CHECK (closed_at IS NULL OR closed_at >= opened_at)
);

-- Create articles table
CREATE TABLE articles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    price DECIMAL(12, 2) NOT NULL,
    category TEXT,
    requires_coffee BOOLEAN NOT NULL DEFAULT false,
    active BOOLEAN NOT NULL DEFAULT true
);

-- Create orders table
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id UUID REFERENCES shifts(id),
    employee_id UUID NOT NULL REFERENCES employees(id),
    source order_source NOT NULL DEFAULT 'pos',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    total DECIMAL(12, 2) NOT NULL,
    status TEXT NOT NULL DEFAULT 'completed'
);

-- Create order_items table
CREATE TABLE order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    article_id UUID REFERENCES articles(id),
    article_name TEXT NOT NULL,
    unit_price DECIMAL(12, 2) NOT NULL,
    quantity INTEGER NOT NULL CHECK (quantity > 0)
);

-- Create bean_bags table
CREATE TABLE bean_bags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id),
    shift_id UUID NOT NULL REFERENCES shifts(id),
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ended_at TIMESTAMPTZ,
    expected_yield INTEGER NOT NULL,
    coffee_count INTEGER NOT NULL DEFAULT 0,
    flagged BOOLEAN NOT NULL DEFAULT false
);

-- RLS Policies

ALTER TABLE employees ENABLE ROW LEVEL SECURITY;
ALTER TABLE shifts ENABLE ROW LEVEL SECURITY;
ALTER TABLE articles ENABLE ROW LEVEL SECURITY;
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE order_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE bean_bags ENABLE ROW LEVEL SECURITY;

-- Admin can do anything
CREATE POLICY "Admins have full access" ON employees FOR ALL TO authenticated USING (auth.jwt() ->> 'role' = 'admin');
CREATE POLICY "Admins have full access" ON shifts FOR ALL TO authenticated USING (auth.jwt() ->> 'role' = 'admin');
CREATE POLICY "Admins have full access" ON articles FOR ALL TO authenticated USING (auth.jwt() ->> 'role' = 'admin');
CREATE POLICY "Admins have full access" ON orders FOR ALL TO authenticated USING (auth.jwt() ->> 'role' = 'admin');
CREATE POLICY "Admins have full access" ON order_items FOR ALL TO authenticated USING (auth.jwt() ->> 'role' = 'admin');
CREATE POLICY "Admins have full access" ON bean_bags FOR ALL TO authenticated USING (auth.jwt() ->> 'role' = 'admin');

-- Worker policies
-- Workers can see their own info (needed for PIN login result?)
-- Actually PIN login returns a token.

-- Workers can view articles (name and price only - handled by view or restricted select?)
-- Prompt says: "View the article list (name + price only — no cost, no margin, no sales totals)."
-- Our articles table doesn't have cost/margin yet, so simple SELECT is fine for now.
CREATE POLICY "Workers can view active articles" ON articles FOR SELECT TO authenticated USING (active = true);

-- Workers can open/close their own shifts
CREATE POLICY "Workers can view their own shifts" ON shifts FOR SELECT TO authenticated USING (employee_id = (auth.jwt() ->> 'sub')::UUID);
CREATE POLICY "Workers can insert their own shifts" ON shifts FOR INSERT TO authenticated WITH CHECK (employee_id = (auth.jwt() ->> 'sub')::UUID);
CREATE POLICY "Workers can update their own shifts" ON shifts FOR UPDATE TO authenticated USING (employee_id = (auth.jwt() ->> 'sub')::UUID);

-- Workers can take orders
CREATE POLICY "Workers can view orders for their shifts" ON orders FOR SELECT TO authenticated USING (employee_id = (auth.jwt() ->> 'sub')::UUID);
CREATE POLICY "Workers can insert orders" ON orders FOR INSERT TO authenticated WITH CHECK (employee_id = (auth.jwt() ->> 'sub')::UUID);

CREATE POLICY "Workers can insert order items" ON order_items FOR INSERT TO authenticated WITH CHECK (
    EXISTS (
        SELECT 1 FROM orders
        WHERE id = order_id AND employee_id = (auth.jwt() ->> 'sub')::UUID
    )
);

-- Workers can press "1kg of beans" (insert bean_bags)
CREATE POLICY "Workers can view their bean bags" ON bean_bags FOR SELECT TO authenticated USING (employee_id = (auth.jwt() ->> 'sub')::UUID);
CREATE POLICY "Workers can insert bean bags" ON bean_bags FOR INSERT TO authenticated WITH CHECK (employee_id = (auth.jwt() ->> 'sub')::UUID);
CREATE POLICY "Workers can update their active bean bag" ON bean_bags FOR UPDATE TO authenticated USING (employee_id = (auth.jwt() ->> 'sub')::UUID);

-- Realtime setup
ALTER PUBLICATION supabase_realtime ADD TABLE orders;
