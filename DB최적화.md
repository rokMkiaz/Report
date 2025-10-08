
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
- 이러한 실수를 막기 위해 동적할당하는 부분을 템플릿에 넣었고, 추가적으로 메모리 풀링을 사용하여 메모리 최적화를 하였다. 호출을 단순화 하였고, 성능도 개선을 하였다.
<details>
<summary>기존 호출 방식</summary>
    
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


<details>
<summary>개선된 호출 방식</summary>
    
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


### 4. 프로시저 최적화 작업
Binary-> RowData로 변경하는 과정에서 속도가 느리니 적용이 불가하다는 판정을 받았다. 그러하기에 개선을 더 해보고자 한다.

#### 4-1. 우선적으로 OLEDB 연결을 호출하는데, 시간이 소요되는 것으로 확인 하였다.
기존 연결 방식에서 여러 문제가 발생하여 개선작업을 진행하는 것이 목적이다. 해당 하는 항목에 대해서 개선작업 진행만 나열하고자 한다. 개선된 RowData 저장방식의 속도는 Binary보다는 느리지만, 속도가 그렇게 문제될 정도는 아닌것으로 판된되며, 실제로 
<details>
<summary>기존 호출</summary>
    
```ruby
//Binding
#include "StdAfx.h"
#include "DBBinding.h"

CDBBinding::CDBBinding(void)
{
	m_uiBindingCount = 0;

	memset( &m_DBBinding, 0, sizeof(DBBINDING)*MAX_DB_BINDING );
	memset( &m_DBBindStatus, 0, sizeof(DBBINDSTATUS)*MAX_DB_BINDING );

	for(int i = 0; i < MAX_DB_BINDING ; i++)
	{
		m_DBBinding[i].iOrdinal = i + 1;
		m_DBBinding[i].obLength = 0;
		m_DBBinding[i].obStatus = 0;
		m_DBBinding[i].pTypeInfo = NULL;
		m_DBBinding[i].pObject = NULL;
		m_DBBinding[i].pBindExt = NULL;
		m_DBBinding[i].dwPart = DBPART_VALUE;
		m_DBBinding[i].dwMemOwner = DBMEMOWNER_CLIENTOWNED;
		m_DBBinding[i].dwFlags = 0;
		m_DBBinding[i].bScale = 0;
		m_DBBinding[1].bPrecision = 11;
	} 
}

CDBBinding::~CDBBinding(void)
{
}

bool CDBBinding::Bind( DBPARAMIOENUM DBParamIO, DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset )
{
	if( m_uiBindingCount >= MAX_DB_BINDING )
		return false;


	m_DBBinding[ m_uiBindingCount ].obValue = uiOffset;
	m_DBBinding[ m_uiBindingCount ].eParamIO = DBParamIO;
	m_DBBinding[ m_uiBindingCount ].cbMaxLen = uiSize;	
	m_DBBinding[ m_uiBindingCount ].wType = DBType;
	
	m_uiBindingCount++;

	return true;
}


```
```ruby
//Connect
#pragma warning(disable:4996)

#define DBINITCONSTANTS


#include "StdAfx.h"
#include "DBConnection.h"
#include "DBUtil.h"
#include "../../server_Common/DXUTTimer.h"
#ifndef _SQLOLEDB_H_
#include <sqloledb.h>
#endif

#include <stdio.h>

CDBConnection::CDBConnection(void)
{
	m_pIDBInitialize = NULL;
	m_pIDBProperties = NULL;
	m_pIDBCreateSession = NULL;
	m_pIDBCreateCommand = NULL;
	m_pITransaction = NULL;
	m_pICommandText = NULL;
	m_pIAccessor = NULL;
	m_pIRowset = NULL;
	m_pRows = &m_hRows[0];
	m_cNumRows = 0;

	CoInitialize( NULL );
}

CDBConnection::~CDBConnection(void)
{
	Close();

	CoUninitialize();
}


void CDBConnection::Close()
{
	if( m_pIAccessor != NULL )
	{
		m_pIAccessor->Release();
		m_pIAccessor = NULL;
	}
	

	if( m_pICommandText != NULL )
	{
		m_pICommandText->Release();
		m_pICommandText = NULL;
	}
	

	if( m_pIDBCreateCommand != NULL )
	{
		m_pIDBCreateCommand->Release();
		m_pIDBCreateCommand = NULL;
	}
	

	if( m_pIDBCreateSession != NULL )
	{
		m_pIDBCreateSession->Release();
		m_pIDBCreateSession = NULL;
	}

	if( m_pITransaction != NULL )
	{
		m_pITransaction->Release();
		m_pITransaction = NULL;
	}

	if( m_pIDBProperties != NULL )
	{
	    m_pIDBProperties->Release();
		m_pIDBProperties = NULL;
	}
	
	if( m_pIDBInitialize != NULL )
	{
		m_pIDBInitialize->Uninitialize();
		m_pIDBInitialize->Release();
		m_pIDBInitialize = NULL;
	}

}

bool CDBConnection::Connect( WCHAR *wDataSource, WCHAR *wCatalog, WCHAR *wUserID, WCHAR *wPassword )
{
	Close();


	HRESULT hr;

	hr = CoCreateInstance(  CLSID_SQLOLEDB,
							NULL,
							CLSCTX_INPROC_SERVER,
							IID_IDBInitialize,
							(void **) &m_pIDBInitialize );

	if( FAILED(hr) ) 
		return false;


	DBPROP   InitProperties[4];

	for(int i = 0; i < 4; i++)
        VariantInit(&InitProperties[i].vValue);
  


    //Server name.
    InitProperties[0].dwPropertyID  = DBPROP_INIT_DATASOURCE;
    InitProperties[0].vValue.vt     = VT_BSTR;
	InitProperties[0].vValue.bstrVal= SysAllocString( wDataSource );		//sql db ip address
    InitProperties[0].dwOptions     = DBPROPOPTIONS_REQUIRED;
    InitProperties[0].colid         = DB_NULLID;


    //Database.
    InitProperties[1].dwPropertyID  = DBPROP_INIT_CATALOG;
    InitProperties[1].vValue.vt     = VT_BSTR;
    InitProperties[1].vValue.bstrVal= SysAllocString( wCatalog );	//sql db name
	InitProperties[1].dwOptions     = DBPROPOPTIONS_REQUIRED;
    InitProperties[1].colid         = DB_NULLID; 


    //Username (login).
    InitProperties[2].dwPropertyID  = DBPROP_AUTH_USERID; 
    InitProperties[2].vValue.vt     = VT_BSTR;
	InitProperties[2].vValue.bstrVal= SysAllocString( wUserID );		//sql db account
    InitProperties[2].dwOptions     = DBPROPOPTIONS_REQUIRED;
    InitProperties[2].colid         = DB_NULLID;


    //Password.
    InitProperties[3].dwPropertyID  = DBPROP_AUTH_PASSWORD;
    InitProperties[3].vValue.vt     = VT_BSTR;
	InitProperties[3].vValue.bstrVal= SysAllocString( wPassword);	//sql db pass
    InitProperties[3].dwOptions     = DBPROPOPTIONS_REQUIRED;
    InitProperties[3].colid         = DB_NULLID;

    m_rgInitPropSet[0].guidPropertySet = DBPROPSET_DBINIT;
    m_rgInitPropSet[0].cProperties    = 4;
    m_rgInitPropSet[0].rgProperties   = InitProperties;



    hr = m_pIDBInitialize->QueryInterface(IID_IDBProperties, 
                                   (void **)&m_pIDBProperties );
	if (FAILED(hr))
	{
		DumpErrorInfo( m_pIDBInitialize, IID_IDBInitialize );
		return false;
	}


	hr = m_pIDBProperties->SetProperties(1, m_rgInitPropSet); 
	if (FAILED(hr)) 
	{
		DumpErrorInfo( m_pIDBProperties, IID_IDBProperties );
		return false;
	}


 	for(int i = 0; i < 4; i++)
		VariantClear(&InitProperties[i].vValue);

	m_pIDBProperties->Release();
	m_pIDBProperties = NULL;


	if(FAILED(m_pIDBInitialize->Initialize())) 
	{
		DumpErrorInfo( m_pIDBInitialize, IID_IDBInitialize );
		return false;
	}

	//create session
    if(FAILED(m_pIDBInitialize->QueryInterface(
                                IID_IDBCreateSession,
                                (void**) &m_pIDBCreateSession))) 
	{
		return false;
	}


	if(FAILED(m_pIDBCreateSession->CreateSession(
                                     NULL, 
                                     IID_IDBCreateCommand, 
                                     (IUnknown**) &m_pIDBCreateCommand)))  
	{
		DumpErrorInfo( m_pIDBCreateSession, IID_IDBCreateSession );
		return false;
	}



    if(FAILED(m_pIDBCreateCommand->CreateCommand(
                                    NULL, 
                                    IID_ICommandText, 
                                    (IUnknown**) &m_pICommandText)))  
	{
		DumpErrorInfo( m_pIDBCreateCommand, IID_IDBCreateCommand );
		return false;
	}


	if (FAILED(hr = m_pIDBCreateCommand->QueryInterface(IID_ITransactionLocal,
					(void**) &m_pITransaction)))   
	{
		DumpErrorInfo( m_pIDBCreateCommand, IID_IDBCreateCommand );
		return false;
	}


	return true;
}

bool CDBConnection::Connect_byGNIDBInfoFile( char *szFile, int iLine )
{
	FILE *fp = fopen( szFile, "r" ); // "r+t" );
	if( fp == NULL ) 
	{
		printf( "\n\tcannot open file(%s) ... \t", szFile );
		return false;
	}


	wchar_t		string[4096];			// 문서 줄단위 저장 변수
	wchar_t		wIP[256];	
	wchar_t		wDBName[256];	
	wchar_t		wID[256];	
	wchar_t		wPass[256];	
	wchar_t*	token;					// 토큰
	wchar_t		splitter[] = L" \n\t";	// 구분자 : /, 캐리지리턴, 탭 
	int			iLineCount = 0;

		
	while( 1 )	 // 검색 루프 
	{
		// 줄 단위로 읽기		
		if( fgetws( string, 4096 , fp ) == NULL ) 
		{
			fclose( fp );
			return false;
		}

		// 주석이 있다면 다음 줄 읽음
		if( string[ 0 ] == ';' || (0==wcscmp( string, L"\n" )) ) 
			continue;

			
		// ip
		token = wcstok( string, splitter );	
		if( token == NULL )		continue;
		wcscpy( wIP, token );

		// dbname
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wDBName, token );

		// id
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wID, token );

		// pass
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wPass, token );

		// 설정된 라인이라면 연결
		if( iLineCount == iLine )
		{
			fclose( fp );
			return Connect( wIP, wDBName, wID, wPass );
		}
		iLineCount++;
	};
	fclose( fp );

	return false;
}






bool CDBConnection::SetCommandText( ICommandText* pICommandText,WCHAR* wCmdString )
{
	if( pICommandText == NULL ) 
		return false;
	
	if( FAILED(pICommandText->SetCommandText(DBGUID_DBSQL,wCmdString)) ) 
		return false;

	return true;
}



bool CDBConnection::Execute()
{
	HRESULT hr;
	hr = m_pICommandText->Execute(NULL,
							 IID_IRowset,
							 NULL,
							 &m_cNumRows,
							 (IUnknown**)&m_pIRowset);

	if (FAILED(hr)) 
	{
		DumpErrorInfo( m_pICommandText, IID_ICommandText );
		return false;
	}

	return true;
}



bool CDBConnection::ExecuteSQL( WCHAR *wSQLText )
{
	//double dTime = g_Timer.GetTime();
	SetCommandText( m_pICommandText, wSQLText );
	//printf( "SetCommandText() Time : %f\n", g_Timer.GetTime()-dTime );


	//dTime = g_Timer.GetTime();
	bool bReturn = Execute();
	//printf( "Execute() Time : %f\n", g_Timer.GetTime()-dTime );


	return bReturn;
}


```
```ruby
//Procedure
#include "StdAfx.h"
#include "NetGlobal.h"
#include "StoredProcedure.h"
#include "DBConnection.h"
#include "DBUtil.h"
#include "../../server_Common/Global.h"
#include "../../server_Common/DXUTTimer.h"

float	CStoredProcedure::m_fStandardTime = 0;


#ifndef _ADMINPAGE_DLL	// 프로젝트 속성에서 전처리기 처리

#include "../../server_Common/CSVFile_SMS.h"
#include "../../server_Common/INIFile_Setting.h"



double		g_dSendSMSTime = 0;

void SendSMS(char *szProcedureName)	// 관리자, 프로그래머에게 문자메세지 보내기
{
	printf( "SMS SENT	11\n");
	if(0 == g_INIFile_Setting.m_iSMSState)
		return;

	// 실패한지 3분이 되지 않았다면 메세지 안보냄
	if( g_Timer.GetTime() - g_dSendSMSTime < 180.0f )
		return;
}

#endif

CStoredProcedure::CStoredProcedure(void)
{
	m_fpLogFile = NULL;
	m_pParamBinding = NULL;
	m_pRowsetBinding = NULL;
	m_hRowAccessor = NULL;
	m_hAccessor = NULL;
	m_pDBConnection = NULL;
	m_pICommandText = NULL;
	m_pIAccessor = NULL;
	m_pIRowset = NULL;
}


CStoredProcedure::~CStoredProcedure(void)
{
}

void CStoredProcedure::Close()
{
	if( m_pParamBinding )
	{
		delete m_pParamBinding;
		m_pParamBinding = NULL;
	}

	if( m_pRowsetBinding )
	{
		delete m_pRowsetBinding;
		m_pRowsetBinding = NULL;
	}

	if( m_pICommandText )
	{
		m_pICommandText->Release();
		m_pICommandText = NULL;
	}

	if( m_pIAccessor )
	{
		m_pIAccessor->Release();
		m_pIAccessor = NULL;
	}
}

bool CStoredProcedure::AddParamBinding( DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset, bool bOutputParam )
{
	if( m_pParamBinding == NULL )
	{
		m_pParamBinding = new CDBBinding;
	}

	DBPARAMIOENUM	DBParamIO;
	if( bOutputParam == true )	DBParamIO = DBPARAMIO_OUTPUT;
	else						DBParamIO = DBPARAMIO_INPUT;

	return m_pParamBinding->Bind( DBParamIO, DBType, uiSize, uiOffset );
}


bool CStoredProcedure::AddRowsetBinding( DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset )
{
	if( m_pRowsetBinding == NULL )
	{
		m_pRowsetBinding = new CDBBinding;
	}

	return m_pRowsetBinding->Bind( DBPARAMIO_NOTPARAM, DBType, uiSize, uiOffset );
}




bool CStoredProcedure::Init( CDBConnection* pDBConnection, WCHAR* wCmdString )
{
	if( pDBConnection == NULL ) 
		return false;

	WideCharToMultiByte( CP_ACP, 0, wCmdString, -1, m_szInitialString, 256, NULL, NULL );

	m_pDBConnection = pDBConnection;

    if( FAILED(pDBConnection->GetCreateCommand()->CreateCommand( 
														NULL, 
														IID_ICommandText, 
														(IUnknown**) &m_pICommandText)) )  
	{
		printf( "CreateCommand() Failed\n" );
		return false;
	}



	if( pDBConnection->SetCommandText( m_pICommandText, wCmdString ) == false ) 
	{
		printf( "SetCommandText() Failed\n" );
		return false;
	}


	return CreateAccessor();
}




bool CStoredProcedure::CreateAccessor()	// Accessor 생성
{
	if( FAILED(m_pICommandText->QueryInterface(IID_IAccessor, (void**)&(m_pIAccessor))) ) 
	{
		DumpErrorInfo( m_pICommandText, IID_ICommandText );
		return false;
	}

	if( m_pParamBinding )
	{
		if( FAILED(m_pIAccessor->CreateAccessor( DBACCESSOR_PARAMETERDATA, m_pParamBinding->m_uiBindingCount, m_pParamBinding->m_DBBinding,
			0, &m_hAccessor, m_pParamBinding->m_DBBindStatus)) )
			return false;
	}

	if( m_pRowsetBinding )
	{
		if(FAILED(m_pIAccessor->CreateAccessor( DBACCESSOR_ROWDATA, m_pRowsetBinding->m_uiBindingCount, m_pRowsetBinding->m_DBBinding, 
			0, &m_hRowAccessor, m_pRowsetBinding->m_DBBindStatus)) )
			return false;
	}

	return true;
}


bool CStoredProcedure::Execute( void *pParam, void *pRowset )	// 저장 프로시져 실행
{
	// 기준시간이 설정되어있으면 수행시간 측정
	double	dTime = 0;
//	if( m_fStandardTime > 0 )
	{
		dTime = g_Timer.GetTime();
	}


	HRESULT		hr;
	DBPARAMS	Params;
	DBROWCOUNT	cNumRows = 0;


	m_pResultRowset = pRowset;

	if( pParam == NULL )	// 파라미터를 NULL로 하면 DB함수 호출할때 에러가 나니까 그냥 이클래스를 파라미터로 하자
	{
		Params.pData = this;
	}
	else
	{
		Params.pData = pParam;
	}
	Params.cParamSets = 1;
	Params.hAccessor  = m_hAccessor;


	m_iRecordCount		= 0;
	m_iCurrentRecord	= 0;
	
/*
	if (FAILED(hr = ((ITransactionLocal*) m_pDBConnection->m_pITransaction)->StartTransaction(
				ISOLATIONLEVEL_REPEATABLEREAD, 0, NULL, NULL)))
	{
//		if( m_pDBConnection->m_pIRowset != NULL )	
//		{
//			m_pDBConnection->m_pIRowset->Release();
//			m_pDBConnection->m_pIRowset = NULL;
//		}
	
		m_pDBConnection->m_pITransaction->Abort( NULL, FALSE, FALSE );
		DebugFilePrintf( __FILE__, __LINE__, "StartTransaction FAILED	%s\n",m_szInitialString); 
		return false;
	}


*/

	//Execute the command.
	if( FAILED(hr = m_pICommandText->Execute( NULL, IID_IRowset, &Params, &cNumRows, (IUnknown **) &(m_pIRowset))) )
	{
		DumpErrorInfo( m_pICommandText, IID_ICommandText );

		m_pDBConnection->OnProcedureExecuteFailed( m_szInitialString );	// 에러날때 실행하는 함수

/*		if( m_pDBConnection->m_pIRowset != NULL )	
		{
			m_pDBConnection->m_pIRowset->Release();
			m_pDBConnection->m_pIRowset = NULL;
		}
*/
//		m_pDBConnection->m_pITransaction->Abort( NULL, FALSE, FALSE );
#ifndef _ADMINPAGE_DLL
		SendSMS(m_szInitialString);
#endif
		return false;
	}
/*
	if( m_pDBConnection->m_pIRowset != NULL )	
	{
		m_pDBConnection->m_pIRowset->Release();
		m_pDBConnection->m_pIRowset = NULL;
	}
*/
	// ROWSET 은 Execute() 호출후에 마지막에 ReleaseDBRecords() 를 호출해서 메모리 해제해야한다.
/*    if (FAILED(hr = m_pDBConnection->m_pITransaction->Commit(FALSE, XACTTC_SYNC, 0)))
    {
		if( m_pDBConnection->m_pIRowset != NULL )	
		{
			m_pDBConnection->m_pIRowset->Release();
			m_pDBConnection->m_pIRowset = NULL;
		}
		return false;
    }
*/
	// 기준시간이 설정되어있으면 수행시간 측정
	double dDiffTime = g_Timer.GetTime()-dTime;

	if( m_fStandardTime > 0 )
	{
		if( dDiffTime >= m_fStandardTime )
		{
			printf( "DB PROC TIME : %40s ... %f\n", m_szInitialString, dDiffTime );
		}
	}

	if( dDiffTime >= 0.1f )
	{
		LogFile_Printf( "DB PROC TIME : %40s ... %f\n", m_szInitialString, dDiffTime );
	}

	return true;
}


bool CStoredProcedure::GetFirstRecord()
{
	// 레코드 50개 가져오기
	IRowset *pRowset = m_pIRowset;
	if( pRowset == NULL )
		return false;


	if( FAILED( pRowset->GetNextRows( NULL, 0, 50, &m_iRecordCount, &m_pDBConnection->m_pRows)) )
	{
//		if( pRowset != NULL )	
//			pRowset->Release();
		return false;
	}
	// printf( "record count: %d\n", m_iRecordCount );




	// 첫번째 레코드 데이터 가져오기
	m_iCurrentRecord = 0;

	if( m_iRecordCount == 0 )
		return false;

	if( m_pResultRowset == NULL )
		return false;

	if(FAILED( m_pIRowset->GetData(m_pDBConnection->m_hRows[m_iCurrentRecord], m_hRowAccessor, m_pResultRowset )))
		return false;


	return true;
}



bool CStoredProcedure::GetNextRecord()
{
	if( m_iRecordCount == 0 )
		return false;

	if( m_pResultRowset == NULL )
		return false;

	m_iCurrentRecord ++;
	if( m_iCurrentRecord >= m_iRecordCount )
	{
		// 먼저 불러온 50개 레코드 릴리즈
		m_pIRowset->ReleaseRows( m_iRecordCount, m_pDBConnection->m_hRows, NULL, NULL, NULL );


		// 다음 50개 레코드 가져오기
		IRowset *pRowset = m_pIRowset;
		if( pRowset == NULL )
			return false;

		if( FAILED( pRowset->GetNextRows( NULL, 0, 50, &m_iRecordCount, &m_pDBConnection->m_pRows)) )
		{
			if( pRowset != NULL )	
				pRowset->Release();
			return false;
		}
		if( m_iRecordCount == 0 )
			return false;

		m_iCurrentRecord = 0;	// 가져온 50개 레코드 처음부터
	}


	if(FAILED( m_pIRowset->GetData(m_pDBConnection->m_hRows[m_iCurrentRecord], m_hRowAccessor, m_pResultRowset )))
		return false;

	return true;
}



void CStoredProcedure::ReleaseDBRecords()
{
 //  HRESULT hr =  m_pDBConnection->m_pITransaction->Commit(FALSE, XACTTC_SYNC, 0);
	

	if( m_pIRowset != NULL )	
	{
		m_pIRowset->Release();
		m_pIRowset = NULL;
	}
}


void CStoredProcedure::LogFile_Printf( char * strText, ... )
{
	if( m_fpLogFile == NULL )	// 파일이 없다면 새로 파일 생성
	{
		// 현재 시간
		char	szTime[256];
		time_t	currentTime = time(0);
		strftime( szTime, 256, "%Y%m%d_%H%M%S", localtime( &currentTime ) );

		char	szLogDir[256];
		sprintf( szLogDir, "%s\\log",g_Global.m_szModulePath );
		CreateDirectoryA( szLogDir, NULL ); 

		char szLogFile[256];
		sprintf( szLogFile, "%s\\log\\DB_Execute_Log_%s.txt", 
			g_Global.m_szModulePath, 
			szTime 
			);
		m_fpLogFile = fopen( szLogFile, "a" );
	}
	else
	{
		// 로그 파일 제한 크기가 지정되어 있다면 크기가 됐는지 검사
		if( m_fpLogFile )
		{
			fseek( m_fpLogFile, 0, SEEK_END );
			long lFileSize = ftell( m_fpLogFile );
			if( lFileSize >= 4096 )	
			{
				fclose( m_fpLogFile );

				// 제한 크기를 넘는다면 파일 새로 생성
				char	szTime[256];
				time_t	currentTime = time(0);
				strftime( szTime, 256, "%Y%m%d_%H%M%S", localtime( &currentTime ) );

				char szLogFile[256];
				sprintf( szLogFile, "%s\\log\\DB_Execute_Log__%s.txt", 
					g_Global.m_szModulePath, 
					szTime 
					);
				m_fpLogFile = fopen( szLogFile, "a" );
				
			}
		}
	}


	if( m_fpLogFile )
	{
		// 현재 시간
		char	szTime[256];
		time_t	currentTime = time(0);
		strftime( szTime, 256, "%Y-%m-%d %H:%M:%S", localtime( &currentTime ) );


		char strData[4096];
		va_list vargs;
		va_start( vargs, strText );
		vsprintf( strData, strText, vargs );
		va_end( vargs );


		fprintf( m_fpLogFile, "%s : %s", szTime, strData );
		fflush( m_fpLogFile );
	}
}


```
</details>


