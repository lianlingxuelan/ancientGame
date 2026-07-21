import { createServer } from "node:http";
import {
  closeDatabase,
  getChapters,
  getFormation,
  getPlayer,
  getPlayerCharacters,
  getProgress,
  initializeDatabase,
  saveFormation,
  saveProgress,
  updatePlayerName,
} from "./database.mjs";

const host = process.env.SHOUYOU_SERVER_HOST ?? "127.0.0.1";
const port = Number(process.env.SHOUYOU_SERVER_PORT ?? 5188);
const maximumBodySize = 1024 * 1024;

initializeDatabase();

const server = createServer(async (request, response) => {
  // Unity 编辑器、Windows 构建和本地网页调试都允许调用本地接口。
  setCorsHeaders(response);

  if (request.method === "OPTIONS") {
    response.writeHead(204);
    response.end();
    return;
  }

  try {
    const url = new URL(request.url ?? "/", `http://${request.headers.host}`);
    const pathname = normalizePath(url.pathname);

    if (request.method === "GET" && pathname === "/") {
      sendHtml(response, createHomePage());
      return;
    }

    if (request.method === "GET" && pathname === "/api/health") {
      sendJson(response, 200, {
        ok: true,
        service: "ShouyouServer",
        version: "0.1.0",
        time: new Date().toISOString(),
      });
      return;
    }

    if (request.method === "GET" && pathname === "/openapi.json") {
      sendJson(response, 200, createOpenApiDocument());
      return;
    }

    if (request.method === "GET" && pathname === "/api/v1/player/profile") {
      const playerId = getRequiredPlayerId(url);
      const player = getPlayer(playerId);
      sendEntityOrNotFound(response, player, "玩家不存在");
      return;
    }

    if (request.method === "PUT" && pathname === "/api/v1/player/profile") {
      const playerId = getRequiredPlayerId(url);
      ensurePlayerExists(playerId);
      const body = await readJsonBody(request);
      const name = requireNonEmptyString(body.name, "name");
      sendJson(response, 200, updatePlayerName(playerId, name));
      return;
    }

    if (request.method === "GET" && pathname === "/api/v1/characters") {
      const playerId = getRequiredPlayerId(url);
      ensurePlayerExists(playerId);
      sendJson(response, 200, {
        playerId,
        characters: getPlayerCharacters(playerId),
      });
      return;
    }

    if (request.method === "GET" && pathname === "/api/v1/chapters") {
      sendJson(response, 200, { chapters: getChapters() });
      return;
    }

    if (request.method === "GET" && pathname === "/api/v1/formation") {
      const playerId = getRequiredPlayerId(url);
      ensurePlayerExists(playerId);
      sendJson(response, 200, {
        playerId,
        slots: getFormation(playerId),
      });
      return;
    }

    if (request.method === "PUT" && pathname === "/api/v1/formation") {
      const playerId = getRequiredPlayerId(url);
      ensurePlayerExists(playerId);
      const body = await readJsonBody(request);
      const slots = validateFormationSlots(body.slots);
      sendJson(response, 200, {
        playerId,
        slots: saveFormation(playerId, slots),
      });
      return;
    }

    if (request.method === "GET" && pathname === "/api/v1/save") {
      const playerId = getRequiredPlayerId(url);
      ensurePlayerExists(playerId);
      sendEntityOrNotFound(response, getProgress(playerId), "存档不存在");
      return;
    }

    if (request.method === "PUT" && pathname === "/api/v1/save") {
      const playerId = getRequiredPlayerId(url);
      ensurePlayerExists(playerId);
      const body = await readJsonBody(request);
      const progress = {
        currentChapterId: requireNonEmptyString(
          body.currentChapterId,
          "currentChapterId",
        ),
        currentStageId: requireNonEmptyString(
          body.currentStageId,
          "currentStageId",
        ),
        completedStageIds: validateStringArray(
          body.completedStageIds ?? [],
          "completedStageIds",
        ),
      };
      sendJson(response, 200, saveProgress(playerId, progress));
      return;
    }

    sendJson(response, 404, {
      error: "接口不存在",
      path: pathname,
    });
  } catch (error) {
    const statusCode = Number(error.statusCode ?? 500);
    const publicMessage =
      statusCode >= 500 ? "服务器内部错误" : error.message;

    console.error(`[${new Date().toISOString()}]`, error);
    sendJson(response, statusCode, {
      error: publicMessage,
    });
  }
});

server.listen(port, host, () => {
  console.log(`手游本地服务器已启动：http://${host}:${port}`);
  console.log(`健康检查：http://${host}:${port}/api/health`);
  console.log(`接口文档：http://${host}:${port}/`);
});

function setCorsHeaders(response) {
  response.setHeader("Access-Control-Allow-Origin", "*");
  response.setHeader(
    "Access-Control-Allow-Methods",
    "GET,PUT,POST,DELETE,OPTIONS",
  );
  response.setHeader(
    "Access-Control-Allow-Headers",
    "Content-Type,Authorization",
  );
}

function normalizePath(pathname) {
  if (pathname.length > 1 && pathname.endsWith("/")) {
    return pathname.slice(0, -1);
  }
  return pathname;
}

