## Ticket 1: Dockerfile für .NET 8 Discord-Bot erstellen

**Titel:** Dockerfile für .NET 8 Discord-Bot erstellen (Release-Build & Slim-Image)

**Beschreibung:**

Erstelle ein `Dockerfile`, das den Discord-Bot (GlobalStatsBot) als .NET 8 Console-App baut und als schlankes Runtime-Image bereitstellt. Das Image soll auf ARM64 laufen (Pi 5).

**Akzeptanzkriterien:**

- Dockerfile baut den Bot im `Release`-Modus.
- Ergebnis-Image basiert auf `mcr.microsoft.com/dotnet/runtime:8.0` oder `aspnet` (falls später Web dabei).
- Image läuft auf ARM64 (Pi 5, PiOS Lite 64-bit).
- Bot startet über `dotnet /app/GlobalStatsBot.dll` o.ä.
- Bot-Token und DB-Connectionstring werden per Environment-Variablen konfiguriert.

**Aufgaben:**

1. Im Projektroot ein `Dockerfile` erstellen.
2. Multi-Stage-Build:
   - Stage `build`:
     - Base: `mcr.microsoft.com/dotnet/sdk:8.0`
     - Projektdateien kopieren.
     - `dotnet restore`
     - `dotnet publish -c Release -o /app`
   - Stage `runtime`:
     - Base: `mcr.microsoft.com/dotnet/runtime:8.0`
     - Verzeichnis `/app` erstellen.
     - Publish-Output aus `build`-Stage nach `/app` kopieren.
     - `ENTRYPOINT ["dotnet", "GlobalStatsBot.dll"]` (ggf. Name anpassen).
3. Konfiguration:
   - Bot liest Token & Connectionstring aus Env-Variablen (z.B. `DISCORD_TOKEN`, `ConnectionStrings__DefaultConnection`).
   - Sicherstellen, dass `appsettings.json` optional ist und durch Environment überschrieben werden kann.
4. Test:
   - Lokal auf x64 bauen & starten: `docker build -t globalstatsbot .` und `docker run --rm -e DISCORD_TOKEN=... globalstatsbot`.

---

## Ticket 2: Docker-Compose-Datei für Bot + MariaDB erstellen

**Titel:** `docker-compose.yml` für Bot + MariaDB erstellen

**Beschreibung:**

Erstelle eine `docker-compose.yml`, um den Discord-Bot und eine MariaDB-Instanz gemeinsam zu starten. Später auf dem Raspberry Pi einsetzbar.

**Akzeptanzkriterien:**

- Service `db` (MariaDB) mit:
  - Volumes für persistente Daten.
  - Initialer DB-User/Pass und DB-Name.
- Service `bot`:
  - Abhängig von `db`.
  - Liest Token & Connectionstring aus Environment-Variablen.
  - Kann via `docker compose up -d` gestartet werden.
- Netzwerk: Standard-Bridge reicht; Bot muss `db` über Hostnamen `db` erreichen.

**Aufgaben:**

1. Datei `docker-compose.yml` anlegen.
2. Service `db` definieren:
   - Image: `mariadb:10.11` (oder passende Version).
   - Environment:
     - `MYSQL_ROOT_PASSWORD`
     - `MYSQL_DATABASE=discord_identity`
     - `MYSQL_USER=botuser`
     - `MYSQL_PASSWORD=starkespasswort`
   - Volumes:
     - `./data/mariadb:/var/lib/mysql`
   - Ports (optional, für lokale DB-Tools): `3306:3306`.
3. Service `bot` definieren:
   - Build: `.` (verwendet dein `Dockerfile`).
   - Environment:
     - `DISCORD_TOKEN=...`
     - `ConnectionStrings__DefaultConnection=Server=db;Port=3306;Database=discord_identity;User=botuser;Password=starkespasswort;SslMode=Preferred;TreatTinyAsBoolean=true;`
   - `depends_on: [db]`.
   - `restart: unless-stopped`.
4. Test mit `docker compose up --build` auf deinem Entwicklungsrechner.
5. Dokumentieren, wie das gleiche Setup auf dem Pi verwendet wird (siehe nächstes Ticket).

---

## Ticket 3: Projekt für Docker-Betrieb vorbereiten (Config über Env-Vars)

**Titel:** Konfiguration für Docker-Betrieb anpassen (Environment-Variablen)

**Beschreibung:**

Passe das Projekt so an, dass es sich in Docker gut konfigurieren lässt. Keine Secrets in Dateien, stattdessen Environment-Variablen und/oder `appsettings.Docker.json`.

**Akzeptanzkriterien:**

- Bot-Token wird nicht in `appsettings.json` gespeichert.
- Bot-Token & Connectionstring können über Environment-Variablen gesetzt werden.
- Logging ist konsolenfreundlich (stdout), sodass `docker logs` alles anzeigt.

**Aufgaben:**

1. `appsettings.json`:
   - Optional: Platzhalter für ConnectionStrings, aber ohne echte Passwörter.
2. In `Program.cs`:
   - `Host.CreateDefaultBuilder` nutzen (liest automatisch Env-Vars, JSON, etc.).
   - `configuration.GetConnectionString("DefaultConnection")` verwenden.
3. Bot-Token:
   - Token aus `IConfiguration` lesen, z.B. `configuration["Discord:Token"]` oder einfach `Environment.GetEnvironmentVariable("DISCORD_TOKEN")`.
   - Falls Token fehlt → klare Fehlermeldung ins Log, Bot startet nicht.
4. Logging:
   - Standard-Console-Logger nutzen.
   - Optional Log-Level reduzieren (`Information` oder `Warning`) für weniger Spam im Container.