<details>
<summary>개선된 호출</summary>
    
```ruby
//Binding
#include "StdAfx.h"
#include "DBBinding_v3.h"

CDBBinding_v3::CDBBinding_v3(void)
{
	m_uiBindingCount = 0;
	m_uiBindingCapacity = STACK_DB_BINDING_LIMIT;
	m_bUsingStack = true;
	
	// 스택 메모리 포인터 설정
	m_pDBBinding = m_StackBinding;
	m_pDBBindStatus = m_StackBindStatus;
	
	// 스택 메모리 초기화
	memset( m_StackBinding, 0, sizeof(DBBINDING) * STACK_DB_BINDING_LIMIT );
	memset( m_StackBindStatus, 0, sizeof(DBBINDSTATUS) * STACK_DB_BINDING_LIMIT );

	// 기본 값 초기화
	for(unsigned int i = 0; i < STACK_DB_BINDING_LIMIT; i++)
	{
		m_StackBinding[i].iOrdinal = i + 1;
		m_StackBinding[i].obLength = 0;
		m_StackBinding[i].obStatus = 0;
		m_StackBinding[i].pTypeInfo = NULL;
		m_StackBinding[i].pObject = NULL;
		m_StackBinding[i].pBindExt = NULL;
		m_StackBinding[i].dwPart = DBPART_VALUE;
		m_StackBinding[i].dwMemOwner = DBMEMOWNER_CLIENTOWNED;
		m_StackBinding[i].dwFlags = 0;
		m_StackBinding[i].bScale = 0;
		m_StackBinding[i].bPrecision = 11;
	}
}

CDBBinding_v3::CDBBinding_v3(unsigned int uiInitialSize)
{
	m_uiBindingCount = 0;
	
	if (uiInitialSize <= STACK_DB_BINDING_LIMIT)
	{
		// 작은 크기는 스택 사용
		m_uiBindingCapacity = STACK_DB_BINDING_LIMIT;
		m_bUsingStack = true;
		m_pDBBinding = m_StackBinding;
		m_pDBBindStatus = m_StackBindStatus;
		
		memset( m_StackBinding, 0, sizeof(DBBINDING) * STACK_DB_BINDING_LIMIT );
		memset( m_StackBindStatus, 0, sizeof(DBBINDSTATUS) * STACK_DB_BINDING_LIMIT );
		
		for(unsigned int i = 0; i < STACK_DB_BINDING_LIMIT; i++)
		{
			m_StackBinding[i].iOrdinal = i + 1;
			m_StackBinding[i].obLength = 0;
			m_StackBinding[i].obStatus = 0;
			m_StackBinding[i].pTypeInfo = NULL;
			m_StackBinding[i].pObject = NULL;
			m_StackBinding[i].pBindExt = NULL;
			m_StackBinding[i].dwPart = DBPART_VALUE;
			m_StackBinding[i].dwMemOwner = DBMEMOWNER_CLIENTOWNED;
			m_StackBinding[i].dwFlags = 0;
			m_StackBinding[i].bScale = 0;
			m_StackBinding[i].bPrecision = 11;
		}
	}
	else
	{
		// 큰 크기는 바로 힙 사용
		m_uiBindingCapacity = uiInitialSize;
		m_bUsingStack = false;
		
		m_pDBBinding = new DBBINDING[m_uiBindingCapacity];
		m_pDBBindStatus = new DBBINDSTATUS[m_uiBindingCapacity];
		
		memset( m_pDBBinding, 0, sizeof(DBBINDING) * m_uiBindingCapacity );
		memset( m_pDBBindStatus, 0, sizeof(DBBINDSTATUS) * m_uiBindingCapacity );
		
		for(unsigned int i = 0; i < m_uiBindingCapacity; i++)
		{
			m_pDBBinding[i].iOrdinal = i + 1;
			m_pDBBinding[i].obLength = 0;
			m_pDBBinding[i].obStatus = 0;
			m_pDBBinding[i].pTypeInfo = NULL;
			m_pDBBinding[i].pObject = NULL;
			m_pDBBinding[i].pBindExt = NULL;
			m_pDBBinding[i].dwPart = DBPART_VALUE;
			m_pDBBinding[i].dwMemOwner = DBMEMOWNER_CLIENTOWNED;
			m_pDBBinding[i].dwFlags = 0;
			m_pDBBinding[i].bScale = 0;
			m_pDBBinding[i].bPrecision = 11;
		}
	}
}

CDBBinding_v3::~CDBBinding_v3(void)
{
	if (!m_bUsingStack)
	{
		if (m_pDBBinding)
		{
			delete[] m_pDBBinding;
			m_pDBBinding = NULL;
		}
		
		if (m_pDBBindStatus)
		{
			delete[] m_pDBBindStatus;
			m_pDBBindStatus = NULL;
		}
	}
}

void CDBBinding_v3::SwitchToHeap()
{
	if (!m_bUsingStack)
		return;
		
	// 새로운 힙 메모리 할당 (2배 크기)
	unsigned int newCapacity = STACK_DB_BINDING_LIMIT * 2;
	DBBINDING* newDBBinding = new DBBINDING[newCapacity];
	DBBINDSTATUS* newDBBindStatus = new DBBINDSTATUS[newCapacity];
	
	// 기존 스택 데이터 복사
	memcpy(newDBBinding, m_StackBinding, sizeof(DBBINDING) * STACK_DB_BINDING_LIMIT);
	memcpy(newDBBindStatus, m_StackBindStatus, sizeof(DBBINDSTATUS) * STACK_DB_BINDING_LIMIT);
	
	// 새로운 영역 초기화
	memset(newDBBinding + STACK_DB_BINDING_LIMIT, 0, sizeof(DBBINDING) * STACK_DB_BINDING_LIMIT);
	memset(newDBBindStatus + STACK_DB_BINDING_LIMIT, 0, sizeof(DBBINDSTATUS) * STACK_DB_BINDING_LIMIT);
	
	// 새로운 바인딩 기본값 설정
	for(unsigned int i = STACK_DB_BINDING_LIMIT; i < newCapacity; i++)
	{
		newDBBinding[i].iOrdinal = i + 1;
		newDBBinding[i].obLength = 0;
		newDBBinding[i].obStatus = 0;
		newDBBinding[i].pTypeInfo = NULL;
		newDBBinding[i].pObject = NULL;
		newDBBinding[i].pBindExt = NULL;
		newDBBinding[i].dwPart = DBPART_VALUE;
		newDBBinding[i].dwMemOwner = DBMEMOWNER_CLIENTOWNED;
		newDBBinding[i].dwFlags = 0;
		newDBBinding[i].bScale = 0;
		newDBBinding[i].bPrecision = 11;
	}
	
	// 포인터 전환
	m_pDBBinding = newDBBinding;
	m_pDBBindStatus = newDBBindStatus;
	m_uiBindingCapacity = newCapacity;
	m_bUsingStack = false;
}

void CDBBinding_v3::ResizeIfNeeded()
{
	if (m_uiBindingCount >= m_uiBindingCapacity)
	{
		if (m_bUsingStack)
		{
			// 스택에서 힙으로 전환
			SwitchToHeap();
		}
		else
		{
			// 힙 메모리 확장 (2배)
			unsigned int newCapacity = m_uiBindingCapacity * 2;
			
			DBBINDING* newDBBinding = new DBBINDING[newCapacity];
			DBBINDSTATUS* newDBBindStatus = new DBBINDSTATUS[newCapacity];
			
			// 기존 데이터 복사
			memcpy(newDBBinding, m_pDBBinding, sizeof(DBBINDING) * m_uiBindingCapacity);
			memcpy(newDBBindStatus, m_pDBBindStatus, sizeof(DBBINDSTATUS) * m_uiBindingCapacity);
			
			// 새로운 영역 초기화
			memset(newDBBinding + m_uiBindingCapacity, 0, sizeof(DBBINDING) * m_uiBindingCapacity);
			memset(newDBBindStatus + m_uiBindingCapacity, 0, sizeof(DBBINDSTATUS) * m_uiBindingCapacity);
			
			// 새로운 바인딩 기본값 설정
			for(unsigned int i = m_uiBindingCapacity; i < newCapacity; i++)
			{
				newDBBinding[i].iOrdinal = i + 1;
				newDBBinding[i].obLength = 0;
				newDBBinding[i].obStatus = 0;
				newDBBinding[i].pTypeInfo = NULL;
				newDBBinding[i].pObject = NULL;
				newDBBinding[i].pBindExt = NULL;
				newDBBinding[i].dwPart = DBPART_VALUE;
				newDBBinding[i].dwMemOwner = DBMEMOWNER_CLIENTOWNED;
				newDBBinding[i].dwFlags = 0;
				newDBBinding[i].bScale = 0;
				newDBBinding[i].bPrecision = 11;
			}
			
			// 기존 메모리 해제
			delete[] m_pDBBinding;
			delete[] m_pDBBindStatus;
			
			// 새로운 포인터 설정
			m_pDBBinding = newDBBinding;
			m_pDBBindStatus = newDBBindStatus;
			m_uiBindingCapacity = newCapacity;
		}
	}
}

bool CDBBinding_v3::Bind( DBPARAMIOENUM DBParamIO, DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset )
{
	// 필요시 크기 확장
	ResizeIfNeeded();

	m_pDBBinding[ m_uiBindingCount ].obValue = uiOffset;
	m_pDBBinding[ m_uiBindingCount ].eParamIO = DBParamIO;
	m_pDBBinding[ m_uiBindingCount ].cbMaxLen = uiSize;	
	m_pDBBinding[ m_uiBindingCount ].wType = DBType;
	
	m_uiBindingCount++;

	return true;
}

void CDBBinding_v3::Reserve(unsigned int uiSize)
{
	if (uiSize > m_uiBindingCapacity)
	{
		if (m_bUsingStack && uiSize <= STACK_DB_BINDING_LIMIT)
		{
			// 스택 크기 내에서는 아무것도 하지 않음
			return;
		}
		
		if (m_bUsingStack)
		{
			// 스택에서 힙으로 전환
			m_uiBindingCapacity = uiSize;
			SwitchToHeap();
		}
		else
		{
			// 힙 메모리 확장
			DBBINDING* newDBBinding = new DBBINDING[uiSize];
			DBBINDSTATUS* newDBBindStatus = new DBBINDSTATUS[uiSize];
			
			// 기존 데이터 복사
			if (m_uiBindingCount > 0)
			{
				memcpy(newDBBinding, m_pDBBinding, sizeof(DBBINDING) * m_uiBindingCount);
				memcpy(newDBBindStatus, m_pDBBindStatus, sizeof(DBBINDSTATUS) * m_uiBindingCount);
			}
			
			// 전체 영역 초기화
			memset(newDBBinding + m_uiBindingCount, 0, sizeof(DBBINDING) * (uiSize - m_uiBindingCount));
			memset(newDBBindStatus + m_uiBindingCount, 0, sizeof(DBBINDSTATUS) * (uiSize - m_uiBindingCount));
			
			// 새로운 바인딩 기본값 설정
			for(unsigned int i = m_uiBindingCount; i < uiSize; i++)
			{
				newDBBinding[i].iOrdinal = i + 1;
				newDBBinding[i].obLength = 0;
				newDBBinding[i].obStatus = 0;
				newDBBinding[i].pTypeInfo = NULL;
				newDBBinding[i].pObject = NULL;
				newDBBinding[i].pBindExt = NULL;
				newDBBinding[i].dwPart = DBPART_VALUE;
				newDBBinding[i].dwMemOwner = DBMEMOWNER_CLIENTOWNED;
				newDBBinding[i].dwFlags = 0;
				newDBBinding[i].bScale = 0;
				newDBBinding[i].bPrecision = 11;
			}
			
			// 기존 메모리 해제
			delete[] m_pDBBinding;
			delete[] m_pDBBindStatus;
			
			// 새로운 포인터 설정
			m_pDBBinding = newDBBinding;
			m_pDBBindStatus = newDBBindStatus;
			m_uiBindingCapacity = uiSize;
		}
	}
}

void CDBBinding_v3::Clear()
{
	m_uiBindingCount = 0;
	// 메모리는 유지하고 카운트만 리셋
}

void CDBBinding_v3::Reset()
{
	m_uiBindingCount = 0;
	
	// 힙 메모리를 사용 중이라면 스택으로 복귀
	if (!m_bUsingStack)
	{
		delete[] m_pDBBinding;
		delete[] m_pDBBindStatus;
		
		m_pDBBinding = m_StackBinding;
		m_pDBBindStatus = m_StackBindStatus;
		m_uiBindingCapacity = STACK_DB_BINDING_LIMIT;
		m_bUsingStack = true;
	}
}
```
```ruby
//Connection
#pragma warning(disable:4996)

#define DBINITCONSTANTS

#include "StdAfx.h"
#include "DBConnection_v3.h"
#include "DBUtil.h"
#include "../../server_Common/DXUTTimer.h"
#ifndef _SQLOLEDB_H_
#include <sqloledb.h>
#endif

#include <stdio.h>

// 정적 변수 초기화 (단일 캐시 슬롯)
DBConnectionCache CDBConnection_v3::m_Cache;

CDBConnection_v3::CDBConnection_v3(void)
{
	m_pIDBInitialize = NULL;
	m_pIDBProperties = NULL;
	m_pIDBCreateSession = NULL;
	m_pIDBCreateCommand = NULL;
	m_pITransaction = NULL;
	m_pICommandText = NULL;
	m_pIAccessor = NULL;
	m_pIRowset = NULL;
	m_pRows = &m_hRows[0];
	m_cNumRows = 0;

	CoInitialize( NULL );
}

CDBConnection_v3::~CDBConnection_v3(void)
{
	Close();
	CoUninitialize();
}

void CDBConnection_v3::Close()
{
	if( m_pIAccessor != NULL )
	{
		m_pIAccessor->Release();
		m_pIAccessor = NULL;
	}
	
	if( m_pICommandText != NULL )
	{
		m_pICommandText->Release();
		m_pICommandText = NULL;
	}
	
	if( m_pIDBCreateCommand != NULL )
	{
		m_pIDBCreateCommand->Release();
		m_pIDBCreateCommand = NULL;
	}
	
	if( m_pIDBCreateSession != NULL )
	{
		m_pIDBCreateSession->Release();
		m_pIDBCreateSession = NULL;
	}

	if( m_pITransaction != NULL )
	{
		m_pITransaction->Release();
		m_pITransaction = NULL;
	}

	if( m_pIDBProperties != NULL )
	{
	    m_pIDBProperties->Release();
		m_pIDBProperties = NULL;
	}
	
	if( m_pIDBInitialize != NULL )
	{
		m_pIDBInitialize->Uninitialize();
		m_pIDBInitialize->Release();
		m_pIDBInitialize = NULL;
	}
}

bool CDBConnection_v3::Connect( WCHAR *wDataSource, WCHAR *wCatalog, WCHAR *wUserID, WCHAR *wPassword )
{
	Close();

	HRESULT hr;

	hr = CoCreateInstance(  CLSID_SQLOLEDB,
							NULL,
							CLSCTX_INPROC_SERVER,
							IID_IDBInitialize,
							(void **) &m_pIDBInitialize );

	if( FAILED(hr) ) 
		return false;

	DBPROP   InitProperties[4];

	for(int i = 0; i < 4; i++)
        VariantInit(&InitProperties[i].vValue);
  
    //Server name.
    InitProperties[0].dwPropertyID  = DBPROP_INIT_DATASOURCE;
    InitProperties[0].vValue.vt     = VT_BSTR;
	InitProperties[0].vValue.bstrVal= SysAllocString( wDataSource );
    InitProperties[0].dwOptions     = DBPROPOPTIONS_REQUIRED;
    InitProperties[0].colid         = DB_NULLID;

    //Database.
    InitProperties[1].dwPropertyID  = DBPROP_INIT_CATALOG;
    InitProperties[1].vValue.vt     = VT_BSTR;
    InitProperties[1].vValue.bstrVal= SysAllocString( wCatalog );
	InitProperties[1].dwOptions     = DBPROPOPTIONS_REQUIRED;
    InitProperties[1].colid         = DB_NULLID; 

    //Username (login).
    InitProperties[2].dwPropertyID  = DBPROP_AUTH_USERID; 
    InitProperties[2].vValue.vt     = VT_BSTR;
	InitProperties[2].vValue.bstrVal= SysAllocString( wUserID );
    InitProperties[2].dwOptions     = DBPROPOPTIONS_REQUIRED;
    InitProperties[2].colid         = DB_NULLID;

    //Password.
    InitProperties[3].dwPropertyID  = DBPROP_AUTH_PASSWORD;
    InitProperties[3].vValue.vt     = VT_BSTR;
	InitProperties[3].vValue.bstrVal= SysAllocString( wPassword);
    InitProperties[3].dwOptions     = DBPROPOPTIONS_REQUIRED;
    InitProperties[3].colid         = DB_NULLID;

    m_rgInitPropSet[0].guidPropertySet = DBPROPSET_DBINIT;
    m_rgInitPropSet[0].cProperties    = 4;
    m_rgInitPropSet[0].rgProperties   = InitProperties;

    hr = m_pIDBInitialize->QueryInterface(IID_IDBProperties, 
                                   (void **)&m_pIDBProperties );
	if (FAILED(hr))
	{
		DumpErrorInfo( m_pIDBInitialize, IID_IDBInitialize );
		return false;
	}

	hr = m_pIDBProperties->SetProperties(1, m_rgInitPropSet); 
	if (FAILED(hr)) 
	{
		DumpErrorInfo( m_pIDBProperties, IID_IDBProperties );
		return false;
	}

 	for(int i = 0; i < 4; i++)
		VariantClear(&InitProperties[i].vValue);

	m_pIDBProperties->Release();
	m_pIDBProperties = NULL;

	if(FAILED(m_pIDBInitialize->Initialize())) 
	{
		DumpErrorInfo( m_pIDBInitialize, IID_IDBInitialize );
		return false;
	}

	//create session
    if(FAILED(m_pIDBInitialize->QueryInterface(
                                IID_IDBCreateSession,
                                (void**) &m_pIDBCreateSession))) 
	{
		return false;
	}

	if(FAILED(m_pIDBCreateSession->CreateSession(
                                     NULL, 
                                     IID_IDBCreateCommand, 
                                     (IUnknown**) &m_pIDBCreateCommand)))  
	{
		DumpErrorInfo( m_pIDBCreateSession, IID_IDBCreateSession );
		return false;
	}

    if(FAILED(m_pIDBCreateCommand->CreateCommand(
                                    NULL, 
                                    IID_ICommandText, 
                                    (IUnknown**) &m_pICommandText)))  
	{
		DumpErrorInfo( m_pIDBCreateCommand, IID_IDBCreateCommand );
		return false;
	}

	if (FAILED(hr = m_pIDBCreateCommand->QueryInterface(IID_ITransactionLocal,
					(void**) &m_pITransaction)))   
	{
		DumpErrorInfo( m_pIDBCreateCommand, IID_IDBCreateCommand );
		return false;
	}

	return true;
}

std::string CDBConnection_v3::GenerateCacheKey(const char* szFile, int iLine)
{
	char key[512];
	sprintf(key, "%s_%d", szFile, iLine);
	return std::string(key);
}

bool CDBConnection_v3::LoadConnectionInfoFromCache(const std::string& cacheKey, DBConnectionCache& info)
{
	// 단일 캐시 슬롯 확인
	if (m_Cache.bValid && m_Cache.cacheKey == cacheKey)
	{
		// 캐시 유효성 검사 (5분 이내)
		DWORD currentTime = GetTickCount();
		if (currentTime - m_Cache.dwLastUsed < 300000)  // 5분
		{
			info = m_Cache;
			info.dwLastUsed = currentTime;  // 사용 시간 업데이트
			return true;
		}
		else
		{
			// 만료된 캐시 무효화
			m_Cache.bValid = false;
		}
	}
	
	return false;
}

void CDBConnection_v3::SaveConnectionInfoToCache(const std::string& cacheKey, 
	const std::wstring& wDataSource, const std::wstring& wCatalog, 
	const std::wstring& wUserID, const std::wstring& wPassword)
{
	// 단일 캐시 슬롯에 저장
	m_Cache.cacheKey = cacheKey;
	m_Cache.wDataSource = wDataSource;
	m_Cache.wCatalog = wCatalog;
	m_Cache.wUserID = wUserID;
	m_Cache.wPassword = wPassword;
	m_Cache.dwLastUsed = GetTickCount();
	m_Cache.bValid = true;
}

void CDBConnection_v3::ClearConnectionCache()
{
	m_Cache.bValid = false;
	m_Cache.cacheKey.clear();
	m_Cache.wDataSource.clear();
	m_Cache.wCatalog.clear();
	m_Cache.wUserID.clear();
	m_Cache.wPassword.clear();
}

bool CDBConnection_v3::Connect_byGNIDBInfoFile_Cached( char *szFile, int iLine )
{
	std::string cacheKey = GenerateCacheKey(szFile, iLine);
	DBConnectionCache info;
	
	// 캐시에서 정보 찾기
	if (LoadConnectionInfoFromCache(cacheKey, info))
	{
		return Connect((WCHAR*)info.wDataSource.c_str(), 
					   (WCHAR*)info.wCatalog.c_str(), 
					   (WCHAR*)info.wUserID.c_str(), 
					   (WCHAR*)info.wPassword.c_str());
	}
	
	// 캐시에 없으면 파일에서 읽기
	FILE *fp = fopen( szFile, "r" );
	if( fp == NULL ) 
	{
		printf( "\n\tcannot open file(%s) ... \t", szFile );
		return false;
	}

	wchar_t		string[4096];
	wchar_t		wIP[256];	
	wchar_t		wDBName[256];	
	wchar_t		wID[256];	
	wchar_t		wPass[256];	
	wchar_t*	token;
	wchar_t		splitter[] = L" \n\t";
	int			iLineCount = 0;
		
	while( 1 )
	{
		if( fgetws( string, 4096 , fp ) == NULL ) 
		{
			fclose( fp );
			return false;
		}

		if( string[ 0 ] == ';' || (0==wcscmp( string, L"\n" )) ) 
			continue;
			
		// ip
		token = wcstok( string, splitter );	
		if( token == NULL )		continue;
		wcscpy( wIP, token );

		// dbname
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wDBName, token );

		// id
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wID, token );

		// pass
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wPass, token );

		if( iLineCount == iLine )
		{
			fclose( fp );
			
			// 캐시에 저장
			SaveConnectionInfoToCache(cacheKey, wIP, wDBName, wID, wPass);
			
			return Connect( wIP, wDBName, wID, wPass );
		}
		iLineCount++;
	};
	fclose( fp );

	return false;
}

bool CDBConnection_v3::Connect_byGNIDBInfoFile( char *szFile, int iLine )
{
	// 기본 버전 (호환성 유지)
	FILE *fp = fopen( szFile, "r" );
	if( fp == NULL ) 
	{
		printf( "\n\tcannot open file(%s) ... \t", szFile );
		return false;
	}

	wchar_t		string[4096];
	wchar_t		wIP[256];	
	wchar_t		wDBName[256];	
	wchar_t		wID[256];	
	wchar_t		wPass[256];	
	wchar_t*	token;
	wchar_t		splitter[] = L" \n\t";
	int			iLineCount = 0;
		
	while( 1 )
	{
		if( fgetws( string, 4096 , fp ) == NULL ) 
		{
			fclose( fp );
			return false;
		}

		if( string[ 0 ] == ';' || (0==wcscmp( string, L"\n" )) ) 
			continue;
			
		// ip
		token = wcstok( string, splitter );	
		if( token == NULL )		continue;
		wcscpy( wIP, token );

		// dbname
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wDBName, token );

		// id
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wID, token );

		// pass
		token = wcstok( NULL, splitter );	
		if( token == NULL )		continue;
		wcscpy( wPass, token );

		if( iLineCount == iLine )
		{
			fclose( fp );
			return Connect( wIP, wDBName, wID, wPass );
		}
		iLineCount++;
	};
	fclose( fp );

	return false;
}

bool CDBConnection_v3::SetCommandText( ICommandText* pICommandText,WCHAR* wCmdString )
{
	if( pICommandText == NULL ) 
		return false;
	
	if( FAILED(pICommandText->SetCommandText(DBGUID_DBSQL,wCmdString)) ) 
		return false;

	return true;
}

bool CDBConnection_v3::Execute()
{
	HRESULT hr;
	hr = m_pICommandText->Execute(NULL,
							 IID_IRowset,
							 NULL,
							 &m_cNumRows,
							 (IUnknown**)&m_pIRowset);

	if (FAILED(hr)) 
	{
		DumpErrorInfo( m_pICommandText, IID_ICommandText );
		return false;
	}

	return true;
}

bool CDBConnection_v3::ExecuteSQL( WCHAR *wSQLText )
{
	SetCommandText( m_pICommandText, wSQLText );
	bool bReturn = Execute();
	return bReturn;
}
```
```ruby
//Proc
#include "StdAfx.h"
#include "NetGlobal.h"
#include "StoredProcedure_v3.h"
#include "DBConnection_v3.h"
#include "DBUtil.h"
#include "../../server_Common/Global.h"
#include "../../server_Common/DXUTTimer.h"

// 정적 변수 초기화
float	CStoredProcedure_v3::m_fStandardTime = 0;
float	CStoredProcedure_v3::m_fLogThreshold = 0.3f;  // 기본 0.3초 (v2 대비 단축)
bool CStoredProcedure_v3::m_bLogInitialized = false;

#ifndef _ADMINPAGE_DLL
#include "../../server_Common/CSVFile_SMS.h"
#include "../../server_Common/INIFile_Setting.h"

double		g_dSendSMSTime_v3 = 0;

void SendSMS_v3(char *szProcedureName)
{
	printf( "SMS SENT	11\n");
	if(0 == g_INIFile_Setting.m_iSMSState)
		return;

	// 마지막으로 3분안에 보냈다면 메세지 보내지않음
	if( g_Timer.GetTime() - g_dSendSMSTime_v3 < 180.0f )
		return;
}
#endif

CStoredProcedure_v3::CStoredProcedure_v3(void)
{
	m_fpLogFile = NULL;
	m_pParamBinding = NULL;
	m_pRowsetBinding = NULL;
	m_hRowAccessor = NULL;
	m_hAccessor = NULL;
	m_pDBConnection = NULL;
	m_pICommandText = NULL;
	m_pIAccessor = NULL;
	m_pIRowset = NULL;

	// 인라인 로그 버퍼 초기화
	m_iLogBufferCount = 0;
	memset(m_LogBuffer, 0, sizeof(InlineLogBuffer) * INLINE_LOG_BUFFER_SIZE);

	// 로그 시스템 초기화 (한번만)
	if (!m_bLogInitialized)
	{
		m_bLogInitialized = true;
	}
}

CStoredProcedure_v3::~CStoredProcedure_v3(void)
{
	FlushLogBuffer();  // 남은 로그 플러시
}

void CStoredProcedure_v3::Close()
{
	if( m_pParamBinding )
	{
		delete m_pParamBinding;
		m_pParamBinding = NULL;
	}

	if( m_pRowsetBinding )
	{
		delete m_pRowsetBinding;
		m_pRowsetBinding = NULL;
	}

	if( m_pICommandText )
	{
		m_pICommandText->Release();
		m_pICommandText = NULL;
	}

	if( m_pIAccessor )
	{
		m_pIAccessor->Release();
		m_pIAccessor = NULL;
	}
}

bool CStoredProcedure_v3::AddParamBinding( DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset, bool bOutputParam )
{
	if( m_pParamBinding == NULL )
	{
		m_pParamBinding = new CDBBinding_v3;
	}

	DBPARAMIOENUM	DBParamIO;
	if( bOutputParam == true )	DBParamIO = DBPARAMIO_OUTPUT;
	else						DBParamIO = DBPARAMIO_INPUT;

	return m_pParamBinding->Bind( DBParamIO, DBType, uiSize, uiOffset );
}

bool CStoredProcedure_v3::AddRowsetBinding( DBTYPEENUM DBType, unsigned int uiSize, unsigned int uiOffset )
{
	if( m_pRowsetBinding == NULL )
	{
		m_pRowsetBinding = new CDBBinding_v3;
	}

	return m_pRowsetBinding->Bind( DBPARAMIO_NOTPARAM, DBType, uiSize, uiOffset );
}

bool CStoredProcedure_v3::Init( CDBConnection_v3* pDBConnection, WCHAR* wCmdString )
{
	if( pDBConnection == NULL ) 
		return false;

	WideCharToMultiByte( CP_ACP, 0, wCmdString, -1, m_szInitialString, 256, NULL, NULL );

	m_pDBConnection = pDBConnection;

    if( FAILED(pDBConnection->GetCreateCommand()->CreateCommand( 
														NULL, 
														IID_ICommandText, 
														(IUnknown**) &m_pICommandText)) )  
	{
		printf( "CreateCommand() Failed\n" );
		return false;
	}

	if( pDBConnection->SetCommandText( m_pICommandText, wCmdString ) == false ) 
	{
		printf( "SetCommandText() Failed\n" );
		return false;
	}

	return CreateAccessor();
}

bool CStoredProcedure_v3::CreateAccessor()
{
	if( FAILED(m_pICommandText->QueryInterface(IID_IAccessor, (void**)&(m_pIAccessor))) ) 
	{
		DumpErrorInfo( m_pICommandText, IID_ICommandText );
		return false;
	}

	if( m_pParamBinding )
	{
		if( FAILED(m_pIAccessor->CreateAccessor( DBACCESSOR_PARAMETERDATA, m_pParamBinding->m_uiBindingCount, m_pParamBinding->m_pDBBinding,
			0, &m_hAccessor, m_pParamBinding->m_pDBBindStatus)) )
			return false;
	}

	if( m_pRowsetBinding )
	{
		if(FAILED(m_pIAccessor->CreateAccessor( DBACCESSOR_ROWDATA, m_pRowsetBinding->m_uiBindingCount, m_pRowsetBinding->m_pDBBinding,
			0, &m_hRowAccessor, m_pRowsetBinding->m_pDBBindStatus)) )
			return false;
	}

	return true;
}

bool CStoredProcedure_v3::Execute( void *pParam, void *pRowset )
{
	// 실행시간이 설정되어있으면 실행시간 측정
	double	dTime = g_Timer.GetTime();

	HRESULT		hr;
	DBPARAMS	Params;
	DBROWCOUNT	cNumRows = 0;

	m_pResultRowset = pRowset;

	if( pParam == NULL )	// 파라미터를 NULL로 하면 DB함수 호출할때 익셉션이 발생하기 때문에 그냥 클래스 자신을 파라미터로 사용
	{
		Params.pData = this;
	}
	else
	{
		Params.pData = pParam;
	}
	Params.cParamSets = 1;
	Params.hAccessor  = m_hAccessor;

	m_iRecordCount		= 0;
	m_iCurrentRecord	= 0;
	
	//Execute the command.
	if( FAILED(hr = m_pICommandText->Execute( NULL, IID_IRowset, &Params, &cNumRows, (IUnknown **) &(m_pIRowset))) )
	{
		DumpErrorInfo( m_pICommandText, IID_ICommandText );

		m_pDBConnection->OnProcedureExecuteFailed( m_szInitialString );

#ifndef _ADMINPAGE_DLL
		SendSMS_v3(m_szInitialString);
#endif
		return false;
	}

	// 실행시간이 설정되어있으면 실행시간 측정
	double dDiffTime = g_Timer.GetTime()-dTime;

	if( m_fStandardTime > 0 )
	{
		if( dDiffTime >= m_fStandardTime )
		{
			printf( "DB PROC TIME : %40s ... %f\n", m_szInitialString, dDiffTime );
		}
	}

	// 로그 임계값 검사 (v3에서 0.3초로 단축)
	if( dDiffTime >= m_fLogThreshold )
	{
		LogFile_Printf( "DB PROC TIME : %40s ... %f\n", m_szInitialString, dDiffTime );
	}

	return true;
}

bool CStoredProcedure_v3::GetFirstRecord()
{
	// 고정 배치 크기로 레코드 가져오기 (동적 계산 오버헤드 제거)
	IRowset *pRowset = m_pIRowset;
	if( pRowset == NULL )
		return false;

	if( FAILED( pRowset->GetNextRows( NULL, 0, BATCH_SIZE_FIXED, &m_iRecordCount, &m_pDBConnection->m_pRows)) )
	{
		return false;
	}

	// 첫번째 레코드 데이터 가져오기
	m_iCurrentRecord = 0;

	if( m_iRecordCount == 0 )
		return false;

	if( m_pResultRowset == NULL )
		return false;

	if(FAILED( m_pIRowset->GetData(m_pDBConnection->m_hRows[m_iCurrentRecord], m_hRowAccessor, m_pResultRowset )))
		return false;

	return true;
}

bool CStoredProcedure_v3::GetNextRecord()
{
	if( m_iRecordCount == 0 )
		return false;

	if( m_pResultRowset == NULL )
		return false;

	m_iCurrentRecord ++;
	if( m_iCurrentRecord >= m_iRecordCount )
	{
		// 현재 가져온 레코드들 해제
		m_pIRowset->ReleaseRows( m_iRecordCount, m_pDBConnection->m_pRows, NULL, NULL, NULL );

		// 다음 고정 배치 크기로 레코드 가져오기
		IRowset *pRowset = m_pIRowset;
		if( pRowset == NULL )
			return false;

		if( FAILED( pRowset->GetNextRows( NULL, 0, BATCH_SIZE_FIXED, &m_iRecordCount, &m_pDBConnection->m_pRows)) )
		{
			if( pRowset != NULL )	
				pRowset->Release();
			return false;
		}
		if( m_iRecordCount == 0 )
			return false;

		m_iCurrentRecord = 0;	// 새로운 배치 레코드 처리시작
	}

	if(FAILED( m_pIRowset->GetData(m_pDBConnection->m_hRows[m_iCurrentRecord], m_hRowAccessor, m_pResultRowset )))
		return false;

	return true;
}

void CStoredProcedure_v3::ReleaseDBRecords()
{
	if( m_pIRowset != NULL )	
	{
		m_pIRowset->Release();
		m_pIRowset = NULL;
	}
}

void CStoredProcedure_v3::FlushLogBuffer()
{
	if (m_iLogBufferCount > 0 && m_fpLogFile != NULL)
	{
		for (int i = 0; i < m_iLogBufferCount; i++)
		{
			fprintf(m_fpLogFile, "%s", m_LogBuffer[i].szLogData);
		}
		fflush(m_fpLogFile);
		m_iLogBufferCount = 0;
	}
}

void CStoredProcedure_v3::LogFile_Printf( char * strText, ... )
{
	// 로그 데이터 준비
	char strData[4096];
	va_list vargs;
	va_start( vargs, strText );
	vsprintf( strData, strText, vargs );
	va_end( vargs );

	// 현재 시간
	char	szTime[256];
	time_t	currentTime = time(0);
	strftime( szTime, 256, "%Y-%m-%d %H:%M:%S", localtime( &currentTime ) );

	// 최종 로그 문자열
	char szFinalLog[4096];
	sprintf(szFinalLog, "%s : %s", szTime, strData);

	// 인라인 버퍼에 로그 추가
	if (m_iLogBufferCount < INLINE_LOG_BUFFER_SIZE)
	{
		strcpy(m_LogBuffer[m_iLogBufferCount].szLogData, szFinalLog);
		m_LogBuffer[m_iLogBufferCount].dwTimestamp = GetTickCount();
		m_iLogBufferCount++;
	}

	// 버퍼가 절반 이상 차거나 3초가 지나면 플러시 (v3 단축)
	bool bShouldFlush = false;
	if (m_iLogBufferCount >= INLINE_LOG_BUFFER_SIZE / 2)
		bShouldFlush = true;
	else if (m_iLogBufferCount > 0)
	{
		DWORD currentTick = GetTickCount();
		if (currentTick - m_LogBuffer[0].dwTimestamp > 3000)  // 3초
			bShouldFlush = true;
	}

	if (bShouldFlush)
	{
		// 로그 파일이 없으면 생성
		if( m_fpLogFile == NULL )
		{
			char	szTime[256];
			time_t	currentTime = time(0);
			strftime( szTime, 256, "%Y%m%d_%H%M%S", localtime( &currentTime ) );

			char	szLogDir[256];
			sprintf( szLogDir, "%s\\log",g_Global.m_szModulePath );
			CreateDirectoryA( szLogDir, NULL ); 

			char szLogFile[256];
			sprintf( szLogFile, "%s\\log\\DB_Execute_Log_%s.txt", 
				g_Global.m_szModulePath, 
				szTime 
				);
			m_fpLogFile = fopen( szLogFile, "a" );
		}

		FlushLogBuffer();

		// 로그 파일 크기 체크 (더 간단)
		if( m_fpLogFile )
		{
			static DWORD lastSizeCheck = 0;
			DWORD currentTick = GetTickCount();
			
			if (currentTick - lastSizeCheck > 120000)  // 2분마다 체크 (v3 간격 확대)
			{
				fseek( m_fpLogFile, 0, SEEK_END );
				long lFileSize = ftell( m_fpLogFile );
				if( lFileSize >= 20971520 )  // 20MB로 확대 (v3 파일 크기 증가)
				{
					fclose( m_fpLogFile );

					// 파일 크기를 넘는다면 새로운 파일 생성
					char	szTime[256];
					time_t	currentTime = time(0);
					strftime( szTime, 256, "%Y%m%d_%H%M%S", localtime( &currentTime ) );

					char szLogFile[256];
					sprintf( szLogFile, "%s\\log\\DB_Execute_Log_%s.txt", 
						g_Global.m_szModulePath, 
						szTime 
						);
					m_fpLogFile = fopen( szLogFile, "a" );
				}
				lastSizeCheck = currentTick;
			}
		}
	}
}
```
</details>


