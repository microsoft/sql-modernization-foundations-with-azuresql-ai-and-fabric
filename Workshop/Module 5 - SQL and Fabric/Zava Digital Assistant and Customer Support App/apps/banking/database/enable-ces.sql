SET NOCOUNT ON;

DECLARE @HubNamespace sysname = N'<enter hub namespace>';
DECLARE @HubName sysname = N'<enter hub name>s';
DECLARE @KeyName sysname = N'<event-hubs-key-name>';
DECLARE @PolicyKey nvarchar(256) = N'<event-hubs-shared-access-key>';
DECLARE @MasterKeyPassword nvarchar(128) = N'<strong-unique-master-key-password>';


IF EXISTS (
    SELECT 1
    FROM (VALUES (@HubNamespace), (@HubName), (@KeyName), (@PolicyKey), (@MasterKeyPassword)) AS settings(value)
    WHERE value IS NULL OR LTRIM(RTRIM(value)) = N'' OR LTRIM(RTRIM(value)) LIKE N'<%>'
        OR LTRIM(RTRIM(REPLACE(value, N'*', N''))) = N''
)
    THROW 51000, 'Replace empty, placeholder, or masked values in a private SQL query window. Do not save secrets to the repository.', 1;
IF @HubNamespace LIKE N'%[^a-zA-Z0-9.-]%' OR @HubNamespace NOT LIKE N'%.servicebus.windows.net'
    OR @HubName LIKE N'%[^a-zA-Z0-9._-]%'
    THROW 51002, 'Use a namespace hostname without protocol, port, or trailing slash, and a hub name, not a connection string.', 1;
IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb') OR OBJECT_ID(N'dbo.ChatEscalations', N'U') IS NULL
    THROW 51001, 'Connect to the workshop Fabric SQL database and run setup.ps1 first.', 1;

DECLARE @sql nvarchar(max);
DECLARE @result int;
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_event_stream_enabled = 1)
BEGIN
    EXEC @result = sys.sp_enable_change_event_stream;
    IF @result <> 0 THROW 51003, 'Could not enable CES. Inspect permissions and platform support.', 1;
END;

-- Match the Fabric SQL preview helper result shapes, including mirroring and CES columns.
DECLARE @Groups TABLE (
    table_group_id uniqueidentifier,
    table_group_name nvarchar(140),
    destination_location nvarchar(max),
    destination_credential nvarchar(128) NULL,
    workspace_id nvarchar(max),
    synapse_workgroup_name nvarchar(max),
    enabled bit,
    destination_type int,
    max_message_size_bytes int,
    partition_scheme int,
    partition_column_name nvarchar(128) NULL,
    encoding int,
    streaming_dest_type nvarchar(100),
    certificate_path nvarchar(max),
    service_principal nvarchar(max),
    token_config nvarchar(max),
    event_ordering nvarchar(max),
    streaming_source nvarchar(max)
);
INSERT INTO @Groups EXEC sys.sp_help_change_feed_table_groups;

DECLARE @Destination nvarchar(4000) = @HubNamespace + N':9093/' + @HubName;
DECLARE @GroupId uniqueidentifier;
SELECT @GroupId = table_group_id FROM @Groups WHERE table_group_name = N'ZavaEscalations';

IF @GroupId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM @Groups
    WHERE table_group_id = @GroupId
        AND destination_location = @Destination
        AND destination_credential = N'ZavaCesSender'
        AND destination_type = 1
        AND streaming_dest_type = N'AzureEventHubs'
        AND enabled = 1
)
BEGIN
    SELECT * FROM @Groups WHERE table_group_id = @GroupId;
    THROW 51006, 'ZavaEscalations exists with a different destination, credential, type, or enabled state. Resolve the mismatch before rerunning; do not modify Replicator.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##')
