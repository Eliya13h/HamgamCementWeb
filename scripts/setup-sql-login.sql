-- Grant SQL Server access to IIS Application Pool identities (Cement + Transport)
-- Run in SSMS as sysadmin
-- Folders: C:\inetpub\Cement  |  C:\inetpub\Transport
-- App pools: Cement  |  Transport

USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'IIS APPPOOL\Cement')
    CREATE LOGIN [IIS APPPOOL\Cement] FROM WINDOWS;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'IIS APPPOOL\Transport')
    CREATE LOGIN [IIS APPPOOL\Transport] FROM WINDOWS;
GO

-- Cement -> HamgamNimroz
IF DB_ID(N'HamgamNimroz') IS NULL
    RAISERROR('Database HamgamNimroz does not exist. Create it first.', 16, 1);
GO
USE [HamgamNimroz];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'IIS APPPOOL\Cement')
    CREATE USER [IIS APPPOOL\Cement] FOR LOGIN [IIS APPPOOL\Cement];
GO
ALTER ROLE [db_owner] ADD MEMBER [IIS APPPOOL\Cement];
GO

-- Transport -> HamgamTransport
IF DB_ID(N'HamgamTransport') IS NULL
    RAISERROR('Database HamgamTransport does not exist. Create it first.', 16, 1);
GO
USE [HamgamTransport];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'IIS APPPOOL\Transport')
    CREATE USER [IIS APPPOOL\Transport] FOR LOGIN [IIS APPPOOL\Transport];
GO
ALTER ROLE [db_owner] ADD MEMBER [IIS APPPOOL\Transport];
GO

-- Shared reference DB (optional)
USE [master];
GO
IF DB_ID(N'HamgamReference') IS NULL
    PRINT 'WARNING: HamgamReference does not exist yet (optional until currency sync is used).';
ELSE
BEGIN
    DECLARE @sql nvarchar(max) = N'
USE [HamgamReference];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N''IIS APPPOOL\Cement'')
    CREATE USER [IIS APPPOOL\Cement] FOR LOGIN [IIS APPPOOL\Cement];
ALTER ROLE [db_owner] ADD MEMBER [IIS APPPOOL\Cement];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N''IIS APPPOOL\Transport'')
    CREATE USER [IIS APPPOOL\Transport] FOR LOGIN [IIS APPPOOL\Transport];
ALTER ROLE [db_owner] ADD MEMBER [IIS APPPOOL\Transport];
PRINT ''Both pools granted on HamgamReference'';
';
    EXEC sys.sp_executesql @sql;
END
GO

PRINT 'Done. Restart pools: Restart-WebAppPool Cement ; Restart-WebAppPool Transport';
GO
