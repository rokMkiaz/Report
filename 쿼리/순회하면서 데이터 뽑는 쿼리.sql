DECLARE @i INT = 11;--10 --순회 시작할 그룹번호
DECLARE @db SYSNAME;
DECLARE @sql NVARCHAR(MAX);

WHILE @i <= 35
BEGIN
    SET @db = CONCAT(N'csb_game_', @i); --DB이름 변경

    IF DB_ID(@db) IS NOT NULL
    BEGIN
        /* 1) TInventory -> (groupserver, charunique, items) */
        SET @sql = N'
        INSERT INTO [TEST].[dbo].[260122_CSItems]
            (groupserver, charunique, accunique, items)
        SELECT
              ' + CAST(@i AS NVARCHAR(10)) + N' AS groupserver
            , I.charunique
            , NULL AS accunique
            , I.items
        FROM ' + QUOTENAME(@db) + N'.dbo.TInventory AS I WITH (NOLOCK);
        ';

        EXEC sp_executesql @sql;
        PRINT CONCAT(@db, ' : Inventory OK');

        /* 2) TStorage -> (groupserver, accunique) */
        SET @sql = N'
        INSERT INTO [TEST].[dbo].[260122_CSItems]
            (groupserver, charunique, accunique, items)
        SELECT
              ' + CAST(@i AS NVARCHAR(10)) + N' AS groupserver
            , NULL AS charunique
            , S.accunique
            , S.storage
        FROM ' + QUOTENAME(@db) + N'.dbo.TStorage AS S WITH (NOLOCK);
        ';

        EXEC sp_executesql @sql;
        PRINT CONCAT(@db, ' : Storage OK');
    END
    ELSE
    BEGIN
        PRINT CONCAT(@db, ' : DB NOT FOUND');
    END

     SET @db = CONCAT(N'csb_trade_', @i);  --DB이름 변경

    IF DB_ID(@db) IS NOT NULL
    BEGIN
        SET @sql = N'
        INSERT INTO [TEST].[dbo].[260122_CSItems_2]
            (groupserver, charunique, accunique, item_save_info)
        SELECT
              ' + CAST(@i AS NVARCHAR(10)) + N' AS groupserver
            , I.charunique
            , I.accunique
            , I.item_save_info
        FROM ' + QUOTENAME(@db) + N'.dbo.TTrade AS I WITH (NOLOCK)
        WHERE I.itemindex BETWEEN 1191 AND 1194;
        ';

        EXEC sp_executesql @sql;
        PRINT CONCAT(@db, ' : Trade OK');
    END
    ELSE
    BEGIN
        PRINT CONCAT(@db, ' : DB NOT FOUND');
    END


    SET @i += 1;
END
