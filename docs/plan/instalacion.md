# Instalación: lo que cada integrante hace una sola vez

Tiempo estimado: entre 45 y 60 minutos. Sigue los pasos en orden. Donde hay dos columnas, usa la de tu sistema: **Windows** (Jordin y José Pablo) o **Linux** (Emilio y Dominique).

> En Linux, los comandos son para Ubuntu o Debian. Si usan otra distribución (Fedora, Arch…), instalen lo mismo desde la página oficial que se indica en cada paso.

---

## 1. Cuentas

- [ ] **GitHub:** acepta la invitación al repositorio `jordin-garcia/shapi`, que llega por correo o aparece en https://github.com/notifications. **Emilio (MiloDou):** Jordin te agregará en JG-01; acepta la invitación en cuanto te llegue.
- [ ] **Suscripción de tu agente de IA:** Claude (Pro o superior), ChatGPT (para Codex), una cuenta de Google (para Antigravity o Gemini) u otra. Cualquiera sirve. Jordin usa Claude Pro.

## 2. Herramientas base

| Herramienta | Windows (PowerShell) | Linux (Ubuntu o Debian) |
|---|---|---|
| **Git** | `winget install --id Git.Git -e` | `sudo apt update && sudo apt install -y git` |
| **GitHub CLI** | `winget install --id GitHub.cli -e` | Sigue https://github.com/cli/cli/blob/trunk/docs/install_linux.md (repositorio apt oficial) y luego `sudo apt install gh` |
| **Docker** | Primero `wsl --install` (y reinicia si lo pide). Luego `winget install --id Docker.DockerDesktop -e`, ábrelo y deja que termine de iniciar | Docker Desktop (https://docs.docker.com/desktop/setup/install/linux/) **o** Docker Engine con el complemento compose (https://docs.docker.com/engine/install/). Después `sudo usermod -aG docker $USER`, y cierra sesión y vuelve a entrar |
| **.NET 10 SDK** | `winget install --id Microsoft.DotNet.SDK.10 -e` | `sudo apt install -y dotnet-sdk-10.0` (si tu versión no lo tiene: https://learn.microsoft.com/dotnet/core/install/linux) |
| **dotnet-ef** | `dotnet tool install --global dotnet-ef` | `dotnet tool install --global dotnet-ef` (y agrega `~/.dotnet/tools` a tu `PATH`) |
| **Node.js 24 LTS** | `winget install --id OpenJS.NodeJS.LTS -e` | Con nvm (https://github.com/nvm-sh/nvm): `nvm install 24 && nvm alias default 24` |
| **pnpm** (la versión fijada en `frontend/package.json`) | `npm install -g pnpm@latest` | `npm install -g pnpm@latest` |
| **Editor (opcional)** | VS Code: `winget install --id Microsoft.VisualStudioCode -e` | https://code.visualstudio.com |

**Después de instalar, cierra y vuelve a abrir la terminal** para que se actualice el `PATH`.

## 3. Configurar Git y GitHub

```bash
git config --global user.name "Tu Nombre Completo"
git config --global user.email "el-correo-de-tu-cuenta-de-github"
git config --global core.autocrlf false
git config --global init.defaultBranch main
git config --global pull.rebase true
gh auth login          # elige GitHub.com → HTTPS → Login with a web browser
gh auth setup-git
```
Solo en Windows: `git config --global core.longpaths true`.

## 4. Clonar el repositorio y verificar el equipo

```bash
cd <la carpeta donde guardas tus proyectos>
gh repo clone jordin-garcia/shapi
cd shapi
node scripts/verificar-entorno.mjs
```
Todo debe aparecer con ✅. Si algo falla, el mismo script dice qué instalar.

## 5. Levantar el entorno de desarrollo

> Esta sección funciona **a medida que se terminan** JZ-01 (infraestructura), JG-01 (backend), DC-01 (frontend) y JZ-05 (siembra). Antes de eso, no hay nada que levantar. Los comandos definitivos quedan en `docs/manual-tecnico.md`.

```bash
cp .env.example .env                                  # PowerShell: Copy-Item .env.example .env
docker compose -f infra/compose.yml up -d             # PostgreSQL, Redis, Mailpit, Caddy y los orígenes de demostración
dotnet run --project src/Shapi.Api                    # terminal 1: API de control (aplica migraciones al iniciar en desarrollo)
dotnet run --project src/Shapi.Compuerta              # terminal 2: compuerta
dotnet run --project src/Shapi.Trabajador             # terminal 3: trabajador
cd frontend && pnpm install && pnpm dev               # terminal 4: panel y portal
```
Luego abre https://shapi.localhost. El correo simulado se ve en https://correo.shapi.localhost.

### 5.1 Confiar en el certificado local (una sola vez, después de JZ-01)

Caddy firma los certificados con su propia autoridad. Para que el navegador no muestre advertencias:

**Windows (PowerShell):**
```powershell
docker compose -f infra/compose.yml cp borde:/data/caddy/pki/authorities/local/root.crt .\caddy-root.crt
Import-Certificate -FilePath .\caddy-root.crt -CertStoreLocation Cert:\CurrentUser\Root
Remove-Item .\caddy-root.crt
```
Chrome y Edge lo toman de inmediato. En Firefox, activa `security.enterprise_roots.enabled` en `about:config`.

**Linux:**
```bash
docker compose -f infra/compose.yml cp borde:/data/caddy/pki/authorities/local/root.crt ./caddy-root.crt
sudo cp caddy-root.crt /usr/local/share/ca-certificates/caddy-shapi.crt && sudo update-ca-certificates
sudo apt install -y libnss3-tools && certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n caddy-shapi -i caddy-root.crt   # Chrome y Chromium
rm caddy-root.crt
```
En Firefox: Ajustes → Privacidad y seguridad → Certificados → Ver certificados → Autoridades → Importar.

---

## 6. Instalar y configurar tu agente de IA

Elige **uno**. Todos leen automáticamente las reglas del proyecto (`AGENTS.md`), así que no hay que copiar nada.

### Opción A: Claude Code (recomendado para Jordin)
1. Instalar:
   - Windows: `irm https://claude.ai/install.ps1 | iex`
   - Linux: `curl -fsSL https://claude.ai/install.sh | bash`
2. Dentro de la carpeta `shapi`, ejecuta `claude` e inicia sesión con tu cuenta.
3. El proyecto ya trae `CLAUDE.md`, que importa `AGENTS.md`, los permisos en `.claude/settings.json` y el subagente `revisor`.
4. Modo de trabajo: usa el **modo auto** (`Shift+Tab` hasta que la barra diga *auto mode*) para que no pida permiso en cada comando.
5. **Recomendado:** navegador para comparar pantallas: `claude mcp add playwright -- npx -y @playwright/mcp@latest`.

### Opción B: Codex (OpenAI)
1. Instalar: `npm install -g @openai/codex`. En Windows también existe el instalador oficial de https://github.com/openai/codex.
2. Dentro de `shapi`, ejecuta `codex` e inicia sesión con tu cuenta de ChatGPT.
3. Codex lee `AGENTS.md` automáticamente.
4. Modo de trabajo: configura las aprobaciones para que pueda **editar archivos y ejecutar comandos en la carpeta del proyecto sin preguntar** (comando `/approvals` dentro de Codex, o el equivalente que muestre `codex --help`). También necesita acceso a la red, para `gh`, `pnpm` y `dotnet`.
5. **Recomendado:** navegador. Agrega esto a `~/.codex/config.toml`:
   ```toml
   [mcp_servers.playwright]
   command = "npx"
   args = ["-y", "@playwright/mcp@latest"]
   ```

### Opción C: Google Antigravity
1. Descarga el instalador de https://antigravity.google, instálalo e inicia sesión con tu cuenta de Google.
2. Abre la carpeta `shapi` (File → Open Folder). Antigravity lee `AGENTS.md` automáticamente (versión 1.20.3 o posterior).
3. Modo de trabajo: en la configuración del agente, permite que **ejecute comandos de terminal sin pedir confirmación** en este proyecto, para que complete la tarea sin detenerse.
4. **Recomendado:** agrega el servidor MCP de Playwright desde la tienda de MCP de Antigravity.

### Opción D: Gemini CLI
1. `npm install -g @google/gemini-cli`, y luego `gemini` dentro de `shapi` para iniciar sesión.
2. El proyecto trae `.gemini/settings.json`, que le indica leer `AGENTS.md`.

### Opción E: GitHub Copilot (modo agente de VS Code) o Cursor
- Los dos leen `AGENTS.md`. Activa el **modo agente** y la aprobación automática de comandos de terminal para este espacio de trabajo.

### Comprobación final de tu agente
Dentro de `shapi`, escríbele a tu agente:
> Soy <tu nombre>. Revisa el plan y dime qué tareas me tocan.

Debería ejecutar `node scripts/tareas.mjs --persona <tu-clave>` y mostrarte tus tareas. Si lo hace, ya está todo listo.

---

## 7. Tareas únicas de Jordin (coordinador)

Estas las resuelve tu agente dentro de JG-01 y JG-03, pero hay pasos que requieren a una persona:
- [ ] Tener `gh` autenticado con tu cuenta, que es la administradora del repositorio. JG-01 lo usa para invitar a MiloDou, activar el auto-merge y proteger `main`.
- [ ] Para la revisión automática con Claude en GitHub (JG-03): ejecutar `claude setup-token` en tu terminal y pegarle a tu agente el token que te muestre, o guardarlo tú mismo con `gh secret set CLAUDE_CODE_OAUTH_TOKEN`. **Solo hace falta tu token**: sirve para todos los PR del equipo, y el consumo se descuenta de tu plan Pro.
