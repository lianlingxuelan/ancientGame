import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { DatabaseSync } from "node:sqlite";

// 启动脚本会先进入 ShouyouServer，因此从当前工作目录定位数据目录。
const projectDirectory = resolve(process.cwd());
const databaseDirectory = resolve(projectDirectory, "data");
const configuredDatabasePath = process.env.SHOUYOU_DATABASE_PATH;
// 测试时可以传入 :memory:；正常双击启动时使用 data/shouyou.db。
const databasePath =
  configuredDatabasePath === ":memory:"
    ? ":memory:"
    : resolve(configuredDatabasePath ?? databaseDirectory, configuredDatabasePath ? "" : "shouyou.db")
        .replaceAll("\\", "/");

if (databasePath !== ":memory:") {
  mkdirSync(databaseDirectory, { recursive: true });
}

// Node.js 自带的 SQLite 数据库，不需要额外安装 MySQL 或 PostgreSQL。
export const database = new DatabaseSync(databasePath);

// 开启外键检查，避免角色、关卡和玩家之间出现无效引用。
// 本地 Demo 使用内存日志，避免某些 Windows 环境阻止 SQLite 创建临时日志文件。
database.exec("PRAGMA journal_mode = MEMORY;");
database.exec("PRAGMA foreign_keys = ON;");

export function initializeDatabase() {
  database.exec(`
    CREATE TABLE IF NOT EXISTS players (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL,
      level INTEGER NOT NULL DEFAULT 1,
      coins INTEGER NOT NULL DEFAULT 0,
      jade INTEGER NOT NULL DEFAULT 0,
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS characters (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL,
      rarity TEXT NOT NULL,
      role TEXT NOT NULL,
      word_intent TEXT NOT NULL,
      description TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS player_characters (
      player_id TEXT NOT NULL,
      character_id TEXT NOT NULL,
      level INTEGER NOT NULL DEFAULT 1,
      bond_level INTEGER NOT NULL DEFAULT 0,
      unlocked INTEGER NOT NULL DEFAULT 1,
      PRIMARY KEY (player_id, character_id),
      FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS chapters (
      id TEXT PRIMARY KEY,
      title TEXT NOT NULL,
      subtitle TEXT NOT NULL,
      sort_order INTEGER NOT NULL
    );

    CREATE TABLE IF NOT EXISTS stages (
      id TEXT PRIMARY KEY,
      chapter_id TEXT NOT NULL,
      title TEXT NOT NULL,
      recommended_level INTEGER NOT NULL,
      energy_cost INTEGER NOT NULL,
      default_unlocked INTEGER NOT NULL DEFAULT 0,
      sort_order INTEGER NOT NULL,
      FOREIGN KEY (chapter_id) REFERENCES chapters(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS formations (
      player_id TEXT NOT NULL,
      slot_index INTEGER NOT NULL,
      character_id TEXT,
      updated_at TEXT NOT NULL,
      PRIMARY KEY (player_id, slot_index),
      FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE SET NULL,
      CHECK (slot_index BETWEEN 1 AND 6)
    );

    CREATE TABLE IF NOT EXISTS player_progress (
      player_id TEXT PRIMARY KEY,
      current_chapter_id TEXT NOT NULL,
      current_stage_id TEXT NOT NULL,
      completed_stage_ids TEXT NOT NULL DEFAULT '[]',
      updated_at TEXT NOT NULL,
      FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
      FOREIGN KEY (current_chapter_id) REFERENCES chapters(id),
      FOREIGN KEY (current_stage_id) REFERENCES stages(id)
    );
  `);

  seedInitialData();
}

function seedInitialData() {
  const now = new Date().toISOString();

  database
    .prepare(`
      INSERT OR IGNORE INTO players
      (id, name, level, coins, jade, created_at, updated_at)
      VALUES (?, ?, ?, ?, ?, ?, ?)
    `)
    .run("demo-player", "玩家名", 1, 9999, 120, now, now);

  const insertCharacter = database.prepare(`
    INSERT OR IGNORE INTO characters
    (id, name, rarity, role, word_intent, description)
    VALUES (?, ?, ?, ?, ?, ?)
  `);

  insertCharacter.run(
    "li-qingzhao",
    "李清照",
    "SSR",
    "词意输出 / 群体辅助",
    "如梦令",
    "以笔墨突破闺阁边界，在现实与梦域之间寻找未竟之愿。",
  );
  insertCharacter.run(
    "wanhe",
    "婉禾",
    "SR",
    "治疗 / 协奏辅助",
    "灯下共稿",
    "李清照的好友，以协奏和羁绊增益支援队伍。",
  );

  const insertPlayerCharacter = database.prepare(`
    INSERT OR IGNORE INTO player_characters
    (player_id, character_id, level, bond_level, unlocked)
    VALUES (?, ?, ?, ?, ?)
  `);
  insertPlayerCharacter.run("demo-player", "li-qingzhao", 1, 1, 1);
  insertPlayerCharacter.run("demo-player", "wanhe", 1, 0, 0);

  database
    .prepare(`
      INSERT OR IGNORE INTO chapters
      (id, title, subtitle, sort_order)
      VALUES (?, ?, ?, ?)
    `)
    .run("chapter-1", "卷一·汴京春深", "雅集初会", 1);

  const stages = [
    ["1-1", "明水入汴京", 1, 6, 1],
    ["1-2", "雅集赴会", 1, 6, 1],
    ["1-3", "词论初临", 2, 6, 0],
    ["1-4", "风雨前夜", 2, 6, 0],
    ["1-5", "故人入梦", 2, 6, 0],
    ["1-6", "潮声再起", 2, 6, 0],
  ];
  const insertStage = database.prepare(`
    INSERT OR IGNORE INTO stages
    (id, chapter_id, title, recommended_level, energy_cost, default_unlocked, sort_order)
    VALUES (?, ?, ?, ?, ?, ?, ?)
  `);
  stages.forEach(([id, title, level, energy, unlocked], index) => {
    insertStage.run(id, "chapter-1", title, level, energy, unlocked, index + 1);
  });

  const insertFormation = database.prepare(`
    INSERT OR IGNORE INTO formations
    (player_id, slot_index, character_id, updated_at)
    VALUES (?, ?, ?, ?)
  `);
  for (let slot = 1; slot <= 6; slot += 1) {
    insertFormation.run(
      "demo-player",
      slot,
      slot === 1 ? "li-qingzhao" : null,
      now,
    );
  }

  database
    .prepare(`
      INSERT OR IGNORE INTO player_progress
      (player_id, current_chapter_id, current_stage_id, completed_stage_ids, updated_at)
      VALUES (?, ?, ?, ?, ?)
    `)
    .run("demo-player", "chapter-1", "1-1", "[]", now);
}

