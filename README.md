# QuizIt

QuizIt is a real-time multiplayer quiz application for collaborative learning.
Players join a room with a short code, answer questions together, and review
explanations after each answer is revealed.

The initial quiz format is inspired by *Who Wants to Be a Millionaire?*: every
question has four answer options and one correct answer. Questions can include
code context, explanations, and a difficulty from 0 to 1000 in steps of 100.

## Features

- Create a room for a selected quiz and share its game code.
- Anonymous room-scoped player credentials with host-only game controls.
- SignalR lobby updates with connected and disconnected player states.
- Configurable question count, answer time limit, and question order.
- Ascending difficulty, a specific difficulty, or mixed question selection.
- Server-side answer validation, scoring, and persisted game session results.
- Player-safe question payloads that do not expose correct answers before reveal.
- Code blocks and post-answer explanation dialogs for learning-focused quizzes.

## Architecture

```text
Webui     SvelteKit, Bits UI, SignalR client
Backend   ASP.NET Core Web API, SignalR, Entity Framework Core
Database  PostgreSQL
```

`GameRoom` is active runtime state stored in memory. `Quiz`, questions, answer
options, and completed `GameSession` data are persisted in PostgreSQL.
The current persisted schema is documented in [database-schema.sql](database-schema.sql).

## Repository layout

```text
Backend/        ASP.NET Core API, SignalR hub, EF Core migrations
Backend.Tests/  Unit tests for game rules and connection-string handling
Webui/          SvelteKit frontend
docker-compose.yml
```

## Run locally with Docker

Prerequisites: Docker Desktop with Docker Compose.

```powershell
Copy-Item .env.example .env
docker compose up --build -d
```

Open:

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:8080`
- Health check: `http://localhost:8080/health`

The backend applies EF Core migrations at startup. With
`SEED_DEMO_DATA=true`, it also seeds the ITP Workshop: Objects quiz.

Stop the stack:

```powershell
docker compose down
```

See [Docker deployment](docker-deployment.md) for environment variables and
operational commands.

## Development commands

Backend:

```powershell
dotnet test Backend.Tests/Backend.Tests.csproj
dotnet run --project Backend/Quizit.Api.csproj
```

Frontend:

```powershell
cd Webui
npm install
npm run dev
```

## Deployment

- [Railway deployment](railway-deployment.md) covers PostgreSQL, backend
  variables, CORS, health checks, and runtime constraints.
- Netlify deploys `Webui` with `Webui` as its Base directory. Set
  `VITE_API_BASE_URL` to the public backend URL. The Netlify adapter and
  publish directory are configured in `Webui/netlify.toml`.

For production, set `Database__SeedDemoData=false`, use a strong database
password, and run the backend as one replica while rooms remain in memory.
