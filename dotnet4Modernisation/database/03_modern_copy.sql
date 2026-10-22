/*
    Copy ApexCounter onto ApexCounter_Modern.

    The legacy app keeps writing to ApexCounter. The transformed .NET 8 app
    writes to the copy, so a repair run cannot change the ground truth it is
    being compared with. Run this after 02_seed.sql. reset.ps1 does that.
*/
IF DB_ID(N'ApexCounter') IS NULL
BEGIN
    RAISERROR('ApexCounter does not exist. Run 01_schema.sql and 02_seed.sql first.', 16, 1);
    RETURN;
END
GO

IF DB_ID(N'ApexCounter_Modern') IS NOT NULL
BEGIN
    ALTER DATABASE ApexCounter_Modern SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ApexCounter_Modern;
END
GO

DECLARE @dir nvarchar(400) = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(400));
IF @dir IS NULL OR LEN(@dir) = 0
BEGIN
    RAISERROR('SQL Server did not report InstanceDefaultDataPath.', 16, 1);
    RETURN;
END
IF RIGHT(@dir, 1) <> '\' SET @dir = @dir + '\';

DECLARE @bak nvarchar(500) = @dir + N'ApexCounter_copy.bak';
DECLARE @data nvarchar(500) = @dir + N'ApexCounter_Modern.mdf';
DECLARE @log nvarchar(500) = @dir + N'ApexCounter_Modern_log.ldf';

BACKUP DATABASE ApexCounter TO DISK = @bak WITH INIT, COPY_ONLY;

RESTORE DATABASE ApexCounter_Modern FROM DISK = @bak
    WITH MOVE N'ApexCounter' TO @data,
         MOVE N'ApexCounter_log' TO @log,
         REPLACE;
GO
