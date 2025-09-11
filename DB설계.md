
## RowData관리

### 1.RowData Save를 위한 Tvp(Table-Valued Parameter)구조 

1. Data를 유지보수하는데, 과도한 프로시저 호출은 결국 성능저하를 가져온다.
2. 과도한 호출을 피하며, 한번에 넘겨 쿼리로 관리하기위해 Tvp구조의 개발이 필요해 졌다.

#### 1-1. 기존 Server내 Tvp호출을 위한 구조 개선
그동안 Row, Param으로만 호출되던 구조를 개선하는것이 목표다.
```ruby
/*------------- CDBBinding.h -------------*/
#pragma once

#include <unknwn.h>    // IUnknown
#include <oledb.h>     // DBBINDING, DBBINDSTATUS, etc.

#define MAX_DB_BINDING 32

class CDBBinding
{
public:
    CDBBinding();
    ~CDBBinding();

    // 기존 파라미터/로우셋 바인딩
    bool Bind(DBPARAMIOENUM DBParamIO, DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset);

public:
    unsigned int    m_uiBindingCount;
    DBBINDING       m_DBBinding[MAX_DB_BINDING];
    DBBINDSTATUS    m_DBBindStatus[MAX_DB_BINDING];
};

/*------------- CDBBinding.cpp -------------*/
#include "CDBBinding.h"

CDBBinding::CDBBinding()
    : m_uiBindingCount(0)
{
}

CDBBinding::~CDBBinding()
{
}

bool CDBBinding::Bind(DBPARAMIOENUM DBParamIO, DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset)
{
    if (m_uiBindingCount >= MAX_DB_BINDING)
        return false;
    DBBINDING& b = m_DBBinding[m_uiBindingCount];
    b.iOrdinal    = m_uiBindingCount + 1;
    b.obValue     = uiOffset;
    b.obLength    = uiOffset + uiSize;
    b.dwPart      = DBPART_VALUE | DBPART_LENGTH | DBPART_STATUS;
    b.wType       = (DBType & ~DBTYPE_BYREF);
    b.cbMaxLen    = uiSize;
    b.dwMemOwner  = DBMEMOWNER_CLIENTOWNED;
    b.eParamIO    = DBParamIO;
    m_DBBindStatus[m_uiBindingCount] = DBBINDSTATUS_OK;
    ++m_uiBindingCount;
    return true;
}

/*--------- CStoredProcedure.h ---------*/
#pragma once
#include "CDBBinding.h"
#include <ocidl.h>    // ICommandText, IAccessor, IRowset

class CStoredProcedure
{
public:
    CStoredProcedure();
    ~CStoredProcedure();

    bool Init(CDBConnection* pDBConnection, WCHAR* wCmdString);
    void Close();

    bool AddParamBinding(DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset, bool bOutputParam=false);
    bool AddRowsetBinding(DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset);

    bool Execute(void* pParam, void* pRowset);

public:
    CDBBinding*     m_pParamBinding;
    CDBBinding*     m_pRowsetBinding;
    ICommandText*   m_pICommandText;
    IAccessor*      m_pIAccessor;
    HACCESSOR       m_hAccessor;
    IRowset*        m_pIRowset;
};

/*-------- CStoredProcedure.cpp --------*/
#include "CStoredProcedure.h"
#include "CDBConnection.h"
#include <cstdio>

bool CStoredProcedure::AddParamBinding(DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset, bool bOutputParam)
{
    if (!m_pParamBinding)
        m_pParamBinding = new CDBBinding;
    return m_pParamBinding->Bind(
        bOutputParam ? DBPARAMIO_OUTPUT : DBPARAMIO_INPUT,
        DBType, uiSize, uiOffset
    );
}

bool CStoredProcedure::AddRowsetBinding(DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset)
{
    if (!m_pRowsetBinding)
        m_pRowsetBinding = new CDBBinding;
    return m_pRowsetBinding->Bind(
        DBPARAMIO_NOTPARAM,
        DBType, uiSize, uiOffset
    );
}

bool CStoredProcedure::Execute(void* pParam, void* pRowset)
{
    DBPARAMS Params = {0};
    DBROWCOUNT cNumRows = 0;

    Params.pData      = pParam ? pParam : this;
    Params.cParamSets = 1;
    Params.hAccessor  = m_hAccessor;

    HRESULT hr = m_pICommandText->Execute(
        nullptr, IID_IRowset, &Params, &cNumRows,
        reinterpret_cast<IUnknown**>(&m_pIRowset)
    );
    if (FAILED(hr)) return false;
    return true;
}
```
코드를 GPT를 사용해 개선을 해보았다. 그러나 사용되지 않았는데, 분명 버전은 지원하나 문제가 하나 더 있었다.
<img width="645" height="328" alt="image" src="https://github.com/user-attachments/assets/06b9f0cf-69b9-4992-b759-da61294732e9" />

