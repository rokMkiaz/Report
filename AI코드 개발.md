## AI를 활용하여 직접적으로 개발하기

### 1.ChatGPT활용 서버 개발
가장 사용난이도가 쉽고 친숙한 편이어서 채택을 하였으나, \
Pro를 활용해도 일정이상 사용시 오류와 전체적인 코드 파악에 대해 어려움이 있었다.\
물론 간단한 코드나 해당 블록에 대한 최적화는 뛰어난 편이다.

### 2.Claude활용
VS Code를 쓰면 편한점과 디렉토리 전체를 쉽게 학습이 가능하여 놀라웠다.\
Linux를 활용한적이 없다면 설치하는 것 부터가 약간은 힘들 수 있다.

#### 2-1.MCP Server추가
MCP 서버는 LLM(대규모 언어 모델)이 다양한 기능이나 데이터를 활용할 수 있도록 연결해주는 중개 서버입니다.\
Model Context Protocol(MCP)을 기반으로 구축되며, LLM이 외부 서비스나 기능에 접근할 수 있도록 API 역할을 수행합니다.\
MCP 서버는 기능을 제공하는 서버 역할을 맡고, 이를 호출하여 사용하는 LLM은 클라이언트 역할을 합니다.

#### 2-2.Super Claude업데이트
SuperClaude의 가장 혁신적인 기능은, 이른바 "일반형 AI의 한계"를 해결하기 위한 접근법인 페르소나(persona)입니다.
기존의 단일하고 획일적인 AI와 상호작용하는 방식과 달리, SuperClaude는 아홉 가지 독특하고 전문화된 인지 원형(cognitive archetype)을 제공합니다.

사용자가 페르소나를 활성화하면(예: /persona:architect), AI의 사고방식, 우선순위, 커뮤니케이션 스타일, 심지어 선호하는 도구까지 완전히 달라집니다.
이는 하나의 제너럴리스트 AI 대신, 필요에 따라 호출 가능한 전문가 팀을 갖는 것과 같은 경험을 제공합니다.

실제 사용 결과, 다양한 의견을 제시하며 사용자와 합의를 유도하고, 그에 따라 코드의 품질이 향상되는 효과를 확인할 수 있었습니다.
L 사용결과 여러가지 의견을 제시하고 동의 여하에 따라 코드의 질이 상승하는 것을 경험 하였다.

### 3.응용 및 적용
#### 3.1 밸런스 인포 추출
서버에 하드코딩된 계산식들을 도식화가 필요하였다. 해당 계산식이 있는 부분에 대하여 추출을 요청해 보았다.
<details>
<summary>입력 사진</summary>

<img width="774" height="657" alt="image" src="https://github.com/user-attachments/assets/cba23a6e-751d-4031-a7cc-d9713f9605dc" />
<img width="725" height="597" alt="image" src="https://github.com/user-attachments/assets/0363be38-40cd-4d63-9e9a-01d7b9e7567c" />

</details>
자료입력은 위와 같이 하였고,

<details>
<summary>출력 사진</summary>
<img width="757" height="741" alt="image" src="https://github.com/user-attachments/assets/030704a6-8743-4fa0-9cd6-675e21900cf2" />
<img width="1373" height="685" alt="image" src="https://github.com/user-attachments/assets/a0e66b8d-aef7-4e6a-a8b3-7777100161b0" />

</details>
초기에 내부 변수명으로 출력이 되어있어, 요청하는 사항에 맞춰서 손을 보고 마무리를 하였다. 

#### 3.2 중복 코드 템플릿화
DB개발을 지속하며 코드들이 불필요하게 반복이 되는것이 느껴졌다. 해당 부분들을 클로드 코드로 템플릿화 시켜보았다.
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
while문, GetInstance, 동적할당 등 프로시저를 호출할 대 중복되는 부분들이 굉장히 많이 보인다. 중간중간 값입력부분만 함수화해서 넣어주면 사용하기 좋아질 것으로 보였다.


</details>



* 클로드코드를 활용한 함수 템플릿 작성
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
중복되는 부분들은 전부 묶었으며, 함수 포인터를 사용해 개별적으로 다른 부분을 반영할 수 있게 작성되었다.

