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
or `Oidc__RequireHttps=false` in Railway.

### Generate OIDC credentials

Run the following commands in PowerShell. They create a two-year RSA signing
certificate with a private key and export it as a password-protected PFX file:

```powershell
$password = Read-Host "PFX password" -AsSecureString

$certificate = New-SelfSignedCertificate `
  -Type Custom `
  -Subject "CN=QuizIt OIDC Signing" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -KeyUsage DigitalSignature `
  -KeyExportPolicy Exportable `
  -HashAlgorithm SHA256 `
  -NotAfter (Get-Date).AddYears(2)

Export-PfxCertificate `
  -Cert $certificate `
  -FilePath ".\quizit-oidc-signing.pfx" `
  -Password $password
```

Copy the PFX file as a Base64 string and set it as
`Oidc__SigningCertificateBase64` in Railway:

```powershell
[Convert]::ToBase64String(
  [System.IO.File]::ReadAllBytes(".\quizit-oidc-signing.pfx")
) | Set-Clipboard
```

Set `Oidc__SigningCertificatePassword` to the password entered above. Generate
the 32-byte AES encryption key, then paste the Base64 value into
`Oidc__EncryptionKeyBase64`:

```powershell
$key = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($key)
$rng.Dispose()

[Convert]::ToBase64String($key) | Set-Clipboard
```

Do not commit the PFX file, its password, or any of these Base64 values. Keep
all three Railway variables stable: rotating a signing certificate or the
encryption key invalidates issued access and refresh tokens.

Configure the service health check path as `/health`.

## Frontend

- Create a second service from the same GitHub repository.
- Set **Root Directory** to `Webui`.
- Generate a public domain for this service.
- Set `BACKEND_API_URL` to the backend URL reachable from the frontend host.
- Set `BACKEND_PUBLIC_URL` to the public backend URL used by the browser for
  the SignalR WebSocket connection.
- When deploying the frontend to Railway, set `ORIGIN` to its exact public
  frontend URL, for example `https://quizit-production.up.railway.app`.

For a Netlify frontend, both values are normally the backend's Railway public
URL. The SvelteKit server reads them in Netlify Functions at runtime, so set
them in the Netlify UI with the `Functions` scope (or all scopes). A variable
change needs a new deploy, but does not require changing a build-time API
variable or frontend source code.

## Runtime constraints

- Keep the backend at one replica: active `GameRoom` state is in memory and
  SignalR has no shared backplane.
- The backend applies EF Core migrations at startup. Keep a single replica
  until migrations are moved to a dedicated deployment job.
