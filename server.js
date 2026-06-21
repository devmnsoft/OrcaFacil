import Fastify from 'fastify';
import fastifyStatic from '@fastify/static';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const PORT = Number(process.env.PORT || 8095);
const HOST = process.env.HOST || '0.0.0.0';

const app = Fastify({ logger: true });

await app.register(fastifyStatic, {
  root: path.join(__dirname, 'public'),
  prefix: '/',
  index: ['index.html'],
  decorateReply: false
});

await app.register(fastifyStatic, {
  root: path.join(__dirname, 'public'),
  prefix: '/public/',
  index: ['index.html'],
  decorateReply: false
});

app.get('/health', async () => ({ status: 'ok', app: 'orcafacil', port: PORT }));

app.setNotFoundHandler((req, reply) => {
  if (req.raw.method === 'GET' && !req.url.startsWith('/api/') && !req.url.startsWith('/health')) {
    return reply.sendFile('index.html');
  }
  return reply.code(404).send({ error: 'Not found' });
});

try {
  await app.listen({ port: PORT, host: HOST });
  console.log(`OrçaFácil rodando em http://localhost:${PORT}`);
} catch (err) {
  app.log.error(err);
  process.exit(1);
}
