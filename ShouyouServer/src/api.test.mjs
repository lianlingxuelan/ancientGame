import { describe, it, before, after } from "node:test";
import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import net from "node:net";

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
let serverProcess;
let serverUrl;

function probePort(port) {
  return new Promise((resolve, reject) => {
    const s = net.createServer();
    s.unref();
    s.once("error", reject);
    s.listen({ host: "127.0.0.1", port, exclusive: true }, () => {
      s.close(() => resolve(port));
    });
  });
}

async function findFreePort() {
  for (let port = 5190; port < 5210; port += 1) {
    try {
      return await probePort(port);
    } catch {}
  }
  return probePort(0); // OS picks
}

describe("ShouyouServer API", () => {
  before(async () => {
    const port = await findFreePort();
    serverUrl = `http://127.0.0.1:${port}`;

    serverProcess = spawn(
      process.execPath,
      ["--disable-warning=ExperimentalWarning", "src/server.mjs"],
      {
        cwd: projectRoot,
        env: {
          ...process.env,
          SHOUYOU_DATABASE_PATH: ":memory:",
          SHOUYOU_SERVER_PORT: String(port),
        },
        stdio: ["ignore", "pipe", "pipe"],
      },
    );

    // 等待服务器就绪。
    for (let attempt = 0; attempt < 30; attempt += 1) {
      try {
        const res = await fetch(`${serverUrl}/api/health`);
        if (res.ok) return;
      } catch {
        await new Promise((r) => setTimeout(r, 200));
      }
    }
    throw new Error("Server did not start within 6 seconds");
  });

  after(() => {
    if (serverProcess) {
      serverProcess.kill("SIGTERM");
      serverProcess = null;
    }
  });

  // ═══════════════════════════════════════════════
  // 健康检查
  // ═══════════════════════════════════════════════

  describe("GET /api/health", () => {
    it("returns ok", async () => {
      const res = await fetch(`${serverUrl}/api/health`);
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.equal(json.ok, true);
      assert.equal(json.service, "ShouyouServer");
    });
  });

  // ═══════════════════════════════════════════════
  // 玩家资料
  // ═══════════════════════════════════════════════

  describe("GET /api/v1/player/profile", () => {
    it("returns demo player data", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/player/profile?playerId=demo-player`,
      );
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.equal(json.name, "玩家名");
      assert.equal(json.level, 1);
      assert.ok(json.coins > 0);
    });

    it("returns 404 for unknown player", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/player/profile?playerId=nobody`,
      );
      assert.equal(res.status, 404);
    });
  });

  // ═══════════════════════════════════════════════
  // 角色
  // ═══════════════════════════════════════════════

  describe("GET /api/v1/characters", () => {
    it("returns at least 2 characters for demo player", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/characters?playerId=demo-player`,
      );
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.ok(json.characters.length >= 2);
      const qingzhao = json.characters.find((c) => c.id === "li-qingzhao");
      assert.ok(qingzhao);
      assert.equal(qingzhao.name, "李清照");
      assert.equal(qingzhao.rarity, "SSR");
      assert.equal(qingzhao.unlocked, true);
    });

    it("wanhe exists but is locked", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/characters?playerId=demo-player`,
      );
      const json = await res.json();
      const wanhe = json.characters.find((c) => c.id === "wanhe");
      assert.ok(wanhe);
      assert.equal(wanhe.unlocked, false);
    });
  });

  // ═══════════════════════════════════════════════
  // 编队
  // ═══════════════════════════════════════════════

  describe("GET /api/v1/formation", () => {
    it("returns 6 slots with seeded default", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/formation?playerId=demo-player`,
      );
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.equal(json.slots.length, 6);
      assert.equal(json.slots[0].slotIndex, 1);
      assert.equal(json.slots[5].slotIndex, 6);
      // 种子数据：槽位1预填李清照。
      assert.equal(json.slots[0].characterId, "li-qingzhao");
    });
  });

  describe("PUT /api/v1/formation", () => {
    it("saves and reads back valid formation", async () => {
      const slots = ["li-qingzhao", "wanhe", null, null, null, null];
      const putRes = await fetch(
        `${serverUrl}/api/v1/formation?playerId=demo-player`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ slots }),
        },
      );
      assert.equal(putRes.status, 200);

      const getRes = await fetch(
        `${serverUrl}/api/v1/formation?playerId=demo-player`,
      );
      const json = await getRes.json();
      assert.equal(json.slots[0].characterId, "li-qingzhao");
      assert.equal(json.slots[1].characterId, "wanhe");
      assert.equal(json.slots[2].characterId, null);
    });

    it("rejects duplicate characters", async () => {
      const slots = ["li-qingzhao", "li-qingzhao", null, null, null, null];
      const res = await fetch(
        `${serverUrl}/api/v1/formation?playerId=demo-player`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ slots }),
        },
      );
      assert.equal(res.status, 400);
    });

    it("rejects wrong slot count", async () => {
      const slots = ["li-qingzhao"];
      const res = await fetch(
        `${serverUrl}/api/v1/formation?playerId=demo-player`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ slots }),
        },
      );
      assert.equal(res.status, 400);
    });

    it("accepts empty formation (all null)", async () => {
      const slots = [null, null, null, null, null, null];
      const res = await fetch(
        `${serverUrl}/api/v1/formation?playerId=demo-player`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ slots }),
        },
      );
      assert.equal(res.status, 200);
      const json = await res.json();
      json.slots.forEach((s) => assert.equal(s.characterId, null));
    });
  });

  // ═══════════════════════════════════════════════
  // 关卡进度
  // ═══════════════════════════════════════════════

  describe("GET /api/v1/stages/progress", () => {
    it("returns 6 stages for chapter-1 with 1-1 and 1-2 unlocked", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/stages/progress?playerId=demo-player`,
      );
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.equal(json.stages.length, 6);
      const s1 = json.stages.find((s) => s.id === "1-1");
      const s2 = json.stages.find((s) => s.id === "1-2");
      const s3 = json.stages.find((s) => s.id === "1-3");
      assert.equal(s1.unlocked, true);
      assert.equal(s2.unlocked, true);
      // 1-3 默认不自动解锁。
      assert.equal(s3.unlocked, false);
    });
  });

  describe("PUT /api/v1/stages/complete", () => {
    it("completes stage 1-1 on first clear", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/stages/complete?playerId=demo-player`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ stageId: "1-1" }),
        },
      );
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.equal(json.progressAdvanced, true);
    });

    it("repeat clear returns progressAdvanced false", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/stages/complete?playerId=demo-player`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ stageId: "1-1" }),
        },
      );
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.equal(json.progressAdvanced, false);
    });

    it("unlocks 1-3 after clearing 1-1 and 1-2", async () => {
      await fetch(
        `${serverUrl}/api/v1/stages/complete?playerId=demo-player`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ stageId: "1-2" }),
        },
      );
      const res = await fetch(
        `${serverUrl}/api/v1/stages/progress?playerId=demo-player`,
      );
      const json = await res.json();
      const s3 = json.stages.find((s) => s.id === "1-3");
      assert.equal(s3.unlocked, true);
    });

    it("returns 404 for nonexistent stage", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/stages/complete?playerId=demo-player`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ stageId: "1-99" }),
        },
      );
      assert.equal(res.status, 404);
    });
  });

  // ═══════════════════════════════════════════════
  // 章节
  // ═══════════════════════════════════════════════

  describe("GET /api/v1/chapters", () => {
    it("returns chapter-1 with stages", async () => {
      const res = await fetch(`${serverUrl}/api/v1/chapters`);
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.equal(json.chapters.length, 1);
      assert.equal(json.chapters[0].id, "chapter-1");
      assert.equal(json.chapters[0].stages.length, 6);
    });
  });

  // ═══════════════════════════════════════════════
  // 存档
  // ═══════════════════════════════════════════════

  describe("GET|PUT /api/v1/save", () => {
    it("saves and reads back progress", async () => {
      await fetch(`${serverUrl}/api/v1/save?playerId=demo-player`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          currentChapterId: "chapter-1",
          currentStageId: "1-2",
          completedStageIds: ["1-1"],
        }),
      });

      const res = await fetch(
        `${serverUrl}/api/v1/save?playerId=demo-player`,
      );
      const json = await res.json();
      assert.equal(res.status, 200);
      assert.equal(json.currentStageId, "1-2");
      assert.deepEqual(json.completedStageIds, ["1-1"]);
    });

    it("returns 404 for player without save", async () => {
      const res = await fetch(
        `${serverUrl}/api/v1/save?playerId=nobody`,
      );
      assert.equal(res.status, 404);
    });
  });

  // ═══════════════════════════════════════════════
  // 错误处理
  // ═══════════════════════════════════════════════

  describe("404 for unknown endpoints", () => {
    it("returns 404 with error message", async () => {
      const res = await fetch(`${serverUrl}/api/v1/does-not-exist`);
      assert.equal(res.status, 404);
    });
  });
});
