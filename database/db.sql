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
--  Tabellen (vorher alte Tabellen droppen, falls vorhanden)
-- ============================================================================

DROP TABLE IF EXISTS `UserBadges`;
DROP TABLE IF EXISTS `UserStats`;
DROP TABLE IF EXISTS `GuildSubscriptions`;
DROP TABLE IF EXISTS `Badges`;
DROP TABLE IF EXISTS `Guilds`;
DROP TABLE IF EXISTS `Users`;

-- 1) Users: globale User-Identität
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
--  Testdaten einfügen
-- ============================================================================

USE `discord_identity`;

-- Basisdaten: Users
INSERT INTO `Users` (
    `DiscordUserId`, `Username`, `Discriminator`, `AvatarUrl`,
    `FirstSeen`, `LastSeen`, `IsBot`, `IsBanned`, `GlobalXpCache`
) VALUES
(100000000000000001, 'UserAlpha',   '0001', NULL, NOW(6), NOW(6), 0, 0, 1500),
(100000000000000002, 'UserBeta',    '0002', NULL, NOW(6), NOW(6), 0, 0, 250),
(100000000000000003, 'UserGamma',   '0003', NULL, NOW(6), NOW(6), 0, 0, 0),
(100000000000000004, 'StatsBot',    '9999', NULL, NOW(6), NOW(6), 1, 0, 0);

-- Basisdaten: Guilds
INSERT INTO `Guilds` (
    `DiscordGuildId`, `Name`, `IconUrl`,
    `JoinedAt`, `IsXpEnabled`, `SettingsJson`
) VALUES
(200000000000000001, 'Guild One',   NULL, NOW(6), 1, JSON_OBJECT('xp_rate', 1, 'cooldown_seconds', 30)),
(200000000000000002, 'Guild Two',   NULL, NOW(6), 1, JSON_OBJECT('xp_rate', 2, 'cooldown_seconds', 20)),
(200000000000000003, 'No XP Guild', NULL, NOW(6), 0, JSON_OBJECT('xp_rate', 0, 'cooldown_seconds', 0));

-- Basisdaten: Badges
INSERT INTO `Badges` (
    `Key`, `Name`, `Description`, `IconUrl`,
    `IsSystem`, `IsPremium`, `DisplayOrder`
) VALUES
('helper',          'Helper',          'Hilft häufig anderen Nutzern.',                      NULL, 1, 0, 10),
('top_contributor', 'Top Contributor', 'Sehr aktive Teilnahme an Diskussionen.',             NULL, 1, 0, 20),
('early_supporter', 'Early Supporter', 'Unterstützt das Projekt in der Frühphase.',         NULL, 1, 1, 30),
('server_owner',    'Server Owner',    'Betreibt einen Server, auf dem der Bot aktiv ist.', NULL, 0, 1, 40);

-- Beispiel-Stats: UserStats
SELECT @UserAlphaId := `Id` FROM `Users` WHERE `DiscordUserId` = 100000000000000001;
SELECT @UserBetaId  := `Id` FROM `Users` WHERE `DiscordUserId` = 100000000000000002;

SELECT @GuildOneId  := `Id` FROM `Guilds` WHERE `DiscordGuildId` = 200000000000000001;
SELECT @GuildTwoId  := `Id` FROM `Guilds` WHERE `DiscordGuildId` = 200000000000000002;

INSERT INTO `UserStats` (
    `UserId`, `GuildId`, `Xp`, `Messages`, `LastMessageAt`
) VALUES
(@UserAlphaId, @GuildOneId, 1000,  500, NOW(6) - INTERVAL 1 DAY),
(@UserAlphaId, @GuildTwoId,  500,  200, NOW(6) - INTERVAL 2 DAY),
(@UserBetaId,  @GuildOneId,  250,  100, NOW(6) - INTERVAL 3 DAY);

-- Beispiel-Badges: UserBadges
SELECT @HelperId          := `Id` FROM `Badges` WHERE `Key` = 'helper';
SELECT @TopContributorId  := `Id` FROM `Badges` WHERE `Key` = 'top_contributor';
SELECT @EarlySupporterId  := `Id` FROM `Badges` WHERE `Key` = 'early_supporter';

INSERT INTO `UserBadges` (
    `UserId`, `BadgeId`, `GrantedByDiscordUserId`, `GrantedAt`, `Reason`
) VALUES
(@UserAlphaId, @HelperId,         100000000000000004, NOW(6) - INTERVAL 5 DAY, 'Hilft aktiv im Support-Channel.'),
(@UserAlphaId, @TopContributorId, 100000000000000004, NOW(6) - INTERVAL 2 DAY, 'Sehr viele nützliche Beiträge.'),
(@UserBetaId,  @EarlySupporterId, 100000000000000004, NOW(6) - INTERVAL 10 DAY,'Schon früh den Bot genutzt.');

-- Optionale Testaufrufe der Stored Procedures (kannst du auskommentieren, falls nicht gewünscht)

-- Beispiel: neuen User per SP anlegen
-- SET @NewUserId = 0;
-- CALL sp_GetOrCreateUserByDiscordId(100000000000000010, 'NewUser', 0, @NewUserId);
-- SELECT @NewUserId AS NewUserId;

-- Beispiel: neue Guild per SP anlegen
-- SET @NewGuildId = 0;
-- CALL sp_GetOrCreateGuildByDiscordId(200000000000000010, 'Test Guild', @NewGuildId);
-- SELECT @NewGuildId AS NewGuildId;

-- Beispiel: XP hinzufügen
-- CALL sp_AddXpForUserInGuild(@UserAlphaId, @GuildOneId, 10, 3);

-- Beispiel: Badge vergeben
-- CALL sp_GiveBadgeToUser(
--     100000000000000001,
--     'UserAlpha',
--     'server_owner',
--     100000000000000004,
--     'Betreibt einen Test-Server.'
-- );

-- Beispiel: Profil abrufen
-- CALL sp_GetUserProfile(100000000000000001, 3);

-- ============================================================================
--  Ende
-- ============================================================================