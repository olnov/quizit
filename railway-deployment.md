# Railway deployment

Deploy three Railway services from this repository: PostgreSQL, backend, and
frontend. Do not deploy the root `docker-compose.yml`; it is for local use.

## Backend

- Create a service from the GitHub repository.
- Set **Root Directory** to `Backend`. Railway will detect `Backend/Dockerfile`.
- Generate a public domain for this service.
- Set these variables, replacing `Postgres` with the actual Railway PostgreSQL
  service name:

```dotenv
DATABASE_URL=${{Postgres.DATABASE_URL}}
Cors__AllowedOrigins__0=https://<frontend-domain>
Database__SeedDemoData=false
```

The backend accepts both Npgsql `Host=...;Database=...` strings and Railway's
`postgresql://...` format. Add the variable through Railway's **Add reference**
control, selecting `DATABASE_URL` from the PostgreSQL service; replace
`Postgres` above with that service's exact name. The backend reads
`ConnectionStrings__Postgres` first, then `DATABASE_PRIVATE_URL`, then
`DATABASE_URL`. `PORT` and `RAILWAY_ENVIRONMENT` are supplied by Railway. The backend reads
`PORT`, binds to all interfaces, trusts Railway's forwarded HTTPS headers, and
exposes `GET /health` for a Railway health check.

Configure the service health check path as `/health`.

## Frontend

- Create a second service from the same GitHub repository.
- Set **Root Directory** to `Webui`.
- Generate a public domain for this service.
- Set the Docker build argument `VITE_API_BASE_URL` to the backend public URL,
  for example `https://api-production-xxxx.up.railway.app`.

Vite embeds this value into the browser bundle, so changing it requires a new
frontend build and deployment.

## Runtime constraints

- Keep the backend at one replica: active `GameRoom` state is in memory and
  SignalR has no shared backplane.
- The backend applies EF Core migrations at startup. Keep a single replica
  until migrations are moved to a dedicated deployment job.
