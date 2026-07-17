FROM node:24.13.0-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . ./

ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ARG DEPLOY_TARGET=docker
ENV DEPLOY_TARGET=${DEPLOY_TARGET}
RUN npm run build

FROM node:24.13.0-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000

COPY --from=build /app/build ./build
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/package.json ./package.json

USER node

EXPOSE 3000

CMD ["node", "build"]
