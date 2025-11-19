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
-- ============================================================================
--  Datenbank anlegen
-- ============================================================================

CREATE DATABASE IF NOT EXISTS `discord_identity`
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;

USE `discord_identity`;

SET NAMES utf8mb4;
SET time_zone = '+00:00';

-- ============================================================================
--  Tabellen
-- ============================================================================

-- 1) Users: globale User-Identität
DROP TABLE IF EXISTS `UserBadges`;
DROP TABLE IF EXISTS `UserStats`;
DROP TABLE IF EXISTS `GuildSubscriptions`;
DROP TABLE IF EXISTS `Badges`;
DROP TABLE IF EXISTS `Guilds`;
DROP TABLE IF EXISTS `Users`;

CREATE TABLE `Users` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `DiscordUserId` BIGINT UNSIGNED NOT NULL,
    `Username` VARCHAR(100) NOT NULL,
    `Discriminator` VARCHAR(10) NULL,
    `AvatarUrl` VARCHAR(500) NULL,
    `FirstSeen` DATETIME(6) NOT NULL,
    `LastSeen` DATETIME(6) NOT NULL,
    `IsBot` TINYINT(1) NOT NULL DEFAULT 0,
    `IsBanned` TINYINT(1) NOT NULL DEFAULT 0,
    `GlobalXpCache` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_Users_DiscordUserId` (`DiscordUserId`),
    KEY `IX_Users_FirstSeen` (`FirstSeen`),
    KEY `IX_Users_LastSeen` (`LastSeen`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;

-- 2) Guilds: Discord-Guilds (Server)
CREATE TABLE `Guilds` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `DiscordGuildId` BIGINT UNSIGNED NOT NULL,
    `Name` VARCHAR(200) NOT NULL,
    `IconUrl` VARCHAR(500) NULL,
    `JoinedAt` DATETIME(6) NOT NULL,
    `LeftAt` DATETIME(6) NULL,
    `IsXpEnabled` TINYINT(1) NOT NULL DEFAULT 1,
    `SettingsJson` JSON NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_Guilds_DiscordGuildId` (`DiscordGuildId`),
    KEY `IX_Guilds_JoinedAt` (`JoinedAt`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;

-- 3) UserStats: Stats pro (User, Guild)
CREATE TABLE `UserStats` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `UserId` BIGINT UNSIGNED NOT NULL,
    `GuildId` BIGINT UNSIGNED NOT NULL,
    `Xp` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `Messages` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `LastMessageAt` DATETIME(6) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_UserStats_User_Guild` (`UserId`, `GuildId`),
    KEY `IX_UserStats_UserId` (`UserId`),
    KEY `IX_UserStats_GuildId` (`GuildId`),
    KEY `IX_UserStats_LastMessageAt` (`LastMessageAt`),
    CONSTRAINT `FK_UserStats_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT `FK_UserStats_Guilds`
        FOREIGN KEY (`GuildId`) REFERENCES `Guilds` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;

-- 4) Badges: globale Badge-Typen
CREATE TABLE `Badges` (
    `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `Key` VARCHAR(50) NOT NULL,
    `Name` VARCHAR(100) NOT NULL,
    `Description` VARCHAR(500) NOT NULL,
    `IconUrl` VARCHAR(500) NULL,
    `IsSystem` TINYINT(1) NOT NULL DEFAULT 0,
    `IsPremium` TINYINT(1) NOT NULL DEFAULT 0,
    `DisplayOrder` INT NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_Badges_Key` (`Key`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;

-- 5) UserBadges: Verknüpfung User <-> Badge
CREATE TABLE `UserBadges` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `UserId` BIGINT UNSIGNED NOT NULL,
    `BadgeId` INT UNSIGNED NOT NULL,
    `GrantedByDiscordUserId` BIGINT UNSIGNED NOT NULL,
    `GrantedAt` DATETIME(6) NOT NULL,
    `Reason` VARCHAR(255) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_UserBadges_User_Badge` (`UserId`, `BadgeId`),
    KEY `IX_UserBadges_UserId` (`UserId`),
    KEY `IX_UserBadges_BadgeId` (`BadgeId`),
    CONSTRAINT `FK_UserBadges_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT `FK_UserBadges_Badges`
        FOREIGN KEY (`BadgeId`) REFERENCES `Badges` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;

-- 6) GuildSubscriptions (optional, für Premium-Features)
CREATE TABLE `GuildSubscriptions` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `GuildId` BIGINT UNSIGNED NOT NULL,
    `PlanKey` VARCHAR(50) NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `ValidFrom` DATETIME(6) NOT NULL,
    `ValidTo` DATETIME(6) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    KEY `IX_GuildSubscriptions_GuildId` (`GuildId`),
    KEY `IX_GuildSubscriptions_PlanKey` (`PlanKey`),
    CONSTRAINT `FK_GuildSubscriptions_Guilds`
        FOREIGN KEY (`GuildId`) REFERENCES `Guilds` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