기나긴 대화 끝에 결론은 구버전 라이브러리를 사용중이어서 생긴 문제였다. 기존 코드를 업데이트가 불가능한 상황이라, 다른 방법을 찾아봐야 했다.
#### 1-2. 효율적이며 적합한 쿼리문 제작
MERGE,Type를 활용한 일괄적인 관리를 지향하였으나, 상단의 문제로 사용을 못 하게 되었다..
```ruby
-- 1) 먼저 TVP 형식 정의
CREATE TYPE dbo.InventoryItemType AS TABLE(
  ItemUnique BIGINT,
  Slot       INT,
  ItemIndex  INT,
  Option1    INT,
  Option2    INT
  /*…*/
);
GO

-- 2) MERGE 기반 프로시저
CREATE PROCEDURE dbo.usp_SaveInventory
  @CharUnique INT,
  @Items      dbo.InventoryItemType READONLY
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRAN;

  MERGE INTO TInventoryItem AS target
  USING @Items        AS src
    ON target.ItemUnique = src.ItemUnique
  WHEN MATCHED AND target.CharUnique = @CharUnique
    THEN UPDATE
      SET Slot      = src.Slot
        , ItemIndex = src.ItemIndex
        , Option1   = src.Option1
        , Option2   = src.Option2
        , ModifiedAt= SYSUTCDATETIME()
  WHEN NOT MATCHED BY TARGET
    THEN INSERT(ItemUnique, CharUnique, Slot, ItemIndex, Option1, Option2, CreatedAt, ModifiedAt)
         VALUES(src.ItemUnique, @CharUnique, src.Slot, src.ItemIndex, src.Option1, src.Option2, SYSUTCDATETIME(), SYSUTCDATETIME())
  WHEN NOT MATCHED BY SOURCE 
       AND target.CharUnique = @CharUnique
    THEN DELETE  -- 슬롯에서 빠진(삭제된) 아이템 제거

  COMMIT;
END;
```

### 2. Json 타입 입력방식
초기에 나왔던 2가지 방식중 Tvp구조가 적용이 힘들어 조금 더 쉬운 방식으로 접근을 하였다. 대규모 입력은 Nvarchar(Max)로 하나의 변수로 받지만, 키값을 매핑하고있어 대규모 테이블 자체를 입력이 가능하다는 장점이 있다. 쿼리 1회 호출로 몇백의 데이터를 동시에 Insert & Update를 호출한다.

