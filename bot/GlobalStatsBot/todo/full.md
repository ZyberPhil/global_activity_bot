Hier sind konkrete Tickets (Issues) für die Implementierung der Methoden nach dem EF-Core-Scaffold. Sie sind so formuliert, dass du sie 1:1 in GitHub anlegen kannst.

---

### Ticket 1: `UserService` erstellen – User holen/anlegen & Basis-Updates

**Titel:** `UserService` implementieren (GetOrCreate, LastSeen, Avatar etc.)

**Beschreibung:**

Erstelle einen `UserService`, der alle Operationen rund um die `Users`-Tabelle kapselt.

**Aufgaben:**

1. **Klasse anlegen**
   - Namespace z.B. `GlobalStatsBot.Services`.
   - Konstruktor: nimmt `DiscordIdentityContext` (bzw. deinen `AppDbContext`) und `ILogger<UserService>` via DI.
   - Lebenszeit: `Scoped` im DI-Container registrieren.

2. **Methode: `Task<Users> GetOrCreateUserAsync(ulong discordUserId, string username, bool isBot, string? discriminator, string? avatarUrl, CancellationToken ct = default)`**
   - Zweck:
     - User anhand von `DiscordUserId` laden.
     - Wenn nicht vorhanden:
       - neuen `Users`-Eintrag erstellen mit:
         - `DiscordUserId`, `Username`, `Discriminator`, `AvatarUrl`
         - `FirstSeen = UtcNow`, `LastSeen = UtcNow`
         - `IsBot = isBot`
       - speichern.
     - Wenn vorhanden:
       - `Username`, `Discriminator`, `AvatarUrl` aktualisieren (falls geändert).
       - `LastSeen = UtcNow`.
       - speichern.
   - Rückgabewert: das `Users`-Entity.

3. **Methode: `Task<Users?> GetUserByDiscordIdAsync(ulong discordUserId, CancellationToken ct = default)`**
   - Zweck:
     - Nur lesen, kein Anlegen.
     - `Users`-Eintrag anhand von `DiscordUserId` laden oder `null` zurückgeben.
   - Verwendung:
     - Für reine Abfragen (z.B. in `/badge list`).

4. **Methode: `Task UpdateLastSeenAsync(ulong discordUserId, CancellationToken ct = default)`**
   - Zweck:
     - `LastSeen` für existierenden User auf `UtcNow` setzen.
   - Verhalten:
     - Wenn User nicht existiert → nichts tun oder ins Log schreiben (keine Exception).

5. **Methode: `Task<bool> SetUserBannedAsync(ulong discordUserId, bool isBanned, CancellationToken ct = default)`**
   - Zweck:
     - `IsBanned`-Flag setzen, um User ggf. komplett auszuschließen.
   - Rückgabewert:
     - `true`, wenn User gefunden und aktualisiert wurde.
     - `false`, wenn kein User mit dieser Discord-ID existiert.

6. **Logging & Fehlerbehandlung**
   - Bei Datenbankfehlern Exceptions loggen.
   - Keine `async void`, immer `async Task`.

---

### Ticket 2: `GuildService` erstellen – Guilds registrieren & Settings lesen

**Titel:** `GuildService` implementieren (GetOrCreateGuild, IsXpEnabled etc.)

**Beschreibung:**

Erstelle einen `GuildService`, der Operationen rund um die `Guilds`-Tabelle kapselt.

**Aufgaben:**

1. **Klasse anlegen**
   - Namespace z.B. `GlobalStatsBot.Services`.
   - Konstruktor mit `DiscordIdentityContext` und `ILogger<GuildService>`.
   - In DI als `Scoped` registrieren.

