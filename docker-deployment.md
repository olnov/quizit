# Docker deployment

## Local start

```powershell
Copy-Item .env.example .env
docker compose up --build -d
```

The frontend is available at `http://localhost:3000` and the API at
`http://localhost:8080`. PostgreSQL data is retained in the
`postgres_data` Docker volume.

The backend applies EF Core migrations when it starts. `SEED_DEMO_DATA=true`
also creates the development ITP Workshop quiz.

## Production configuration

Set a strong `POSTGRES_PASSWORD`, then set these values in `.env` for the
public domains used by browsers:

```dotenv
PUBLIC_API_BASE_URL=https://api.example.com
FRONTEND_ORIGIN=https://quiz.example.com
SEED_DEMO_DATA=false
```

`PUBLIC_API_BASE_URL` is the address exposed to browsers. Inside Docker, the
SvelteKit server uses the internal `http://backend:8080` address for its HTTP
proxy and uses `PUBLIC_API_BASE_URL` only when it returns the SignalR endpoint
to a browser. `FRONTEND_ORIGIN` also configures SvelteKit's `ORIGIN` runtime
variable for CSRF protection, so it must exactly match the browser URL,
including its scheme and port.

`OIDC_REQUIRE_HTTPS=false` is appropriate only for this local HTTP setup. Keep
it `true` in production, where the frontend and backend must be served through
HTTPS.

Put the frontend and backend behind a TLS-terminating reverse proxy. Do not
publish the PostgreSQL port outside the host unless it is explicitly needed.

## Operations

```powershell
docker compose logs -f backend
docker compose down
```
