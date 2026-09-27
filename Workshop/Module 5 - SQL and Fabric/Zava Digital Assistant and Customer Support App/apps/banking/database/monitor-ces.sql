SELECT name, is_event_stream_enabled FROM sys.databases WHERE name = DB_NAME();
SELECT name, is_replicated FROM sys.tables WHERE name IN (N'FaqEntries', N'ChatSessions', N'ChatMessages', N'ChatEscalations');
EXEC sys.sp_help_change_feed_table @source_schema = N'dbo', @source_name = N'ChatEscalations';
SELECT TOP (50) * FROM sys.dm_change_feed_errors;
SELECT TOP (50) * FROM sys.dm_change_feed_log_scan_sessions;

SELECT State, COUNT(*) AS SessionCount FROM dbo.ChatSessions GROUP BY State;
SELECT Role, Status, COUNT(*) AS MessageCount FROM dbo.ChatMessages GROUP BY Role, Status;
SELECT TOP (20) EscalationId, SessionId, CreatedAt, DATALENGTH(TranscriptJson) AS TranscriptSqlBytes
FROM dbo.ChatEscalations ORDER BY CreatedAt DESC;