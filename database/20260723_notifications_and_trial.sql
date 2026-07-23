create schema if not exists orcafacil;

create table if not exists orcafacil.notifications (
    id uuid primary key,
    user_id uuid not null,
    title varchar(160) not null,
    message varchar(800) not null,
    type varchar(20) not null default 'Info',
    category varchar(30) not null default 'System',
    action_url varchar(400),
    action_text varchar(80),
    is_read boolean not null default false,
    read_at timestamp with time zone,
    document_id uuid,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone,
    is_deleted boolean not null default false
);

create index if not exists ix_notifications_user_read_deleted on orcafacil.notifications (user_id, is_read, is_deleted);

alter table orcafacil.subscriptions add column if not exists trial_started_at timestamp with time zone;
alter table orcafacil.subscriptions add column if not exists trial_ends_at timestamp with time zone;
alter table orcafacil.subscriptions add column if not exists trial_used boolean not null default false;
alter table orcafacil.subscriptions add column if not exists trial_status varchar(30) not null default 'NotStarted';
