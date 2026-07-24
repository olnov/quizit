# sv

Everything you need to build a Svelte project, powered by [`sv`](https://github.com/sveltejs/cli).

## Creating a project

If you're seeing this, you've probably already done this step. Congrats!

```sh
# create a new project
npx sv create my-app
```

To recreate this project with the same configuration:

```sh
# recreate this project
npx sv@0.16.3 create --template minimal --types ts --add prettier eslint vitest="usages:unit,component" sveltekit-adapter="adapter:node" --install npm .
```

## Developing

Once you've created a project and installed dependencies with `npm install` (or `pnpm install` or `yarn`), start a development server:

```sh
npm run dev

# or start the server and open the app in a new browser tab
npm run dev -- --open
```

## Building

To create a production version of your app:

```sh
npm run build
```

You can preview the production build with `npm run preview`.

> To deploy your app, you may need to install an [adapter](https://svelte.dev/docs/kit/adapters) for your target environment.

## Deployment targets

The project supports both Docker and Netlify builds:

- Docker uses `@sveltejs/adapter-node` by default.
- Netlify uses `@sveltejs/adapter-netlify` automatically through Netlify's
  `NETLIFY=true` build variable. `DEPLOY_TARGET=netlify` is also supported for
  local verification.

For a Netlify deployment, set both `BACKEND_API_URL` and
`BACKEND_PUBLIC_URL` to the public Railway backend URL. SvelteKit reads
`BACKEND_API_URL` at runtime for its HTTP proxy; `BACKEND_PUBLIC_URL` is
returned at runtime only for the SignalR WebSocket connection.

Set these in the Netlify UI under **Environment variables**, not in
`netlify.toml`. Their scope must include **Functions** (or use the default
all-scopes setting), since the SvelteKit server endpoints run as Netlify
Functions. Trigger a new deploy after changing either variable.

Netlify uses `netlify.toml` to publish the adapter's `build` directory, which
contains the SvelteKit CSS and JavaScript assets.
