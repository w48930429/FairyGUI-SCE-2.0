#!/usr/bin/env node
"use strict";

const { spawnSync } = require("node:child_process");

const result = spawnSync("ospec", ["index", "build", "."], {
  stdio: "inherit",
  shell: process.platform === "win32",
});

if (result.error) {
  console.error(result.error.message);
  process.exit(1);
}

process.exit(result.status ?? 1);