* 기존 코드 적용사례
```ruby
//창고
                 SaveDataInChunks<CProcedure_Save_Storage, _PROCEDURE_SAVE_STORAGE_PARAM, _PROCEDURE_SAVE_STORAGE_ROW >
			(0, pMsg->m_dwAccUnique, MAX_CHANNEL_COMMON_STORAGE_COUNT, JSON_CHUNK_SIZE_50, "Avatar",
				[&](_PROCEDURE_SAVE_STORAGE_ROW& row, INT32 index) -> bool {
					//초기화
					row.i32Slot = index;
					memcpy(&row.info, &pMsg->stStorageInfo[index], sizeof(ItemInfo));


					return true;  // 버퍼에 추가
				},
				[&](_PROCEDURE_SAVE_STORAGE_PARAM* params, const wstring& json) -> void {
					params->dwAccunique = pMsg->m_dwAccUnique;  

					memcpy(params->wcJson, json.c_str(), (json.size() + 1) * sizeof(WCHAR));
				}
				);
```
코드의 가독성이 증가하였고, 템플릿화된 코드내에서 동적할당,해제가 되어 안전성이 증가했다.


#### 3.3 DB테이블 바이너리 해체 작업
<img width="579" height="616" alt="image" src="https://github.com/user-attachments/assets/97eff88d-2077-4337-bf38-e6370dc43b97" /> \
복잡도가 높은 바이너리 구조체를 해체하는 작업을 진행했습니다.처음에는 구조를 한 번에 처리하는 복잡한 쿼리 방식을 시도했지만, 기존에 알고 있던 지식과 GPT가 제안한 방법을 결합해 테이블을 3개로 분리하는 접근으로 방향을 바꿨습니다.

이 방식을 적용하니 데이터 흐름이 한층 명확해졌고, 완성된 코드에 대한 AI의 검토 피드백 기능은 개발 효율을 높이는 데 상당히 도움이 되었습니다.