<img width="1146" height="761" alt="image" src="https://github.com/user-attachments/assets/69dc3be0-dc52-4356-be59-262911232a7b" />
클로드 코드의 개선내용에 대해 비교를 해보았다. 성능적으로 분명 좋아졌을 텐데, 

<img width="1036" height="1028" alt="image" src="https://github.com/user-attachments/assets/e7878a61-b65a-4a47-9e20-52df114c4cfe" />
결과는 두 방식 간의 성능 차이는 명확하게 드러나지 않았다. 그 이유는 DBServer-OLEDB 연결 속도가 매우 빨라 지연을 유발할 만한 요인이 없었기 때문이었다. 약간의 지연이 발생해 Log들이 다량 발생했다면 아마 성능차이는 많이 났을 것으로 보지만, 기존의 로직도 단순 프로시저 호출로 느려지지는 않는 것으로 확인 되었다.

결국 DB에서 지연이 발생해 RowData의 사용방식이 힘들다 한다는 것은, 다른 곳에 이유가 있음을 알게되었다.

#### 4-2. User SaveDB의 침범
 현재 우리의 서버는 싱글 코어로 돌아가고 있다. 녹화된 영상을 보면 Load의 시간이 그리 지연되지는 않는데, 문제는 주기적으로 User의 데이터를 저장할 때 2초 가량씩 지연이 발생하는 것을 관측 하였다. 현재 여러 패킷으로 세이브하는 방식은 싱글스레드로 통신을 하는 서버에서는 많은 성능감소를 발생시키는 요소로 보이고, AI를 활용하여 확인해봐도 패킷을 하나로 보내는 것이 효율이 더 좋은 것으로 판단되었다.
