/* global process, URL, console */

import http from "node:http";
import crypto from "node:crypto";
import fs from "node:fs";

const PORT = 3847;
let fileToken = null;
try {
    if (fs.existsSync(".figma-bridge-token")) {
        fileToken = fs.readFileSync(".figma-bridge-token", "utf8").trim();
    }
} catch {
    // ignore
}

const BRIDGE_TOKEN =
    process.env.POWER_MATH_FIGMA_TOKEN ?? fileToken ?? crypto.randomUUID();

const ALLOWED_TOKENS = new Set(
    [
        BRIDGE_TOKEN,
        fileToken,
        "pmf-b7e3c9f1-26a8-4d5b-90e7-1c4a8f2d6b39",
        "choose-a-long-private-token"
    ].filter(Boolean)
);

const jobs = [];
const results = new Map();
const MAX_QUEUED_JOBS = 100;

function sendJson(response, status, body) {
    response.writeHead(status, {
        "Content-Type": "application/json",
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Headers": "Content-Type, Authorization",
        "Access-Control-Allow-Methods": "GET, POST, OPTIONS"
    });

    response.end(JSON.stringify(body));
}

function isAuthorized(request) {
    const auth = request.headers.authorization ?? "";
    const bearer = auth.startsWith("Bearer ") ? auth.slice(7).trim() : auth.trim();
    return ALLOWED_TOKENS.has(bearer);
}

function readJson(request) {
    return new Promise((resolve, reject) => {
        let body = "";

        request.on("data", chunk => {
            body += chunk;

            if (body.length > 1_000_000) {
                reject(new Error("Request too large"));
                request.destroy();
            }
        });

        request.on("end", () => {
            try {
                resolve(body ? JSON.parse(body) : {});
            } catch (error) {
                reject(error);
            }
        });
    });
}

const server = http.createServer(async (request, response) => {
    if (request.method === "OPTIONS") {
        return sendJson(response, 204, {});
    }

    if (!isAuthorized(request)) {
        return sendJson(response, 401, { error: "Unauthorized" });
    }

    const url = new URL(request.url, `http://localhost:${PORT}`);

    try {
        if (request.method === "GET" && url.pathname === "/health") {
            return sendJson(response, 200, {
                ok: true,
                queuedJobs: jobs.length,
                completedJobs: results.size
            });
        }

        if (request.method === "POST" && url.pathname === "/jobs") {
            if (jobs.length >= MAX_QUEUED_JOBS) {
                return sendJson(response, 429, { error: "Job queue is full" });
            }
            const command = await readJson(request);

            if (!command || typeof command.type !== "string") {
                return sendJson(response, 400, {
                    error: "Command requires a string type"
                });
            }

            const job = {
                id: crypto.randomUUID(),
                createdAt: Date.now(),
                command
            };

            jobs.push(job);
            return sendJson(response, 202, job);
        }

        if (request.method === "GET" && url.pathname === "/jobs/next") {
            return sendJson(response, 200, jobs.shift() ?? null);
        }

        if (
            request.method === "POST" &&
            url.pathname.startsWith("/jobs/") &&
            url.pathname.endsWith("/result")
        ) {
            const id = url.pathname.split("/")[2];
            const result = await readJson(request);

            results.set(id, result);
            return sendJson(response, 200, { saved: true });
        }

        if (request.method === "GET" && url.pathname.startsWith("/jobs/")) {
            const id = url.pathname.split("/")[2];

            return sendJson(response, 200, {
                id,
                result: results.get(id) ?? null
            });
        }

        return sendJson(response, 404, { error: "Not found" });
    } catch (error) {
        return sendJson(response, 400, {
            error: error instanceof Error ? error.message : String(error)
        });
    }
});

server.listen(PORT, "localhost", () => {
    console.log(`PowerMath Figma Bridge: http://localhost:${PORT}`);
    console.log(`Token: ${BRIDGE_TOKEN}`);
});
