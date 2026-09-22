// Leaderboard API (Vercel serverless function).
//   GET  /api/scores -> { top: [{ rank, name, victory, score, clearSeconds }] }  (top 10)
//   POST /api/scores  { name, victory, animals, berries, remainingMinutes, stamina, clearSeconds }  (animals = caught, of 20)
//                    -> { rank, score, top }
// The score is recomputed here from the submitted counts (clamped to what one game
// can produce), so a client can't simply send a huge total.
// Storage is the Redis database connected to the project under Storage in the Vercel
// dashboard, which provides its connection URL as sproutfarm_REDIS_URL.

const { randomUUID } = require("crypto");
const { createClient } = require("redis");

const LEADERBOARD_KEY = "sproutfarm:leaderboard";
const TOP_COUNT = 10;
const NAME_MAX_LENGTH = 10;
const SUBMIT_INTERVAL_SECONDS = 10;

// Same formula as GameResult.Calculate in the Unity project (Assets/Scripts/GameResult.cs).
const POINTS = { animal: 100, berry: 20, clearBonus: 1000, remainingMinute: 2, stamina: 5 };
// Animals in one game; remaining-time points are scaled by the share of them caught.
const TOTAL_ANIMALS = 20;
// Most one game can produce: every animal, a berry every 15s over an 18-minute day,
// and 9:00 to midnight left on the clock. Update these if the game changes.
const LIMITS = { animals: TOTAL_ANIMALS, berries: 100, remainingMinutes: 15 * 60, stamina: 100, clearSeconds: 60 * 60 };

function redisUrl() {
  return process.env.sproutfarm_REDIS_URL || process.env.REDIS_URL;
}

// One connection per function instance, reused across warm invocations.
let clientPromise = null;

function getClient() {
  if (!clientPromise) {
    const client = createClient({ url: redisUrl() });
    client.on("error", (error) => console.error("Redis client error", error));
    clientPromise = client.connect().then(() => client).catch((error) => {
      clientPromise = null;
      throw error;
    });
  }
  return clientPromise;
}

async function redis(client, command) {
  return client.sendCommand(command.map(String));
}

function clampCount(value, max) {
  const number = Math.floor(Number(value));
  return Number.isFinite(number) ? Math.min(Math.max(number, 0), max) : 0;
}

// Remaining time only counts for the share of animals caught, so giving up early
// doesn't earn more time points than playing on.
function scoreOf(record) {
  const timePoints = Math.floor((record.remainingMinutes * POINTS.remainingMinute * record.animals) / TOTAL_ANIMALS);
  return (
    record.animals * POINTS.animal +
    record.berries * POINTS.berry +
    timePoints +
    record.stamina * POINTS.stamina +
    (record.victory ? POINTS.clearBonus : 0)
  );
}

function cleanName(name) {
  return Array.from(String(name ?? "").replace(/[\u0000-\u001f\u007f]/g, "").trim())
    .slice(0, NAME_MAX_LENGTH)
    .join("");
}

async function topEntries(client) {
  const flat = await redis(client, ["ZREVRANGE", LEADERBOARD_KEY, 0, TOP_COUNT - 1, "WITHSCORES"]);
  const entries = [];
  for (let i = 0; i < flat.length; i += 2) {
    const entry = JSON.parse(flat[i]);
    entries.push({ rank: i / 2 + 1, name: entry.name, victory: entry.victory, score: Number(flat[i + 1]), clearSeconds: entry.clearSeconds || 0 });
  }
  return entries;
}

async function submit(client, req, res) {
  const body = typeof req.body === "string" ? JSON.parse(req.body || "{}") : req.body || {};
  const name = cleanName(body.name);
  if (!name) {
    return res.status(400).json({ error: "name_required" });
  }

  const ip = String(req.headers["x-forwarded-for"] || "unknown").split(",")[0].trim();
  const allowed = await redis(client, ["SET", `sproutfarm:ratelimit:${ip}`, "1", "NX", "EX", SUBMIT_INTERVAL_SECONDS]);
  if (allowed !== "OK") {
    return res.status(429).json({ error: "too_many_requests" });
  }

  const animals = clampCount(body.animals, LIMITS.animals);
  const record = {
    victory: body.victory === true && animals === TOTAL_ANIMALS,
    animals,
    berries: clampCount(body.berries, LIMITS.berries),
    remainingMinutes: clampCount(body.remainingMinutes, LIMITS.remainingMinutes),
    stamina: clampCount(body.stamina, LIMITS.stamina),
  };
  const score = scoreOf(record);
  // Time taken to clear is shown on the board only; it doesn't affect the score
  const clearSeconds = record.victory ? clampCount(body.clearSeconds, LIMITS.clearSeconds) : 0;
  const member = JSON.stringify({ id: randomUUID(), name, victory: record.victory, clearSeconds, at: new Date().toISOString() });

  await redis(client, ["ZADD", LEADERBOARD_KEY, score, member]);
  const rank = (await redis(client, ["ZREVRANK", LEADERBOARD_KEY, member])) + 1;
  return res.status(200).json({ rank, score, top: await topEntries(client) });
}

module.exports = async function handler(req, res) {
  res.setHeader("Cache-Control", "no-store");
  if (!redisUrl()) {
    return res.status(503).json({ error: "not_configured" });
  }

  try {
    const client = await getClient();
    if (req.method === "GET") {
      return res.status(200).json({ top: await topEntries(client) });
    }
    if (req.method === "POST") {
      return await submit(client, req, res);
    }
    res.setHeader("Allow", "GET, POST");
    return res.status(405).json({ error: "method_not_allowed" });
  } catch (error) {
    console.error(error);
    return res.status(502).json({ error: "storage_error" });
  }
};
