BEGIN;
CREATE INDEX index_user_id on orders(user_id);
COMMIT;