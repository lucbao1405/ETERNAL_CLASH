#!/usr/bin/env node

const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");

function stripJsonComments(source) {
  let result = "";
  let inString = false;
  let inLineComment = false;
  let inBlockComment = false;
  let escaped = false;

  for (let index = 0; index < source.length; index += 1) {
    const current = source[index];
    const next = source[index + 1];

    if (inLineComment) {
      if (current === "\n" || current === "\r") {
        inLineComment = false;
        result += current;
      } else {
        result += " ";
      }
      continue;
    }

    if (inBlockComment) {
      if (current === "*" && next === "/") {
        result += "  ";
        index += 1;
        inBlockComment = false;
      } else {
        result += current === "\n" || current === "\r" ? current : " ";
      }
      continue;
    }

    if (inString) {
      result += current;
      if (escaped) {
        escaped = false;
      } else if (current === "\\") {
        escaped = true;
      } else if (current === '"') {
        inString = false;
      }
      continue;
    }

    if (current === '"') {
      inString = true;
      result += current;
    } else if (current === "/" && next === "/") {
      result += "  ";
      index += 1;
      inLineComment = true;
    } else if (current === "/" && next === "*") {
      result += "  ";
      index += 1;
      inBlockComment = true;
    } else {
      result += current;
    }
  }

  return result;
}

function removeTrailingCommas(source) {
  let result = "";
  let inString = false;
  let escaped = false;

  for (let index = 0; index < source.length; index += 1) {
    const current = source[index];

    if (inString) {
      result += current;
      if (escaped) {
        escaped = false;
      } else if (current === "\\") {
        escaped = true;
      } else if (current === '"') {
        inString = false;
      }
      continue;
    }

    if (current === '"') {
      inString = true;
      result += current;
      continue;
    }

    if (current === ",") {
      let lookahead = index + 1;
      while (/\s/.test(source[lookahead] ?? "")) {
        lookahead += 1;
      }
      if (source[lookahead] === "}" || source[lookahead] === "]") {
        continue;
      }
    }

    result += current;
  }

  return result;
}

function parseJsonc(source) {
  return JSON.parse(removeTrailingCommas(stripJsonComments(source)));
}

function resolveConfigPath(explicitPath) {
  if (explicitPath) {
    return path.resolve(explicitPath);
  }

  const configRoot = process.env.KILO_CONFIG_DIR
    ? path.resolve(process.env.KILO_CONFIG_DIR)
    : process.env.XDG_CONFIG_HOME
      ? path.join(process.env.XDG_CONFIG_HOME, "kilo")
      : path.join(os.homedir(), ".config", "kilo");

  for (const fileName of ["kilo.jsonc", "kilo.json"]) {
    const candidate = path.join(configRoot, fileName);
    if (fs.existsSync(candidate)) {
      return candidate;
    }
  }

  return path.join(configRoot, "kilo.jsonc");
}

function getBaseUrl(provider) {
  return (
    provider?.options?.baseURL ??
    provider?.options?.base_url ??
    provider?.baseURL ??
    provider?.base_url
  );
}

function main() {
  const providerId = process.argv[2] ?? "custom";
  const configPath = resolveConfigPath(process.argv[3] ?? process.env.KILO_CONFIG);

  if (!fs.existsSync(configPath)) {
    throw new Error(`Không tìm thấy cấu hình Kilo: ${configPath}`);
  }

  const config = parseJsonc(fs.readFileSync(configPath, "utf8"));
  const providers = config.providers ?? config.provider;
  const provider = providers?.[providerId];

  if (!provider) {
    const available = providers ? Object.keys(providers).join(", ") : "(không có)";
    throw new Error(`Không tìm thấy provider '${providerId}'. Providers hiện có: ${available}`);
  }

  const baseUrl = getBaseUrl(provider);
  if (typeof baseUrl !== "string" || baseUrl.length === 0) {
    throw new Error(`Provider '${providerId}' không có baseURL/base_url.`);
  }

  console.log(baseUrl);
}

try {
  main();
} catch (error) {
  console.error(error instanceof Error ? error.message : String(error));
  process.exitCode = 1;
}