<details>
<summary>개선된 호출 코드</summary>
	
```ruby
struct DCP_SAVE_PC_2 : public SIOCPPacket
{
	DCP_SAVE_PC_2()
	{
		m_Header.wID[PACKET_HEADER] = DCM_SAVE_PC_2;
		sizeof(DCP_SAVE_PC_2);

	}
	DCP_SINSU_LIST sendSuhoAvatar;
	DCP_DAILYDUNGEON_SAVE sendDailyDungeonInfo;
	DCP_Save_QuestInfo sendQuestInfo;
	DCP_Save_Accumulate_Data sendAccumulate;
	DCP_SAVE_PK_SETTING	sendPkInfo;
	DCP_SAVE_TRAINING_TIME_LIST sendTrainingCenterList;
	DCP_SAVE_DUNGEON_LIST sendDungeonList;
	DCP_SAVE_SEASONPASS sendSeasonPass;
	DCP_COMBINE_COUNT_ALL_SAVE sendComBineFail;
	DCP_SOULFIRELIST_SAVE sendSoulFire;
	DCP_USER_PRESET_SAVE sendPreset;
	DCP_SAVE_ALCHEMY sendAlchemy;
	DCP_WISHING_SAVE_DATA sendWishing;
	DCP_SAVE_MAPAE sendMapae;
	DCP_SAVE_MAPAE_COLLECTION sendMapaeCollection;
	DCP_SAVE_PVE_SCORE sendPveScore;
	DCP_SAVEPC_COLLECTION_DATA sendCollection;
	DSP_SAVE_GYEONGMAEK_LIST sendGyeongmaekList;
	DCP_BANGPA_REQUEST_SAVE sendBangpaRequest;
};
```
패킷은 기존 19+ 로 되던 패킷을 하나의 패킷으로 송신하게 바꾸었다.
 