BEGIN
    SET @sql = N'CREATE MASTER KEY ENCRYPTION BY PASSWORD = N''' + REPLACE(@MasterKeyPassword, N'''', N'''''') + N''';';
    EXEC sys.sp_executesql @sql;
END;
SET @sql = CASE WHEN EXISTS (SELECT 1 FROM sys.database_scoped_credentials WHERE name = N'ZavaCesSender')
    THEN N'ALTER DATABASE SCOPED CREDENTIAL [ZavaCesSender] WITH IDENTITY = N'''
    ELSE N'CREATE DATABASE SCOPED CREDENTIAL [ZavaCesSender] WITH IDENTITY = N'''
    END
    + REPLACE(@KeyName, N'''', N'''''') + N''', SECRET = N''' + REPLACE(@PolicyKey, N'''', N'''''') + N''';';
EXEC sys.sp_executesql @sql;

IF @GroupId IS NULL
BEGIN
    EXEC @result = sys.sp_create_event_stream_group
        @stream_group_name = N'ZavaEscalations',
        @destination_type = N'AzureEventHubs',
        @destination_location = @Destination,
        @destination_credential = N'ZavaCesSender',
        @max_message_size_kb = 256,
        @partition_key_scheme = N'Table',
        @encoding = N'JSON';

    IF @result <> 0
        THROW 51004, 'Could not create ZavaEscalations. Inspect the destination and credential.', 1;

    DELETE FROM @Groups;
    INSERT INTO @Groups EXEC sys.sp_help_change_feed_table_groups;
    SELECT @GroupId = table_group_id FROM @Groups WHERE table_group_name = N'ZavaEscalations';
END;
IF @GroupId IS NULL
    THROW 51007, 'ZavaEscalations was not found after creation. Inspect CES configuration before continuing.', 1;

DECLARE @Tables TABLE (
    table_group_id uniqueidentifier,
    table_group_name nvarchar(140),
    schema_name sysname,
    table_name sysname,
    table_id uniqueidentifier,
    destination_location nvarchar(max),
    workspace_id nvarchar(max),
    state tinyint,
    table_object_id int,
    version binary(10),
    enable_lsn binary(10),
    disable_lsn binary(10),
    snapshot_phase tinyint,
    snapshot_current_phase_time datetime2(7),
    snapshot_retry_count int,
    snapshot_start_time datetime2(7),
    snapshot_end_time datetime2(7),
    snapshot_row_count bigint,
    include_old_values bit,
    include_all_columns bit,
    include_old_lob_values bit,
    destination_fqdn nvarchar(max)
);
INSERT INTO @Tables
EXEC sys.sp_help_change_feed_table @source_schema = N'dbo', @source_name = N'ChatEscalations';

-- Fabric mirroring can set is_replicated without membership in this CES group.
IF NOT EXISTS (
    SELECT 1 FROM @Tables
    WHERE table_group_id = @GroupId AND table_object_id = OBJECT_ID(N'dbo.ChatEscalations')
)
BEGIN
    EXEC @result = sys.sp_add_object_to_event_stream_group N'ZavaEscalations', N'dbo.ChatEscalations';
    IF @result <> 0
        THROW 51005, 'Could not add ChatEscalations to CES. Inspect monitoring before continuing.', 1;
END;

DELETE FROM @Tables;
INSERT INTO @Tables
EXEC sys.sp_help_change_feed_table @source_schema = N'dbo', @source_name = N'ChatEscalations';
IF NOT EXISTS (
    SELECT 1 FROM @Tables
    WHERE table_group_id = @GroupId AND table_object_id = OBJECT_ID(N'dbo.ChatEscalations')
        AND state IN (1, 2, 3, 4)
)
BEGIN
    SELECT * FROM @Tables WHERE table_group_id = @GroupId;
    THROW 51008, 'ChatEscalations is missing or not enabled in ZavaEscalations. Inspect monitoring before continuing.', 1;
END;

SELECT name, is_event_stream_enabled FROM sys.databases WHERE name = DB_NAME();
SELECT * FROM @Groups WHERE table_group_id = @GroupId;
SELECT * FROM @Tables WHERE table_group_id = @GroupId;
PRINT 'CES configuration verified. Create a fresh escalation and check monitor-ces.sql and Eventstream to verify delivery.';