---

## Ticket 4: Raspberry Pi 5 (PiOS Lite 64‑bit) für Docker vorbereiten

**Titel:** Raspberry Pi 5 für Docker-Betrieb vorbereiten (PiOS Lite 64-bit)

**Beschreibung:**

Bereite den Raspberry Pi 5 mit PiOS Lite 64‑bit so vor, dass Docker und docker-compose ausgeführt werden können.

**Akzeptanzkriterien:**

- Docker Engine ist installiert und lauffähig (`docker run hello-world` funktioniert).
- `docker compose` (Plugin oder `docker-compose`) ist verfügbar.
- Der Standard-User (z.B. `pi`) kann Docker ohne `sudo` nutzen (Gruppe `docker`).

**Aufgaben:**

1. PiOS Lite 64‑bit installieren und einrichten (SSH, Updates).
2. Docker installieren, z.B. via offizielles Script oder Paketmanager:
   - `curl -sSL https://get.docker.com | sh` (oder Anleitung von [docs.docker.com](https://docs.docker.com)).
3. User zur Gruppe `docker` hinzufügen:
   - `sudo usermod -aG docker $USER`.
   - Neu einloggen.
4. `docker compose` installieren:
   - Entweder Docker Compose Plugin (aktuelle Docker-Versionen) oder `docker-compose` Binary.
   - Test: `docker compose version`.
5. Test-Container laufen lassen:
   - `docker run --rm arm64v8/alpine echo "Hello from Pi"`.

---

## Ticket 5: Build & Deployment-Workflow für Raspberry Pi definieren

**Titel:** Build- & Deployment-Prozess für Raspberry Pi Container definieren

**Beschreibung:**

Lege fest, wie das Docker-Image für den Bot auf deinen Raspberry Pi kommt und dort gestartet wird (lokaler Build vs. Registry).

**Akzeptanzkriterien:**

- Dokumentierter Weg:
  - Entweder direkt auf dem Pi bauen (`docker compose build`),
  - oder auf Entwicklungsrechner bauen & in Registry (Docker Hub / GitHub Container Registry) pushen.
- Klarer Befehl zum Starten/Updaten:
  - `docker compose pull && docker compose up -d` o.ä.

**Aufgaben:**

1. Variante definieren:
   - A: Build direkt auf dem Pi (einfach, aber langsam).
   - B: Build auf Dev-Maschine, Push zu Registry, Pull auf Pi (empfohlen).
2. Für Variante B:
   - Image-Tag festlegen, z.B. `zyberphil/globalstatsbot:latest`.
   - Anleitung:
     - `docker build -t zyberphil/globalstatsbot:latest .`
     - `docker push zyberphil/globalstatsbot:latest`
   - Auf dem Pi:
     - `docker pull zyberphil/globalstatsbot:latest`
     - `docker compose up -d`.
3. `docker-compose.yml` ggf. so anpassen, dass `image:` verwendet wird statt `build:` (für den Pi).

---

## Ticket 6: Systemdienst / Autostart für Bot-Stack auf dem Pi

**Titel:** Autostart für Docker-Stack auf Raspberry Pi konfigurieren

**Beschreibung:**

Sorge dafür, dass der Docker-Stack (Bot + MariaDB) nach einem Reboot des Raspberry Pi automatisch wieder gestartet wird.

**Akzeptanzkriterien:**

- Nach einem Neustart des Pis läuft `docker ps` und zeigt `bot` + `db`-Container.
- Kein manueller Eingriff nötig.

**Aufgaben:**

1. In `docker-compose.yml` für beide Services `restart: unless-stopped` setzen (falls noch nicht geschehen).
2. Prüfen, dass Docker beim Systemstart automatisch startet (Systemd-Service `docker` aktiv).
3. Optional: Systemd-Unit erstellen, die bei Boot `docker compose up -d` im Projektverzeichnis ausführt (falls du nicht auf `restart` allein vertrauen möchtest).
4. Reboot-Test:
   - `sudo reboot`.
   - Nach Neustart: `docker ps` prüfen.
   - Logs ansehen: `docker logs <bot-container>`.

---

## Ticket 7: Health-Check & Logging-Überwachung

**Titel:** Health-Check & Logging für Docker-Bot auf dem Pi einrichten

**Beschreibung:**

Richte einfache Überwachung ein, um zu erkennen, ob der Bot noch korrekt läuft, und Logs leicht auswerten zu können.

**Akzeptanzkriterien:**

- Einfacher Health-Check: z.B. Bot-Prozess im Container läuft, Container nicht ständig neu startet.
- Zugriff auf Logs über `docker logs`.
- Optional: Healthcheck im Dockerfile / Compose.

**Aufgaben:**

1. Logging:
   - Sicherstellen, dass der Bot alle Logs auf stdout/stderr schreibt (kein File-Logging).
   - Anleitung: `docker logs -f <bot-container-name>`.
2. Optional Healthcheck im Dockerfile oder Compose:
   - Z.B. ein einfacher `CMD`-Check, ob der Prozess noch lebt (schwierig ohne HTTP, optional).
3. In DSharpPlus:
   - LogLevel sinnvoll konfigurieren (z.B. `Information`), um CPU/IO zu sparen.
4. Dokumentation:
   - Kurze README-Sektion: „Wie prüfe ich auf dem Pi, ob der Bot läuft / wie sehe ich Logs?“.

---

Wenn du möchtest, kann ich dir im nächsten Schritt direkt ein Beispiel für:

- `Dockerfile`
- `docker-compose.yml`
- und die Codeanpassung in `Program.cs` (Env-Variablen lesen)

als vollständige Dateien im passenden Format schreiben.