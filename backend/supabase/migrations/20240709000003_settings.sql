-- Add expected_yield and tolerance to employees or articles?
-- Prompt says: "Admin sets an expected yield: how many coffees a single 1kg bag should produce"
-- and "may keep separate yields per bean type/product"
-- and "caisse fond (opening float) — globally or per worker/shift"

ALTER TABLE employees ADD COLUMN default_opening_float DECIMAL(12, 2) DEFAULT 50.00;
ALTER TABLE articles ADD COLUMN expected_yield INTEGER; -- if we want per-article yield?
-- Actually, bean bag has expected_yield. We might need a settings table.

CREATE TABLE settings (
    key TEXT PRIMARY KEY,
    value JSONB NOT NULL
);

INSERT INTO settings (key, value) VALUES
('bean_bag_config', '{"expected_yield": 50, "tolerance_percent": 10}'::jsonb),
('global_opening_float', '50.00'::jsonb);
