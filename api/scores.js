// Leaderboard API (Vercel serverless function).
//   GET  /api/scores -> { top: [{ rank, name, victory, score, clearSeconds }] }  (top 10)
//   POST /api/scores  { name, victory, animals, berries, remainingMinutes, stamina, clearSeconds }  (animals = caught, of 20)
//                    -> { rank, score, top }
// The score is recomputed here from the submitted counts (clamped to what one game
// can produce), so a client can't simply send a huge total.
// Storage is the Neon Postgres database connected to the project under Storage in the Vercel
// dashboard, which provides its connection URL as sproutfarm_DATABASE_URL (or DATABASE_URL).
// Neon's free plan only suspends an idle database and wakes it on the next query, so the
// leaderboard isn't lost when nobody plays for a while. The tables are created on first use.

const { neon } = require("@neondatabase/serverless");

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

function databaseUrl() {
  return process.env.sproutfarm_DATABASE_URL || process.env.DATABASE_URL;
}

// Tables are checked once per function instance, reused across warm invocations.
let readyPromise = null;

function getDatabase() {
  if (!readyPromise) {
    const sql = neon(databaseUrl());
    readyPromise = (async () => {
      await sql`CREATE TABLE IF NOT EXISTS sproutfarm_scores (
        id bigserial PRIMARY KEY,
        name text NOT NULL,
        victory boolean NOT NULL,
        score integer NOT NULL,
        clear_seconds integer NOT NULL DEFAULT 0,
        created_at timestamptz NOT NULL DEFAULT now()
      )`;
      await sql`CREATE INDEX IF NOT EXISTS sproutfarm_scores_rank ON sproutfarm_scores (score DESC, id)`;
      await sql`CREATE TABLE IF NOT EXISTS sproutfarm_submits (ip text PRIMARY KEY, submitted_at timestamptz NOT NULL)`;
      return sql;
    })().catch((error) => {
      readyPromise = null;
      throw error;
    });
  }
  return readyPromise;
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

// Ties go to whoever got there first
async function topEntries(sql) {
  const rows = await sql`SELECT name, victory, score, clear_seconds FROM sproutfarm_scores ORDER BY score DESC, id LIMIT ${TOP_COUNT}`;
  return rows.map((row, i) => ({ rank: i + 1, name: row.name, victory: row.victory, score: row.score, clearSeconds: row.clear_seconds }));
}

async function submit(sql, req, res) {
  const body = typeof req.body === "string" ? JSON.parse(req.body || "{}") : req.body || {};
  const name = cleanName(body.name);
  if (!name) {
    return res.status(400).json({ error: "name_required" });
  }

  // One submit per IP every SUBMIT_INTERVAL_SECONDS: the upsert only returns a row when the
  // previous submit is old enough (or there was none)
  const ip = String(req.headers["x-forwarded-for"] || "unknown").split(",")[0].trim();
  const allowed = await sql`INSERT INTO sproutfarm_submits (ip, submitted_at) VALUES (${ip}, now())
    ON CONFLICT (ip) DO UPDATE SET submitted_at = now()
    WHERE sproutfarm_submits.submitted_at < now() - make_interval(secs => ${SUBMIT_INTERVAL_SECONDS})
    RETURNING ip`;
  if (allowed.length === 0) {
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

  const [{ id }] = await sql`INSERT INTO sproutfarm_scores (name, victory, score, clear_seconds)
    VALUES (${name}, ${record.victory}, ${score}, ${clearSeconds}) RETURNING id`;
  const [{ ahead }] = await sql`SELECT count(*)::int AS ahead FROM sproutfarm_scores
    WHERE score > ${score} OR (score = ${score} AND id < ${id})`;
  return res.status(200).json({ rank: ahead + 1, score, top: await topEntries(sql) });
}

module.exports = async function handler(req, res) {
  res.setHeader("Cache-Control", "no-store");
  if (!databaseUrl()) {
    return res.status(503).json({ error: "not_configured" });
  }

  try {
    const sql = await getDatabase();
    if (req.method === "GET") {
      return res.status(200).json({ top: await topEntries(sql) });
    }
    if (req.method === "POST") {
      return await submit(sql, req, res);
    }
    res.setHeader("Allow", "GET, POST");
    return res.status(405).json({ error: "method_not_allowed" });
  } catch (error) {
    console.error(error);
    return res.status(502).json({ error: "storage_error" });
  }
};
