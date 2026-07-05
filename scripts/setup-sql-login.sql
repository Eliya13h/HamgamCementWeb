-- Grant SQL Server access to IIS Application Pool identity
-- Run in SQL Server Management Studio (SSMS) as sysadmin
-- Application Pool name must match: HamgamCementWeb

USE [master];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_principals
    WHERE name = N'IIS APPPOOL\HamgamCementWeb'
)
BEGIN
    CREATE LOGIN [IIS APPPOOL\HamgamCementWeb] FROM WINDOWS;
    PRINT 'Login created: IIS APPPOOL\HamgamCementWeb';
END
ELSE
BEGIN
    PRINT 'Login already exists: IIS APPPOOL\HamgamCementWeb';
END
GO

IF DB_ID(N'HamgamNimroz') IS NULL
BEGIN
    RAISERROR('Database HamgamNimroz does not exist. Create it and run EF migrations first.', 16, 1);
    RETURN;
END
GO

USE [HamgamNimroz];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.database_principals
    WHERE name = N'IIS APPPOOL\HamgamCementWeb'
)
BEGIN
    CREATE USER [IIS APPPOOL\HamgamCementWeb] FOR LOGIN [IIS APPPOOL\HamgamCementWeb];
    PRINT 'User created in HamgamNimroz';
END
ELSE
BEGIN
    PRINT 'User already exists in HamgamNimroz';
END
GO

ALTER ROLE [db_owner] ADD MEMBER [IIS APPPOOL\HamgamCementWeb];
GO

PRINT 'Done. Restart IIS app pool: Restart-WebAppPool -Name HamgamCementWeb';
GO
