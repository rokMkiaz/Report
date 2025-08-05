## AI를 활용하여 직접적으로 개발하기

### 1.ChatGPT활용 서버 개발
가장 사용난이도가 쉽고 친숙한 편이어서 채택을 하였으나, \
Pro를 활용해도 일정이상 사용시 오류와 전체적인 코드 파악에 대해 어려움이 있었다.\
물론 간단한 코드나 해당 블록에 대한 최적화는 뛰어난 편

### 2.Claude활용
VS Code를 쓰면 편한점과 디렉토리 전체를 쉽게 학습이 가능하여 놀라웠다.\
Linux를 활용한적이 없다면 설치하는 것 부터가 약간은 힘들 수 있다.

#### 2-1.MCP Server추가
MCP 서버는 LLM(대규모 언어 모델)이 다양한 기능이나 데이터를 사용하도록 연결해주는 일종의 서버입니다. Model Context Protocol(MCP)을 기반으로 구축되며, LLM이 외부 서비스를 활용할 수 있도록 API 역할을 합니다. MCP 서버는 특정 기능을 제공하는 쪽, 즉 서버 역할을 하고, 클라이언트는 이 기능을 사용하는 쪽, 즉 클라이언트 역할을 합니다. 

#### 2-2.Super Claude업데이트
SuperClaude의 가장 획기적인 기능은 "일반적인 AI" 문제에 대한 해결책인 페르소나일 것입니다. 단일하고 획일적인 AI와 상호 작용하는 대신, SuperClaude는 아홉 가지의 독특하고 전문화된 "인지 원형" 목록을 제공합니다. 페르소나를 활성화하면(예: /persona:architect) AI의 사고방식, 우선순위, 커뮤니케이션 스타일, 심지어 선호하는 도구까지 완전히 바뀝니다. 이는 제너럴리스트를 갖는 것과 온디맨드 전문가 팀 전체를 갖는 것의 차이입니다.\
L 사용결과 여러가지 의견을 제시하고 동의 여하에 따라 코드의 질이 상승하였다.

### 3.응용 및 적용

DB개발을 지속하며 코드들이 불필요하게 반복이 되는것이 느겨졋다. 해당 부분들을 클로드 코드로 템플릿화 시켜보앗다.
<details>
<summary>기존코드</summary>

  
```ruby
//창고
	{
		INT32 i32dx = 0;
		vector<_PROCEDURE_SAVE_STORAGE_ROW> buffer;
		CProcedure_Save_Storage* pSaveStorage = CProcedure_Save_Storage::GetInstance();
		_PROCEDURE_SAVE_STORAGE_PARAM* pstorageparams = new _PROCEDURE_SAVE_STORAGE_PARAM();
		buffer.reserve(JSON_CHUNK_SIZE_50);

		while (i32dx < MAX_CHANNEL_COMMON_STORAGE_COUNT) {

			memset(pstorageparams, 0, sizeof(_PROCEDURE_SAVE_STORAGE_PARAM));
			INT32 i32taken = 0;
			buffer.clear();

			// 50개 단위로 Push
			while (i32taken < JSON_CHUNK_SIZE_50 && i32dx < MAX_CHANNEL_COMMON_STORAGE_COUNT) {

				_PROCEDURE_SAVE_STORAGE_ROW row;
				memset(&row, 0x00, sizeof(_PROCEDURE_SAVE_STORAGE_ROW));
				row.i32Slot = i32dx;
				memcpy(&row.info, &pMsg->stStorageInfo[i32dx], sizeof(ItemInfo));
				buffer.push_back(row);
				++i32taken;
				++i32dx;
			}
			if (buffer.empty()) continue;

			// Build JSON and prepare parameters
			wstring json = BuildJson(buffer,"storage", pMsg->m_dwAccUnique, i32dx);
			pstorageparams->dwAccunique = pMsg->m_dwAccUnique;

			memcpy(pstorageparams->wcJson, json.c_str(), (json.size() + 1) * sizeof(WCHAR));

			// Execute and release
			if (!pSaveStorage->Execute(pstorageparams, nullptr))
			{
				printf("CProcedure_Save_Storage::Execute() Failed.\n");
			}
			pSaveStorage->ReleaseDBRecords();
		}
		delete pstorageparams;
		pstorageparams = NULL;
	}
```
</details>



<details>
<summary>기존코드</summary>

작성된 템플릿은 아래 코드와 갓다.
```ruby
template<typename ProcedureType, typename ParamType,  typename RowType >
void SaveDataInChunks(DWORD dwCharunique, DWORD dwAccunique, INT32 maxCount, INT32 chunkSize, const char* jsonTypeName,
	/*데이터 저장조건*/function<bool(RowType&, INT32)>dataProcessor,
	/*마지막 파람에 넣을 조건*/function<void(ParamType*, const wstring&, DWORD, DWORD)> dataParam)
{
	INT32 index = 0;
	vector<RowType> buffer;
	ProcedureType* procedure = ProcedureType::GetInstance();
	ParamType* params = new ParamType();
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

		// JSON 빌드 및 실행
		wstring json = BuildJson(buffer, jsonTypeName, dwAccunique, index);

		// 사용자 정의 파라미터 설정 로직
		dataParam(params, json, dwCharunique, dwAccunique);

		if (!procedure->Execute(params, nullptr)) {
			printf("%s::Execute() Failed.\n",typeid(ProcedureType).name());
		}
		procedure->ReleaseDBRecords();
	}

	delete params;
}

```