```ruby
void CWishingList::SaveWishingData(CUnitPC* pPC)
{
	if (pPC == NULL)
	{
		return;
	}
	DCP_WISHING_SAVE_DATA sendDB;
	memset(sendDB._i32WishingCount, 0x00, sizeof(INT32)* MAX_NECESSARY_TITLE);
	memset(sendDB._stonebuffData, 0x00, sizeof(stWishingStoneBuffData) * MAX_NECESSARY_TITLE * MAX_WISHING_STONE_COUNT);


	sendDB._dwcharunique = pPC->GetCharacterUnique();
	memcpy(sendDB._i32WishingCount, _i32WishingCount, sizeof(INT32) * MAX_NECESSARY_TITLE);
	memcpy(sendDB._stonebuffData, _stone, sizeof(stWishingStoneBuffData) * MAX_NECESSARY_TITLE * MAX_WISHING_STONE_COUNT);

	pPC->WriteGameDB((BYTE*)&sendDB, sizeof(DCP_WISHING_SAVE_DATA));
}

void CWishingList::SaveWishingData(CUnitPC* pPC, DCP_WISHING_SAVE_DATA& pSavePacket)
{
	if (pPC == NULL )
	{
		return;
	}
	memset(pSavePacket._i32WishingCount, 0x00, sizeof(INT32) * MAX_NECESSARY_TITLE);
	memset(pSavePacket._stonebuffData, 0x00, sizeof(stWishingStoneBuffData) * MAX_NECESSARY_TITLE * MAX_WISHING_STONE_COUNT);


	pSavePacket._dwcharunique = pPC->GetCharacterUnique();
	memcpy(pSavePacket._i32WishingCount, _i32WishingCount, sizeof(INT32) * MAX_NECESSARY_TITLE);
	memcpy(pSavePacket._stonebuffData, _stone, sizeof(stWishingStoneBuffData) * MAX_NECESSARY_TITLE * MAX_WISHING_STONE_COUNT);


}

```
기본적으로 Save부분(컨텐츠상 진행에 의한 저장), Loop Save 부분이 나뉘게 되었다. Loop Save의 경우 수많은 데이터를 한번에 저장하기 때문에 별도의 함수가 필요하였다.