#### 2-1. 코드 구현
기존 데이터를 Json형태로 입력가능하게 바꿔주는 부분이 필요해 졌다. 해당부분 역시 AI를 활용해 컨테이너의 형태로 만드는게 사용하기 쉬워 보였다.
```ruby

// -----------------------------------------------------------------------------
// 1) JsonFields<T> primary template: 기본적으로 비어있으므로, 특수화하지 않은 T에 대해서는
//    컴파일 에러가 발생하도록 됩니다.
// -----------------------------------------------------------------------------
template<typename T>
struct JsonFields {
    // 반드시 T타입에 대해 아래 메서드를 특수화해 주세요!
    static void AppendFields(const T& /*obj*/, wstringstream& /*ss*/) {
        static_assert(sizeof(T) == 0,
            "JsonFields<T> must be specialized for your type T");
    }
};

//-----------------------------------------------------------------------------
// 2) BuildJson 
//    - Type 은 iterator_traits 로 자동 유추
//    - itemFmt, item._i64unique 등 이전 파라미터는 전혀 필요 없습니다.
//-----------------------------------------------------------------------------   
template<typename T>
wstring BuildJson(const vector<T>& rows, string pProcName, INT32 KeyVal1, INT32 KeyVal2) {
    wstringstream ss;
    ss.clear();
    ss << L"[";
    bool firstRow = true;

    for (auto const& r : rows) {
        if (!firstRow) ss << L",";
        firstRow = false;
        ss << L"{";
        JsonFields<T>::AppendFields(r, ss);
        ss << L"}";
    }

    ss << L"]";

    if (ss.str().size() >= MAX_JSON_LEN) {
        // 길이 제한 체크 - 에러 로그를 파일로 출력
        char exePath[MAX_PATH];
        GetModuleFileNameA(NULL, exePath, MAX_PATH);
        char* lastSlash = strrchr(exePath, '\\');
        if (lastSlash) *(lastSlash + 1) = '\0';
        
        char logPath[MAX_PATH];
        sprintf(logPath, "%sJsonErrorLog\\json_error_log_%s_%d_%d.txt", exePath, pProcName.c_str(), KeyVal1, KeyVal2);
        
        // 디렉토리 생성
        char dirPath[MAX_PATH];
        sprintf(dirPath, "%sJsonErrorLog", exePath);
        CreateDirectoryA(dirPath, NULL);
        
        FILE* errorFile = fopen(logPath, "a");
        if (errorFile) {
            fprintf(errorFile, "[%s] MAX_JSONSTRING_ERROR::SAVE_FAILED - JSON size: %zu, MAX_JSON_LEN: %d\n", 
                    __TIMESTAMP__, ss.str().size(), MAX_JSON_LEN);
            fprintf(errorFile, "JSON Content:\n%ls\n\n", ss.str().c_str());
            fclose(errorFile);
        }
        printf("MAX_JSONSTRING_ERROR::SAVE_FAILED\n");
        return L"";
    }
    return ss.str();
}

```
- 초기구조에 추가적으로 Json을 만들다 실패할 시 문서로 만드는 로직을 추가
- 글자 수는 최대 2만자로 제한, 데이터는 1회 50개의 RowData를 입력가능하게 조정.

```ruby
ALTER PROCEDURE [dbo].[gp_save_pc_inventory]
       @CharUnique INT,
       @JsonItems  NVARCHAR(MAX)
   AS
   BEGIN
       SET NOCOUNT ON;
       BEGIN TRANSACTION;

       -- Parse JSON array into rows
       MERGE dbo.TInventory AS target
       USING (
           SELECT
               JSON_VALUE(value, '$.a')  AS Inventoryslot,
               JSON_VALUE(value, '$.b')  AS ItemUnique,
               JSON_VALUE(value, '$.c')  AS ItemIndex,
               JSON_VALUE(value, '$.d')  AS lock,
               JSON_VALUE(value, '$.e' ) AS Identity1,
               JSON_VALUE(value, '$.f')  AS Identity2,
               JSON_VALUE(value, '$.g')  AS Identity3,
               JSON_VALUE(value, '$.h')  AS ItemCount,
               JSON_VALUE(value, '$.i')  AS BoundType,
               JSON_VALUE(value, '$.j')  AS Hitrate,
               JSON_VALUE(value, '$.k')  AS ItemDurability,
               JSON_VALUE(value, '$.l')  AS Elemental,
               JSON_VALUE(value, '$.n')  AS ElementalVal,
               JSON_VALUE(value, '$.m')  AS Enchant,
               JSON_VALUE(value, '$.o')  AS Reforge,
               JSON_VALUE(value, '$.p')  AS Quality,
               JSON_VALUE(value, '$.q')  AS DelTimeStamp
           FROM OPENJSON(@JsonItems)
       ) AS src
         ON target.CharUnique    = @CharUnique
        AND target.Inventoryslot = src.Inventoryslot
       WHEN MATCHED THEN
         UPDATE SET
           target.Item_Unique = src.ItemUnique,
           target.Item_Index  = src.ItemIndex,
           target.[Item_Identity1]    = src.Identity1 ,
        target.[Item_Identity2]    = src.Identity2  ,
        target.[Item_Identity3]    = src.Identity3  ,
        target.[Item_Count]			= src.ItemCount,
        target.[Item_BoundType]		= src.BoundType,
        target.[Item_Elemental]      = src.Elemental,
        target.[Item_Elemental_Value]= src.ElementalVal,
        target.[Item_HitRate]		= src.Hitrate,
        target.[Item_Lock]			= src.lock,
        target.[Item_Durabillity]	= src.ItemDurability,
        target.[Item_Enachant]		= src.Enchant,
        target.[Item_Reforge]		= src.Reforge,
           target.[Item_Quality]     = src.Quality,
        target.[Item_DelTimeStamp]   = src.DelTimeStamp


       WHEN NOT MATCHED BY TARGET THEN
         INSERT (CharUnique,
         Inventoryslot,
         Item_Unique, 
         Item_Index,
         [Item_Identity1],
         [Item_Identity2],
         [Item_Identity3],
         [Item_Count],
         [Item_BoundType],
         [Item_Elemental],
         [Item_Elemental_Value],
         [Item_HitRate],
         [Item_Lock],
         [Item_Durabillity],
         [Item_Enachant],
         [Item_Reforge],
         [Item_Quality],
         [Item_DelTimeStamp])
         VALUES (@CharUnique, 
         src.Inventoryslot,
         src.ItemUnique,
         src.ItemIndex,
         src.Identity1,
         src.Identity2,
         src.Identity3,
         src.ItemCount,
         src.BoundType,
         src.Elemental,
         src.ElementalVal,
         src.Hitrate,
         src.lock,
         src.ItemDurability,
         src.Enchant,
         src.Reforge,
         src.Quality,
         src.DelTimeStamp);

       COMMIT;
   END;
```
<img width="1524" height="372" alt="image" src="https://github.com/user-attachments/assets/befdb93c-6b80-4278-aac0-7419dc3a6df4" />

