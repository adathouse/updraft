# updraft

## Database bootstrap

Run the bootstrap SQL as a superuser to create the application role, database, schema, and grants.

```bash
psql -v ON_ERROR_STOP=1 -f db/bootstrap.sql
```

## Connection environment

```bash
export PGUSER=updraft
export PGPASSWORD=updraft
export PGDATABASE=updraft
export PGHOST=db
export PGPORT=5432
```

## Flyway migration

Apply schema migrations with Flyway.

```bash
cd flyway; flyway migrate
```

Check migration state.

```bash
cd flyway; flyway info
```