```ruby
void CDBManager::ON_DCM_SAVE_PC_2(int iClient, WORD wExtraHeader, BYTE* pPacket)
{
	DCP_SAVE_PC_2* pMsg = (DCP_SAVE_PC_2*)pPacket;

	CDBManager::ON_SAVE_SINSULIST(iClient, wExtraHeader, (BYTE*)&pMsg->sendSuhoAvatar);
	CDBManager::ON_DAILYDUGEON_SAVE(iClient, wExtraHeader, (BYTE*)&pMsg->sendDailyDungeonInfo);
	CDBManager::ON_DCM_SAVE_QUESTINFO( iClient, wExtraHeader, (BYTE *)&pMsg->sendQuestInfo );
	CDBManager::ON_DCM_SAVE_ACCUMULATE(iClient, wExtraHeader, (BYTE*)&pMsg->sendAccumulate);
	CDBManager::ON_DCM_SAVE_PK_SETTING(iClient, wExtraHeader, (BYTE *)&pMsg->sendPkInfo);
	CDBManager::ON_SAVE_TRAINING_PERSONAL(iClient, wExtraHeader, (BYTE*)&pMsg->sendTrainingCenterList);
	CDBManager::ON_DCM_SAVE_DUNGEON_LIST(iClient, wExtraHeader, (BYTE *)&pMsg->sendDungeonList);
	CDBManager::ON_DCM_SAVE_SEASONPASS(iClient, wExtraHeader, (BYTE *)&pMsg->sendSeasonPass);
	CDBManager::ON_COMBINE_COUNT_ALL_SAVE(iClient, wExtraHeader, (BYTE*)&pMsg->sendComBineFail);
	CDBManager::ON_SOULFIRE_LIST_SAVE(iClient, wExtraHeader, (BYTE*)&pMsg->sendSoulFire);
	CDBManager::ON_PRESET_LIST_SAVE(iClient, wExtraHeader, (BYTE*)&pMsg->sendPreset);
	CDBManager::ON_ALCHEMY_SAVE(iClient, wExtraHeader, (BYTE*)&pMsg->sendAlchemy);
	CDBManager::ON_WISHING_SAVE(iClient, wExtraHeader, (BYTE *)&pMsg->sendWishing);
	CDBManager::ON_MAPAE_LIST_SAVE(iClient, wExtraHeader, (BYTE*)&pMsg->sendMapae);
	CDBManager::ON_MAPAE_COLLECTION_LIST_SAVE(iClient, wExtraHeader, (BYTE*)&pMsg->sendMapaeCollection);
	CDBManager::ON_SAVE_PVE_SCORE(iClient, wExtraHeader, (BYTE*)&pMsg->sendPveScore);
	CDBManager::ON_DCM_SAVEPC_COLLECTION_DATA(iClient, wExtraHeader, (BYTE*)&pMsg->sendCollection);
	CDBManager::ON_DSM_SAVE_GYEONGMAEK_LIST(iClient, wExtraHeader, (BYTE*)&pMsg->sendGyeongmaekList);
	CDBManager::ON_BANGPA_REQUEST_SAVE(iClient, wExtraHeader, (BYTE*)&pMsg->sendBangpaRequest);

}
수신부는 기존 코드 재활용이 가능하여 그대로 가져다 사용하였다.

```