2. **Methode: `Task<Guilds> GetOrCreateGuildAsync(ulong discordGuildId, string name, string? iconUrl, CancellationToken ct = default)`**
   - Zweck:
     - Guild anhand von `DiscordGuildId` laden.
     - Wenn nicht vorhanden:
       - neuen `Guilds`-Eintrag erstellen mit:
         - `DiscordGuildId`, `Name`, `IconUrl`
         - `JoinedAt = UtcNow`
         - `IsXpEnabled = true` (Default).
       - speichern.
     - Wenn vorhanden:
       - `Name` und `IconUrl` aktualisieren (falls geändert).
       - `UpdatedAt` wird durch DB-Trigger/Definition gesetzt.
   - Rückgabewert: `Guilds`-Entity.

3. **Methode: `Task<Guilds?> GetGuildByDiscordIdAsync(ulong discordGuildId, CancellationToken ct = default)`**
   - Zweck:
     - Nur lesen, kein Anlegen.

4. **Methode: `Task<bool> SetXpEnabledAsync(ulong discordGuildId, bool isEnabled, CancellationToken ct = default)`**
   - Zweck:
     - Flag `IsXpEnabled` setzen, z.B. gesteuert durch `/guild xp enable/disable`.
   - Rückgabewert:
     - `true` bei Erfolg, `false`, wenn keine Guild mit dieser ID existiert.

5. **Methode (optional für später): `Task<string?> GetSettingsJsonAsync(ulong discordGuildId, CancellationToken ct = default)`**
   - Zweck:
     - `SettingsJson`-Spalte auslesen (z.B. für spätere Channel-Filter).
   - Kein Schreiben in diesem Ticket.

---

### Ticket 3: `StatsService` erstellen – XP & Nachrichtenverarbeitung

**Titel:** `StatsService` implementieren (XP für Nachrichten, globale XP-Cache)

**Beschreibung:**

Erstelle einen `StatsService`, der die XP-/Nachrichten-Logik über `UserStats` und `Users.GlobalXpCache` kapselt. Er wird vom Message-Handler verwendet.

**Aufgaben:**

1. **Klasse anlegen**
   - Namespace: `GlobalStatsBot.Services`.
   - Konstruktor-Parameter:
     - `DiscordIdentityContext`
     - `UserService`
     - `GuildService`
     - `ILogger<StatsService>`.

2. **Methode: `Task AddXpForMessageAsync(DiscordUser discordUser, DiscordGuild guild, long xpDelta = 1, long msgDelta = 1, CancellationToken ct = default)`**
   - Zweck:
     - Wird vom `MessageCreated`-Event aufgerufen (nach Cooldown-Check).
   - Schritte:
     1. Über `UserService.GetOrCreateUserAsync(...)` User holen/anlegen.
     2. Über `GuildService.GetOrCreateGuildAsync(...)` Guild holen/anlegen.
     3. Passenden `UserStats`-Datensatz für `(UserId, GuildId)` laden.
        - Wenn nicht vorhanden:
          - neuen Datensatz mit `Xp = xpDelta`, `Messages = msgDelta`, `LastMessageAt = UtcNow` anlegen.
        - Wenn vorhanden:
          - `Xp += xpDelta`, `Messages += msgDelta`, `LastMessageAt = UtcNow` aktualisieren.
     4. `Users.GlobalXpCache += xpDelta` hochzählen (am User-Entity).
     5. `SaveChangesAsync(ct)`.

3. **Methode: `Task<long> GetGlobalXpAsync(ulong discordUserId, CancellationToken ct = default)`**
   - Zweck:
     - Global XP eines Users ermitteln.
   - Implementierung:
     - Variante A (einfach): `Users.GlobalXpCache` zurückgeben.
     - Falls User nicht existiert: `0`.

4. **Methode: `Task<(long globalXp, int guildCount)> GetAggregatedStatsAsync(ulong discordUserId, CancellationToken ct = default)`**
   - Zweck:
     - Summe über alle `UserStats.Xp` + Anzahl Guilds.
   - Schritte:
     - User anhand `DiscordUserId` ermitteln (Join über `Users` bzw. vorher per `UserService`).
     - Alle `UserStats` mit `UserId` laden und aggregieren:
       - `globalXp = SUM(Xp)`
       - `guildCount = COUNT(distinct GuildId)` oder `COUNT(*)`.
     - Falls keine Stats: `(0, 0)`.