--  Stored Procedures
-- ============================================================================

DELIMITER $$

-- 1) User holen oder anlegen
DROP PROCEDURE IF EXISTS `sp_GetOrCreateUserByDiscordId` $$
CREATE PROCEDURE `sp_GetOrCreateUserByDiscordId` (
    IN  p_DiscordUserId BIGINT UNSIGNED,
    IN  p_Username      VARCHAR(100),
    IN  p_IsBot         TINYINT(1),
    OUT p_UserId        BIGINT UNSIGNED
)
BEGIN
    DECLARE v_Id BIGINT UNSIGNED;

    START TRANSACTION;

    SELECT `Id` INTO v_Id
    FROM `Users`
    WHERE `DiscordUserId` = p_DiscordUserId
    FOR UPDATE;

    IF v_Id IS NULL THEN
        INSERT INTO `Users` (`DiscordUserId`, `Username`, `FirstSeen`, `LastSeen`, `IsBot`)
        VALUES (p_DiscordUserId, p_Username, NOW(6), NOW(6), p_IsBot);

        SET v_Id = LAST_INSERT_ID();
    ELSE
        UPDATE `Users`
        SET `Username` = p_Username,
            `LastSeen` = NOW(6)
        WHERE `Id` = v_Id;
    END IF;

    COMMIT;

    SET p_UserId = v_Id;
END$$

-- 2) Guild holen oder anlegen
DROP PROCEDURE IF EXISTS `sp_GetOrCreateGuildByDiscordId` $$
CREATE PROCEDURE `sp_GetOrCreateGuildByDiscordId` (
    IN  p_DiscordGuildId BIGINT UNSIGNED,
    IN  p_Name           VARCHAR(200),
    OUT p_GuildId        BIGINT UNSIGNED
)
BEGIN
    DECLARE v_Id BIGINT UNSIGNED;

    START TRANSACTION;

    SELECT `Id` INTO v_Id
    FROM `Guilds`
    WHERE `DiscordGuildId` = p_DiscordGuildId
    FOR UPDATE;

    IF v_Id IS NULL THEN
        INSERT INTO `Guilds` (`DiscordGuildId`, `Name`, `JoinedAt`)
        VALUES (p_DiscordGuildId, p_Name, NOW(6));

        SET v_Id = LAST_INSERT_ID();
    ELSE
        UPDATE `Guilds`
        SET `Name` = p_Name,
            `UpdatedAt` = NOW(6)
        WHERE `Id` = v_Id;
    END IF;

    COMMIT;

    SET p_GuildId = v_Id;
END$$

-- 3) XP & Messages inkrementieren
DROP PROCEDURE IF EXISTS `sp_AddXpForUserInGuild` $$
CREATE PROCEDURE `sp_AddXpForUserInGuild` (
    IN p_UserId   BIGINT UNSIGNED,
    IN p_GuildId  BIGINT UNSIGNED,
    IN p_XpDelta  BIGINT UNSIGNED,
    IN p_MsgDelta BIGINT UNSIGNED
)
BEGIN
    START TRANSACTION;

    INSERT INTO `UserStats` (`UserId`, `GuildId`, `Xp`, `Messages`, `LastMessageAt`)
    VALUES (p_UserId, p_GuildId, p_XpDelta, p_MsgDelta, NOW(6))
    ON DUPLICATE KEY UPDATE
        `Xp` = `Xp` + VALUES(`Xp`),
        `Messages` = `Messages` + VALUES(`Messages`),
        `LastMessageAt` = VALUES(`LastMessageAt`),
        `UpdatedAt` = NOW(6);

    UPDATE `Users`
    SET `GlobalXpCache` = `GlobalXpCache` + p_XpDelta,
        `LastSeen` = NOW(6)
    WHERE `Id` = p_UserId;

    COMMIT;
