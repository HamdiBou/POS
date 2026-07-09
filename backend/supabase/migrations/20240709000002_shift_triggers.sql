-- Trigger to close previous shift when a new one is opened
CREATE OR REPLACE FUNCTION close_previous_shift()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE shifts
    SET closed_at = now()
    WHERE id <> NEW.id
      AND closed_at IS NULL;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_close_previous_shift
BEFORE INSERT ON shifts
FOR EACH ROW
EXECUTE FUNCTION close_previous_shift();
