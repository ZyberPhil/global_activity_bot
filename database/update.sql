-- Update script to add ChannelStats table for existing installations
USE `discord_identity`;

CREATE TABLE IF NOT EXISTS `ChannelStats` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `UserId` BIGINT UNSIGNED NOT NULL,
    `GuildId` BIGINT UNSIGNED NOT NULL,
    `ChannelId` BIGINT UNSIGNED NOT NULL,
    `Xp` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `Messages` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `LastMessageAt` DATETIME(6) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_ChannelStats_User_Guild_Channel` (`UserId`, `GuildId`, `ChannelId`),
    KEY `IX_ChannelStats_UserId` (`UserId`),
    KEY `IX_ChannelStats_GuildId` (`GuildId`),
    KEY `IX_ChannelStats_ChannelId` (`ChannelId`),
    KEY `IX_ChannelStats_LastMessageAt` (`LastMessageAt`),
    CONSTRAINT `FK_ChannelStats_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT `FK_ChannelStats_Guilds`
        FOREIGN KEY (`GuildId`) REFERENCES `Guilds` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;