- SaveInventory의 예시이다. upsert를 활용하여 데이터의 유실을 최소화 하였고, Json입력을 통해 서버와 테이블의 형식으로 데이터를 주고 받는다.
- 저장속도 측정은 3500만의 데이터가 있을 경우 랜덤한 옵션값으로 500 칸을 업데이트하는데 80ms의 속도가 나왔다. Binary방식에 비해 속도는 빠르지만 좀 더 개선할 방법이 필요해 보였다.

### 3. 호출 간소화 작업
코드
```ruby
template <typename T>
class MemoryPool {
private:
	vector<unique_ptr<T>> m_pool;
	vector<T*> m_free_list;

	MemoryPool(size_t initial_size = 10) {
		m_pool.reserve(initial_size);
		for (size_t i = 0; i < initial_size; ++i) {
			m_pool.push_back(std::make_unique<T>());
			m_free_list.push_back(m_pool.back().get());
		}
	}

	// 복사 생성자 및 대입 연산자 비활성화
	MemoryPool(const MemoryPool&) = delete;
	MemoryPool& operator=(const MemoryPool&) = delete;

public:
	static MemoryPool& GetInstance() {
		static MemoryPool instance;
		return instance;
	}

	T* Acquire() {
		if (m_free_list.empty()) {
			printf("AddMemory!!\n");
			// 풀이 비었으면 새로 할당
			size_t current_size = m_pool.size();
			m_pool.reserve(current_size + 10);
			for (size_t i = 0; i < 10; ++i) {
				m_pool.push_back(std::make_unique<T>());
				m_free_list.push_back(m_pool.back().get());
			}
		}
		T* obj = m_free_list.back();
		m_free_list.pop_back();
		return obj;
	}

	void Release(T* obj) {
		if (obj) {
			// 사용이 끝난 객체를 풀에 반환
			m_free_list.push_back(obj);
		}
	}

	size_t GetPoolSize() const {
		return m_pool.size();
	}
};


template<typename ProcedureType, typename ParamType,  typename RowType >
void SaveDataInChunks(
	/*오류시 저장유니크 없을시 0*/DWORD dwCharunique,
	/*오류시 저장유니크 없을시 0*/ DWORD dwAccunique,
	/*데이터 최대 입력갯수*/INT32 maxCount, 
	/*데이터 묶음 갯수*/INT32 chunkSize,
	/*오류시 제목*/const char* jsonTypeName,
	/*데이터 저장조건*/function<bool(RowType&, INT32)>dataProcessor,
	/*마지막 파람에 넣을 조건*/function<void(ParamType*, const wstring&)> dataParam)
{
	INT32 index = 0;
	vector<RowType> buffer;
	ProcedureType* procedure = ProcedureType::GetInstance();
	//ParamType* params = new ParamType();
	ParamType* params = MemoryPool<ParamType>::GetInstance().Acquire();
	buffer.reserve(chunkSize);

	while (index < maxCount) {
		memset(params, 0, sizeof(ParamType));
		INT32 taken = 0;
		buffer.clear();

		// 청크 단위로 처리
		while (taken < chunkSize && index < maxCount) {
			RowType row;
			memset(&row, 0x00, sizeof(RowType));

			// 사용자 정의 데이터 처리 로직
			if (dataProcessor(row, index)) {
				buffer.push_back(row);
			}
			++taken;
			++index;
		}

		if (buffer.empty()) continue;

		wstring json = L"";

		// JSON 빌드 및 실행
		if (dwAccunique != 0)
		{
			json = BuildJson(buffer, jsonTypeName, dwAccunique, index);
		}
		else if (dwCharunique != 0)
		{
			json = BuildJson(buffer, jsonTypeName, dwCharunique, index);
		}


		// 사용자 정의 파라미터 설정 로직
		dataParam(params, json);

		if (!procedure->Execute(params, nullptr)) {
			printf("%s::Execute() Failed.\n",typeid(ProcedureType).name());
		}
		procedure->ReleaseDBRecords();
	}
	MemoryPool<ParamType>::GetInstance().Release(params);
	//delete params;
}

```
- 기존에는 코드를 호출할 시 굉장히 많은 절차가 필요하였는데, 빼먹거나 수정해야하는 부분에서 실수가 일어날 수 있었다. 그러므로 템플릿을 활용하여 하나로 처리하였다.
<details>
<summary>기존 호출</summary>
    
