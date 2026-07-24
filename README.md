# QuizIt

QuizIt is a real-time quiz application for collaborative learning. Players can
either join a room with a short code or start an immediate solo game, answer
questions, and review explanations after each answer is revealed.

The initial quiz format is inspired by *Who Wants to Be a Millionaire?*: every
question has four answer options and one correct answer. Questions can include
code context, explanations, and a difficulty from 0 to 1000 in steps of 100.

## Features

- Create a room for a selected quiz and share its game code.
- Start a solo game directly from the same quiz and round settings, without a
  lobby or waiting for another player.
- Anonymous room-scoped player credentials with host-only game controls.
- SignalR lobby updates with connected and disconnected player states.
- Configurable question count, answer time limit, and question order.
- Ascending difficulty, a specific difficulty, or mixed question selection.
- Server-side answer validation, scoring, and persisted game session results.
- Player-safe question payloads that do not expose correct answers before reveal.
- Code blocks and post-answer explanation dialogs for learning-focused quizzes.
- Protected quiz administration for `Admin` and `QuizAuthor` users, including
  quiz editing, publishing, import, and export.

## Architecture

```text
Webui     SvelteKit, Bits UI, SignalR client
Backend   ASP.NET Core Web API, SignalR, Entity Framework Core
Database  PostgreSQL
```

`GameRoom` is active runtime state stored in memory. `Quiz`, questions, answer
options, and completed `GameSession` data are persisted in PostgreSQL.
Solo games use the same room and session lifecycle, but the host starts the
first question immediately and is the only player in the room.
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
  `BACKEND_API_URL` and `BACKEND_PUBLIC_URL` to the public Railway backend
  URL in **Environment variables**. Keep their scope as **All scopes** or at
  least include **Functions**, because SvelteKit reads them in Netlify
  Functions at runtime. The adapter and publish directory are configured in
  `Webui/netlify.toml`.

The frontend proxies HTTP requests such as `/api/v1/quizes` and
`/api/v1/connect/token` through SvelteKit to `BACKEND_API_URL`. The browser
uses `BACKEND_PUBLIC_URL` only for the direct SignalR WebSocket connection at
`/api/v1/hubs/game`. On Netlify and Railway these values normally match. A
variable change requires a new Netlify deploy, but does not require changing a
build-time API variable or frontend source code.

For production, set `Database__SeedDemoData=false`, use a strong database
password, and run the backend as one replica while rooms remain in memory.