export function getPlayer(playerId) {
  return database
    .prepare(`
      SELECT id, name, level, coins, jade, created_at AS createdAt, updated_at AS updatedAt
      FROM players
      WHERE id = ?
    `)
    .get(playerId);
}

export function updatePlayerName(playerId, name) {
  database
    .prepare("UPDATE players SET name = ?, updated_at = ? WHERE id = ?")
    .run(name, new Date().toISOString(), playerId);
  return getPlayer(playerId);
}

export function getPlayerCharacters(playerId) {
  return database
    .prepare(`
      SELECT
        c.id,
        c.name,
        c.rarity,
        c.role,
        c.word_intent AS wordIntent,
        c.description,
        pc.level,
        pc.bond_level AS bondLevel,
        pc.unlocked
      FROM player_characters pc
      JOIN characters c ON c.id = pc.character_id
      WHERE pc.player_id = ?
      ORDER BY pc.unlocked DESC, c.name ASC
    `)
    .all(playerId)
    .map((item) => ({ ...item, unlocked: Boolean(item.unlocked) }));
}

export function getChapters() {
  const chapters = database
    .prepare(`
      SELECT id, title, subtitle, sort_order AS sortOrder
      FROM chapters
      ORDER BY sort_order
    `)
    .all();

  const findStages = database.prepare(`
    SELECT
      id,
      title,
      recommended_level AS recommendedLevel,
      energy_cost AS energyCost,
      default_unlocked AS defaultUnlocked,
      sort_order AS sortOrder
    FROM stages
    WHERE chapter_id = ?
    ORDER BY sort_order
  `);

  return chapters.map((chapter) => ({
    ...chapter,
    stages: findStages.all(chapter.id).map((stage) => ({
      ...stage,
      defaultUnlocked: Boolean(stage.defaultUnlocked),
    })),
  }));
}

export function getFormation(playerId) {
  return database
    .prepare(`
      SELECT
        f.slot_index AS slotIndex,
        f.character_id AS characterId,
        c.name AS characterName,
        c.word_intent AS wordIntent
      FROM formations f
      LEFT JOIN characters c ON c.id = f.character_id
      WHERE f.player_id = ?
      ORDER BY f.slot_index
    `)
    .all(playerId);
}

export function saveFormation(playerId, slots) {
  const updateSlot = database.prepare(`
    UPDATE formations
    SET character_id = ?, updated_at = ?
    WHERE player_id = ? AND slot_index = ?
  `);

  // Node 内置 SQLite 没有 transaction() 帮助方法，因此手动控制事务。
  database.exec("BEGIN;");
  try {
    const now = new Date().toISOString();
    slots.forEach((characterId, index) => {
      updateSlot.run(characterId || null, now, playerId, index + 1);
    });
    database.exec("COMMIT;");
  } catch (error) {
    database.exec("ROLLBACK;");
    throw error;
  }

  return getFormation(playerId);
}

export function getProgress(playerId) {
  const progress = database
    .prepare(`
      SELECT
        player_id AS playerId,
        current_chapter_id AS currentChapterId,
        current_stage_id AS currentStageId,
        completed_stage_ids AS completedStageIds,
        updated_at AS updatedAt
      FROM player_progress
      WHERE player_id = ?
    `)
    .get(playerId);

  if (!progress) {
    return undefined;
  }

  return {
    ...progress,
    completedStageIds: JSON.parse(progress.completedStageIds),
  };
}

export function saveProgress(playerId, progress) {
  database
    .prepare(`
      UPDATE player_progress
      SET current_chapter_id = ?,
          current_stage_id = ?,
          completed_stage_ids = ?,
          updated_at = ?
      WHERE player_id = ?
    `)
    .run(
      progress.currentChapterId,
      progress.currentStageId,
      JSON.stringify(progress.completedStageIds ?? []),
      new Date().toISOString(),
      playerId,
    );
  return getProgress(playerId);
}

export function closeDatabase() {
  database.close();
}
