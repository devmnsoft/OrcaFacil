-- Sprint 42 / V4.3: operação de campo. Somente DDL aditivo e idempotente.
CREATE SCHEMA IF NOT EXISTS orcafacil;
CREATE TABLE IF NOT EXISTS orcafacil.field_teams (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_teams_account ON orcafacil.field_teams(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_team_members (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_team_members_account ON orcafacil.field_team_members(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_technician_profiles (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_technician_profiles_account ON orcafacil.field_technician_profiles(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_availability_windows (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_availability_windows_account ON orcafacil.field_availability_windows(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_dispatch_boards (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_dispatch_boards_account ON orcafacil.field_dispatch_boards(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_dispatch_events (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_dispatch_events_account ON orcafacil.field_dispatch_events(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_service_routes (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_service_routes_account ON orcafacil.field_service_routes(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_route_stops (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_route_stops_account ON orcafacil.field_route_stops(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_route_assignments (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_route_assignments_account ON orcafacil.field_route_assignments(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_sessions (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_sessions_account ON orcafacil.field_visit_sessions(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_checkins (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_checkins_account ON orcafacil.field_visit_checkins(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_checkouts (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_checkouts_account ON orcafacil.field_visit_checkouts(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_geo_events (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_geo_events_account ON orcafacil.field_visit_geo_events(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_evidences (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_evidences_account ON orcafacil.field_visit_evidences(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_evidence_reviews (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_evidence_reviews_account ON orcafacil.field_visit_evidence_reviews(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_checklists (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_checklists_account ON orcafacil.field_visit_checklists(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_checklist_items (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_checklist_items_account ON orcafacil.field_visit_checklist_items(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_checklist_answers (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_checklist_answers_account ON orcafacil.field_visit_checklist_answers(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_signatures (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_signatures_account ON orcafacil.field_visit_signatures(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_material_usages (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_material_usages_account ON orcafacil.field_visit_material_usages(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_time_entries (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_time_entries_account ON orcafacil.field_visit_time_entries(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_expenses (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_expenses_account ON orcafacil.field_visit_expenses(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_quality_reviews (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_quality_reviews_account ON orcafacil.field_visit_quality_reviews(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_visit_customer_feedback (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_visit_customer_feedback_account ON orcafacil.field_visit_customer_feedback(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_offline_devices (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_offline_devices_account ON orcafacil.field_offline_devices(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_offline_queue_items (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_offline_queue_items_account ON orcafacil.field_offline_queue_items(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_offline_sync_runs (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_offline_sync_runs_account ON orcafacil.field_offline_sync_runs(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_offline_sync_conflicts (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_offline_sync_conflicts_account ON orcafacil.field_offline_sync_conflicts(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_mobile_notifications (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_mobile_notifications_account ON orcafacil.field_mobile_notifications(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_service_outcomes (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_service_outcomes_account ON orcafacil.field_service_outcomes(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_safety_reports (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_safety_reports_account ON orcafacil.field_safety_reports(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_vehicle_assets (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_vehicle_assets_account ON orcafacil.field_vehicle_assets(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_vehicle_assignments (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_vehicle_assignments_account ON orcafacil.field_vehicle_assignments(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_travel_logs (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_travel_logs_account ON orcafacil.field_travel_logs(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_geofences (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_geofences_account ON orcafacil.field_geofences(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.field_geofence_events (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    work_order_id uuid NULL,
    user_id uuid NULL,
    team_id uuid NULL,
    status varchar(32) NOT NULL DEFAULT 'Pending',
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_field_geofence_events_account ON orcafacil.field_geofence_events(account_id);
-- Idempotência da fila offline é isolada por conta.
CREATE UNIQUE INDEX IF NOT EXISTS ux_field_offline_queue_account_key ON orcafacil.field_offline_queue_items(account_id, ((data_json->>'idempotencyKey'))) WHERE is_deleted = false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_field_feedback_work_order ON orcafacil.field_visit_customer_feedback(account_id, work_order_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_field_visits_open ON orcafacil.field_visit_sessions(account_id, work_order_id) WHERE status = 'Open' AND is_deleted = false;
