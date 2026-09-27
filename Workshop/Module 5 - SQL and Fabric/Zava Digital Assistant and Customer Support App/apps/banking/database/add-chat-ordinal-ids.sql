-- Adds operational ordinal IDs to existing chat tables without recreating them.
-- Existing rows receive identity values automatically. GUID keys and relationships remain unchanged.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ChatSessions', N'U') IS NULL
    OR OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
    OR OBJECT_ID(N'dbo.ChatEscalations', N'U') IS NULL
    THROW 51030, 'The chat tables must exist before applying this delta.', 1;

BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.ChatSessions', N'SessionOrdinalId') IS NULL
    ALTER TABLE dbo.ChatSessions ADD SessionOrdinalId bigint IDENTITY(1, 1) NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatSessions')
      AND name = N'UQ_ChatSessions_Ordinal')
    ALTER TABLE dbo.ChatSessions ADD CONSTRAINT UQ_ChatSessions_Ordinal UNIQUE (SessionOrdinalId);

IF COL_LENGTH(N'dbo.ChatMessages', N'MessageOrdinalId') IS NULL
    ALTER TABLE dbo.ChatMessages ADD MessageOrdinalId bigint IDENTITY(1, 1) NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatMessages')
      AND name = N'UQ_ChatMessages_Ordinal')
    ALTER TABLE dbo.ChatMessages ADD CONSTRAINT UQ_ChatMessages_Ordinal UNIQUE (MessageOrdinalId);

IF COL_LENGTH(N'dbo.ChatEscalations', N'EscalationOrdinalId') IS NULL
    ALTER TABLE dbo.ChatEscalations ADD EscalationOrdinalId bigint IDENTITY(1, 1) NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatEscalations')
      AND name = N'UQ_ChatEscalations_Ordinal')
    ALTER TABLE dbo.ChatEscalations ADD CONSTRAINT UQ_ChatEscalations_Ordinal UNIQUE (EscalationOrdinalId);

COMMIT;