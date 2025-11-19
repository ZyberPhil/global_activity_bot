- C#
- DSharpPlus + SlashCommands
- **Database-First** mit **MariaDB**

---

## 1. Produktidee & Zielbild

**Bot-Idee:**  
Ein globaler „Identity & Stats“-Bot für Discord, der über alle Server hinweg:

- Aktivitäten von Nutzern sammelt (XP, Nachrichten, Server)
- **globale Profile** anbietet (`/me`)
- **globale Badges** verwaltet, die auf jedem Server sichtbar sind
- später **öffentliche Profilseiten** (Web) bieten kann

**Langfristiges Ziel:**  
Ein Ökosystem, nicht nur ein Ein-Server-Bot. Basis für:

- Premium-Features (mehr Analytics, Custom-Badges, Branding)
- Externes Web-Dashboard
- Monetarisierung (z.B. Pro-Server-Plan oder Pro-User-Profil)

---

## 2. Funktionsumfang des MVP

### 2.1. Für normale Nutzer

1. **Globales Profil (`/me`)**
   - Zeigt:
     - Discord-Name + Avatar
     - **Global XP**
     - Globales Level (z.B. `Level = XP / 100`)
     - Anzahl Server, auf denen der User aktiv war
     - bis zu 3 wichtigste Badges (Name + Beschreibung)

2. **XP-Sammeln**
   - Für Textnachrichten (`MessageCreated`):
     - Pro Nutzer alle X Sekunden (z.B. 30s) **1 XP**.
     - Anti-Spam-Cooldown im Speicher (Dictionary).
   - XP & Nachrichtenzahl werden **pro User + pro Server** gespeichert.

3. **Schöne Darstellung**
   - Profil-Ausgabe als Embed:
     - Farbe, Thumbnail = Avatar
     - klare, kurze Felder (XP, Level, Badges).

---

### 2.2. Für Admins / Moderatoren

1. **Badges vergeben (`/badge give`)**
   - `/badge give @User badge_key`
   - Nur bei ausreichenden Rechten (z.B. `ManageGuild`).
   - Badge ist **global**: auf allen Servern im Profil des Users sichtbar.

2. **Badges anzeigen (`/badge list`)**
   - `/badge list` → zeigt eigene Badges
   - `/badge list @User` → zeigt Badges eines anderen Users

3. **(optional im MVP) Infos zu diesem Server (`/guild info`)**
   - Zeigt, ob auf diesem Server XP-Tracking aktiv ist (später für Settings).

---

### 2.3. Technische Basis

1. **User-Registrierung**
   - Beim ersten Kontakt (Message, Command):
     - Eintrag in `Users`-Tabelle mit `DiscordUserId`, `Username`, `FirstSeen`, `LastSeen`.

2. **Guild-Registrierung**
   - Wenn der Bot auf einen Server kommt / `GuildAvailable`:
     - Eintrag in `Guilds` mit `DiscordGuildId`, Name, `JoinedAt`.

3. **Persistenz**
   - Alle Daten in **MariaDB**, Modell entsteht **Database-First**:
     - Tabellen direkt in der DB erstellen
     - dann mit EF Core generieren (`dbcontext scaffold`).

---

## 3. Architektur

### 3.1. Komponentenübersicht

