/* ====== 설정 ====== */
DECLARE @BakDir  NVARCHAR(260) = N'D:\Work_1003bm\260122_CS\Backup\';        -- .bak 있는 폴더 (마지막 \ 포함)
DECLARE @DataDir NVARCHAR(260) = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\';     -- mdf 저장 폴더 /알아서 변경
DECLARE @LogDir  NVARCHAR(260) = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\';      -- ldf 저장 폴더 / 알아서 변경
DECLARE @Pattern NVARCHAR(100) = N'csb_trade_*.bak';     -- 대상 패턴

/* ====== xp_cmdshell 켜기 (권한 필요) ====== */
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;

/* ====== 1) 파일 목록 수집 ====== */
IF OBJECT_ID('tempdb..#files') IS NOT NULL DROP TABLE #files;
CREATE TABLE #files (line NVARCHAR(4000));

DECLARE @cmd NVARCHAR(4000) =
    N'cmd /c dir /b "' + @BakDir + @Pattern + N'"';

INSERT INTO #files(line)
EXEC xp_cmdshell @cmd;

DELETE FROM #files
WHERE line IS NULL OR line = N'' OR line LIKE N'File Not Found%';

/* ====== 2) 파일별로 RESTORE ====== */
DECLARE @file NVARCHAR(4000),
        @db   SYSNAME,
        @bak  NVARCHAR(4000);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT line FROM #files ORDER BY line;

OPEN cur;
FETCH NEXT FROM cur INTO @file;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- 파일명에서 확장자 제거 => DB명으로 사용 (csb_game_11.bak -> csb_game_11)
    SET @db  = REPLACE(@file, N'.bak', N'');
    SET @db  = REPLACE(@db, N'.BAK', N'');
    SET @bak = @BakDir + @file;

    /* --- 2-1) 해당 bak의 논리 파일명 가져오기 --- */
    IF OBJECT_ID('tempdb..#fl') IS NOT NULL DROP TABLE #fl;
    CREATE TABLE #fl (
        LogicalName NVARCHAR(128),
        PhysicalName NVARCHAR(260),
        [Type] CHAR(1),
        FileGroupName NVARCHAR(128) NULL,
        Size BIGINT,
        MaxSize BIGINT,
        FileId INT,
        CreateLSN NUMERIC(25,0) NULL,
        DropLSN NUMERIC(25,0) NULL,
        UniqueId UNIQUEIDENTIFIER NULL,
        ReadOnlyLSN NUMERIC(25,0) NULL,
        ReadWriteLSN NUMERIC(25,0) NULL,
        BackupSizeInBytes BIGINT NULL,
        SourceBlockSize INT NULL,
        FileGroupId INT NULL,
        LogGroupGUID UNIQUEIDENTIFIER NULL,
        DifferentialBaseLSN NUMERIC(25,0) NULL,
        DifferentialBaseGUID UNIQUEIDENTIFIER NULL,
        IsReadOnly BIT NULL,
        IsPresent BIT NULL,
        TDEThumbprint VARBINARY(32) NULL,
        SnapshotUrl NVARCHAR(360) NULL
    );

    DECLARE @sql NVARCHAR(MAX);

    SET @sql = N'RESTORE FILELISTONLY FROM DISK = N''' + REPLACE(@bak,'''','''''') + N''';';
    INSERT INTO #fl EXEC (@sql);

    DECLARE @logicalData NVARCHAR(128) = (SELECT TOP 1 LogicalName FROM #fl WHERE [Type] = 'D' ORDER BY FileId);
    DECLARE @logicalLog  NVARCHAR(128) = (SELECT TOP 1 LogicalName FROM #fl WHERE [Type] = 'L' ORDER BY FileId);

    /* --- 2-2) 복원 실행 (기존 DB 있으면 덮어씀) --- */
    SET @sql = N'
    RESTORE DATABASE [' + REPLACE(@db,']',']]') + N']
    FROM DISK = N''' + REPLACE(@bak,'''','''''') + N'''
    WITH
    REPLACE,
    RECOVERY,
    MOVE N''' + REPLACE(@logicalData,'''','''''') + N''' TO N''' + REPLACE(@DataDir,'''','''''') + REPLACE(@db,'''','''''') + N'.mdf'',
    MOVE N''' + REPLACE(@logicalLog,'''','''''')  + N''' TO N''' + REPLACE(@LogDir,'''','''''')  + REPLACE(@db,'''','''''') + N'_log.ldf'';
';
    PRINT @sql;   -- 실제 실행될 내용 확인용
    EXEC (@sql);

    FETCH NEXT FROM cur INTO @file;
END

CLOSE cur;
DEALLOCATE cur;