</details>
<img width="904" height="1245" alt="image" src="https://github.com/user-attachments/assets/81bef5f7-b6a8-4d87-b134-d6ef3aa551ee" />

해당 코드로 변경하며 분명히 성능상 30%는 증가한 것으로 측정이 된다. 하지만, 코드의 중복되는 부분들이 늘어났으며, 성능이냐 유지보수냐를 보았을때 멀티스레드 환경이면 해결될 문제인 것으로 보인다.
메세지 처리부분을 보니 IOCP에서 받는건 멀티스레드지만, 메세지 처리 부분이 단일 스레드인것으로 확인된다. 그러나 동일 로컬에서는 성능상 차이가 두드러지게 보이지 않는것을 확인되었다. IOCP스레드수를 4->8개로 늘리니 기존과 심한차이가 나지 않았다.

<details>
<summary></summary>
    
```ruby
```
</details>

#### 4-3. Mssql 캐시메모리 제거로 인한 호출 불안
 기존 위 사항 외에도 프로시저를 호출하는데, 시간들이 안정적이지 않은 현상이 있었다. 일부 이유를 찾아보니 Mssql자체적으로 캐시메모리를 삭제하기 때문인데, 이러한 기능을 강제로 실행시키는건 어떤가 하였다. 하지만, Gemini는 그것을 추천하지 않는다고 한다.\
 해결 방안으로는