END$$

-- 4) Badge vergeben
DROP PROCEDURE IF EXISTS `sp_GiveBadgeToUser` $$
CREATE PROCEDURE `sp_GiveBadgeToUser` (
    IN p_DiscordUserId           BIGINT UNSIGNED,
    IN p_Username                VARCHAR(100),
    IN p_BadgeKey                VARCHAR(50),
    IN p_GrantedByDiscordUserId  BIGINT UNSIGNED,
    IN p_Reason                  VARCHAR(255)
)
BEGIN
    DECLARE v_UserId  BIGINT UNSIGNED;
    DECLARE v_BadgeId INT UNSIGNED;

    START TRANSACTION;

    -- User holen oder erstellen
    SELECT `Id` INTO v_UserId
    FROM `Users`
    WHERE `DiscordUserId` = p_DiscordUserId
    FOR UPDATE;

    IF v_UserId IS NULL THEN
        INSERT INTO `Users` (`DiscordUserId`, `Username`, `FirstSeen`, `LastSeen`)
        VALUES (p_DiscordUserId, p_Username, NOW(6), NOW(6));

        SET v_UserId = LAST_INSERT_ID();
    ELSE
        UPDATE `Users`
        SET `Username` = p_Username,
            `LastSeen` = NOW(6)
        WHERE `Id` = v_UserId;
    END IF;

    -- Badge holen
    SELECT `Id` INTO v_BadgeId
    FROM `Badges`
    WHERE `Key` = p_BadgeKey
    FOR UPDATE;

    IF v_BadgeId IS NULL THEN
        ROLLBACK;
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Badge with given key does not exist.';
    ELSE
        -- Nur ein Eintrag pro (User, Badge)
        IF NOT EXISTS (
            SELECT 1
            FROM `UserBadges`
            WHERE `UserId` = v_UserId
              AND `BadgeId` = v_BadgeId
            FOR UPDATE
        ) THEN
            INSERT INTO `UserBadges` (
                `UserId`, `BadgeId`, `GrantedByDiscordUserId`,
                `GrantedAt`, `Reason`
            )
            VALUES (
                v_UserId, v_BadgeId,
                p_GrantedByDiscordUserId,
                NOW(6), p_Reason
            );
        END IF;
    END IF;

    COMMIT;
END$$

-- 5) Globales Profil lesen (für /me)
DROP PROCEDURE IF EXISTS `sp_GetUserProfile` $$
CREATE PROCEDURE `sp_GetUserProfile` (
    IN p_DiscordUserId BIGINT UNSIGNED,
    IN p_TopBadges     INT
)
BEGIN
    DECLARE v_UserId BIGINT UNSIGNED;

    -- User finden
    SELECT `Id`
    INTO v_UserId
    FROM `Users`
    WHERE `DiscordUserId` = p_DiscordUserId;

    IF v_UserId IS NULL THEN
        -- Leeres Profil zurückgeben
        SELECT
            NULL AS `UserId`,
            NULL AS `DiscordUserId`,
            NULL AS `Username`,
            0    AS `GlobalXp`,
            0    AS `GuildCount`,
            0    AS `Level`;
    ELSE
        -- Basisprofil + aggregierte XP (on the fly)
        SELECT
            u.`Id`            AS `UserId`,
            u.`DiscordUserId` AS `DiscordUserId`,
            u.`Username`      AS `Username`,
            COALESCE(SUM(us.`Xp`), 0) AS `GlobalXp`,
            COUNT(us.`Id`)            AS `GuildCount`,
            FLOOR(COALESCE(SUM(us.`Xp`), 0) / 100) AS `Level`
        FROM `Users` u
        LEFT JOIN `UserStats` us ON us.`UserId` = u.`Id`
        WHERE u.`Id` = v_UserId;

        -- Badges (Top N)
        SELECT
            b.`Key`,
            b.`Name`,
            b.`Description`,
            b.`IconUrl`,
            ub.`GrantedAt`,
            ub.`Reason`
        FROM `UserBadges` ub
        JOIN `Badges` b ON b.`Id` = ub.`BadgeId`
        WHERE ub.`UserId` = v_UserId
        ORDER BY b.`DisplayOrder` ASC, ub.`GrantedAt` ASC
        LIMIT p_TopBadges;
    END IF;
END$$

DELIMITER ;

-- ============================================================================
--  Ende
-- ============================================================================
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
