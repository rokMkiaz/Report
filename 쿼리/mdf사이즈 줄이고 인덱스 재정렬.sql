-- 1단계: 논리적 파일 이름 확인
-- DB의 논리적 파일 이름(Logical File Name)을 확인합니다. 
-- MDF 파일은 보통 '데이터베이스이름' 또는 '데이터베이스이름_Data'와 같습니다.
SELECT 
    name, 
    physical_name AS CurrentLocation, 
    size * 8 / 1024 AS CurrentSizeMB
FROM 
    sys.database_files;

-- 2단계: 파일 축소 실행
-- '논리적파일이름'과 줄이고 싶은 파일의 목표 크기(MB)를 입력합니다.
USE csb_world_20; -- 대상 데이터베이스로 전환
GO
DBCC SHRINKFILE (N'csb_world_20', 8000); 
GO


SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    (dps.avg_fragmentation_in_percent) AS Fragmentation
FROM
    sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'SAMPLED') dps
INNER JOIN
    sys.tables t ON dps.object_id = t.object_id
INNER JOIN
    sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN
    sys.indexes i ON dps.object_id = i.object_id AND dps.index_id = i.index_id
WHERE
    dps.database_id = DB_ID()
    AND dps.avg_fragmentation_in_percent > 10 -- 10% 이상 조각난 인덱스만 보기
ORDER BY
    Fragmentation DESC;


-- 2. 인덱스 재정렬/재구성 실행 (예시)
-- 조각화가 5%~30%인 경우: REORGANIZE (재정렬)
-- 조각화가 30% 이상인 경우: REBUILD (재구성)
ALTER INDEX PK_TBlindPointList ON TBlindPointList REORGANIZE; 
-- 또는
ALTER INDEX [인덱스이름] ON [테이블이름] REBUILD;