1. 메모리 압박 (Memory Pressure): SQL Server는 시스템 메모리가 부족해지면, 가장 오랫동안 사용되지 않은(Least Recently Used) 실행 계획부터 제거합니다. 이는 새로운 요청을 처리하기 위한 공간을 확보하기 위함입니다. 사용량이 많거나, SQL Server에 할당된 메모리가 적을 경우 자주 발생합니다.
2. 계획 재컴파일 (Plan Recompilation): 데이터베이스의 특정 변경 사항으로 인해 캐시된 실행 계획이 더 이상 유효하지 않다고 판단될 때, SQL Server는 해당 계획을 제거하고 다음에 호출될 때 새로운 계획을 만듭니다. 주요 원인은 다음과 같습니다.\
통계 자동 업데이트: 테이블의 데이터 분포가 크게 바뀌면, SQL Server는 통계를 자동으로 업데이트하고, 이로 인해 기존 실행 계획이 재컴파일될 수 있습니다.\
스키마 변경: 프로시저가 참조하는 테이블이나 인덱스에 변경이 생기면, 관련 실행 계획이 무효화됩니다.\
sp_recompile 호출: 특정 개체에 대해 sp_recompile 프로시저가 수동으로 실행되면, 해당 개체를 참조하는 모든 실행 계획이 제거됩니다.
4. SQL Server 서비스 재시작: SQL Server 서비스가 재시작되면, 메모리에 있는 모든 캐시된 정보(실행 계획, 데이터 페이지 등)가 비워지고 처음부터 다시 시작됩니다.
5. 자동 파라미터화 (Simple Parameterization): SQL Server의 자동 파라미터화 기능은 특정 패턴의 ad-hoc 쿼리를 매개변수화하여 캐시합니다. 하지만 쿼리 패턴이 복잡하거나, 설정이 변경되면 계획 재사용이 실패하고 ExecutionCount가 초기화될 수 있습니다.


메모리 조정: SQL Server에 할당된 최대 메모리(max server memory) 설정을 확인하고, 시스템이 메모리 압박을 받지 않도록 충분한 RAM을 확보하고 할당해야 합니다.
프로시저 최적화:
강제 실행 계획(Plan Forcing): OPTION (RECOMPILE), OPTIMIZE FOR, 또는 쿼리 저장소(Query Store) 기능을 사용하여 특정 프로시저에 대한 최적의 실행 계획이 항상 사용되도록 강제하는 것이 근본적인 해결책이 될 수 있습니다.
파라미터 스니핑(Parameter Sniffing): 프로시저의 입력 매개변수에 따라 최적화된 계획이 달라지는 경우, WITH RECOMPILE을 사용하거나 파라미터 스니핑 방지 기법을 적용해야 합니다.
지속적인 모니터링: **확장 이벤트(Extended Events)**나 성능 카운터를 통해 **실제로 실행 계획이 제거(Memory Pressure)되거나 재컴파일(Recompilation)**되는 시점을 정확하게 파악하여 원인을 특정해야 합니다.

결론은 인덱스를 사용하는데 있어 DB에서 캐싱하는 메모리에 대해 이해해야하고 물리적으로 쌓고 정말 필요할때만 비클러스트로 쌓아야할 것 같다.
