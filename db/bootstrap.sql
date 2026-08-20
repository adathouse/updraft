-- Run this script as a PostgreSQL psqladmin
CREATE DATABASE updraft;

\connect updraft

DO
$$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'updraft') THEN
        CREATE ROLE updraft LOGIN PASSWORD 'updraft';
    END IF;
END
$$;

CREATE SCHEMA IF NOT EXISTS updraft AUTHORIZATION updraft;
GRANT USAGE, CREATE ON SCHEMA updraft TO updraft;
ALTER ROLE updraft IN DATABASE updraft SET search_path = updraft, public;

CREATE EXTENSION IF NOT EXISTS pgcrypto;
