-- Ajuste local de usuário/senha para desenvolvimento
-- Execute como usuário postgres/superuser

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'orcafacil_user') THEN
        CREATE USER orcafacil_user WITH PASSWORD '123456';
    ELSE
        ALTER USER orcafacil_user WITH PASSWORD '123456';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'orcafacil') THEN
        CREATE DATABASE orcafacil OWNER orcafacil_user;
    END IF;
END $$;