5. **Methode (optional für später): `Task<int> GetLevelFromXpAsync(long globalXp)`**
   - Zweck:
     - Levelberechnung zentralisieren: z.B. `return (int)(globalXp / 100);`
   - So kann die Formel später leicht geändert werden.

---

### Ticket 4: `BadgeService` erstellen – Badges verwalten & vergeben

**Titel:** `BadgeService` implementieren (Badges lesen, vergeben, prüfen)

**Beschreibung:**

Erstelle einen `BadgeService`, der die Arbeit mit `Badges` und `UserBadges` kapselt, anstatt alles direkt aus Commands zu machen.

**Aufgaben:**

1. **Klasse anlegen**
   - Namespace: `GlobalStatsBot.Services`.
   - Konstruktor mit:
     - `DiscordIdentityContext`
     - `UserService`
     - `ILogger<BadgeService>`.

2. **Methode: `Task<Badges?> GetBadgeByKeyAsync(string badgeKey, CancellationToken ct = default)`**
   - Zweck:
     - Badge anhand ihres `Key` laden (case-insensitive Vergleich empfohlen).

3. **Methode: `Task<IReadOnlyList<Badges>> GetAllBadgesAsync(CancellationToken ct = default)`**
   - Zweck:
     - Alle Badges aus DB lesen (z.B. für Admin-Übersichten).
   - Sortierung:
     - Standardmäßig nach `DisplayOrder` und dann nach `Name`.

4. **Methode: `Task<bool> GiveBadgeToUserAsync(ulong targetDiscordUserId, string targetUsername, string badgeKey, ulong grantedByDiscordUserId, string? reason, CancellationToken ct = default)`**
   - Zweck:
     - Kernlogik für `/badge give`.
   - Schritte:
     1. Über `UserService.GetOrCreateUserAsync` Ziel-User holen/anlegen.
     2. Badge über `GetBadgeByKeyAsync` holen.
        - Wenn nicht gefunden → `false` zurückgeben.
     3. Prüfen, ob in `UserBadges` bereits ein Eintrag `(UserId, BadgeId)` existiert.
        - Wenn ja → nichts tun, `true` zurückgeben (idempotent).
     4. Neuen `UserBadges`-Eintrag erstellen:
        - `UserId`, `BadgeId`
        - `GrantedByDiscordUserId`
        - `GrantedAt = UtcNow`
        - `Reason`.
     5. `SaveChangesAsync`.
   - Rückgabewert:
     - `true` bei Erfolg, `false` wenn Badge nicht existierte.

5. **Methode: `Task<IReadOnlyList<(Badges Badge, DateTime GrantedAt, string? Reason)>> GetBadgesForUserAsync(ulong discordUserId, CancellationToken ct = default)`**
   - Zweck:
     - Liste aller Badges eines Users für `/badge list` und `/me`.
   - Schritte:
     - User anhand `DiscordUserId` finden (oder `null` → leere Liste).
     - `UserBadges` + `Badges` joinen.
     - Sortierung:
       - Erst nach `Badges.DisplayOrder` ASC,
       - dann nach `UserBadges.GrantedAt` ASC.

6. **Methode: `Task<IReadOnlyList<(Badges Badge, DateTime GrantedAt, string? Reason)>> GetTopBadgesForUserAsync(ulong discordUserId, int topN, CancellationToken ct = default)`**
   - Zweck:
     - Nur die wichtigsten Badges (z.B. Top 3) für `/me`.
   - Implementierung:
     - Auf Basis von `GetBadgesForUserAsync`, dann `.Take(topN)` oder direkt per LINQ/SQL mit `Take(topN)`.

---

