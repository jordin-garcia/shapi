#!/usr/bin/env node
// Verifica que el equipo tenga todas las herramientas del proyecto instaladas.
// Uso: node scripts/verificar-entorno.mjs
import { execSync } from "node:child_process";

const checks = [
  { nombre: "Git", cmd: "git --version", min: null },
  { nombre: "GitHub CLI (gh)", cmd: "gh --version", min: null },
  { nombre: "Sesión de GitHub (gh auth)", cmd: "gh auth status", min: null },
  { nombre: "Docker", cmd: "docker version --format {{.Server.Version}}", min: null, ayuda: "¿Está abierto Docker Desktop (o el servicio docker en Linux)?" },
  { nombre: "Docker Compose", cmd: "docker compose version --short", min: null },
  { nombre: ".NET SDK 10", cmd: "dotnet --version", min: 10 },
  { nombre: "dotnet-ef", cmd: "dotnet ef --version", min: 10, ayuda: "dotnet tool install --global dotnet-ef" },
  { nombre: "Node.js 24", cmd: "node --version", min: 24 },
  { nombre: "pnpm", cmd: "pnpm --version", min: 10, ayuda: "npm install -g pnpm@latest" },
];

let fallas = 0;
for (const c of checks) {
  try {
    const out = execSync(c.cmd, { stdio: ["ignore", "pipe", "pipe"] }).toString().trim().split(/\r?\n/).pop();
    let ok = true;
    if (c.min) {
      const m = out.match(/(\d+)\./);
      ok = m && Number(m[1]) >= c.min;
    }
    console.log(`${ok ? "✅" : "⚠️ "} ${c.nombre.padEnd(28)} ${out}${ok ? "" : `  (se requiere ${c.min} o superior)`}`);
    if (!ok) fallas++;
  } catch {
    fallas++;
    console.log(`❌ ${c.nombre.padEnd(28)} NO ENCONTRADO${c.ayuda ? "  → " + c.ayuda : ""}`);
  }
}
try {
  const autocrlf = execSync("git config --get core.autocrlf", { stdio: ["ignore", "pipe", "ignore"] }).toString().trim();
  if (autocrlf && autocrlf !== "false") console.log(`⚠️  git core.autocrlf=${autocrlf} → recomendado: git config --global core.autocrlf false`);
} catch { /* sin configurar: correcto */ }

console.log(fallas ? `\nFaltan ${fallas} herramientas. Vea docs/plan/instalacion.md.` : "\nTodo listo para trabajar en Shapi.");
process.exit(fallas ? 1 : 0);
