FROM node:22-slim

WORKDIR /app

COPY package*.json ./
RUN npm install --omit=dev

COPY tsconfig.json ./
COPY server.ts ./
COPY wwwroot/ ./wwwroot/

EXPOSE 3000
ENV PORT=3000 \
    NODE_ENV=production

CMD ["npx", "tsx", "server.ts"]
