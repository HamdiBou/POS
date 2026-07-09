-- Trigger to increment coffee_count in bean_bags
CREATE OR REPLACE FUNCTION increment_bean_bag_count()
RETURNS TRIGGER AS $$
DECLARE
    v_requires_coffee BOOLEAN;
    v_employee_id UUID;
BEGIN
    SELECT requires_coffee INTO v_requires_coffee FROM articles WHERE id = NEW.article_id;

    IF v_requires_coffee THEN
        SELECT employee_id INTO v_employee_id FROM orders WHERE id = NEW.order_id;

        UPDATE bean_bags
        SET coffee_count = coffee_count + NEW.quantity
        WHERE employee_id = v_employee_id
          AND ended_at IS NULL;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_increment_bean_bag_count
AFTER INSERT ON order_items
FOR EACH ROW
EXECUTE FUNCTION increment_bean_bag_count();

-- Trigger to flag bean bag on close
CREATE OR REPLACE FUNCTION flag_bean_bag()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.ended_at IS NOT NULL AND OLD.ended_at IS NULL THEN
        -- Compute if flagged based on expected_yield (tolerance could be another column)
        -- Assuming a 10% tolerance for now as a default
        IF ABS(NEW.coffee_count - NEW.expected_yield) > (NEW.expected_yield * 0.1) THEN
            NEW.flagged := true;
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_flag_bean_bag
BEFORE UPDATE ON bean_bags
FOR EACH ROW
EXECUTE FUNCTION flag_bean_bag();
