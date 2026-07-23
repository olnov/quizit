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
InitialAdmin__Username=<initial-admin-username>
InitialAdmin__Email=<initial-admin-email>
InitialAdmin__Password=<strong-initial-admin-password>
Oidc__Issuer=https://<backend-domain>
Oidc__WebuiRedirectUri=https://<frontend-domain>/admin/auth/callback
Oidc__WebuiPostLogoutRedirectUri=https://<frontend-domain>/admin/login
Oidc__SigningCertificateBase64=<base64-pfx-with-private-key>
Oidc__SigningCertificatePassword=<pfx-password>
Oidc__EncryptionKeyBase64=<base64-encoded-32-byte-key>
```

The backend accepts both Npgsql `Host=...;Database=...` strings and Railway's
`postgresql://...` format. Add the variable through Railway's **Add reference**
control, selecting `DATABASE_URL` from the PostgreSQL service; replace
`Postgres` above with that service's exact name. The backend reads
`ConnectionStrings__Postgres` first, then `DATABASE_PRIVATE_URL`, then
`DATABASE_URL`. `PORT` and `RAILWAY_ENVIRONMENT` are supplied by Railway. The backend reads
`PORT`, binds to all interfaces, trusts Railway's forwarded HTTPS headers, and
exposes `GET /health` for a Railway health check.

Set all three `InitialAdmin__...` variables only for the first deployment. The
backend provisions the user idempotently and ensures it has the `Admin` role.
After the account exists, remove all three variables from Railway.

OpenIddict uses the signing certificate to issue standard JWTs and the
encryption key to protect authorization codes and refresh tokens. Keep both
values stable across deployments. Do not set `Oidc__AllowEphemeralCredentials`
in Railway.

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