### Ticket 5: `ProfileService` / Profil-Abfrage für `/me`

**Titel:** Profil-Logik für `/me` kapseln (`ProfileService`)

**Beschreibung:**

Erstelle einen dedizierten Service für die Profilabfrage, um die Business-Logik von den SlashCommands zu trennen.

**Aufgaben:**

1. **Klasse anlegen**
   - Name: `ProfileService`.
   - Konstruktor:
     - `UserService`
     - `StatsService`
     - `BadgeService`
     - `ILogger<ProfileService>`.

2. **DTO anlegen: `UserProfileDto`**
   - Properties:
     - `ulong DiscordUserId`
     - `string Username`
     - `long GlobalXp`
     - `int Level`
     - `int GuildCount`
     - `IReadOnlyList<UserBadgeDto> Badges` (Top N)
   - `UserBadgeDto`:
     - `string Key`
     - `string Name`
     - `string Description`
     - `string? IconUrl`
     - `DateTime GrantedAt`
     - `string? Reason`

3. **Methode: `Task<UserProfileDto?> GetUserProfileAsync(ulong discordUserId, int topBadges = 3, CancellationToken ct = default)`**
   - Zweck:
     - Daten für `/me` gebündelt liefern.
   - Schritte:
     1. User über `UserService.GetUserByDiscordIdAsync` laden.
        - Wenn `null` → `null` zurückgeben (Profil existiert (noch) nicht).
     2. Über `StatsService.GetAggregatedStatsAsync` globale XP + Guild-Count holen.
     3. Level über `StatsService.GetLevelFromXpAsync(globalXp)` berechnen.
     4. Top-Badges über `BadgeService.GetTopBadgesForUserAsync`.
     5. `UserProfileDto` aus allen Infos zusammensetzen.

4. **Verwendung:**
   - SlashCommand `/me` ruft nur noch `ProfileService.GetUserProfileAsync` auf und baut daraus ein Embed.
   - Falls `null` → freundliche Meldung: „Du hast noch keine XP gesammelt.“

---

### Ticket 6: Message-Handler / XP-Cooldown-Layer

**Titel:** Message-Handler für XP mit Cooldown (Nutzung von `StatsService`)

**Beschreibung:**

Implementiere den eigentlichen Event-Handler, der DSharpPlus-Events mit den Services verbindet.

**Aufgaben:**

1. **Klasse anlegen**
   - Name: `XpMessageHandler` o.ä.
   - Felder:
     - `DiscordClient`
     - `StatsService`
     - `GuildService`
     - `ILogger<XpMessageHandler>`
     - `ConcurrentDictionary<ulong, DateTime>` o.ä. für Cooldown (Key = `DiscordUserId`).

2. **Registrierung im Bot-Startup**
   - In `BotService` oder vergleichbarer Klasse:
     - `client.MessageCreated += xpHandler.OnMessageCreatedAsync;`

3. **Methode: `Task OnMessageCreatedAsync(DiscordClient client, MessageCreateEventArgs e)`**
   - Schritte:
     1. Bot-Messages und Webhooks ignorieren.
     2. Nur Guild-Nachrichten berücksichtigen, keine DMs.
     3. Über `GuildService.GetGuildByDiscordIdAsync` prüfen, ob Guild existiert und `IsXpEnabled == true`.
        - Falls nicht → abbrechen.
     4. Cooldown prüfen:
        - Wenn `now - lastXp < 30s` → abbrechen.
        - Sonst `lastXp = now`.
     5. `StatsService.AddXpForMessageAsync(e.Author, e.Guild)` aufrufen (ggf. `xpDelta` aus Config).

---

Wenn du möchtest, kann ich dir als Nächstes die Tickets noch in konkrete GitHub-Issue-Markdown-Vorlagen umwandeln (mit Labels/Acceptance Criteria) oder wir starten mit der tatsächlichen Implementierung der ersten Service-Klasse (`UserService` + DI-Registrierung).