#### 3.4 길찾기 로직 최적화
클로드 코드를 활용하여 기존 로직에서 성능을 더 높일 수 없는지 한번 검토해 보았더니 대략 60%정도 더 상향이될 수 있는 부분을 찾았다고 한다. 해당 부분을 수정하여 테스트를 해서 서버의 부하를 더 줄여 보는 것으로 적용을 해보았는데, 어느 정도 테스트가 필요해 보인다.
```ruby
// Binary Heap 구현
Astar::NodeHeap::NodeHeap(int capacity) {
	m_capacity = capacity;
	m_size = 0;
	m_heap = new Node*[capacity];
}

Astar::NodeHeap::~NodeHeap() {
	delete[] m_heap;
}

void Astar::NodeHeap::Push(Node* node) {
	if (m_size >= m_capacity) return;
	
	m_heap[m_size] = node;
	HeapifyUp(m_size);
	m_size++;
}

Astar::Node* Astar::NodeHeap::Pop() {
	if (m_size == 0) return NULL;
	
	Node* result = m_heap[0];
	m_heap[0] = m_heap[--m_size];
	HeapifyDown(0);
	return result;
}

void Astar::NodeHeap::HeapifyUp(int index) {
	while (index > 0) {
		int parentIdx = Parent(index);
		if (m_heap[parentIdx]->F <= m_heap[index]->F) break;
		
		Node* temp = m_heap[parentIdx];
		m_heap[parentIdx] = m_heap[index];
		m_heap[index] = temp;
		index = parentIdx;
	}
}

void Astar::NodeHeap::HeapifyDown(int index) {
	while (LeftChild(index) < m_size) {
		int smallest = index;
		int left = LeftChild(index);
		int right = RightChild(index);
		
		if (left < m_size && m_heap[left]->F < m_heap[smallest]->F)
			smallest = left;
		if (right < m_size && m_heap[right]->F < m_heap[smallest]->F)
			smallest = right;
			
		if (smallest == index) break;
		
		Node* temp = m_heap[index];
		m_heap[index] = m_heap[smallest];
		m_heap[smallest] = temp;
		index = smallest;
	}
}

// 메모리 풀 구현
Astar::NodePool::NodePool(int poolSize) {
	m_poolSize = poolSize;
	m_freeCount = poolSize;
	m_pNodes = new Node[poolSize];
	m_pFreeIndices = new int[poolSize];
	
	// 모든 인덱스를 자유 목록에 추가
	for (int i = 0; i < poolSize; i++) {
		m_pFreeIndices[i] = i;
	}
}

Astar::NodePool::~NodePool() {
	delete[] m_pNodes;
	delete[] m_pFreeIndices;
}

Astar::Node* Astar::NodePool::AllocNode() {
	if (m_freeCount == 0) return NULL;
	
	int index = m_pFreeIndices[--m_freeCount];
	return &m_pNodes[index];
}

void Astar::NodePool::FreeNode(Node* node) {
	if (m_freeCount >= m_poolSize) return;
	
	int index = node - m_pNodes;
	if (index >= 0 && index < m_poolSize) {
		m_pFreeIndices[m_freeCount++] = index;
	}
}

void Astar::NodePool::Reset() {
	m_freeCount = m_poolSize;
	for (int i = 0; i < m_poolSize; i++) {
		m_pFreeIndices[i] = i;
	}
}


void CFindPath::CompressPath() {
	if (m_iPathSize < 2) {
		m_iCompressedSize = 0;
		return;
	}
	
	m_iCompressedSize = 0;
	
	// 첫 번째 세그먼트 시작
	DWORD startPos = m_pPathData[0].dwPos;
	int currentDirection = -1;
	int segmentLength = 1;
	
	for (int i = 1; i < m_iPathSize; i++) {
		// 현재와 이전 위치에서 방향 계산
		DWORD prevPos = m_pPathData[i-1].dwPos;
		DWORD currPos = m_pPathData[i].dwPos;
		
		// 위치를 x, y 좌표로 변환 (맵 크기에 따라 조정 필요)
		int prevX = prevPos % 512; // MAP_SIZE_WIDTH 사용 예정
		int prevY = prevPos / 512;
		int currX = currPos % 512;
		int currY = currPos / 512;
		
		int direction = GetDirection(prevX, prevY, currX, currY);
		
		if (currentDirection == -1) {
			// 첫 번째 방향 설정
			currentDirection = direction;
		} else if (currentDirection == direction) {
			// 같은 방향이면 길이 증가
			segmentLength++;
		} else {
			// 방향이 바뀌면 현재 세그먼트 저장하고 새 세그먼트 시작
			AddCompressedSegment(startPos, currentDirection, segmentLength);
			
			startPos = m_pPathData[i-1].dwPos;
			currentDirection = direction;
			segmentLength = 2; // 이전 점 + 현재 점
		}
	}
	
	// 마지막 세그먼트 저장
	if (currentDirection != -1) {
		AddCompressedSegment(startPos, currentDirection, segmentLength);
	}
}

int CFindPath::GetDirection(int fromX, int fromY, int toX, int toY) {
	int dx = toX - fromX;
	int dy = toY - fromY;
	
	// 8방향 인코딩 (0:북, 1:북동, 2:동, 3:남동, 4:남, 5:남서, 6:서, 7:북서)
	if (dx == 0 && dy == -1) return 0; // 북
	if (dx == 1 && dy == -1) return 1; // 북동
	if (dx == 1 && dy == 0) return 2;  // 동
	if (dx == 1 && dy == 1) return 3;  // 남동
	if (dx == 0 && dy == 1) return 4;  // 남
	if (dx == -1 && dy == 1) return 5; // 남서
	if (dx == -1 && dy == 0) return 6; // 서
	if (dx == -1 && dy == -1) return 7; // 북서
	
	return -1; // 잘못된 방향
}

void CFindPath::AddCompressedSegment(DWORD startPos, int direction, int length) {
	if (m_iCompressedSize >= m_iCompressedCapacity) return;
	
	m_pCompressedPath[m_iCompressedSize].dwPos = startPos;
	m_pCompressedPath[m_iCompressedSize].iDirection = direction;
	m_pCompressedPath[m_iCompressedSize].iLength = length;
	m_iCompressedSize++;
}
```


#### 3.5 Math 로직 최적화
기존의 공격 로직의 분할이 필요해 보여 작업을 하였다.
```ruby
```
