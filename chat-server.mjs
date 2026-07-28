import { WebSocketServer } from 'ws';
import { createServer } from 'http';
import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'fs';

const PORT = 5199;
const LOG = 'docs/AI_CHAT_LOG.md';

if (!existsSync('docs')) mkdirSync('docs', { recursive: true });
if (!existsSync(LOG)) writeFileSync(LOG, '# AI 三人聊天记录\n\n');

const server = createServer((req, res) => {
  if (req.url === '/') {
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(readFileSync('chat.html', 'utf8'));
  } else if (req.url === '/log') {
    res.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8' });
    res.end(readFileSync(LOG, 'utf8'));
  } else {
    res.writeHead(404); res.end();
  }
});

const wss = new WebSocketServer({ server });
const clients = new Set();

function broadcast(msg) {
  const data = JSON.stringify(msg);
  for (const ws of clients) ws.send(data);
}

wss.on('connection', (ws) => {
  clients.add(ws);
  ws.send(JSON.stringify({ type: 'history', text: readFileSync(LOG, 'utf8') }));

  ws.on('message', (raw) => {
    const { author, text } = JSON.parse(raw.toString());
    const ts = new Date().toISOString().replace('T', ' ').slice(0, 19);
    const line = `[${ts}] **${author}**: ${text}\n`;
    writeFileSync(LOG, line, { flag: 'a' });
    broadcast({ type: 'msg', author, text, time: ts });
  });

  ws.on('close', () => clients.delete(ws));
});

server.listen(PORT, () => {
  console.log(`聊天服务: http://127.0.0.1:${PORT}`);
  console.log(`日志文件: ${LOG}`);
});
