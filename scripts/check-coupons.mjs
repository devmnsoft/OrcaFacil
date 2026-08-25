import { requireFiles } from './check-growth-v34-lib.mjs';
requireFiles('coupons', ['database/sprint33_growth_v34.sql'], ['growth_coupon_codes', 'growth_coupon_redemptions', 'discount_amount <= original_amount']);