```ruby
        int i32dx = 0;
		vector<_PROCEDURE_SAVE_INVENTORY_ROW> buffer;
		CProcedure_Save_Inventory* pSaveInven = CProcedure_Save_Inventory::GetInstance();
		_PROCEDURE_SAVE_INVENTORY_PARAM* pInvenparams = new _PROCEDURE_SAVE_INVENTORY_PARAM();
		buffer.reserve(JSON_CHUNK_SIZE_50);
		
		while (i32dx < MAX_INVENTORY_TOTAL_COUNT) 
		{
			memset(pInvenparams, 0x00, sizeof(_PROCEDURE_SAVE_INVENTORY_PARAM));
			buffer.clear();
			int i32taken = 0;
		
		
			// 50개 단위로 Push
			while (i32taken < JSON_CHUNK_SIZE_50 && i32dx < MAX_INVENTORY_TOTAL_COUNT) {
		
				_PROCEDURE_SAVE_INVENTORY_ROW row;
				memset(&row, 0x00, sizeof(_PROCEDURE_SAVE_INVENTORY_ROW));
				row.i32Slot = i32dx;
				memcpy(&row.info, &pMsg->Inventory[i32dx], sizeof(ItemInfo));
				buffer.push_back(row);
				++i32taken;
				++i32dx;
			}
			if (buffer.empty()) continue;
		
			// Build JSON and prepare parameters
			wstring json = BuildJson(buffer,"Inventory", pMsg->m_CharInfo.dwCharunique, i32dx);
			pInvenparams->dwCharunique = pMsg->m_CharInfo.dwCharunique;
			memcpy(pInvenparams->wcJson, json.c_str(), (json.size() + 1) * sizeof(WCHAR));
		
			// Execute and release
			if (!pSaveInven->Execute(pInvenparams, nullptr))
			{
				printf("CProcedure_Save_Inventory::Execute() Failed.\n");
			}
			pSaveInven->ReleaseDBRecords();
		}
		
		delete pInvenparams;
		pInvenparams = NULL;
```
</details>
- 이러한 실수를 막기 위해 동적할당하는 부분을 템플릿에 넣었고, 추가적으로 메모리 풀링을 사용하여 메모리 최적화를 하였다.
<details>
<summary>신규 호출</summary>
    
```ruby
		SaveDataInChunks<CProcedure_Save_Inventory, _PROCEDURE_SAVE_INVENTORY_PARAM,_PROCEDURE_SAVE_INVENTORY_ROW >
			(pMsg->m_CharInfo.dwCharunique, pMsg->m_dwAccUnique, MAX_INVENTORY_TOTAL_COUNT, JSON_CHUNK_SIZE_50, "Inventory",
				[&](_PROCEDURE_SAVE_INVENTORY_ROW& row, INT32 index) -> bool {
					//초기화
					row.i32Slot = index;
					memcpy(&row.info, &pMsg->Inventory[index], sizeof(ItemInfo));

					return true;  // 버퍼에 추가
				},
				[&](_PROCEDURE_SAVE_INVENTORY_PARAM* params, const wstring& json) -> void {
					params->dwCharunique = pMsg->m_CharInfo.dwCharunique;
					memcpy(params->wcJson, json.c_str(), (json.size() + 1) * sizeof(WCHAR));
				}
				);
```
</details>
