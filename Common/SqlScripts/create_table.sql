BEGIN;
CREATE TABLE orders(
    order_id INTEGER PRIMARY KEY,
    product_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    pick_up_point_id INTEGER NOT NULL,
    price DECIMAL NOT NULL,
    status INTEGER NOT NULL
);
COMMIT;