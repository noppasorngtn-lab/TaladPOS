-- One-time manual setup for the Playwright E2E suite.
-- Creates a Cashier staff account so RBAC/negative-path tests and Cashier-checkout
-- tests have a real login to use. Reuses the seeded Admin's password hash verbatim
-- (PasswordHasher.Verify parses "iterations.salt.hash" per-row, independent of the
-- row's identity), so the login password is the same as Admin's: Admin@12345.
--
-- Run against the dev DB, e.g.:
--   docker exec -i taladpos-postgres psql -U postgres -d taladpos -f - < web/tests/fixtures/seed-e2e-cashier.sql
INSERT INTO "Staff" ("Id", "Name", "Username", "PasswordHash", "Role", "IsActive")
VALUES (
  gen_random_uuid(),
  'E2E Cashier',
  'e2e-cashier',
  '100000.qJbT6qmMZ7LM4WSzJQ0kTQ==./j1yVCxzraMfhxXnvOplW7XTVkMGDlqwKpprbahxAgg=',
  'Cashier',
  true
)
ON CONFLICT ("Username") DO NOTHING;
