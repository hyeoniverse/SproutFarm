// Leaderboard API (Vercel serverless function).
//   GET  /api/scores -> { top: [{ rank, name, victory, score }] }  (top 10)
//   POST /api/scores  { name, victory, animals, berries, remainingMinutes, stamina }
//                    -> { rank, score, top }
// The score is recomputed here from the submitted counts (clamped to what one game
// can produce), so a client can't simply send a huge total.
// Storage is Upstash Redis: connect it to the project under Storage in the Vercel
// dashboard, which adds the KV_REST_API_* (or UPSTASH_REDIS_REST_*) variables.

const { randomUUID } = require("crypto");

const LEADERBOARD_KEY = "sproutfarm:leaderboard";
const TOP_COUNT = 10;
const NAME_MAX_LENGTH = 10;
const SUBMIT_INTERVAL_SECONDS = 10;

// Same formula as Assets/Scripts/GameResult.cs in the Unity project.
const POINTS = { animal: 100, berry: 20, clearBonus: 1000, remainingMinute: 2, stamina: 5 };
// Most one game can produce: 20 animals, a berry every 15s over an 18-minute day,
// and 9:00 to midnight left on the clock. Update these if the game changes.
const LIMITS = { animals: 20, berries: 100, remainingMinutes: 15 * 60, stamina: 100 };

function redisConfig() {
  const url = process.env.KV_REST_API_URL || process.env.UPSTASH_REDIS_REST_URL;
  const token = process.env.KV_REST_API_TOKEN || process.env.UPSTASH_REDIS_REST_TOKEN;
  return url && token ? { url, token } : null;
}

async function redis(config, command) {
  const response = await fetch(config.url, {
    method: "POST",
    headers: { Authorization: `Bearer ${config.token}` },
    body: JSON.stringify(command),
  });
  const body = await response.json();
  if (!response.ok || body.error) {
    throw new Error(body.error || `Redis request failed with ${response.status}`);
  }
  return body.result;
}

function clampCount(value, max) {
  const number = Math.floor(Number(value));
  return Number.isFinite(number) ? Math.min(Math.max(number, 0), max) : 0;
}

function scoreOf(record) {
  let score = record.animals * POINTS.animal + record.berries * POINTS.berry;
  if (record.victory) {
    score += POINTS.clearBonus + record.remainingMinutes * POINTS.remainingMinute + record.stamina * POINTS.stamina;
  }
  return score;
}

function cleanName(name) {
  return Array.from(String(name ?? "").replace(/[\u0000-\u001f\u007f]/g, "").trim())
    .slice(0, NAME_MAX_LENGTH)
    .join("");
}

async function topEntries(config) {
  const flat = await redis(config, ["ZREVRANGE", LEADERBOARD_KEY, 0, TOP_COUNT - 1, "WITHSCORES"]);
  const entries = [];
  for (let i = 0; i < flat.length; i += 2) {
    const entry = JSON.parse(flat[i]);
    entries.push({ rank: i / 2 + 1, name: entry.name, victory: entry.victory, score: Number(flat[i + 1]) });
  }
  return entries;
}

async function submit(config, req, res) {
  const body = typeof req.body === "string" ? JSON.parse(req.body || "{}") : req.body || {};
  const name = cleanName(body.name);
  if (!name) {
    return res.status(400).json({ error: "name_required" });
  }

  const ip = String(req.headers["x-forwarded-for"] || "unknown").split(",")[0].trim();
  const allowed = await redis(config, ["SET", `sproutfarm:ratelimit:${ip}`, "1", "NX", "EX", SUBMIT_INTERVAL_SECONDS]);
  if (allowed !== "OK") {
    return res.status(429).json({ error: "too_many_requests" });
  }

  const record = {
    victory: body.victory === true,
    animals: clampCount(body.animals, LIMITS.animals),
    berries: clampCount(body.berries, LIMITS.berries),
    remainingMinutes: clampCount(body.remainingMinutes, LIMITS.remainingMinutes),
    stamina: clampCount(body.stamina, LIMITS.stamina),
  };
  const score = scoreOf(record);
  const member = JSON.stringify({ id: randomUUID(), name, victory: record.victory, at: new Date().toISOString() });

  await redis(config, ["ZADD", LEADERBOARD_KEY, score, member]);
  const rank = (await redis(config, ["ZREVRANK", LEADERBOARD_KEY, member])) + 1;
  return res.status(200).json({ rank, score, top: await topEntries(config) });
}

module.exports = async function handler(req, res) {
  res.setHeader("Cache-Control", "no-store");
  const config = redisConfig();
  if (!config) {
    return res.status(503).json({ error: "not_configured" });
  }

  try {
    if (req.method === "GET") {
      return res.status(200).json({ top: await topEntries(config) });
    }
    if (req.method === "POST") {
      return await submit(config, req, res);
    }
    res.setHeader("Allow", "GET, POST");
    return res.status(405).json({ error: "method_not_allowed" });
  } catch (error) {
    console.error(error);
    return res.status(502).json({ error: "storage_error" });
  }
};
