# updraft

```
CREATE DATABASE updraft;
\connect updraft;
CREATE SCHEMA IF NOT EXISTS updraft;
CREATE ROLE updraft PASSWORD 'updraft' LOGIN;
GRANT USAGE ON SCHEMA updraft TO updraft;
GRANT ALL PRIVILEGES ON SCHEMA updraft TO updraft; 
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

```
export PGUSER=updraft
export PGPASSWORD=updraft
export PGDATABASE=updraft
export PGHOST=db
```