1. **Discord-Bot (C# Console / Worker)**
   - Bibliothek: `DSharpPlus` + `DSharpPlus.SlashCommands`
   - Verantwortlich für:
     - Verbindung zu Discord
     - Events (MessageCreated, GuildAvailable)
     - SlashCommands (`/me`, `/badge ...`)

2. **Service-Schicht**
   - Klassen wie `UserService`, `StatsService`, `BadgeService`
   - Kapseln Zugriffe auf den `AppDbContext`
   - Enthalten Business-Logik (XP-Cooldown, Badge-Vergabe-Regeln)

3. **Datenzugriff (Database-First)**
   - `AppDbContext` + Entity-Klassen werden **aus der existierenden MariaDB** generiert.
   - Kein Code-First/Migrations-Workflow, Änderungen immer zuerst in der DB.

4. **(später) Web-App**
   - ASP.NET Core für:
     - öffentliche Profilseiten (`/u/{discordId}`)
     - Admin-Dashboard für Server-Owner.

---

### 3.2. Datenbankschema (Database-First)

Schema in MariaDB per SQL, z.B.:

```sql
CREATE TABLE Users (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    DiscordUserId BIGINT NOT NULL,
    Username VARCHAR(100) NOT NULL,
    FirstSeen DATETIME NOT NULL,
    LastSeen DATETIME NOT NULL
);

CREATE TABLE Guilds (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    DiscordGuildId BIGINT NOT NULL,
    Name VARCHAR(200) NOT NULL,
    JoinedAt DATETIME NOT NULL
);

CREATE TABLE UserStats (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    UserId BIGINT NOT NULL,
    GuildId BIGINT NOT NULL,
    Xp INT NOT NULL,
    Messages INT NOT NULL,
    LastMessageAt DATETIME NULL,
    CONSTRAINT FK_UserStats_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserStats_Guilds FOREIGN KEY (GuildId) REFERENCES Guilds(Id)
);

CREATE TABLE Badges (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    `Key` VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NOT NULL
);

CREATE TABLE UserBadges (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    UserId BIGINT NOT NULL,
    BadgeId INT NOT NULL,
    GrantedByDiscordUserId BIGINT NOT NULL,
    GrantedAt DATETIME NOT NULL,
    CONSTRAINT FK_UserBadges_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserBadges_Badges FOREIGN KEY (BadgeId) REFERENCES Badges(Id)
);
```

---

## 4. Tech-Stack

- **Sprache / Runtime:** .NET 8 (oder 7), C#
- **Discord:** [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) + `DSharpPlus.SlashCommands`
- **Datenbank:** MariaDB
- **ORM:** Entity Framework Core (Database-First)  
  Provider: `Pomelo.EntityFrameworkCore.MySql`
- **Konfiguration & DI:** `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`
- **Logging:** eingebautes Logging, später ggf. Serilog.

---

## 5. Schritt-für-Schritt-Plan (konkret, Database-First)

### Phase 1: Bot-Grundgerüst

1. **Projekt anlegen**

```bash
dotnet new console -n GlobalStatsBot
cd GlobalStatsBot
```

2. **NuGet-Pakete installieren**

```bash
dotnet add package DSharpPlus
dotnet add package DSharpPlus.SlashCommands

dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Logging
```

3. **Bot-Startup mit DI / Hosting aufsetzen**
   - `Program.cs`: `Host.CreateDefaultBuilder`
   - Dienst `BotService`, der den `DiscordClient` startet.

4. **DSharpPlus konfigurieren**
   - Token aus Umgebungsvariable oder `appsettings.json`.
   - Intents für Nachrichten, Guilds etc. aktivieren.
   - SlashCommands-Extension registrieren.

5. **Test-Command `/ping`**
   - Sicherstellen: Bot online, SlashCommands funktionieren.

---

### Phase 2: MariaDB & EF Core (Database-First)

1. **MariaDB & Datenbank erstellen**
   - DB `globalstatsbot` anlegen.
   - Tabellen mit dem oben beschriebenen SQL anlegen.

2. **EF Core + Pomelo installieren**

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

3. **Connection String in `appsettings.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=globalstatsbot;User=botuser;Password=starkespasswort;SslMode=Preferred;"
  }
}
```

4. **Database-First Scaffold**

```bash
dotnet tool install --global dotnet-ef   # falls noch nicht installiert

dotnet ef dbcontext scaffold \
  "Server=localhost;Port=3306;Database=globalstatsbot;User=botuser;Password=starkespasswort;" \
  Pomelo.EntityFrameworkCore.MySql \
  --context AppDbContext \
  --output-dir Data \
  --use-database-names
```

- Ergebnis:
  - `Data/AppDbContext.cs`
  - Entity-Klassen zu `Users`, `Guilds`, `UserStats`, `Badges`, `UserBadges`.

5. **DbContext in DI registrieren (`Program.cs`)**

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
```

---

### Phase 3: Basis-Services & Registrierungen

1. **`UserService`**
   - Methoden:
     - `Task<Users> GetOrCreateUserAsync(ulong discordUserId, string username)`
     - Aktualisiert auch `LastSeen`.

2. **`GuildService`**
   - Methoden:
     - `Task<Guilds> GetOrCreateGuildAsync(ulong guildId, string name)`

3. **`StatsService`**
   - Methoden:
     - `Task AddXpForMessageAsync(ulong discordUserId, string username, ulong guildId, string guildName)`

4. **Services in DI registrieren**
   - `services.AddScoped<UserService>();` etc.

---

### Phase 4: XP-Tracking (MessageCreated)

1. **Event-Handler registrieren**
   - `client.MessageCreated += OnMessageCreated;`

2. **In `OnMessageCreated`**
   - Bot-Messages ignorieren.
   - DM vs. GuildMessages unterscheiden (erst mal nur Guild).
   - Cooldown prüfen (Dictionary `<ulong userId, DateTime lastXp>`):
     - Wenn `now - lastXp > TimeSpan.FromSeconds(30)`:
       - `StatsService.AddXpForMessageAsync(...)` aufrufen
       - `lastXp` aktualisieren.

3. **`StatsService.AddXpForMessageAsync`**
   - `UserService.GetOrCreateUser`
   - `GuildService.GetOrCreateGuild`
   - passenden `UserStats` Eintrag für `(UserId, GuildId)` suchen oder neu anlegen.
   - `Xp++`, `Messages++`, `LastMessageAt = UtcNow`
   - `SaveChangesAsync`.

---

### Phase 5: `/me`-Command

1. **SlashCommand-Modul `ProfileCommands`**
   - Konstruktor bekommt `AppDbContext` oder `UserService/StatsService` via DI.

2. **Logik**
   - User per `DiscordUserId` aus `Users` holen.
   - Alle `UserStats` zu diesem User laden:
     - Summe über alle Guilds = Global XP.
     - Anzahl verschiedener Guilds = Server-Count.
   - `UserBadges` + `Badges` joinen, Top 3 ausgeben.
   - Level berechnen: z.B. `Level = XP / 100`.

3. **Embed bauen und senden**
   - Titel: `Profil von {Username}`
   - Felder: XP, Level, Server-Anzahl, Badges.

---

### Phase 6: Badges

1. **Standard-Badges in DB einfügen (SQL-Seed)**

```sql
INSERT INTO Badges (`Key`, Name, Description)
VALUES
('helper', 'Helper', 'Hilft regelmäßig anderen in der Community.'),
('top_contributor', 'Top Contributor', 'Sehr aktiv und engagiert.'),
('event_winner', 'Event Winner', 'Hat ein Server-Event gewonnen.');
```

2. **`/badge give`**
   - SlashCommand-Modul `BadgeCommands`.
   - Parameter: `DiscordUser` + `string badgeKey`.
   - Prüfe:
     - Badge existiert?
     - Aufrufer hat nötige Berechtigung (z.B. `ctx.Member.PermissionsIn(ctx.Channel)`).
   - `UserService.GetOrCreateUser` für Ziel-User.
   - Eintrag in `UserBadges` erstellen (`GrantedAt`, `GrantedByDiscordUserId`).

3. **`/badge list`**
   - Optionaler Parameter: User.
   - User in DB suchen.
   - `UserBadges` inkl. `Badges` laden, sortiert nach `GrantedAt`.
   - Embed mit Liste der Badges.

---

### Phase 7: Stabilität & Skalierung

1. **Fehlerbehandlung**
   - Try/Catch um Command-Handler und Event-Handler.
   - Fehler loggen, User-Freundliche Meldung zurückgeben.

2. **Performance**
   - `async/await` überall, keine `.Result`/`.Wait()`.
   - DB-Zugriffe minimieren (z.B. Stats nicht bei jeder Nachricht flushen, später Sammel-Updates).

3. **Konfigurierbarkeit pro Guild (später)**
   - In `Guilds` zusätzliche Spalten (z.B. `IsXpEnabled`, `ExcludedChannelIds`).
   - Commands, um diese Settings pro Server zu ändern.

---

### Phase 8 (später): Web-Profilseiten

1. **ASP.NET Core Projekt hinzufügen**
   - Neues Projekt `GlobalStatsBot.Web`.
2. **Gemeinsames Model teilen**
   - `AppDbContext` und Entitäten in eigene Class Library auslagern, von Bot & Web nutzen.
3. **API & Views**
   - `GET /u/{discordId}` → Profilseite mit XP, Level, Badges.
   - Evtl. OAuth2-Login via Discord für User-spezifische Einstellungen.

---

Wenn du möchtest, kann ich dir jetzt als nächsten Schritt ein **konkretes Minimalgerüst** schreiben (mit Datei-Struktur und Beispiel-Code für `Program.cs`, `BotService`, `ProfileCommands`), alles schon passend für:

- DSharpPlus + SlashCommands
- EF Core Database-First mit MariaDB.
