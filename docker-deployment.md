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

Put the frontend and backend behind a TLS-terminating reverse proxy. Do not
publish the PostgreSQL port outside the host unless it is explicitly needed.

## Operations

```powershell
docker compose logs -f backend
docker compose down
```
