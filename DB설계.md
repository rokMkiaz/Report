
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