function sendJson(response, statusCode, value) {
  const json = JSON.stringify(value, null, 2);
  response.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(json),
  });
  response.end(json);
}

function sendHtml(response, html) {
  response.writeHead(200, {
    "Content-Type": "text/html; charset=utf-8",
    "Content-Length": Buffer.byteLength(html),
  });
  response.end(html);
}

function sendEntityOrNotFound(response, entity, message) {
  if (!entity) {
    throw createHttpError(404, message);
  }
  sendJson(response, 200, entity);
}

function getRequiredPlayerId(url) {
  return requireNonEmptyString(url.searchParams.get("playerId"), "playerId");
}

function ensurePlayerExists(playerId) {
  if (!getPlayer(playerId)) {
    throw createHttpError(404, "玩家不存在");
  }
}

async function readJsonBody(request) {
  const chunks = [];
  let size = 0;

  for await (const chunk of request) {
    size += chunk.length;
    if (size > maximumBodySize) {
      throw createHttpError(413, "请求内容过大");
    }
    chunks.push(chunk);
  }

  if (chunks.length === 0) {
    return {};
  }

  try {
    return JSON.parse(Buffer.concat(chunks).toString("utf8"));
  } catch {
    throw createHttpError(400, "请求内容不是有效 JSON");
  }
}

function requireNonEmptyString(value, fieldName) {
  if (typeof value !== "string" || value.trim().length === 0) {
    throw createHttpError(400, `${fieldName} 不能为空`);
  }
  return value.trim();
}

function validateStringArray(value, fieldName) {
  if (!Array.isArray(value) || value.some((item) => typeof item !== "string")) {
    throw createHttpError(400, `${fieldName} 必须是字符串数组`);
  }
  return value;
}

function validateFormationSlots(value) {
  if (!Array.isArray(value) || value.length !== 6) {
    throw createHttpError(400, "slots 必须正好包含六个编队位置");
  }

  const normalizedSlots = value.map((item) => {
    if (item === null || item === "") {
      return null;
    }
    if (typeof item !== "string") {
      throw createHttpError(400, "编队位置只能是角色 ID 或 null");
    }
    return item;
  });

  const characters = normalizedSlots.filter(Boolean);
  if (new Set(characters).size !== characters.length) {
    throw createHttpError(400, "同一个角色不能重复上阵");
  }
  return normalizedSlots;
}

function createHttpError(statusCode, message) {
  const error = new Error(message);
  error.statusCode = statusCode;
  return error;
}

function createHomePage() {
  return `<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>手游本地服务器</title>
  <style>
    body { max-width: 920px; margin: 40px auto; padding: 0 20px; color: #463024;
      background: #f4efe3; font-family: "Microsoft YaHei", sans-serif; }
    h1 { color: #8b6a35; }
    section { margin: 20px 0; padding: 20px; border: 1px solid #d8b96a;
      border-radius: 16px; background: rgba(255,255,255,.65); }
    code { color: #42645d; }
    a { color: #6f8f88; }
  </style>
</head>
<body>
  <h1>手游本地服务器已运行</h1>
  <p>默认测试玩家：<code>demo-player</code></p>
  <section>
    <h2>可用接口</h2>
    <ul>
      <li><a href="/api/health">GET /api/health</a></li>
      <li><a href="/api/v1/player/profile?playerId=demo-player">GET /api/v1/player/profile</a></li>
      <li><a href="/api/v1/characters?playerId=demo-player">GET /api/v1/characters</a></li>
      <li><a href="/api/v1/chapters">GET /api/v1/chapters</a></li>
      <li><a href="/api/v1/formation?playerId=demo-player">GET /api/v1/formation</a></li>
      <li><a href="/api/v1/save?playerId=demo-player">GET /api/v1/save</a></li>
      <li><a href="/openapi.json">OpenAPI JSON</a></li>
    </ul>
  </section>
  <p>关闭服务器：回到启动服务器的终端窗口，按 <code>Ctrl + C</code>。</p>
</body>
</html>`;
}

function createOpenApiDocument() {
  return {
    openapi: "3.0.3",
    info: {
      title: "手游本地服务器 API",
      version: "0.1.0",
      description: "Unity Demo 使用的玩家、角色、章节、编队和存档接口。",
    },
    servers: [{ url: `http://${host}:${port}` }],
    paths: {
      "/api/health": { get: { summary: "健康检查" } },
      "/api/v1/player/profile": {
        get: { summary: "获取玩家资料" },
        put: { summary: "修改玩家名称" },
      },
      "/api/v1/characters": { get: { summary: "获取玩家角色" } },
      "/api/v1/chapters": { get: { summary: "获取章节和关卡" } },
      "/api/v1/formation": {
        get: { summary: "获取六人编队" },
        put: { summary: "保存六人编队" },
      },
      "/api/v1/save": {
        get: { summary: "获取玩家存档" },
        put: { summary: "保存玩家进度" },
      },
    },
  };
}

function shutdown(signal) {
  console.log(`收到 ${signal}，正在关闭服务器……`);
  server.close(() => {
    closeDatabase();
    process.exit(0);
  });
}

process.on("SIGINT", () => shutdown("SIGINT"));
process.on("SIGTERM", () => shutdown("SIGTERM"));
