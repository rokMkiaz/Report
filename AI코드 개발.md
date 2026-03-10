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
<details>
<summary>수정 코드</summary>

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
사용 방법은 하기와 같다
```ruby
// v2: 메인 길찾기 함수 (기존 함수의 개선된 버전)
int Astar::FindPath_v2(PathOrder* pOrder)
{
	if (!IsValidPath_v2(&(pOrder->m_Start), &(pOrder->m_Dest))) {
		return pOrder->m_iPathResultArray;
	}
	
	int result = FindPath_New_v2(pOrder->m_iPathResultArray, pOrder->m_pGameMap, 
		&(pOrder->m_Start), &(pOrder->m_Dest), pOrder->m_nDistance, 
		pOrder->m_NpcStatus, pOrder->m_iRange);
	
	if (result >= 0) {
		OptimizePath_v2(result);
		
		// 길찾기 성공 후 경로 품질 검사 및 스무딩 자동 적용
		CFindPath* pPath = GetNPCPathObject(result);
		if (pPath) {
			if (!pPath->IsPathOptimal_v2()) {
				pPath->SmoothPath_v2();
				g_DebugManager.WriteFindPathLog(m_id, "Path smoothing applied");
			}
			
			// 임시 객체이므로 삭제
			delete pPath;
		}
	}
	
	return result;
}

// v2: 경로 유효성 사전 검증
bool Astar::IsValidPath_v2(Coordinate* pStartPoint, Coordinate* pEndPoint)
{
	// 시작점과 목적지가 같은 경우
	if (pStartPoint->x == pEndPoint->x && pStartPoint->y == pEndPoint->y) {
		return false;
	}
	
	// 맵 경계 검증
	if (pStartPoint->x < 0 || pStartPoint->y < 0 || 
		pEndPoint->x < 0 || pEndPoint->y < 0) {
		return false;
	}
	
	// 거리 검증 (너무 먼 거리는 제외)
	int distance = abs(pStartPoint->x - pEndPoint->x) + abs(pStartPoint->y - pEndPoint->y);
	if (distance > m_iMaxSearchRadius * 2) {
		return false;
	}
	
	return true;
}

// v2: 힙 최적화를 사용한 개선된 길찾기
int Astar::FindPath_New_v2(int iResultPathArray, CGameMap* pGameMap, Coordinate* pStartPoint,
	Coordinate* pEndPoint, int iDistance, NPC_CURRENT_STATUS npcStatus, int iRange, bool bNear)
{
	DWORD dwCurTime = GetTickCount();
	
	if (NULL != pGameMap && (iResultPathArray < 0 || MAX_NPC_COUNT <= iResultPathArray)) {
		return iResultPathArray;
	}

	// 기본 설정
	m_pGameMap = pGameMap;
	m_idistance = iDistance;
	m_npcStatus = npcStatus;
	m_iRange = iRange;
	m_iCurrentNum = 0;

	corCenter.x = pStartPoint->x;
	corCenter.y = pStartPoint->y;

	// 사전 검증
	if (IsNeighborhood(pStartPoint, pEndPoint) && !ExploreMap(pEndPoint->x, pEndPoint->y)) {
		return iResultPathArray;
	}

	// 힙 기반 오픈 리스트 사용
	if (m_bUseHeapOptimization && m_pOpenHeap) {
		m_pOpenHeap->Clear();
		
		// 시작 노드 설정
		m_pNode[m_iCurrentNum].SetNode(pStartPoint->x, pStartPoint->y, NULL, *pEndPoint);
		m_pOpenHeap->Push(&m_pNode[m_iCurrentNum++]);
		
		Coordinate NewEndPoint = *pEndPoint;
		Node* SNodeNear = NULL;
		int cntLimit = 0;
		
		// 힙 기반 메인 루프
		while (!m_pOpenHeap->IsEmpty() && cntLimit < m_iAstarPathMaxTry) {
			Node* SNode = m_pOpenHeap->Pop();
			
			// 목적지 도달 검사
			if (SNode->point.x == NewEndPoint.x && SNode->point.y == NewEndPoint.y) {
				break;
			}
			
			// 공격 범위 내 도달 검사
			if (NPC_CURRENT_STATUS::ATTACK == m_npcStatus && 
				CheckDIstance(&SNode->point, &NewEndPoint, m_iRange)) {
				NewEndPoint = SNode->point;
				break;
			}
			
			// 노드 탐색
			NewEndPoint = ExploreNode_v2(SNode, NewEndPoint);
			
			// 가장 가까운 노드 업데이트
			if (SNodeNear == NULL || SNodeNear->H > SNode->H) {
				SNodeNear = SNode;
			}
			
			// 닫힌 노드에 추가
			m_CloseNode.set(SNode->point.x * pGameMap->m_MapInfo->_i32MaxSize + SNode->point.y);
			cntLimit++;
		}
		
		// 경로 재구성 및 결과 저장
		// ... (기존과 동일한 경로 재구성 로직)
		
	} else {
		// 기존 방식으로 폴백
		return FindPath_New(iResultPathArray, pGameMap, pStartPoint, pEndPoint, 
			iDistance, npcStatus, iRange, bNear);
	}
	
	g_DebugManager.m_dwNPC_thread_Process_Cnt[m_id]++;
	g_DebugManager.m_dwNPC_thread_Process[m_id] += GetTickCount() - dwCurTime;
	
	return iResultPathArray;
}

// v2: 힙을 사용한 개선된 노드 탐색
Coordinate Astar::ExploreNode_v2(Node* SNode, Coordinate EndPoint)
{
	// 8방향 탐색 최적화
	const int dx[] = {-1, 0, 1, 0, -1, -1, 1, 1};
	const int dy[] = {0, 1, 0, -1, -1, 1, -1, 1};
	const int moveCost[] = {10, 10, 10, 10, 14, 14, 14, 14}; // 직선:10, 대각선:14
	
	for (int dir = 0; dir < 8; dir++) {
		int newX = SNode->point.x + dx[dir];
		int newY = SNode->point.y + dy[dir];
		
		// 맵 경계 및 이동 가능성 검사
		if (!ExploreMap(newX, newY)) {
			// 목적지인 경우 도달 신호
			if (newX == EndPoint.x && newY == EndPoint.y) {
				return SNode->point;
			}
			continue;
		}
		
		// 대각선 이동 시 추가 검증
		if (dir >= 4) {
			int checkX1 = SNode->point.x + dx[dir % 4];
			int checkY1 = SNode->point.y;
			int checkX2 = SNode->point.x;
			int checkY2 = SNode->point.y + dy[dir % 4];
			
			if (!ExploreMap(checkX1, checkY1) || !ExploreMap(checkX2, checkY2)) {
				continue; // 대각선 경로가 막힌 경우
			}
		}
		
		// 기존 노드 확인
		Node* pExistingNode = GetNode(newX, newY);
		if (pExistingNode != NULL) {
			// G값 업데이트 검사
			int newG = SNode->G + moveCost[dir];
			if (pExistingNode->G > newG) {
				pExistingNode->G = newG;
				pExistingNode->F = pExistingNode->G + pExistingNode->H;
				pExistingNode->pParent = SNode;
			}
			continue;
		}
		
		// 닫힌 노드 검사
		if (m_CloseNode[newX * m_pGameMap->m_MapInfo->_i32MaxSize + newY]) {
			continue;
		}
		
		// 새 노드 생성
		if ((g_INIFile_Server.m_iNPC_FIND_Path_Count * 8) <= m_iCurrentNum) {
			return SNode->point; // 메모리 부족
		}
		
		m_pNode[m_iCurrentNum].SetNode(newX, newY, SNode, EndPoint);
		
		// 휴리스틱 가중치 적용
		m_pNode[m_iCurrentNum].H = (int)(m_pNode[m_iCurrentNum].H * m_fHeuristicWeight);
		m_pNode[m_iCurrentNum].F = m_pNode[m_iCurrentNum].G + m_pNode[m_iCurrentNum].H;
		
		SetNode(newX, newY, &m_pNode[m_iCurrentNum]);
		
		if (m_bUseHeapOptimization && m_pOpenHeap) {
			m_pOpenHeap->Push(&m_pNode[m_iCurrentNum++]);
		}
	}
	
	return EndPoint;
}

// v2: 힙 최적화된 다음 노드 찾기 (호환성을 위한 함수)
list<Astar::Node*>::iterator Astar::FindNextNode_v2(list<Astar::Node*>* pOpenNode)
{
	// 힙을 사용하는 경우에는 이 함수가 호출되지 않아야 하지만,
	// 호환성을 위해 기존 방식으로 구현
	return FindNextNode(pOpenNode);
}

// v2: 경로 최적화 후처리
void Astar::OptimizePath_v2(int iResultPathArray)
{
	if (iResultPathArray < 0 || iResultPathArray >= MAX_NPC_COUNT) return;
	
	auto& pathResult = g_PathFindThread.m_pPathResults[iResultPathArray];
	
	// 직선 경로 단순화
	if (pathResult.path.size() > 2) {
		auto iter = pathResult.path.begin();
		auto prev = iter++;
		auto curr = iter++;
		
		while (curr != pathResult.path.end()) {
			// 세 점이 직선상에 있는지 검사
			int dx1 = (*curr)->x - (*prev)->x;
			int dy1 = (*curr)->y - (*prev)->y;
			int dx2 = (*iter)->x - (*curr)->x;
			int dy2 = (*iter)->y - (*curr)->y;
			
			// 방향이 같으면 중간 점 제거
			if (dx1 * dy2 == dy1 * dx2 && 
				((dx1 > 0 && dx2 > 0) || (dx1 < 0 && dx2 < 0) || (dx1 == 0 && dx2 == 0)) &&
				((dy1 > 0 && dy2 > 0) || (dy1 < 0 && dy2 < 0) || (dy1 == 0 && dy2 == 0))) {
				
				// 중간 점 제거
				pathResult.path.erase(curr);
				pathResult.m_iMaxPath--;
				curr = iter;
			} else {
				prev = curr;
				curr = iter;
			}
			
			if (iter != pathResult.path.end()) {
				++iter;
			}
		}
	}
}

// v2: PathResult 인덱스로부터 CFindPath 객체 획득
CFindPath* Astar::GetNPCPathObject(int iResultPathArray)
{
	// 유효성 검사
	if (iResultPathArray < 0 || iResultPathArray >= MAX_NPC_COUNT) {
		g_DebugManager.WriteFindPathStartLog(m_id, __FILE__, __LINE__, 
			"GetNPCPathObject invalid index: %d\n", iResultPathArray);
		return NULL;
	}
	
	// PathResult가 유효한지 확인
	PathResult* pResult = &g_PathFindThread.m_pPathResults[iResultPathArray];
	if (!pResult->m_bFindPath || pResult->path.empty()) {
		return NULL; // 길찾기 실패했거나 경로가 없음
	}
	
	// CFindPath 객체 생성 및 PathResult 데이터 복사
	CFindPath* pFindPath = new CFindPath();
	pFindPath->Init_v2(pResult->m_iMaxPath);
	
	// PathResult의 경로 데이터를 CFindPath로 복사
	if (pResult->m_Path && pResult->m_iMaxPath > 0) {
		// 고정 배열 방식의 경로 데이터 복사
		for (int i = 0; i < pResult->m_iMaxPath; i++) {
			int x = pResult->m_Path[i].x;
			int y = pResult->m_Path[i].y;
			
			// 맵 크기 확인 (기본값 512 사용, 실제로는 MapInfo에서 가져와야 함)
			int mapSize = (pResult->m_pGameMap && pResult->m_pGameMap->m_MapInfo) ? 
						  pResult->m_pGameMap->m_MapInfo->_i32MaxSize : 512;
			
			if (!pFindPath->AddPath_v2(x, y, mapSize, false)) {
				g_DebugManager.WriteFindPathStartLog(m_id, __FILE__, __LINE__, 
					"GetNPCPathObject AddPath failed at index: %d\n", i);
				break;
			}
		}
	} else if (!pResult->path.empty()) {
		// 리스트 방식의 경로 데이터 복사
		int mapSize = (pResult->m_pGameMap && pResult->m_pGameMap->m_MapInfo) ? 
					  pResult->m_pGameMap->m_MapInfo->_i32MaxSize : 512;
		
		for (auto iter = pResult->path.begin(); iter != pResult->path.end(); ++iter) {
			Coordinate* pCoord = *iter;
			if (pCoord) {
				if (!pFindPath->AddPath_v2(pCoord->x, pCoord->y, mapSize, false)) {
					g_DebugManager.WriteFindPathStartLog(m_id, __FILE__, __LINE__, 
						"GetNPCPathObject AddPath from list failed\n");
					break;
				}
			}
		}
	}
	
	return pFindPath;
}

// ==================== v2 개선 함수들 ====================

// v2: 개선된 초기화 (동적 크기 조정 가능)
void CFindPath::Init_v2(int maxPathLength) {
	m_iPathSize = 0;
	m_iCurPos = 0;
	m_iCompressedSize = 0;
	
	// 경로 크기가 기본값보다 크면 재할당
	if (maxPathLength > m_iMaxPathLength) {
		delete[] m_pPathData;
		delete[] m_pCompressedPath;
		
		m_iMaxPathLength = maxPathLength;
		m_pPathData = new path_data[m_iMaxPathLength];
		m_iCompressedCapacity = m_iMaxPathLength / 2;
		m_pCompressedPath = new compressed_path_data[m_iCompressedCapacity];
	}
	
	memset(m_pPathData, 0x00, sizeof(path_data) * m_iMaxPathLength);
}

// v2: 스마트한 현재 위치 반환 (경로 최적화 포함)
path_data* CFindPath::GetCurPos_v2() {
	if (m_iCurPos < m_iPathSize) {
		path_data* current = &m_pPathData[m_iCurPos++];
		
		// 실시간 경로 최적화 옵션
		if (m_bAutoOptimize && m_iCurPos < m_iPathSize - 2) {
			// 다음 몇 개 점이 직선상에 있으면 건너뛰기
			path_data* next1 = &m_pPathData[m_iCurPos];
			path_data* next2 = &m_pPathData[m_iCurPos + 1];
			
			// 좌표 변환 (맵 크기 512 가정)
			int x1 = current->dwPos % 512, y1 = current->dwPos / 512;
			int x2 = next1->dwPos % 512, y2 = next1->dwPos / 512;
			int x3 = next2->dwPos % 512, y3 = next2->dwPos / 512;
			
			// 직선 검사
			if ((x2 - x1) * (y3 - y1) == (y2 - y1) * (x3 - x1)) {
				m_iCurPos++; // 중간 점 건너뛰기
			}
		}
		
		return current;
	}
	return NULL;
}

// v2: 중복 제거 및 최적화가 포함된 경로 추가
BOOL CFindPath::AddPath_v2(int x, int y, INT32 i32MaxSize, bool bOptimize) {
	if (x < 0 || y < 0 || x >= i32MaxSize || y >= i32MaxSize)
		return FALSE;

	if (m_iPathSize >= m_iMaxPathLength)
		return FALSE;

	DWORD newPos = x * i32MaxSize + y;
	
	// 중복 위치 제거
	if (m_iPathSize > 0 && m_pPathData[m_iPathSize - 1].dwPos == newPos) {
		return TRUE; // 중복이지만 성공으로 처리
	}
	
	// 실시간 최적화 (직선 경로 단순화)
	if (bOptimize && m_iPathSize >= 2) {
		DWORD prevPos = m_pPathData[m_iPathSize - 2].dwPos;
		DWORD currPos = m_pPathData[m_iPathSize - 1].dwPos;
		
		int prevX = prevPos % i32MaxSize, prevY = prevPos / i32MaxSize;
		int currX = currPos % i32MaxSize, currY = currPos / i32MaxSize;
		
		// 세 점이 직선상에 있으면 중간점 제거
		if ((currX - prevX) * (y - prevY) == (currY - prevY) * (x - prevX)) {
			m_pPathData[m_iPathSize - 1].dwPos = newPos; // 마지막 점을 새 점으로 교체
			return TRUE;
		}
	}
	
	m_pPathData[m_iPathSize++].dwPos = newPos;
	return TRUE;
}

// v2: 실제 남은 경로 수 (최적화된)
int CFindPath::GetFindPathCnt_v2() {
	int remainingCount = m_iPathSize - m_iCurPos;
	
	// 압축된 경로가 있으면 압축 기준으로 계산
	if (m_iCompressedSize > 0) {
		int compressedRemaining = 0;
		for (int i = 0; i < m_iCompressedSize; i++) {
			compressedRemaining += m_pCompressedPath[i].iLength;
		}
		return compressedRemaining;
	}
	
	return remainingCount;
}

// v2: 개선된 경로 압축 (더 효율적인 알고리즘)
void CFindPath::CompressPath_v2() {
	if (m_iPathSize < 2) {
		m_iCompressedSize = 0;
		return;
	}
	
	m_iCompressedSize = 0;
	
	// 적응형 세그먼트 압축
	int segmentStart = 0;
	
	while (segmentStart < m_iPathSize - 1) {
		DWORD startPos = m_pPathData[segmentStart].dwPos;
		int currentDirection = -1;
		int segmentLength = 1;
		int maxSegmentLength = 1;
		
		// 가능한 한 긴 직선 세그먼트 찾기
		for (int i = segmentStart + 1; i < m_iPathSize; i++) {
			DWORD prevPos = m_pPathData[i-1].dwPos;
			DWORD currPos = m_pPathData[i].dwPos;
			
			int prevX = prevPos % 512, prevY = prevPos / 512;
			int currX = currPos % 512, currY = currPos / 512;
			
			int direction = GetDirection(prevX, prevY, currX, currY);
			
			if (currentDirection == -1) {
				currentDirection = direction;
				maxSegmentLength = 2;
			} else if (currentDirection == direction) {
				maxSegmentLength++;
			} else {
				break; // 방향 변경됨
			}
		}
		
		// 세그먼트 저장
		AddCompressedSegment(startPos, currentDirection, maxSegmentLength);
		segmentStart += maxSegmentLength - 1;
	}
	
	// 압축률 검증
	if (m_iCompressedSize >= m_iPathSize) {
		// 압축 효과가 없으면 무시
		m_iCompressedSize = 0;
	}
}

// v2: 경로가 최적인지 검사
bool CFindPath::IsPathOptimal_v2() {
	if (m_iPathSize < 3) return true;
	
	// 불필요한 지그재그 패턴 검사
	int unnecessaryTurns = 0;
	
	for (int i = 1; i < m_iPathSize - 1; i++) {
		DWORD pos1 = m_pPathData[i-1].dwPos;
		DWORD pos2 = m_pPathData[i].dwPos;
		DWORD pos3 = m_pPathData[i+1].dwPos;
		
		int x1 = pos1 % 512, y1 = pos1 / 512;
		int x2 = pos2 % 512, y2 = pos2 / 512;
		int x3 = pos3 % 512, y3 = pos3 / 512;
		
		// 각도 변화가 큰 경우 카운트
		int dx1 = x2 - x1, dy1 = y2 - y1;
		int dx2 = x3 - x2, dy2 = y3 - y2;
		
		// 방향이 반대인 경우 (지그재그)
		if (dx1 * dx2 < 0 || dy1 * dy2 < 0) {
			unnecessaryTurns++;
		}
	}
	
	// 경로 길이 대비 턴이 많으면 최적이 아님
	return (unnecessaryTurns * 3 < m_iPathSize);
}

// v2: 경로 스무딩 (곡선 보간)
void CFindPath::SmoothPath_v2() {
	if (m_iPathSize < 3 || m_fSmoothingFactor <= 0.0f) return;
	
	path_data* smoothedPath = new path_data[m_iPathSize];
	smoothedPath[0] = m_pPathData[0]; // 첫 점 유지
	smoothedPath[m_iPathSize - 1] = m_pPathData[m_iPathSize - 1]; // 마지막 점 유지
	
	// 중간 점들을 주변 점들의 가중 평균으로 조정
	for (int i = 1; i < m_iPathSize - 1; i++) {
		DWORD pos0 = m_pPathData[i-1].dwPos;
		DWORD pos1 = m_pPathData[i].dwPos;
		DWORD pos2 = m_pPathData[i+1].dwPos;
		
		int x0 = pos0 % 512, y0 = pos0 / 512;
		int x1 = pos1 % 512, y1 = pos1 / 512;
		int x2 = pos2 % 512, y2 = pos2 / 512;
		
		// 가중 평균 계산
		int smoothX = (int)(x1 * (1.0f - m_fSmoothingFactor) + 
							(x0 + x2) * 0.5f * m_fSmoothingFactor);
		int smoothY = (int)(y1 * (1.0f - m_fSmoothingFactor) + 
							(y0 + y2) * 0.5f * m_fSmoothingFactor);
		
		smoothedPath[i].dwPos = smoothX * 512 + smoothY;
	}
	
	// 원본을 스무딩된 경로로 교체
	memcpy(m_pPathData, smoothedPath, sizeof(path_data) * m_iPathSize);
	delete[] smoothedPath;
}

```
대체가 가능한 부분들의 V2함수들을 배치하였고, 해당 부분들을 언제든 뺄 수 있게 Define작업을 하였다.

</details>

#### 3.5 Math 로직 최적화
기존의 공격 로직의 분할이 필요해 보여 작업을 하였다.
<details>
<summary>기존 코드</summary>
	
```ruby

/// <summary>
/// 조선협객전2 공격할 때
/// </summary>
/// <param name="pUnit">공격하는 유닛</param>
/// <param name="pTarget">맞는 유닛</param>
void CMath::AttackTarget_Chosun2M(CUnit* pUnit, CUnit* pTarget)
{

#pragma region 평타 계산식		24.06.14 이제연
	// 24.07.19 이제연 수정
	// 
	// 순서
	// 
	// 적중률 => 1차 대미지 => 2차 대미지 => 3차 대미지 => 최종 대미지 => 추가 대미지 => 경직 및 경직 회피
	// 
	// 적중률 :	기본 70 + 공격자 적중 수치 - 방어자 회피 수치 >= 랜덤값 (1~100) = 적중 
	//			기본 70 + 공격자 적중 수치 - 방어자 회피 수치 <  랜덤값 (1~100) = 회피
	// 
	// 1차 대미지 : (공격력 * ( 1 - (방어력 - 방어력무시) / ( 방어력 + 방어상수 500))) * 랜덤값 0.95 ~ 1.05
	//				(방어력 - 방어력무시) 값이 0이 나오면 공격력 * 랜덤값
	//				
	// 2차 대미지 :	대미지 증가 수치 = 공격자 대미지 증가(근,원,도) - 방어자 대미지 증가 무시(근,원,도)
	//				대미지 증가 수치가 >  0 이면 기본대미지 * (1 + 대미지 증가 수치 * 0.01 )
	//				대미지 증가 수치가 <= 0 이면 기본 대미지
	// 
	// 3차 대미지 :	일격필살 발동 = 일격필살 확률 >= 랜덤값 (1 ~ 100)
	//				일격 필살 대미지 증가 수치 = 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
	// 
	//				일격 필살 미 발동시 대미지 = 2차 대미지
	// 
	//				일격 필살 대미지 증가 수치 > 0 이면 (2차 대미지) * (1.25 + (대미지 증가수치 * 0.001) + (랜덤값 0.01~ 0.05))
	//				일격 필살 대미지 증가 수치 <= 0 이면 2차 대미지
	//																
	//			
	// 최종 대미지 : 3차 대미지 + (pve, pvp 공격력 - pve, pvp 방어력)
	//							 수치가 음수여도 그대로 적용
	// 
	// 추가 대미지 : 공격자 추가 대미지 증가 - 방어자 추가 대미지 증가 무시 (근, 원, 도)
	//				추가 대미지 수치가 > 0 이면 최종 대미지 * (1 + 추가 대미지 증가 *0.01)
	//				추가 대미지 수치가 <= 이면 최종 대미지
	// 
	// 경직 및 경직회피 : 50 + 공격자 경직 적중 수치 - 방어자 경직 회피 수치 >= 랜덤값 ( 1~100) 이면 경직 발동
	//																	< 랜덤값 (1~100)이면 회피
#pragma endregion

	if (pUnit == NULL)
	{
		return;
	}

	if (pTarget == NULL)
	{
		CUnitPC* pcheckPC = dynamic_cast<CUnitPC*>(pUnit); //더미가 상대가 죽은 것을 받지 못하였을 때 버그로 인해 생성
		if (pcheckPC)
		{
			SP_Attack sendCheckmsg;
			sendCheckmsg._result = ENUM_ALL_ERROR_ATTACK_NOT_TARGET;
			pcheckPC->Write((BYTE*)&sendCheckmsg, sizeof(SP_Attack), 0);
		}
		return;
	}

	if (pUnit->GetCurrentMap() == NULL || pTarget->GetCurrentMap() == NULL || pUnit->GetCurrentMap() != pTarget->GetCurrentMap())
	{
		CUnitPC* pcheckPC = dynamic_cast<CUnitPC*>(pUnit); //더미가 상대가 죽은 것을 받지 못하였을 때 버그로 인해 생성
		if (pcheckPC)
		{
			SP_Attack sendCheckmsg;
			sendCheckmsg._result = ENUM_ALL_ERROR_ATTACK_NOT_TARGET;
			pcheckPC->Write((BYTE*)&sendCheckmsg, sizeof(SP_Attack), 0);
		}
		return;
	}

	CGameMap* pCurrentMap = pUnit->GetCurrentMap();

	INT32 i32Range = 1;
	BOOL bRange = FALSE;

	SP_Attack sendmsg;
	sendmsg.dwFieldUnique = pUnit->GetFieldUnique();
	sendmsg.dwFieldUnique_Target = pTarget->GetFieldUnique();
	sendmsg._position = pTarget->GetPosition();

	SP_Attack_Extra extramsg;
	extramsg.dwFieldUnique = pUnit->GetFieldUnique();
	extramsg.dwFieldUnique_Target = pTarget->GetFieldUnique();
	extramsg._position = pTarget->GetPosition();

	SP_Attack_Extra passiveextramsg;
	passiveextramsg._result = 4; // 구분용
	passiveextramsg.dwFieldUnique = pUnit->GetFieldUnique();
	passiveextramsg.dwFieldUnique_Target = pTarget->GetFieldUnique();
	passiveextramsg._position = pTarget->GetPosition();

	//sendmsg.iDamage = 0;
	CUnitPC* pPC = dynamic_cast<CUnitPC*>(pUnit);
	CUnitNPC* pNPC = dynamic_cast<CUnitNPC*>(pUnit);
	CUnitClone* pClonePC = dynamic_cast<CUnitClone*>(pUnit);
	memset(sendmsg._i32AttackType,0,sizeof(INT32) * MAX_COUNT_DAMAGE);
	memset(extramsg._i32AttackType,0,sizeof(INT32) * MAX_COUNT_DAMAGE);
	memset(passiveextramsg._i32AttackType,0,sizeof(INT32) * MAX_COUNT_DAMAGE);
	if (pPC != NULL)
	{
		for (int i = 0 ; i < MAX_COUNT_DAMAGE; i++)
		{
			if (pPC->GetHideAvatar() != TRUE && pPC->_AvatarList.GetUseAvatarIndex() != 0)
			{
				if (pPC->_AvatarList.GetAvatarSkinIndex() != 0)
				{
					// 투명 둔갑이면
					if (pPC->_AvatarList.GetAvatarSkinIndex() == TRANSPARENCY_TRANSFORM_INDEX)
					{// 기존 자신 캐릭터의 어택 타입을 보내기
						sendmsg._i32AttackType[i] = pPC->GetJobCode() + (pPC->GetGender() == PC_MAN ? 0 : 100);
						extramsg._i32AttackType[i] = pPC->GetJobCode() + (pPC->GetGender() == PC_MAN ? 0 : 100);
						passiveextramsg._i32AttackType[i] = pPC->GetJobCode() + (pPC->GetGender() == PC_MAN ? 0 : 100);
					}
					else
					{
						auto pAvatarData = g_AvatarManager.FindAvatarData(pPC->_AvatarList.GetAvatarSkinIndex());
						sendmsg._i32AttackType[i] = pAvatarData->_i32AttackIndex;
						extramsg._i32AttackType[i] = pAvatarData->_i32AttackIndex;
						passiveextramsg._i32AttackType[i] = pAvatarData->_i32AttackIndex;
					}
				}
				else
				{
					auto pAvatarData = g_AvatarManager.FindAvatarData(pPC->_AvatarList.GetUseAvatarIndex());
					sendmsg._i32AttackType[i] = pAvatarData->_i32AttackIndex;
					extramsg._i32AttackType[i] = pAvatarData->_i32AttackIndex;
					passiveextramsg._i32AttackType[i] = pAvatarData->_i32AttackIndex;
				}


			}
			else
			{
				sendmsg._i32AttackType[i] = pPC->GetJobCode() + (pPC->GetGender() == PC_MAN ? 0 : 100);
				extramsg._i32AttackType[i] = pPC->GetJobCode() + (pPC->GetGender() == PC_MAN ? 0 : 100);
				passiveextramsg._i32AttackType[i] = pPC->GetJobCode() + (pPC->GetGender() == PC_MAN ? 0 : 100);
			}
			auto num = g_GameManager.getRandomNumber(0, 1);
			sendmsg._i32AttackType[i] = sendmsg._i32AttackType[i] + (num * RANDOM_NEXT_ATTACK_INDEX);
			extramsg._i32AttackType[i] = sendmsg._i32AttackType[i] + (num * RANDOM_NEXT_ATTACK_INDEX);
			passiveextramsg._i32AttackType[i] = sendmsg._i32AttackType[i] + (num * RANDOM_NEXT_ATTACK_INDEX);
		}
	}
	else if (pNPC != NULL)
	{
		sendmsg._i32AttackType[0] = pNPC->GetAttackIndex();
		auto num = g_GameManager.getRandomNumber(0, 1);
		sendmsg._i32AttackType[0] = sendmsg._i32AttackType[0] + (num * RANDOM_NEXT_ATTACK_INDEX);
	}
	else if (pClonePC)
	{
		if (pClonePC->GetTransformIndex() != 0)
		{
			auto pAvatarData = g_AvatarManager.FindAvatarData(pClonePC->GetTransformIndex());
			sendmsg._i32AttackType[0] = pAvatarData->_i32AttackIndex;
		}
		else
		{
			sendmsg._i32AttackType[0] = pClonePC->GetJobCode() + (pClonePC->GetGender() == PC_MAN ? 0 : 100);
		}
		auto num = g_GameManager.getRandomNumber(0, 1);
		sendmsg._i32AttackType[0] = sendmsg._i32AttackType[0] + (num * RANDOM_NEXT_ATTACK_INDEX);
	}
	memset(sendmsg._ui32Damage, 0x00, sizeof(UINT32) * MAX_COUNT_DAMAGE);
	memset(sendmsg._ui32ExtraDamage, 0x00, sizeof(UINT32) * MAX_COUNT_DAMAGE);
	memset(extramsg._ui32Damage, 0x00, sizeof(UINT32) * MAX_COUNT_DAMAGE);
	memset(passiveextramsg._ui32Damage, 0x00, sizeof(UINT32) * MAX_COUNT_DAMAGE);
	//memset(extramsg._ui32ExtraDamage, 0x00, sizeof(UINT32) * MAX_COUNT_DAMAGE);

	CBuff* newBuff = NULL;
	INT32 i32ItemOptionIndex = 0;
	INT32 i32DurationTime = 0;

	// ============================= 적중률 계산 ================================
	BOOL bMiss = MissCheck(pUnit, pTarget);
	INT32 i32Random = 0;
	// 때리는 사람이 유저 일때
	if (pUnit->GetUnitTYPE() == eUnitType::PC)
	{
		// 때리는 애 
		CUnitPC* pPC = dynamic_cast<CUnitPC*>(pUnit);

		if (pPC == NULL)
		{
			return;
		}
		INT32 i32AttackCount = pPC->GetAttackCount();
		INT32 i32SuccessAttackCount = 0;
		INT32 i32SuccessSpecialCount = 0;
		INT32 i32SuccessSpecialExtraCount = 0; // 얘는 사실상 안씀
		INT32 i32HpPersent = 100;
		i32Range = AttackRange(pPC->GetJobCode());
		INT32 i32Avatar = pPC->_AvatarList.GetUseAvatarIndex();
		if (i32Avatar != 0)
		{
			auto pAvatar = g_AvatarManager.FindAvatarData(i32Avatar);
			if (pAvatar)
			{
				i32Range = pAvatar->_i32AttackRange;
			}
		}

		// 평타 사거리 증가량 확인
		i32Range += pPC->GetRangeAttackAdd();

		// 맞는 애
		//CUnitPC* pTargetPC = dynamic_cast<CUnitPC*>(pTarget);

		if (pTarget == NULL)
		{
			sendmsg._result = ENUM_ALL_ERROR_ATTACK_NOT_TARGET;
			if (i32SuccessAttackCount != 0)
			{
				sendmsg._result = SUCCESS;
			}
			pPC->Write((BYTE*)&sendmsg, sizeof(SP_Attack));
			return;
		}

		if (pTarget->GetAlive() == FALSE)
		{
			sendmsg._result = ENUM_ALL_ERROR_ATTACK_NOT_TARGET;
			if (i32SuccessAttackCount != 0)
			{
				sendmsg._result = SUCCESS;
			}
			pPC->Write((BYTE*)&sendmsg, sizeof(SP_Attack));
			return;
		}


		for (int i = 0; i < i32AttackCount; i++)
		{
			BOOL bExtraPassive = CheckPassiveExtra(pUnit);
			// ======================== PVP ================================
			if (pTarget->GetUnitTYPE() == eUnitType::PC)
			{
				CUnitPC* pTargetPC = dynamic_cast<CUnitPC*>(pTarget);
				if (pTargetPC == NULL)
				{
					return;
				}

				if (pPC->GetAccountType() != ACCTYPE_DUMMY)
				{
					//더미 계정 거리계산 안함
					bRange = g_MapManager.IsRange(i32Range, pUnit->m_X, pUnit->m_Y, pTarget->m_X, pTarget->m_Y);
					if (bRange == FALSE)
					{
						sendmsg._result = ENUM_ALL_ERROR_ATTACK_NOT_RANGE;
						pPC->Write((BYTE*)&sendmsg, sizeof(SP_Attack), 0);
						return;
					}
				}
			
				// 회피 했다.
				if (bMiss == TRUE)
				{
					//sendmsg._result = 2;
					sendmsg._result = 0;
					sendmsg._ui32Damage[i] = 0;
					sendmsg._ui32ExtraDamage[i] = 0;
					INT32 i32AttackAnimationIdx = pUnit->GetAttAnimationIndex(FALSE);
					//sendmsg._result |= (pTarget->GetHpPercent() << 4);
					//sendmsg._result |= i32AttackAnimationIdx << 16;
					i32HpPersent = pTarget->GetHpPercent();
					//pCurrentMap->m_BlockManager.Attack_to_BroadCast(pUnit, pTarget, &sendmsg);
					sendmsg.i32Critical = FALSE;
					sendmsg.i32ExtraCritical = FALSE;
					//pPC->BattleActionEvent(QUEST_PURPOSE_MISS);
					//pTargetPC->BattleActionEvent(QUEST_PURPOSE_GET_MISS);
					i32SuccessAttackCount++;
					bMiss = MissCheck(pUnit, pTarget);
					continue;
				}

				//혼불 사용 여부 판단 하여 차감 진행
				pPC->_SoulfireList.UserSoulFire(pPC, ENUM_SOULFIRE_TYPE::NOMAL_ATTACK);
				pTargetPC->_SoulfireList.UserSoulFire(pTargetPC, ENUM_SOULFIRE_TYPE::NOMAL_DEFENSE);

				// 추가타 확인해서 혼불 차감
				if (bExtraPassive)
				{
					pPC->_SoulfireList.UserSoulFire(pPC, ENUM_SOULFIRE_TYPE::NOMAL_ATTACK);
					pTargetPC->_SoulfireList.UserSoulFire(pTargetPC, ENUM_SOULFIRE_TYPE::NOMAL_DEFENSE);
				}

				/* 여기서 부터 검사할건 모두 했다. */
				// 누가 때린건지 기록
				pTargetPC->AddHurtUnit(pUnit->GetFieldUnique(), pUnit->GetUnitTYPE());

				sendmsg._result = 0;
				extramsg._result = 0;
				BOOL bCritical = FALSE;
				BOOL bExtraCritical = FALSE;
				INT32 i32AttackAnimationIdx = pPC->GetAttAnimationIndex(FALSE);

				// ======================== 대미지 계산 ================================
				INT32 i32AllDamage = AttackTarget_Damage_PCPC(pUnit,pTarget, bCritical);
				INT32 i32SpecialDamage = GetAttackExtraDamage_Chosun2M(pPC, pTarget, i32AllDamage, i32SuccessSpecialCount);
				INT32 i32AllExtraDamage = 0;
				INT32 i32SpecialExtraDamage = 0;
				if (bExtraPassive)
				{
					i32AllExtraDamage = AttackTarget_Damage_PCPC(pUnit, pTarget, bExtraCritical);
					i32SpecialExtraDamage = GetAttackExtraDamage_Chosun2M(pPC, pTarget, i32AllExtraDamage, i32SuccessSpecialExtraCount);
				}
	 
				INT32 i32TotalDamage = i32AllDamage + i32SpecialDamage + i32AllExtraDamage + i32SpecialExtraDamage;
				if (i32TotalDamage <= 0)
				{
					i32TotalDamage = 1;
					//sendmsg.iDamage = i32AllDamage;
				}
				pTargetPC->AddHP(-i32TotalDamage);

				if (pUnit->GetUnitTYPE() == eUnitType::PC)
				{
					CUnitPC* _pUser = dynamic_cast<CUnitPC*>(pUnit);
					if (_pUser != NULL)
					{
						_pUser->CheckMaxDamageLog(pTarget, i32TotalDamage);
					}
				}

				sendmsg._ui32Damage[i] = i32AllDamage;
				sendmsg._ui32ExtraDamage[i] = i32AllExtraDamage;
				sendmsg.i32Critical |= bCritical << i;
				sendmsg.i32ExtraCritical |= bExtraCritical << i;

				extramsg._ui32Damage[i] = i32SpecialDamage;
				passiveextramsg._ui32Damage[i] = i32SpecialExtraDamage;
				extramsg.i32Critical |= bCritical << i;
				passiveextramsg.i32Critical |= bExtraCritical << i;
				//sendmsg.iDamage = i32AllDamage;
				//sendmsg.i32Critical = bCritical;

				// 만약 여기서 타겟이 염화결계 버프를 가지고있으면 일정확률로 화상버프를 나한테 넣어야한다..
				// 반사로 줄 버프 가져오는데 없으면 반사스킬이 적용되어있지않다.
				// 의도된 사항 : 평타만 반사되도록
				stBuffData* reflectBuff = pTargetPC->GetReflectSkillData();
				if (reflectBuff)
				{
					g_BuffManager.AddBuff(reflectBuff, pTargetPC, pPC);	// 반사버프
				}

				//강제 셋팅
				if (pTargetPC->GetCurrentHP() > 0 && pTargetPC->GetStiffnessTime() <= 0)
				{
					// ======================== 경직 계산 ================================
					if (StiffnssCheck(pUnit,pTarget))
					{
						pTargetPC->SetStiffnessTime();
						SP_STIFFNESS sendstiffness;
						sendstiffness._dwTargetFieldUnique = pTargetPC->GetFieldUnique();
						sendstiffness._dwFieldUnique = pUnit->GetFieldUnique();
						if(pTargetPC->GetCurrentMap())
							pTargetPC->GetCurrentMap()->m_BlockManager.BroadCast(pTarget, (BYTE*)&sendstiffness, sizeof(SP_STIFFNESS));
					}
				}

				//데미지 핵 체크 - 로직 수정 필요
				//g_GameManager.WriteAttackException(pPC, sendmsg.iDamage, 0);
			
				// PK 쿨타임 적용
				if (pPC->GetPKCoolTime() <= 0)
				{
					switch (pPC->GetMapType())
					{
					case ENUM_PVP_MAP_TYPE::ENUM_PVP_MAP_TYPE_SAFE:
					{
						pPC->SetPKCoolTime(SAFE_PVP_MAP_COOL);
					}
					break;
					case ENUM_PVP_MAP_TYPE::ENUM_PVP_MAP_TYPE_NORMAL:
					{
						pPC->SetPKCoolTime(NORMAL_PVP_MAP_COOL);
					}
					break;
					case ENUM_PVP_MAP_TYPE::ENUM_PVP_MAP_TYPE_DANGER:
					{
						// 상대 PK꺼져있으면 PK켜주자
						if (pTargetPC->CheckPKMode() == FALSE)
						{
							if (pTargetPC->OptionAttackReturn() == TRUE)
							{//반격 켜두었을 때만 PK 진행
								pTargetPC->SetPKMode(TRUE);
								SP_PKINFO sendpkInfo;
								sendpkInfo.dwField = pTargetPC->GetFieldUnique();
								sendpkInfo.dwteamunique = pTargetPC->GetTeamunique();
								sendpkInfo._i32PKGrade = (INT32)pTargetPC->GetPKGrade();
								sendpkInfo.bPkmode = pTargetPC->CheckPKMode();
								if(pTargetPC->GetCurrentMap())
									pTargetPC->GetCurrentMap()->m_BlockManager.BroadCast(pTargetPC, (BYTE*)&sendpkInfo, sizeof(SP_PKINFO), 0);
								pTargetPC->SetPKCoolTime(DANGER_PVP_MAP_COOL);
							}
						}
						else
						{
							pTargetPC->SetPKCoolTime(DANGER_PVP_MAP_COOL);
						}
						pPC->SetPKCoolTime(DANGER_PVP_MAP_COOL);
					}
					break;
					case ENUM_PVP_MAP_TYPE::ENUM_PVP_MAP_TYPE_DISPUTE:
					{
						pPC->SetPKCoolTime(DISPUTE_PVP_MAP_COOL);
					}
					break;
					default:
					{
						pPC->SetPKCoolTime(0);
					}
					break;
					}
				}
				bMiss = MissCheck(pUnit, pTarget);
				i32SuccessAttackCount++;
			}
			// ======================== PVE ================================
			else if (pTarget->GetUnitTYPE() == eUnitType::NPC)
			{
				CUnitNPC* pTargetNPC = dynamic_cast<CUnitNPC*>(pTarget);
				if (pTargetNPC == NULL || pTargetNPC->GetNPC_CSVData() == NULL)
				{
					CUnitPC* pcheckPC = dynamic_cast<CUnitPC*>(pUnit); //더미가 상대가 죽은 것을 받지 못하였을 때 버그로 인해 생성
					if (pcheckPC)
					{
						SP_Attack sendCheckmsg;
						sendCheckmsg._result = ENUM_ALL_ERROR_ATTACK_NOT_TARGET;
						pcheckPC->Write((BYTE*)&sendCheckmsg, sizeof(SP_Attack), 0);
					}
					return;
				}

				if (pPC->GetAccountType() != ACCTYPE_DUMMY) { //더미 데이터 거리 해제
					bRange = g_MapManager.IsRange(i32Range + pTargetNPC->GetNPC_CSVData()->TileSize + pTargetNPC->GetNPC_CSVData()->_i32HitSize, pUnit->m_X, pUnit->m_Y, pTarget->m_X, pTarget->m_Y);

					if (bRange == FALSE)
					{
						sendmsg._result = ENUM_ALL_ERROR_ATTACK_NOT_RANGE;
						sendmsg._result |= (pTargetNPC->GetHpPercent() << 4);
						pPC->Write((BYTE*)&sendmsg, sizeof(SP_Attack), 0);
						return;
					}
				}

				// 회피 했다.
				if (bMiss == TRUE)
				{
					//sendmsg._result = 2;
					sendmsg._result = 0;
					sendmsg._ui32Damage[i] = 0;
					sendmsg._ui32ExtraDamage[i] = 0;
					INT32 i32AttackAnimationIdx = pUnit->GetAttAnimationIndex(FALSE);
					//sendmsg._result |= (pTarget->GetHpPercent() << 4);
					i32HpPersent = pTargetNPC->GetHpPercent();
					//sendmsg._result |= i32AttackAnimationIdx << 16;
					//pCurrentMap->m_BlockManager.Attack_to_BroadCast(pUnit, pTarget, &sendmsg);
					sendmsg.i32Critical = FALSE;
					sendmsg.i32ExtraCritical = FALSE;
					//pPC->BattleActionEvent(QUEST_PURPOSE_MISS);
					//pTargetPC->BattleActionEvent(QUEST_PURPOSE_GET_MISS);
					if (pTargetNPC->GetNPC_CSVData()->Type != VILLAGE_NPC)
					{
						// 미스도 어그로 끌개 수정
						if (pTargetNPC->GetDamage_Unit()->dwUnitUnique <= 0)
						{
							BOOL bLoopTickChange = FALSE;
							if (pTargetNPC->GetCurrentStatus() != NPC_CURRENT_STATUS::ATTACK
								&& pTargetNPC->GetMoveTile() == FALSE)
							{
								bLoopTickChange = TRUE;
							}
							pTargetNPC->SetDamageUnit(pPC->GetPoolArray(), pPC->GetFieldUnique(), pPC->m_X, pPC->m_Y);
							if (bLoopTickChange)
							{ //남아 있는 틱 제거
								INT32 i32Xcha = abs(pTargetNPC->m_X - pPC->m_X);
								INT32 i32Ycha = abs(pTargetNPC->m_Y - pPC->m_Y);
								if (pPC->GetJobCode() == JOB_TYPE::JOB_TYPE_SWORD)
								{
									pTargetNPC->m_LoopTick = GetTickCount64() + 650;
								}
								else
								{
									//pTargetNPC->m_LoopTick = GetTickCount64() + ((i32Xcha + i32Ycha) * 27.5);
									pTargetNPC->m_LoopTick = GetTickCount64() + ((i32Xcha + i32Ycha) * 25) + 500;
								}
							}
						}
						/*추가 -> 유저 주변 합공몬스터 체크 후 어그로 끌어주기*/
						if(pPC->GetCurrentMap())
							pPC->GetCurrentMap()->m_BlockManager.SetJoinAttackDamageUnit(pPC, MAX_JOIN_ATTACK_SCOPE);
					}
					i32SuccessAttackCount++;
					bMiss = MissCheck(pUnit, pTarget);
					continue;
				}

				//혼불 사용 여부 판단 하여 차감 진행
				pPC->_SoulfireList.UserSoulFire(pPC, ENUM_SOULFIRE_TYPE::NOMAL_ATTACK);

				// 패시브 추가타 있으면 혼불 차감
				if (bExtraPassive)
				{
					pPC->_SoulfireList.UserSoulFire(pPC, ENUM_SOULFIRE_TYPE::NOMAL_ATTACK);
				}

				pPC->SetLastTargetNpcField(pTargetNPC->GetFieldUnique()); // 마지막 공격 대상 필드유니크 세팅
				pPC->ResetConExpTick();

				if (pTargetNPC->GetNPC_CSVData()->Type == VILLAGE_NPC)
				{
					sendmsg._result = ENUM_ALL_ERROR_ATTACK_TOWN_NPC;
					pPC->Write((BYTE*)&sendmsg, sizeof(SP_Attack), 0);
					//printf("SP_ATTACK TARGET TYPE 0\n");
					return;
				}
				else
				{
					// 어그로 끌어주고
					if (pTargetNPC->GetDamage_Unit()->dwUnitUnique <= 0)
					{
						BOOL bLoopTickChange = FALSE;
						if (pTargetNPC->GetCurrentStatus() != NPC_CURRENT_STATUS::ATTACK
							&& pTargetNPC->GetMoveTile() == FALSE)
						{
							bLoopTickChange = TRUE;
						}
						pTargetNPC->SetDamageUnit(pPC->GetPoolArray(), pPC->GetFieldUnique(), pPC->m_X, pPC->m_Y);
						if (bLoopTickChange)
						{ //남아 있는 틱 제거
							INT32 i32Xcha = abs(pTargetNPC->m_X - pPC->m_X);
							INT32 i32Ycha = abs(pTargetNPC->m_Y - pPC->m_Y);
							if (pPC->GetJobCode() == JOB_TYPE::JOB_TYPE_SWORD)
							{
								pTargetNPC->m_LoopTick = GetTickCount64() + 650;
							}
							else
							{
								//pTargetNPC->m_LoopTick = GetTickCount64() + ((i32Xcha + i32Ycha) * 27.5);
								pTargetNPC->m_LoopTick = GetTickCount64() + ((i32Xcha + i32Ycha) * 25) + 500;
							}
						}
					}


					/*추가 -> 유저 주변 합공몬스터 체크 후 어그로 끌어주기*/
					if(pPC->GetCurrentMap())
						pPC->GetCurrentMap()->m_BlockManager.SetJoinAttackDamageUnit(pPC, MAX_JOIN_ATTACK_SCOPE);

					sendmsg._result = 0;
					extramsg._result = 0;
					BOOL bCritical = FALSE;
					BOOL bExtraCritical = FALSE;
					INT32 i32AllDamage = AttackTarget_Damage_PCNPC(pUnit,pTarget,bCritical);
					INT32 i32SpecialDamage = GetAttackExtraDamage_Chosun2M(pPC, pTarget, i32AllDamage, i32SuccessSpecialCount);
					INT32 i32AllExtraDamage = 0;
					INT32 i32SpecialExtraDamage = 0;

					if (bExtraPassive)
					{
						i32AllExtraDamage = AttackTarget_Damage_PCNPC(pUnit, pTarget, bExtraCritical);
						i32SpecialExtraDamage = GetAttackExtraDamage_Chosun2M(pPC, pTarget, i32AllDamage, i32SuccessSpecialCount);
					}

					INT32 i32TotalDamage = i32AllDamage + i32SpecialDamage + i32AllExtraDamage + i32SpecialExtraDamage;
					//INT32 i32AttackAnimationIdx = pPC->GetAttAnimationIndex(FALSE);
					INT32 i32AttackAnimationIdx = 0;
					int iExp = pTargetNPC->GetNpcAttackExp(i32TotalDamage); //

					//int iTendency = pTargetNPC->GetNPCAttackTendency(iDamage);

					int iMyLevel = pPC->GetLevel(); // 내 레벨

					int iLevel = iMyLevel - pTargetNPC->GetLevel();// 레벨 차이

					CParty* pParty = pPC->GetParty();

					// _i32LimitLevel이 1이면 레벨제한x
					if (pTargetNPC->GetNPC_CSVData()->_i32LimitLevel == 0 && iLevel > 0)	// 케릭 레벨이 몬스터 레벨보다 높으면  
					{
						if (iLevel < MAX_XP_LEVEL_GAP)	// 1 ~ 199 렙차 많이 날수록 경험치 깎음, 케릭터가 200이상 높으면 경험치 못먹음 
						{
							iExp -= iExp * iLevel * 0.005;
							//
							//if (pParty)
							//{
							//	int iNeerCnt = pParty->GetScopeUserCount(pPC);
							//	if (iNeerCnt >= 1)
							//	{
							//		pParty->AddExp(iExp, pTargetNPC->GetFieldUnique(), pPC,  pParty->GetScopeLowerLevel(pPC), iNeerCnt);
							//	}
							//	else
							//		pPC->AddExp(iExp, pTargetNPC->GetFieldUnique()); // 나혼자 경험치 먹고 
							//}
							//else
							//{
							pPC->AddExp(iExp, pTargetNPC->GetFieldUnique()); // 나혼자 경험치 먹고 
							//}
				
						}
					}
					else // 케릭 레벨이 몬스터 레벨보다 낮으면  
					{
						//if (pParty)
						//{
						//	int iNeerCnt = pParty->GetScopeUserCount(pPC);
						//	if (iNeerCnt >= 1)
						//	{
						//		pParty->AddExp(iExp, pTargetNPC->GetFieldUnique(), pPC, pParty->GetScopeLowerLevel(pPC), iNeerCnt);
						//	}
						//	else
						//		pPC->AddExp(iExp, pTargetNPC->GetFieldUnique()); // 나혼자 경험치 먹고 
						//}
						//else
							pPC->AddExp(iExp, pTargetNPC->GetFieldUnique());
					}

					pTargetNPC->AddHP(-i32TotalDamage);

					// 공성전 몬스터 쳤을때 방송
					if (i32AllDamage > 0 && pTarget->GetUnitTYPE() == eUnitType::NPC && g_ChosunCastleManager.CheckCastleMap(pPC->GetMapIndex()) == TRUE)
					{
						auto pMap = pTargetNPC->GetCurrentMap();
						if (pMap != NULL)
						{
							if(pMap->m_wMapIndex == CASTLE_MAPINDEX)
								g_ChosunCastleManager.SnedCastleNpcHpList(pTargetNPC, pPC);			
							else if (pMap->m_wMapIndex == WORLD_CASTLE_MAPINDEX)
							{
								g_ChosunCastleManager.SnedWorldCastleNpcHpList(pTargetNPC, pPC);
								g_ChosunCastleManager.CheckWorldCastleHighHpTowerHp();
							}
						}
					}

					if (pUnit->GetUnitTYPE() == eUnitType::PC)
					{
						CUnitPC* _pUser = dynamic_cast<CUnitPC*>(pUnit);
						if (_pUser != NULL)
						{
							_pUser->CheckMaxDamageLog(pTarget, i32TotalDamage);
						}
					}

					//g_GameManager.WriteAttackException(pPC, sendmsg.iDamage, pTargetNPC->GetIndex());

					sendmsg._ui32Damage[i] = i32AllDamage;
					sendmsg._ui32ExtraDamage[i] = i32AllExtraDamage;
					extramsg._ui32Damage[i] = i32SpecialDamage;
					passiveextramsg._ui32Damage[i] = i32SpecialExtraDamage;

					sendmsg.i32Critical |= bCritical << i;
					sendmsg.i32ExtraCritical |= bExtraCritical << i;
					extramsg.i32Critical |= bCritical << i;
					passiveextramsg.i32Critical |= bExtraCritical << i;
					//sendmsg.iDamage = i32AllDamage;
					//sendmsg.i32Critical = bCritical;
					//sendmsg._result |= (pTargetNPC->GetHpPercent() << 4);
					i32HpPersent = pTargetNPC->GetHpPercent();
					//방송
					//pCurrentMap->m_BlockManager.Attack_to_BroadCast(pPC, pTargetNPC, &sendmsg);

					//pPC->AddStatExp(pTargetNPC->GetNPC_CSVData()->Index, STAT_TYPE::STAT_TYPE_STR, 1, true, pTargetNPC->GetNPC_CSVData()->_i32Element, pTargetNPC->GetNPC_CSVData()->_i32StatExp);
					//pPC->ETCEvent(QUEST_PURPOSE_TYPE::QUEST_PURPOSE_GRADE_SELECT, pTargetNPC->GetIndex());

				// 미스아니고, 맞는애가 npc이고, 때리는애가 pc일때 huntEvent에서 체크할 리스트에 insert 해준다. 
				pTargetNPC->InsertHuntEventList(pPC, i32TotalDamage);
				if (pTargetNPC->GetCurrentMap() != NULL)
				{
					if (pTargetNPC->GetCurrentMap()->GetMapTYPE() == MAP_TYPE_BOSS_RAID)
					{
						CGameMap_BossRaidMap* pBossRaidMap = dynamic_cast<CGameMap_BossRaidMap*>(pTargetNPC->GetCurrentMap());
						if (pBossRaidMap != NULL)
						{
							pBossRaidMap->AddDamageList(pPC->GetCharacterUnique(), i32TotalDamage);
						}
					}
					if (pTargetNPC->GetCurrentMap()->GetMapTYPE() == MAP_TYPE_INSTANCE)
					{
						CGameMap_Instance* pInstanceMap = dynamic_cast<CGameMap_Instance*>(pTargetNPC->GetCurrentMap());
						if (pInstanceMap != NULL)
						{
							if (pInstanceMap->GetInstanceType() == ENUM_INSTANCETYPE::ENUM_INSTANCETYPE_DPSDUNGEON)
							{
								pInstanceMap->AddDpsDungeonDamage(i32TotalDamage);
							}
						}
					}
				}

				//강제 셋팅
				if (pTargetNPC->GetCurrentHP() > 0 && pTargetNPC->GetStiffnessTime() <= 0)
				{

						if (StiffnssCheck(pUnit, pTarget))
						{
							pTargetNPC->SetStiffnessTime();
							SP_STIFFNESS sendstiffness;
							sendstiffness._dwTargetFieldUnique = pTargetNPC->GetFieldUnique();
							sendstiffness._dwFieldUnique = pUnit->GetFieldUnique();
							if(pTargetNPC->GetCurrentMap())
								pTargetNPC->GetCurrentMap()->m_BlockManager.BroadCast(pTarget, (BYTE*)&sendstiffness, sizeof(SP_STIFFNESS));
						}
					}
					bMiss = MissCheck(pUnit, pTarget);
					//Kill_Unit(pPC, pTargetNPC, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, sendmsg._i32AttackType[i]);
					i32SuccessAttackCount++;

					if (pTarget->GetAlive() == TRUE)
					{
						/*if(bAttackMultiple)
							pPC->AttackSpecialEffect(pPC, pTarget, sendmsg._ui32Damage[0]);
						else*/
						pPC->AttackSpecialEffect(pPC, pTarget, i32AllDamage);
					}
					pPC->BattleActionEvent(QUEST_PURPOSE_TYPE::QUEST_PURPOSE_HIT);
				}
			} // PVE End
			if (pTarget->GetCurrentHP() <= 0 && pTarget->GetAlive())
			{ // HP 0이며 사망했을 경우 loop 종료
				break;
			}
		}
		i32HpPersent = pTarget->GetHpPercent();
		//방송
		sendmsg._result |= (i32HpPersent << 4);
		//extramsg._result |= (i32HpPersent << 4);
		sendmsg._i32AttackCount = (i32AttackCount << 4) + (i32SuccessAttackCount);
		extramsg._i32AttackCount = (i32AttackCount << 4) + (i32SuccessAttackCount);

		INT32 i32CheckAllDamange = 0;
		for (int j = 0; j< MAX_COUNT_DAMAGE;j++)
		{
			i32CheckAllDamange += sendmsg._ui32Damage[j];
			i32CheckAllDamange += sendmsg._ui32ExtraDamage[j];
			i32CheckAllDamange += extramsg._ui32Damage[j];
			i32CheckAllDamange += passiveextramsg._ui32Damage[j];
		}
		pCurrentMap->m_BlockManager.Attack_to_BroadCast(pPC, pTarget, &sendmsg);
		pCurrentMap->m_BlockManager.Attack_to_BroadCast(pPC, pTarget, &extramsg);
		pCurrentMap->m_BlockManager.Attack_to_BroadCast(pPC, pTarget, &passiveextramsg);
		UINT32 ui32AttackDetail = i32SuccessAttackCount << 16;
		ui32AttackDetail += pPC->GetAttackSpeed();
		if (pTarget->GetUnitTYPE() == eUnitType::PC)
		{
			CUnitPC* pTargetPC = dynamic_cast<CUnitPC*>(pTarget);
			//킬유닛 체크
			if (pTargetPC)
			{
				Kill_Unit(pPC, pTargetPC, pCurrentMap, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, i32CheckAllDamange, sendmsg._i32AttackType[0], ui32AttackDetail);
			}
		}
		else if (pTarget->GetUnitTYPE() == eUnitType::NPC)
		{
			CUnitNPC* pTargetNPC = dynamic_cast<CUnitNPC*>(pTarget);
			//킬유닛 체크
			if (pTargetNPC)
			{
				Kill_Unit(pPC, pTargetNPC, pCurrentMap, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, i32CheckAllDamange, sendmsg._i32AttackType[0], ui32AttackDetail);
			}
		}
	}
	// 때리는 사람이 NPC 일때
	else if (pUnit->GetUnitTYPE() == eUnitType::NPC)
	{
		CUnitNPC* pNPC = dynamic_cast<CUnitNPC*>(pUnit);
		if (pNPC == NULL)
			return;

		if (pTarget->GetAlive() == FALSE)
			return;

		CUnitPC* pTargetPC = dynamic_cast<CUnitPC*>(pTarget);
		sendmsg._i32AttackCount = (1 << 4) + (1);


		UINT32 ui32AttackDetail = 1 << 16;
		ui32AttackDetail += pNPC->GetSendAttackSpeed();

		if (pTarget->GetUnitTYPE() == eUnitType::PC)
		{

			if (pTargetPC == NULL)
				return;

			bRange = g_MapManager.IsRange(pNPC->GetNPC_CSVData()->AttRange + pNPC->GetNPC_CSVData()->TileSize + pNPC->GetNPC_CSVData()->_i32HitSize, pUnit->m_X, pUnit->m_Y, pTarget->m_X, pTarget->m_Y);
			if (bRange == FALSE)
				return;

			// 회피 했다.
			if (bMiss == TRUE)
			{
				sendmsg._result = 2;
				INT32 i32AttackAnimationIdx = pUnit->GetAttAnimationIndex(FALSE);
				sendmsg._result |= (pTarget->GetHpPercent() << 4);
				sendmsg._result |= i32AttackAnimationIdx << 16;
				pCurrentMap->m_BlockManager.Attack_to_BroadCast(pUnit, pTarget, &sendmsg);
				sendmsg.i32Critical = FALSE;
				sendmsg.i32ExtraCritical = FALSE;
				sendmsg._i32AttackCount = (1 << 4) + (1);
				//pPC->BattleActionEvent(QUEST_PURPOSE_MISS);
				//pTargetPC->BattleActionEvent(QUEST_PURPOSE_GET_MISS);
				return;
			}

			pTargetPC->_SoulfireList.UserSoulFire(pTargetPC, ENUM_SOULFIRE_TYPE::NOMAL_DEFENSE);
			//pTargetPC->SetOfflineModeCheckTick();

			/* 여기서 부터 검사할건 모두 했다. */
			// 누가 때린건지 기록
			pTargetPC->AddHurtUnit(pUnit->GetFieldUnique(), pUnit->GetUnitTYPE());

			sendmsg._result = 0;
			BOOL bCritical = FALSE;

			INT32 i32AttackAnimationIdx = pNPC->GetAttAnimationIndex(FALSE);
			sendmsg._result |= i32AttackAnimationIdx << 16;

			//INT32 i32Att = pNPC->GetPower(); // 145 
			
			INT32 i32Damage = AttackTarget_Damage_NPCPC(pUnit, pTarget, bCritical);

			//if (pTargetPC->GetMapIndex() == 901 && pTargetPC->GetAccountType() == eAccountType::ACCTYPE_GM && i32Damage != 1)
			//	i32Damage = i32Damage * g_CastleBattleManager._i32AdminDefencePercent * 0.01;

			pTargetPC->AddHP(-i32Damage);

			if (pUnit->GetUnitTYPE() == eUnitType::PC)
			{
				CUnitPC* _pUser = dynamic_cast<CUnitPC*>(pUnit);
				if (_pUser != NULL)
				{
					_pUser->CheckMaxDamageLog(pTarget, i32Damage);
				}
			}

			sendmsg._ui32Damage[0] = i32Damage;
			sendmsg.i32Critical = bCritical;
			//강제 셋팅
			if (pTargetPC->GetCurrentHP() > 0)
			{
				if (StiffnssCheck(pUnit, pTarget))
				{
					pTargetPC->SetStiffnessTime();
					SP_STIFFNESS sendstiffness;
					sendstiffness._dwTargetFieldUnique = pTargetPC->GetFieldUnique();
					sendstiffness._dwFieldUnique = pUnit->GetFieldUnique();
					if(pTargetPC->GetCurrentMap())
						pTargetPC->GetCurrentMap()->m_BlockManager.BroadCast(pTarget, (BYTE*)&sendstiffness, sizeof(SP_STIFFNESS));
				}
			}

			//방송
			pCurrentMap->m_BlockManager.Attack_to_BroadCast(pNPC, pTargetPC, &sendmsg);

			Kill_Unit(pNPC, pTargetPC, pCurrentMap, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, sendmsg._i32AttackType[0], ui32AttackDetail);

			if (pTarget->GetAlive() == TRUE)
			{
				pNPC->AttackSpecialEffect(pNPC, pTargetPC, i32Damage);
			}

			if (pTargetPC->GetConExpTick() > 0 && pTargetPC->GetConExpTick() < 1400 && pTargetPC->GetLastTargetNpcField() == pNPC->GetFieldUnique()) // 0-1.4초 사이면 공격모션중이다 판별
			{
				if (pNPC->GetIndex() == FIELD_TRAINING_NPC_INDEX && pTargetPC->GetTitleIndex() >= 3 /*일류고수*/)
				{
					return;
				}

				//pTargetPC->AddStatExp(pNPC->GetNPC_CSVData()->Index, STAT_TYPE::STAT_TYPE_CON, sendmsg.iDamage,
				//	true, pNPC->GetNPC_CSVData()->_i32Element, pNPC->GetNPC_CSVData()->_i32StatExp);
			}

			//pTargetPC->BattleActionEvent(QUEST_PURPOSE_TYPE::QUEST_PURPOSE_GET_HIT);
		}
		else if (pTarget->GetUnitTYPE() == eUnitType::CLONE)
		{
			CUnitClone* pTargetClone = dynamic_cast<CUnitClone*>(pTarget);
			if (pTargetClone == NULL)
				return;

			bRange = g_MapManager.IsRange(pNPC->GetNPC_CSVData()->AttRange + pNPC->GetNPC_CSVData()->TileSize + pNPC->GetNPC_CSVData()->_i32HitSize, pUnit->m_X, pUnit->m_Y, pTarget->m_X, pTarget->m_Y);
			if (bRange == FALSE)
				return;

			//pTargetClone->SetOfflineModeCheckTick();

			/* 여기서 부터 검사할건 모두 했다. */
			// 누가 때린건지 기록
			pTargetPC->AddHurtUnit(pUnit->GetFieldUnique(), pUnit->GetUnitTYPE());
			sendmsg._result = 0;
			BOOL bCritical = FALSE;

			INT32 i32AttackAnimationIdx = pNPC->GetAttAnimationIndex(FALSE);
			sendmsg._result |= i32AttackAnimationIdx << 16;

			//INT32 i32Att = pNPC->GetPower(); // 145 

			INT32 i32Damage = AttackTarget_Damage_NPCCLONE(pUnit, pTarget, bCritical);

			//if (pTargetPC->GetMapIndex() == 901 && pTargetPC->GetAccountType() == eAccountType::ACCTYPE_GM && i32Damage != 1)
			//	i32Damage = i32Damage * g_CastleBattleManager._i32AdminDefencePercent * 0.01;

			pTargetClone->AddHP(-i32Damage);


			if (pUnit->GetUnitTYPE() == eUnitType::PC)
			{
				CUnitPC* _pUser = dynamic_cast<CUnitPC*>(pUnit);
				if (_pUser != NULL)
				{
					_pUser->CheckMaxDamageLog(pTarget, i32Damage);
				}
			}

			sendmsg._ui32Damage[0] = i32Damage;
			sendmsg.i32Critical = bCritical;
			//강제 셋팅
			if (pTargetClone->GetCurrentHP() > 0)
			{
				if (StiffnssCheck(pUnit, pTarget))
				{
					pTargetClone->SetStiffnessTime();
					SP_STIFFNESS sendstiffness;
					sendstiffness._dwTargetFieldUnique = pTargetClone->GetFieldUnique();
					sendstiffness._dwFieldUnique = pUnit->GetFieldUnique();
					if (pTargetClone->GetCurrentMap())
						pTargetClone->GetCurrentMap()->m_BlockManager.BroadCast(pTarget, (BYTE*)&sendstiffness, sizeof(SP_STIFFNESS));
				}
			}

			//방송
			pCurrentMap->m_BlockManager.Attack_to_BroadCast(pNPC, pTargetClone, &sendmsg);

			Kill_Unit(pNPC, pTargetClone, pCurrentMap, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, sendmsg._i32AttackType[0]);

			if (pTarget->GetAlive() == TRUE)
			{
				pNPC->AttackSpecialEffect(pNPC, pTargetClone, i32Damage);
			}
			//if (pTargetPC->GetConExpTick() > 0 && pTargetPC->GetConExpTick() < 1400 && pTargetPC->GetLastTargetNpcField() == pNPC->GetFieldUnique()) // 0-1.4초 사이면 공격모션중이다 판별
			//{
			//	if (pNPC->GetIndex() == FIELD_TRAINING_NPC_INDEX && pTargetPC->GetTitleIndex() >= 3 /*일류고수*/)
			//	{
			//		return;
			//	}
			//
			//	//pTargetPC->AddStatExp(pNPC->GetNPC_CSVData()->Index, STAT_TYPE::STAT_TYPE_CON, sendmsg.iDamage,
			//	//	true, pNPC->GetNPC_CSVData()->_i32Element, pNPC->GetNPC_CSVData()->_i32StatExp);
			//}
		}
		else
		{
			return; //NPC가 NPC공격 가능할때 작업하던가 하자. 
		}
	}
	else if (pUnit->GetUnitTYPE() == eUnitType::CLONE)
	{
		CUnitClone* pClone = dynamic_cast<CUnitClone*>(pUnit);
		if (pTarget->GetUnitTYPE() == eUnitType::NPC)
		{
			CUnitNPC* pTargetNPC = dynamic_cast<CUnitNPC*>(pTarget);
			if (pTargetNPC == NULL || pTargetNPC->GetNPC_CSVData() == NULL)
			{
				CUnitClone* pcheckPC = dynamic_cast<CUnitClone*>(pUnit); //더미가 상대가 죽은 것을 받지 못하였을 때 버그로 인해 생성
				if (pcheckPC)
				{
					SP_Attack sendCheckmsg;
					sendCheckmsg._result = ENUM_ALL_ERROR_ATTACK_NOT_TARGET;
					pcheckPC->Write((BYTE*)&sendCheckmsg, sizeof(SP_Attack), 0);
				}
				return;
			}

			if (pTargetNPC->GetNPC_CSVData()->Type == VILLAGE_NPC)
			{
				//sendmsg._result = 1;
				//pPC->Write((BYTE*)&sendmsg, sizeof(SP_Attack), 0);
				//printf("SP_ATTACK TARGET TYPE 0\n");
				return;
			}

			// TODO : 클론은 공속에 따른 어택카운트 어떻게 할지 기획에 확인 후 작업 다시 해줘야합니다.

			// 회피 했다.
			if (bMiss == TRUE)
			{
				sendmsg._result = 2;
				INT32 i32AttackAnimationIdx = pUnit->GetAttAnimationIndex(FALSE);
				sendmsg._result |= (pTarget->GetHpPercent() << 4);
				sendmsg._result |= i32AttackAnimationIdx << 16;
				pCurrentMap->m_BlockManager.Attack_to_BroadCast(pUnit, pTarget, &sendmsg);

				//pPC->BattleActionEvent(QUEST_PURPOSE_MISS);
				//pTargetPC->BattleActionEvent(QUEST_PURPOSE_GET_MISS);
				if (pTargetNPC->GetNPC_CSVData()->Type != VILLAGE_NPC)
				{
					if (pTargetNPC->GetDamage_Unit()->dwUnitUnique <= 0)
						pTargetNPC->SetDamageUnit(pClone->GetPoolArray(), pClone->GetFieldUnique(), pClone->m_X, pClone->m_Y);

					/*추가 -> 유저 주변 합공몬스터 체크 후 어그로 끌어주기*/
					if(pClone->GetCurrentMap())
						pClone->GetCurrentMap()->m_BlockManager.SetJoinAttackDamageUnit(pClone, MAX_JOIN_ATTACK_SCOPE);
				}
				return;
			}
			//pClone->SetLastTargetNpcField(pTargetNPC->GetFieldUnique()); // 마지막 공격 대상 필드유니크 세팅
			//pClone->ResetConExpTick();


				// 어그로 끌어주고 
				if (pTargetNPC->GetDamage_Unit()->dwUnitUnique <= 0)
					pTargetNPC->SetDamageUnit(pClone->GetPoolArray(), pClone->GetFieldUnique(), pClone->m_X, pClone->m_Y);

				/*추가 -> 유저 주변 합공몬스터 체크 후 어그로 끌어주기*/
				if(pClone->GetCurrentMap())
					pClone->GetCurrentMap()->m_BlockManager.SetJoinAttackDamageUnit(pClone, MAX_JOIN_ATTACK_SCOPE);

				sendmsg._result = 0;
				BOOL bCritical = FALSE;
				INT32 i32SuccessSpecialCount = 0;
				INT32 i32AllDamage = AttackTarget_Damage_CLONENPC(pUnit, pTarget, bCritical);
				INT32 i32SpecialDamage = GetAttackExtraDamage_Chosun2M(pUnit, pTarget, i32AllDamage, i32SuccessSpecialCount);
				INT32 i32TotalDamage = i32AllDamage + i32SpecialDamage;
				INT32 i32AttackAnimationIdx = pClone->GetAttAnimationIndex(FALSE);

				int iExp = pTargetNPC->GetNpcAttackExp(i32TotalDamage); //

				//int iTendency = pTargetNPC->GetNPCAttackTendency(iDamage);

				int iMyLevel = pClone->GetLevel(); // 내 레벨

				int iLevel = iMyLevel - pTargetNPC->GetLevel();// 레벨 차이

				//CParty* pParty = pPC->GetParty();

				// _i32LimitLevel이 1이면 레벨제한x
				if (pTargetNPC->GetNPC_CSVData()->_i32LimitLevel == 0 && iLevel > 0)	// 케릭 레벨이 몬스터 레벨보다 높으면  
				{
					if (iLevel < 200)	// 1 ~ 199 렙차 많이 날수록 경험치 깎음, 케릭터가 200이상 높으면 경험치 못먹음 
					{
						iExp -= iExp * iLevel * 0.005;
						//
						//if (pParty)
						//{
						//	int iNeerCnt = pParty->GetScopeUserCount(pPC);
						//	if (iNeerCnt >= 1)
						//	{
						//		pParty->AddExp(iExp, pTargetNPC->GetFieldUnique(), pPC,  pParty->GetScopeLowerLevel(pPC), iNeerCnt);
						//	}
						//	else
						//		pPC->AddExp(iExp, pTargetNPC->GetFieldUnique()); // 나혼자 경험치 먹고 
						//}
						//else
						//{
						//pPC->AddExp(iExp, pTargetNPC->GetFieldUnique()); // 나혼자 경험치 먹고 
						g_CLoneManager.AddExp(pClone,iExp);
						//}

					}
				}
				else // 케릭 레벨이 몬스터 레벨보다 낮으면  
				{
					//if (pParty)
					//{
					//	int iNeerCnt = pParty->GetScopeUserCount(pPC);
					//	if (iNeerCnt >= 1)
					//	{
					//		pParty->AddExp(iExp, pTargetNPC->GetFieldUnique(), pPC, pParty->GetScopeLowerLevel(pPC), iNeerCnt);
					//	}
					//	else
					//		pPC->AddExp(iExp, pTargetNPC->GetFieldUnique()); // 나혼자 경험치 먹고 
					//}
					//else
					//pPC->AddExp(iExp, pTargetNPC->GetFieldUnique());
					g_CLoneManager.AddExp(pClone, iExp);
				}
				
				pTargetNPC->AddHP(-i32TotalDamage);

				if (pTargetNPC->GetCurrentMap() != NULL)
				{
					if (pTargetNPC->GetCurrentMap()->GetMapTYPE() == MAP_TYPE_BOSS_RAID)
					{
						CGameMap_BossRaidMap* pBossRaidMap = dynamic_cast<CGameMap_BossRaidMap*>(pTargetNPC->GetCurrentMap());
						if (pBossRaidMap != NULL)
						{
							pBossRaidMap->AddDamageList(pPC->GetCharacterUnique(), i32TotalDamage);
						}
					}
					if (pTargetNPC->GetCurrentMap()->GetMapTYPE() == MAP_TYPE_INSTANCE)
					{
						CGameMap_Instance* pInstanceMap = dynamic_cast<CGameMap_Instance*>(pTargetNPC->GetCurrentMap());
						if (pInstanceMap != NULL)
						{
							if (pInstanceMap->GetInstanceType() == ENUM_INSTANCETYPE::ENUM_INSTANCETYPE_DPSDUNGEON)
							{
								pInstanceMap->AddDpsDungeonDamage(i32TotalDamage);
							}
						}
					}
				}

				if (pUnit->GetUnitTYPE() == eUnitType::PC)
				{
					CUnitPC* _pUser = dynamic_cast<CUnitPC*>(pUnit);
					if (_pUser != NULL)
					{
						_pUser->CheckMaxDamageLog(pTarget, i32TotalDamage);
					}
				}
				//g_GameManager.WriteAttackException(pPC, sendmsg.iDamage, pTargetNPC->GetIndex());

				sendmsg._ui32Damage[0] = i32AllDamage;
				extramsg._ui32Damage[0] = i32AllDamage;
				sendmsg.i32Critical = bCritical;
				extramsg.i32Critical = bCritical;
				sendmsg._result |= (pTargetNPC->GetHpPercent() << 4);
				//extramsg._result |= (pTargetNPC->GetHpPercent() << 4);
				//방송
				pCurrentMap->m_BlockManager.Attack_to_BroadCast(pClone, pTargetNPC, &sendmsg);
				pCurrentMap->m_BlockManager.Attack_to_BroadCast(pClone, pTargetNPC, &extramsg);

				//pPC->AddStatExp(pTargetNPC->GetNPC_CSVData()->Index, STAT_TYPE::STAT_TYPE_STR, 1, true, pTargetNPC->GetNPC_CSVData()->_i32Element, pTargetNPC->GetNPC_CSVData()->_i32StatExp);
				//pPC->ETCEvent(QUEST_PURPOSE_TYPE::QUEST_PURPOSE_GRADE_SELECT, pTargetNPC->GetIndex());

				// 미스아니고, 맞는애가 npc이고, 때리는애가 pc일때 huntEvent에서 체크할 리스트에 insert 해준다. 
				//pTargetNPC->InsertHuntEventList(pPC, i32AllDamage);
				//강제 셋팅
				if (pTargetNPC->GetCurrentHP() > 0)
				{
					if (StiffnssCheck(pClone, pTarget))
					{
						pTargetNPC->SetStiffnessTime();
						SP_STIFFNESS sendstiffness;
						sendstiffness._dwTargetFieldUnique = pTargetNPC->GetFieldUnique();
						sendstiffness._dwFieldUnique = pUnit->GetFieldUnique();
						if (pTargetNPC->GetCurrentMap())
							pTargetNPC->GetCurrentMap()->m_BlockManager.BroadCast(pTarget, (BYTE*)&sendstiffness, sizeof(SP_STIFFNESS));
					}
				}
				Kill_Unit(pClone, pTargetNPC, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, sendmsg._i32AttackType[0]);

				if (pTarget->GetAlive() == TRUE)
				{
					/*if(bAttackMultiple)
						pPC->AttackSpecialEffect(pPC, pTarget, sendmsg._ui32Damage[0]);
					else*/
					//pPC->AttackSpecialEffect(pPC, pTarget, i32AllDamage);
					pClone->AttackSpecialEffect(pClone, pTarget, i32AllDamage);
				}
				pClone->SetCloneAttackLoopTick(GetTickCount64() + pClone->GetAttackSpeedServer());
				//pPC->BattleActionEvent(QUEST_PURPOSE_TYPE::QUEST_PURPOSE_HIT);

		}
	}
}

// PVE 상황
INT32 CMath::AttackTarget_Damage_PCNPC(CUnit* pUnit, CUnit* pTarget, BOOL &bCriticalout)
{

	auto pPC = dynamic_cast<CUnitPC*> (pUnit);
	auto pTargetPC = dynamic_cast<CUnitNPC*> (pTarget);
	
	if (pPC == NULL || pTargetPC == NULL)
	{
		return 0;
	}
	
	DOUBLE i32FirstDamage = 0;		// 1차 대미지(기본 대미지)
	DOUBLE i32SecondDamage = 0;		// 2차 대미지(대미지 증가)
	DOUBLE i32ThirdDamage = 0;		// 3차 대미지(일격 필살 대미지)
	DOUBLE i32TotalDamage = 0;		// 최종 대미지
	DOUBLE i32AddDamage = 0;		//추가 대미지
	
	
	DOUBLE i32Att = pPC->GetComputedPower(pPC->GetAttackType());				// 공격 타입에 따라 가져온 공격력
	DOUBLE i32Def = pTargetPC->GetComputedDef((INT32)pPC->GetAttackType());	// 공격자의 타입에 따라 가져온 타겟의 방어력
	DOUBLE i32DefIgnore = pPC->GetComputedDefIgnore(pPC->GetAttackType());	// 공격자의 타입에 따라 가져온 방어력 무시

	// 1차 대미지계산 : 기본 대미지
	// (공격력 * ( 1 - ( 방어력 - 방어력 무시) / (방어력 + 방어상수500))) * (0.95~1.05)
	// 만약 방어력 - 방어력무시가 0 이나오면 분자가 0 이므로 공격력 * 랜덤값만 적용
	DOUBLE i32FirstDamageRandom = g_GameManager.getRandomNumber(95, 105) * 0.01;
	if (i32Def - i32DefIgnore <= 0)
	{
		i32FirstDamage = i32Att * i32FirstDamageRandom;
	}
	else
	{
		i32FirstDamage = (i32Att * (1 - (i32Def - i32DefIgnore) / (i32Def + 500))) * i32FirstDamageRandom;
	}

	
	
	// 2차 대미지 계산  : 대미지 증가 계산
	// 대미지 증가 수치 : 공격자 대미지 증가 - 방어자 대미지 증가 무시
	DOUBLE i32IncreaseDamage = pPC->GetComputedIncreaseDamage(pPC->GetAttackType()) - pTargetPC->GetComputedIncreaseDamageIgnore((INT32)pPC->GetAttackType());
	if (i32IncreaseDamage <= 0)
	{
		// 대미지 증가 수치가 <= 0 이면 기본 대미지
		i32SecondDamage = i32FirstDamage;
	}
	else
	{
		// 기본 대미지 * ( 1+ 대미지 증가 수치 * 랜덤값 0.01~0.05)
		i32SecondDamage = i32FirstDamage * (1 + i32IncreaseDamage * 0.01);
	}
	
	// 3차 대미지 계산 : 일격 필살
	// 일격 필살 발동 확률 계산 => 일격 필살 확률 >= 랜덤값 (1 ~ 100);
	bCriticalout = FALSE;
	INT32 i32CriticalRandom = g_GameManager.getRandomNumber(0, 100);
	DOUBLE i32ThirdDamageRandom = g_GameManager.getRandomNumber(1, 5) * 0.01;
	INT32 i32CriticalPersent = (1.0 - 1.0 / (1 + (pPC->m_BalanceInfo._i32PartsArray[BALANCE_POWER_PARTS32::BALANCE_POWER_PARTS32_Critical_Percent] - 0) / 50.0)) * 100;
	if (i32CriticalRandom <= i32CriticalPersent)
	{ //(1-1/(1+(일격필살 확률-일격필살 회피)/50))*100
		bCriticalout = TRUE;
	}
	//if (i32CriticalRandom <= pPC->m_BalanceInfo._i32PartsArray[BALANCE_POWER_PARTS32::BALANCE_POWER_PARTS32_Critical_Percent])
	//{
	//	bCriticalout = TRUE;
	//}


	if (bCriticalout == TRUE)
	{
		// 일격 필살 대미지 증가 수치 : 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
		DOUBLE i32IncreaseCritical = pPC->GetComputedIncreaseCritical() - pTargetPC->GetComputedIncreaseCriticalIgnore();
	
		if (i32IncreaseCritical <= 0)
		{
			// 대미지 증가 수치가 <= 0 이면 2차 대미지
			i32ThirdDamage = i32SecondDamage;
		}
		else
		{
			// 대미지 증가 수치가 > 0 이면  (2차 대미지) *(1.25 + (대미지 증가 수치 * 0.001) + 랜덤값(0.01~0.05))
			i32ThirdDamage = i32SecondDamage * (1.25 + (i32IncreaseCritical * 0.001) + i32ThirdDamageRandom);
		}
		
	}
	else
	{
		// 일격 필살 미 발동 시 대미지 : 2차 대미지
		i32ThirdDamage = i32SecondDamage;
	}
	
	// 최종 대미지 계산 PVP, PVE 공격력 증가, 방어력 증가 이용
	// 최종 대미지 : 3차 대미지  + ( p 공 - p방)
	INT32 i32PveAttack = pPC->GetComputedPvePower(); 
	INT32 i32PveDefense = pTargetPC->GetComputedPveDef(); 
	i32TotalDamage = i32ThirdDamage + (i32PveAttack - i32PveDefense);

		// 추가 대미지 증가 수치 : 공격자 추가 대미지 증가 - 방어자 추가 대미지 증가 무시
	i32AddDamage = pPC->GetComputedAddPowerIncrease(pPC->GetAttackType()) - pTargetPC->GetComputedAddPowerIncreaseIgnore((INT32)pPC->GetAttackType());
	if (i32AddDamage > 0)
	{
		// 추가 대미지 증가 수치가 > 0 이면 최종 대미지 *  ( 1 + 추가 대미지 증가 수치 * 0.01)
		// 아니면 그냥 최종 대미지
		i32TotalDamage = i32TotalDamage * (1 + i32AddDamage * 0.01);
	}

	if ((INT32)i32TotalDamage <= 0)
	{
		i32TotalDamage = 1;
	}

	return i32TotalDamage;
	
}


// 유저 대 유저 대미지 계산식(PVP)
INT32 CMath::AttackTarget_Damage_PCPC(CUnit* pUnit, CUnit* pTarget, BOOL& bCriticalout)
{
	auto pPC = dynamic_cast<CUnitPC*> (pUnit);
	auto pTargetPC = dynamic_cast<CUnitPC*> (pTarget);

	if (pPC == NULL || pTargetPC == NULL)
	{
		return 0;
	}

	DOUBLE i32FirstDamage = 0;		// 1차 대미지(기본 대미지)
	DOUBLE i32SecondDamage = 0;		// 2차 대미지(대미지 증가)
	DOUBLE i32ThirdDamage = 0;		// 3차 대미지(일격 필살 대미지)
	DOUBLE i32TotalDamage = 0;		// 최종 대미지
	DOUBLE i32AddDamage = 0;			//추가 대미지
	
	DOUBLE i32Att = pPC->GetComputedPower(pPC->GetAttackType());				// 공격 타입에 따라 가져온 공격력
	DOUBLE i32Def = pTargetPC->GetComputedDef(pPC->GetAttackType());			// 공격자의 타입에 따라 가져온 타겟의 방어력
	DOUBLE i32DefIgnore = pPC->GetComputedDefIgnore(pPC->GetAttackType());	// 공격자의 타입에 따라 가져온 방어력 무시
	
	// 1차 대미지계산 : 기본 대미지
	// (공격력 * ( 1 - ( 방어력 - 방어력 무시) / (방어력 + 방어상수500))) * (0.95~1.05)
	// 만약 방어력 - 방어력무시가 0 이나오면 분자가 0 이므로 공격력 * 랜덤값만 적용
	DOUBLE i32FirstDamageRandom = g_GameManager.getRandomNumber(95, 105) * 0.01;
	if (i32Def - i32DefIgnore <= 0)
	{
		i32FirstDamage = i32Att * i32FirstDamageRandom;
	}
	else
	{
		i32FirstDamage = (i32Att * (1 - (i32Def - i32DefIgnore) / (i32Def + 500))) * i32FirstDamageRandom;
	}

	// 2차 대미지 계산  : 대미지 증가 계산
	// 대미지 증가 수치 : 공격자 대미지 증가 - 방어자 대미지 증가 무시
	DOUBLE i32IncreaseDamage = pPC->GetComputedIncreaseDamage(pPC->GetAttackType()) - pTargetPC->GetComputedIncreaseDamageIgnore(pPC->GetAttackType());
	if (i32IncreaseDamage <= 0)
	{	
		// 대미지 증가 수치가 <= 0 이면 기본 대미지
		i32SecondDamage =  i32FirstDamage;
	}
	else
	{
		// 기본 대미지 * ( 1+ 대미지 증가 수치 * 0.01)
		i32SecondDamage = i32FirstDamage * (1 + i32IncreaseDamage * 0.01);
	}

	// 3차 대미지 계산 : 일격 필살
	// 일격 필살 발동 확률 계산 => 일격 필살 확률 >= 랜덤값 (1 ~ 100);
	bCriticalout = FALSE;
	INT32 i32CriticalRandom = g_GameManager.getRandomNumber(0, 100);		
	DOUBLE i32ThirdDamageRandom = g_GameManager.getRandomNumber(1, 5) * 0.01f;
	//if (i32CriticalRandom <= pPC->m_BalanceInfo._i32PartsArray[BALANCE_POWER_PARTS32::BALANCE_POWER_PARTS32_Critical_Percent])
	//{
	//	bCriticalout = TRUE;
	//}
	INT32 i32CriticalPersent = (1.0 - 1.0 / (1 + (pPC->m_BalanceInfo._i32PartsArray[BALANCE_POWER_PARTS32::BALANCE_POWER_PARTS32_Critical_Percent] - 0) / 50.0)) * 100;
	if (i32CriticalRandom <= i32CriticalPersent)
	{ //(1-1/(1+(일격필살 확률-일격필살 회피)/50))*100
		bCriticalout = TRUE;
	}
	if (bCriticalout == TRUE)
	{
		// 일격 필살 대미지 증가 수치 : 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
		DOUBLE i32IncreaseCritical = pPC->GetComputedIncreaseCritical() - pTargetPC->GetComputedIncreaseCriticalIgnore();
		
		if (i32IncreaseCritical <= 0)
		{
			// 대미지 증가 수치가 <= 0 이면 2차 대미지
			i32ThirdDamage = i32SecondDamage;
		}
		else
		{
			// 대미지 증가 수치가 > 0 이면  (2차 대미지) *(1.25 + (대미지 증가 수치 * 0.001) + 랜덤값(0.01~0.05))
			i32ThirdDamage = i32SecondDamage * (1.25 + (i32IncreaseCritical * 0.001) + i32ThirdDamageRandom);
		}
	}
	else
	{
		// 일격 필살 미 발동 시 대미지 : 2차 대미지
		i32ThirdDamage = i32SecondDamage;
	}

	/// 최종 대미지 계산 PVP, PVE 공격력 증가, 방어력 증가 이용
	// 최종 대미지 : 3차 대미지  + ( p 공 - p방)
	INT32 i32PvPAttack = pPC->GetComputedPvpPower(); 
	INT32 i32PvPDefense = pTargetPC->GetComputedPvpDef(); 
	i32TotalDamage = i32ThirdDamage + (i32PvPAttack - i32PvPDefense);

	// 추가 대미지 : 혼불 계산
	INT32 i32ComputedAddPowerIncreaseIgnore = 0;
	i32ComputedAddPowerIncreaseIgnore = pTargetPC->GetComputedAddPowerIncreaseIgnore(pPC->GetAttackType());
	// 추가 대미지 증가 수치 : 공격자 추가 대미지 증가 - 방어자 추가 대미지 증가 무시
	i32AddDamage = pPC->GetComputedAddPowerIncrease(pPC->GetAttackType()) - i32ComputedAddPowerIncreaseIgnore;
	if (i32AddDamage > 0)
	{
		// 추가 대미지 증가 수치가 > 0 이면 최종 대미지 *  ( 1 + 추가 대미지 증가 수치 * 0.01)
		// 아니면 그냥 최종 대미지
		i32TotalDamage = i32TotalDamage * (1 + i32AddDamage * 0.01);
	}
	i32TotalDamage *= PVP_DAMAGE_PERSENT;

	if ((INT32)i32TotalDamage <= 0)
	{
		i32TotalDamage = 1;
	}

	return i32TotalDamage;
}

// NPC가 PC 때릴때 혼불 x
INT32 CMath::AttackTarget_Damage_NPCPC(CUnit* pUnit, CUnit* pTarget, BOOL& bCriticalout)
{
	auto pNPC = dynamic_cast<CUnitNPC*>(pUnit);
	auto pTargetPC = dynamic_cast<CUnitPC*> (pTarget);
	
	if (pNPC == NULL || pTargetPC == NULL)
	{
		return 0;
	}
	
	DOUBLE i32FirstDamage = 0;		// 1차 대미지(기본 대미지)
	DOUBLE i32SecondDamage = 0;		// 2차 대미지(대미지 증가)
	DOUBLE i32ThirdDamage = 0;		// 3차 대미지(일격 필살 대미지)
	DOUBLE i32TotalDamage = 0;		// 최종 대미지
	DOUBLE i32AddDamage = 0;			//추가 대미지
	
	// 1차 대미지계산 : 기본 대미지
	DOUBLE i32Att = pNPC->GetComputedPower(pNPC->GetAttackType());				// 공격 타입에 따라 가져온 공격력
	DOUBLE i32Def = pTargetPC->GetComputedDef((USER_ATTACK_TYPE)pNPC->GetAttackType());			// 공격자의 타입에 따라 가져온 타겟의 방어력
	DOUBLE i32DefIgnore = pNPC->GetComputedDefIgnore(pNPC->GetAttackType());	// 공격자의 타입에 따라 가져온 방어력 무시
	
	// 1차 대미지계산 : 기본 대미지
	// (공격력 * ( 1 - ( 방어력 - 방어력 무시) / (방어력 + 방어상수500))) * (0.95~1.05)
	// 만약 방어력 - 방어력무시가 0 이나오면 분자가 0 이므로 공격력 * 랜덤값만 적용
	DOUBLE i32FirstDamageRandom = g_GameManager.getRandomNumber(95, 105) * 0.01;
	if (i32Def - i32DefIgnore <= 0)
	{
		i32FirstDamage = i32Att * i32FirstDamageRandom;
	}
	else
	{
		i32FirstDamage = (i32Att * (1 - (i32Def - i32DefIgnore) / (i32Def + 500))) * i32FirstDamageRandom;
	}
	
	// 2차 대미지 계산  : 대미지 증가 계산
	// 대미지 증가 수치 : 공격자 대미지 증가 - 방어자 대미지 증가 무시
	DOUBLE i32IncreaseDamage = pNPC->GetComputedIncreaseDamage(pNPC->GetAttackType()) - pTargetPC->GetComputedIncreaseDamageIgnore((USER_ATTACK_TYPE)pNPC->GetAttackType());
	if (i32IncreaseDamage <= 0)
	{
		// 대미지 증가 수치가 <= 0 이면 기본 대미지
		i32SecondDamage = i32FirstDamage;
	}
	else
	{
		// 기본 대미지 * ( 1+ 대미지 증가 수치 * 랜덤값 0.01~0.05)
		i32SecondDamage = i32FirstDamage * (1 + i32IncreaseDamage * 0.01);
	}
	
	// 3차 대미지 계산 : 일격 필살
	// 일격 필살 발동 확률 계산 => 일격 필살 확률 >= 랜덤값 (1 ~ 100);
	bCriticalout = FALSE;
	INT32 i32CriticalRandom = g_GameManager.getRandomNumber(0, 100);
	DOUBLE i32ThirdDamageRandom = g_GameManager.getRandomNumber(1, 5) * 0.01f;
	INT32 i32CriticalPersent = (1.0 - 1.0 / (1 + (pNPC->GetComputedCriticalPercent() - 0) / 50.0)) * 100;

	if (i32CriticalRandom <= i32CriticalPersent)
	{
		bCriticalout = TRUE;
	}

	if (bCriticalout == TRUE)
	{
		// 일격 필살 대미지 증가 수치 : 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
		DOUBLE i32IncreaseCritical = pNPC->GetComputedIncreaseCritical() - pTargetPC->GetComputedIncreaseCriticalIgnore();
	
		if (i32IncreaseCritical <= 0)
		{
			// 대미지 증가 수치가 <= 0 이면 2차 대미지
			i32ThirdDamage = i32SecondDamage;
		}
		else
		{
			// 대미지 증가 수치가 > 0 이면  (2차 대미지) *(1.25 + (대미지 증가 수치 * 0.001) + 랜덤값(0.01~0.05))
			i32ThirdDamage = i32SecondDamage * (1.25 + (i32IncreaseCritical * 0.001) + i32ThirdDamageRandom);
		}
	}
	else
	{
		// 일격 필살 미 발동 시 대미지 : 2차 대미지
		i32ThirdDamage = i32SecondDamage;
	}
	
	// 최종 대미지 계산 PVP, PVE 공격력 증가, 방어력 증가 이용
	// 최종 대미지 : 3차 대미지 * ( 1 + (PVE 공격력 - PVE 방어력) * 0.01)
	INT32 i32PveAttack = pNPC->GetComputedPvePower(); // PVE 공격력
	INT32 i32PveDefense = pTargetPC->GetComputedPveDef(); // PVE 방어력
	i32TotalDamage = i32ThirdDamage + (i32PveAttack - i32PveDefense);

	if ((INT32)i32TotalDamage <= 0)
	{
		i32TotalDamage = 1;
	}

	return i32TotalDamage;
	
}

// NPC가 NPC 때릴때 혼불 x
INT32 CMath::AttackTarget_Damage_NPCNPC(CUnit* pUnit, CUnit* pTarget, BOOL& bCriticalout)
{
	auto pNPC = dynamic_cast<CUnitNPC*>(pUnit);
	auto pTargetNPC = dynamic_cast<CUnitNPC*> (pTarget);

	if (pNPC == NULL || pTargetNPC == NULL)
	{
		return 0;
	}

	DOUBLE i32FirstDamage = 0;		// 1차 대미지(기본 대미지)
	DOUBLE i32SecondDamage = 0;		// 2차 대미지(대미지 증가)
	DOUBLE i32ThirdDamage = 0;		// 3차 대미지(일격 필살 대미지)
	DOUBLE i32TotalDamage = 0;		// 최종 대미지
	DOUBLE i32AddDamage = 0;			//추가 대미지

	DOUBLE i32Att = pNPC->GetComputedPower(pNPC->GetAttackType());				// 공격 타입에 따라 가져온 공격력
	DOUBLE i32Def = pTargetNPC->GetComputedDef(pNPC->GetAttackType());			// 공격자의 타입에 따라 가져온 타겟의 방어력
	DOUBLE i32DefIgnore = pNPC->GetComputedDefIgnore(pNPC->GetAttackType());	// 공격자의 타입에 따라 가져온 방어력 무시


	// 1차 대미지계산 : 기본 대미지
	// (공격력 * ( 1 - ( 방어력 - 방어력 무시) / (방어력 + 방어상수500))) * (0.95~1.05)
	// 만약 방어력 - 방어력무시가 0 이나오면 분자가 0 이므로 공격력 * 랜덤값만 적용
	DOUBLE i32FirstDamageRandom = g_GameManager.getRandomNumber(95, 105) * 0.01;
	if (i32Def - i32DefIgnore <= 0)
	{
		i32FirstDamage = i32Att * i32FirstDamageRandom;
	}
	else
	{
		i32FirstDamage = (i32Att * (1 - (i32Def - i32DefIgnore) / (i32Def + 500))) * i32FirstDamageRandom;
	}

	// 2차 대미지 계산  : 대미지 증가 계산
	// 대미지 증가 수치 : 공격자 대미지 증가 - 방어자 대미지 증가 무시
	DOUBLE i32IncreaseDamage = pNPC->GetComputedIncreaseDamage(pNPC->GetAttackType()) - pTargetNPC->GetComputedIncreaseDamageIgnore(pNPC->GetAttackType());
	if (i32IncreaseDamage <= 0)
	{
		// 대미지 증가 수치가 <= 0 이면 기본 대미지
		i32SecondDamage = i32FirstDamage;
	}
	else
	{
		// 기본 대미지 * ( 1+ 대미지 증가 수치 * 랜덤값 0.01~0.05)
		i32SecondDamage = i32FirstDamage * (1 + i32IncreaseDamage * 0.01);
	}

	// 3차 대미지 계산 : 일격 필살
	// 일격 필살 발동 확률 계산 => 일격 필살 확률 >= 랜덤값 (1 ~ 100);
	bCriticalout = FALSE;
	INT32 i32CriticalRandom = g_GameManager.getRandomNumber(0, 100);
	DOUBLE i32ThirdDamageRandom = g_GameManager.getRandomNumber(1, 5) * 0.01f;
	if (i32CriticalRandom <= pNPC->GetComputedCriticalPercent())
	{
		bCriticalout = TRUE;
	}

	if (bCriticalout == TRUE)
	{
		// 일격 필살 대미지 증가 수치 : 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
		DOUBLE i32IncreaseCritical = pNPC->GetComputedIncreaseCritical() - pTargetNPC->GetComputedIncreaseCriticalIgnore();
		if (i32IncreaseCritical <= 0)
		{
			// 대미지 증가 수치가 <= 0 이면 2차 대미지
			i32ThirdDamage = i32SecondDamage;
		}
		else
		{
			// 대미지 증가 수치가 > 0 이면  (2차 대미지) *(1.25 + (대미지 증가 수치 * 0.001) + 랜덤값(0.01~0.05))
			i32ThirdDamage = i32SecondDamage * (1.25 + (i32IncreaseCritical * 0.001) + i32ThirdDamageRandom);
		}
		
	}
	else
	{
		// 일격 필살 미 발동 시 대미지 : 2차 대미지
		i32ThirdDamage = i32SecondDamage;
	}

	// 최종 대미지 계산 PVP, PVE 공격력 증가, 방어력 증가 이용
	// 최종 대미지 : 3차 대미지 * ( 1 + (PVE 공격력 - PVE 방어력) * 0.01)
	INT32 i32PveAttack = pNPC->GetComputedPvePower(); // PVE 공격력
	INT32 i32PveDefense = pTargetNPC->GetComputedPveDef(); // PVE 방어력
	i32TotalDamage = i32ThirdDamage + (i32PveAttack - i32PveDefense);

	if ((INT32)i32TotalDamage <= 0)
	{
		i32TotalDamage = 1;
	}

	return i32TotalDamage;
}

INT32 CMath::AttackTarget_Damage_CLONENPC(CUnit* pUnit, CUnit* pTarget, BOOL& bCriticalout)
{
	auto pPC = dynamic_cast<CUnitClone*> (pUnit);
	auto pNPC = dynamic_cast<CUnitNPC*> (pTarget);
	if (pPC == NULL || pNPC == NULL)
	{
		return 0;
	}
	
	DOUBLE i32FirstDamage = 0;		// 1차 대미지(기본 대미지)
	DOUBLE i32SecondDamage = 0;		// 2차 대미지(대미지 증가)
	DOUBLE i32ThirdDamage = 0;		// 3차 대미지(일격 필살 대미지)
	DOUBLE i32TotalDamage = 0;		// 최종 대미지
	DOUBLE i32AddDamage = 0;			//추가 대미지
	
	// 1차 대미지계산 : 기본 대미지
	DOUBLE i32Att = pPC->GetComputedPower(pPC->GetAttackType());				// 공격 타입에 따라 가져온 공격력
	DOUBLE i32Def = pNPC->GetComputedDef((INT32)pPC->GetAttackType());			// 공격자의 타입에 따라 가져온 타겟의 방어력
	DOUBLE i32DefIgnore = pPC->GetComputedDefIgnore(pPC->GetAttackType());	// 공격자의 타입에 따라 가져온 방어력 무시
	
	// 1차 대미지계산 : 기본 대미지
	// (공격력 * ( 1 - ( 방어력 - 방어력 무시) / (방어력 + 방어상수500))) * (0.95~1.05)
	// 만약 방어력 - 방어력무시가 0 이나오면 분자가 0 이므로 공격력 * 랜덤값만 적용
	DOUBLE i32FirstDamageRandom = g_GameManager.getRandomNumber(95, 105) * 0.01;
	if (i32Def - i32DefIgnore <= 0)
	{
		i32FirstDamage = i32Att * i32FirstDamageRandom;
	}
	else
	{
		i32FirstDamage = (i32Att * (1 - (i32Def - i32DefIgnore) / (i32Def + 500))) * i32FirstDamageRandom;
	}
	
	// 2차 대미지 계산  : 대미지 증가 계산
	// 대미지 증가 수치 : 공격자 대미지 증가 - 방어자 대미지 증가 무시
	DOUBLE i32IncreaseDamage = pPC->GetComputedIncreaseDamage(pPC->GetAttackType()) - pNPC->GetComputedIncreaseDamageIgnore((INT32)pPC->GetAttackType());
	if (i32IncreaseDamage <= 0)
	{
		// 대미지 증가 수치가 <= 0 이면 기본 대미지
		i32SecondDamage = i32FirstDamage;
	}
	else
	{
		// 기본 대미지 * ( 1+ 대미지 증가 수치 * 랜덤값 0.01~0.05)
		i32SecondDamage = i32FirstDamage * (1 + i32IncreaseDamage * 0.01);
	}
	
	// 3차 대미지 계산 : 일격 필살
	// 일격 필살 발동 확률 계산 => 일격 필살 확률 >= 랜덤값 (1 ~ 100);
	bCriticalout = FALSE;
	INT32 i32CriticalRandom = g_GameManager.getRandomNumber(0, 100);
	DOUBLE i32ThirdDamageRandom = g_GameManager.getRandomNumber(1, 5) * 0.01f;
	if (i32CriticalRandom <= pPC->m_BalanceInfo._i32PartsArray[BALANCE_POWER_PARTS32::BALANCE_POWER_PARTS32_Critical_Percent])
	{
		bCriticalout = TRUE;
	}
	if (bCriticalout == TRUE)
	{
		// 일격 필살 대미지 증가 수치 : 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
		DOUBLE i32IncreaseCritical = pPC->GetComputedIncreaseCritical() - pNPC->GetComputedIncreaseCriticalIgnore();
	
		if (i32IncreaseCritical <= 0)
		{
			// 대미지 증가 수치가 <= 0 이면 2차 대미지
			i32ThirdDamage = i32SecondDamage;
		}
		else
		{
			// 대미지 증가 수치가 > 0 이면  (2차 대미지) *(1.25 + (대미지 증가 수치 * 0.001) + 랜덤값(0.01~0.05))
			i32ThirdDamage = i32SecondDamage * (1.25 + (i32IncreaseCritical * 0.001) + i32ThirdDamageRandom);
		}
	}
	else
	{
		// 일격 필살 미 발동 시 대미지 : 2차 대미지
		i32ThirdDamage = i32SecondDamage;
	}
	
	// 최종 대미지 계산 PVP, PVE 공격력 증가, 방어력 증가 이용
	// 최종 대미지 : 3차 대미지 * ( 1 + (PVE 공격력 - PVE 방어력) * 0.01)
	INT32 i32PvPAttack = pPC->GetComputedPvePower(); // PVE 공격력
	INT32 i32PvPDefense = pNPC->GetComputedPveDef(); // PVE 방어력
	i32TotalDamage = i32ThirdDamage + (i32PvPAttack - i32PvPDefense);
	
	if ((INT32)i32TotalDamage <= 0)
	{
		i32TotalDamage = 1;
	}

	return i32TotalDamage;
}

INT32 CMath::AttackTarget_Damage_CLONEPC(CUnit* pUnit, CUnit* pTarget, BOOL& bCriticalout)
{
	auto pPC = dynamic_cast<CUnitClone*> (pUnit);
	auto pTargetPC = dynamic_cast<CUnitPC*> (pTarget);
	if (pPC == NULL || pTargetPC == NULL)
	{
		return 0;
	}

	DOUBLE i32FirstDamage = 0;		// 1차 대미지(기본 대미지)
	DOUBLE i32SecondDamage = 0;		// 2차 대미지(대미지 증가)
	DOUBLE i32ThirdDamage = 0;		// 3차 대미지(일격 필살 대미지)
	DOUBLE i32TotalDamage = 0;		// 최종 대미지
	DOUBLE i32AddDamage = 0;			//추가 대미지

	// 1차 대미지계산 : 기본 대미지
	DOUBLE i32Att = pPC->GetComputedPower(pPC->GetAttackType());				// 공격 타입에 따라 가져온 공격력
	DOUBLE i32Def = pTargetPC->GetComputedDef(pPC->GetAttackType());			// 공격자의 타입에 따라 가져온 타겟의 방어력
	DOUBLE i32DefIgnore = pPC->GetComputedDefIgnore(pPC->GetAttackType());	// 공격자의 타입에 따라 가져온 방어력 무시



	// 1차 대미지계산 : 기본 대미지
	// (공격력 * ( 1 - ( 방어력 - 방어력 무시) / (방어력 + 방어상수500))) * (0.95~1.05)
	// 만약 방어력 - 방어력무시가 0 이나오면 분자가 0 이므로 공격력 * 랜덤값만 적용
	DOUBLE i32FirstDamageRandom = g_GameManager.getRandomNumber(95, 105) * 0.01;
	if (i32Def - i32DefIgnore <= 0)
	{
		i32FirstDamage = i32Att * i32FirstDamageRandom;
	}
	else
	{
		i32FirstDamage = (i32Att * (1 - (i32Def - i32DefIgnore) / (i32Def + 500))) * i32FirstDamageRandom;
	}


	// 2차 대미지 계산  : 대미지 증가 계산
	// 대미지 증가 수치 : 공격자 대미지 증가 - 방어자 대미지 증가 무시
	DOUBLE i32IncreaseDamage = pPC->GetComputedIncreaseDamage(pPC->GetAttackType()) - pTargetPC->GetComputedIncreaseDamageIgnore(pPC->GetAttackType());
	if (i32IncreaseDamage <= 0)
	{
		// 대미지 증가 수치가 <= 0 이면 기본 대미지
		i32SecondDamage = i32FirstDamage;
	}
	else
	{
		// 기본 대미지 * ( 1+ 대미지 증가 수치 * 랜덤값 0.01~0.05)
		i32SecondDamage = i32FirstDamage * (1 + i32IncreaseDamage * 0.01);
	}

	// 3차 대미지 계산 : 일격 필살
	// 일격 필살 발동 확률 계산 => 일격 필살 확률 >= 랜덤값 (1 ~ 100);
	bCriticalout = FALSE;
	INT32 i32CriticalRandom = g_GameManager.getRandomNumber(0, 100);
	DOUBLE i32ThirdDamageRandom = g_GameManager.getRandomNumber(1, 5) * 0.01f;
	if (i32CriticalRandom <= pPC->m_BalanceInfo._i32PartsArray[BALANCE_POWER_PARTS32::BALANCE_POWER_PARTS32_Critical_Percent])
	{
		bCriticalout = TRUE;
	}

	if (bCriticalout == TRUE)
	{
		// 일격 필살 대미지 증가 수치 : 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
		DOUBLE i32IncreaseCritical = pPC->GetComputedIncreaseCritical() - pTargetPC->GetComputedIncreaseCriticalIgnore();


		if (i32IncreaseCritical <= 0)
		{
			// 대미지 증가 수치가 <= 0 이면 2차 대미지
			i32ThirdDamage = i32SecondDamage;
		}
		else
		{
			// 대미지 증가 수치가 > 0 이면  (2차 대미지) *(1.25 + (대미지 증가 수치 * 0.001) + 랜덤값(0.01~0.05))
			i32ThirdDamage = i32SecondDamage * (1.25 + (i32IncreaseCritical * 0.001) + i32ThirdDamageRandom);
		}
	}
	else
	{
		// 일격 필살 미 발동 시 대미지 : 2차 대미지
		i32ThirdDamage = i32SecondDamage;
	}

	// 최종 대미지 계산 PVP, PVE 공격력 증가, 방어력 증가 이용
	// 최종 대미지 : 3차 대미지 * ( 1 + (PVP 공격력 - PVP 방어력) * 0.01)
	INT32 i32PvPAttack = pPC->GetComputedPvpPower(); // PVP 공격력
	INT32 i32PvPDefense = pTargetPC->GetComputedPvpDef(); // PVP 방어력
	i32TotalDamage = i32ThirdDamage + (i32PvPAttack - i32PvPDefense);

	if ((INT32)i32TotalDamage <= 0)
	{
		i32TotalDamage = 1;
	}

	return i32TotalDamage;
}

INT32 CMath::AttackTarget_Damage_NPCCLONE(CUnit* pUnit, CUnit* pTarget, BOOL& bCriticalout)
{
	auto pPC = dynamic_cast<CUnitNPC*>(pUnit);
	auto pTargetPC = dynamic_cast<CUnitClone*> (pTarget);

	if (pPC == NULL || pTargetPC == NULL)
	{
		return 0;
	}

	DOUBLE i32FirstDamage = 0;		// 1차 대미지(기본 대미지)
	DOUBLE i32SecondDamage = 0;		// 2차 대미지(대미지 증가)
	DOUBLE i32ThirdDamage = 0;		// 3차 대미지(일격 필살 대미지)
	DOUBLE i32TotalDamage = 0;		// 최종 대미지
	DOUBLE i32AddDamage = 0;			//추가 대미지
	
	// 1차 대미지계산 : 기본 대미지
	DOUBLE i32Att = pPC->GetComputedPower(pPC->GetAttackType());				// 공격 타입에 따라 가져온 공격력
	DOUBLE i32Def = pTargetPC->GetComputedDef((USER_ATTACK_TYPE)pPC->GetAttackType());			// 공격자의 타입에 따라 가져온 타겟의 방어력
	DOUBLE i32DefIgnore = pPC->GetComputedDefIgnore(pPC->GetAttackType());	// 공격자의 타입에 따라 가져온 방어력 무시

	// 1차 대미지계산 : 기본 대미지
	// (공격력 * ( 1 - ( 방어력 - 방어력 무시) / (방어력 + 방어상수500))) * (0.95~1.05)
	// 만약 방어력 - 방어력무시가 0 이나오면 분자가 0 이므로 공격력 * 랜덤값만 적용
	DOUBLE i32FirstDamageRandom = g_GameManager.getRandomNumber(95, 105) * 0.01;
	if (i32Def - i32DefIgnore <= 0)
	{
		i32FirstDamage = i32Att * i32FirstDamageRandom;
	}
	else
	{
		i32FirstDamage = (i32Att * (1 - (i32Def - i32DefIgnore) / (i32Def + 500))) * i32FirstDamageRandom;
	}

	// 2차 대미지 계산  : 대미지 증가 계산
	// 대미지 증가 수치 : 공격자 대미지 증가 - 방어자 대미지 증가 무시
	DOUBLE i32IncreaseDamage = pPC->GetComputedIncreaseDamage(pPC->GetAttackType()) - pTargetPC->GetComputedIncreaseDamageIgnore((USER_ATTACK_TYPE)pPC->GetAttackType());
	if (i32IncreaseDamage <= 0)
	{
		// 대미지 증가 수치가 <= 0 이면 기본 대미지
		i32SecondDamage = i32FirstDamage;
	}
	else
	{
		// 기본 대미지 * ( 1+ 대미지 증가 수치 * 랜덤값 0.01~0.05)
		i32SecondDamage = i32FirstDamage * (1 + i32IncreaseDamage * 0.01);
	}

	// 3차 대미지 계산 : 일격 필살
	// 일격 필살 발동 확률 계산 => 일격 필살 확률 >= 랜덤값 (1 ~ 100);
	bCriticalout = FALSE;
	INT32 i32CriticalRandom = g_GameManager.getRandomNumber(0, 100);
	DOUBLE i32ThirdDamageRandom = g_GameManager.getRandomNumber(1, 5) * 0.01f;
	if (i32CriticalRandom <= pPC->GetComputedCriticalPercent())
	{
		bCriticalout = TRUE;
	}

	if (bCriticalout == TRUE)
	{
		// 일격 필살 대미지 증가 수치 : 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
		DOUBLE i32IncreaseCritical = pPC->GetComputedIncreaseCritical() - pTargetPC->GetComputedIncreaseCriticalIgnore();

		if (i32IncreaseCritical <= 0)
		{
			// 대미지 증가 수치가 <= 0 이면 2차 대미지
			i32ThirdDamage = i32SecondDamage;
		}
		else
		{
			// 대미지 증가 수치가 > 0 이면  (2차 대미지) *(1.25 + (대미지 증가 수치 * 0.001) + 랜덤값(0.01~0.05))
			i32ThirdDamage = i32SecondDamage * (1.25 + (i32IncreaseCritical * 0.001) + i32ThirdDamageRandom);
		}
		
	}
	else
	{
		// 일격 필살 미 발동 시 대미지 : 2차 대미지
		i32ThirdDamage = i32SecondDamage;
		
	}

	// 최종 대미지 계산 PVP, PVE 공격력 증가, 방어력 증가 이용
	// 최종 대미지 : 3차 대미지 * ( 1 + (PVE 공격력 - PVE 방어력) * 0.01)
	INT32 i32PvPAttack = pPC->GetComputedPvePower(); // PVE 공격력
	INT32 i32PvPDefense = pTargetPC->GetComputedPveDef(); // PVE 방어력
	i32TotalDamage = i32ThirdDamage + (i32PvPAttack - i32PvPDefense);

	if ((INT32)i32TotalDamage <= 0)
	{
		i32TotalDamage = 1;
	}

	return i32TotalDamage;
}

INT32 CMath::AttackTarget_Damage_PCCLONE(CUnit* pUnit, CUnit* pTarget, BOOL& bCriticalout)
{
	auto pPC = dynamic_cast<CUnitPC*> (pUnit);
	auto pTargetPC = dynamic_cast<CUnitClone*> (pTarget);

	if (pPC == NULL || pTargetPC == NULL)
	{
		return 0;
	}

	DOUBLE i32FirstDamage = 0;		// 1차 대미지(기본 대미지)
	DOUBLE i32SecondDamage = 0;		// 2차 대미지(대미지 증가)
	DOUBLE i32ThirdDamage = 0;		// 3차 대미지(일격 필살 대미지)
	DOUBLE i32TotalDamage = 0;		// 최종 대미지
	DOUBLE i32AddDamage = 0;			//추가 대미지
	

	// 1차 대미지계산 : 기본 대미지
	DOUBLE i32Att = pPC->GetComputedPower(pPC->GetAttackType());				// 공격 타입에 따라 가져온 공격력
	DOUBLE i32Def = pTargetPC->GetComputedDef(pPC->GetAttackType());			// 공격자의 타입에 따라 가져온 타겟의 방어력
	DOUBLE i32DefIgnore = pPC->GetComputedDefIgnore(pPC->GetAttackType());	// 공격자의 타입에 따라 가져온 방어력 무시

	// 1차 대미지계산 : 기본 대미지
	// (공격력 * ( 1 - ( 방어력 - 방어력 무시) / (방어력 + 방어상수500))) * (0.95~1.05)
	// 만약 방어력 - 방어력무시가 0 이나오면 분자가 0 이므로 공격력 * 랜덤값만 적용
	DOUBLE i32FirstDamageRandom = g_GameManager.getRandomNumber(95, 105) * 0.01;
	if (i32Def - i32DefIgnore <= 0)
	{
		i32FirstDamage = i32Att * i32FirstDamageRandom;
	}
	else
	{
		i32FirstDamage = (i32Att * (1 - (i32Def - i32DefIgnore) / (i32Def + 500))) * i32FirstDamageRandom;
	}


	// 2차 대미지 계산  : 대미지 증가 계산
	// 대미지 증가 수치 : 공격자 대미지 증가 - 방어자 대미지 증가 무시
	DOUBLE i32IncreaseDamage = pPC->GetComputedIncreaseDamage(pPC->GetAttackType()) - pTargetPC->GetComputedIncreaseDamageIgnore(pPC->GetAttackType());
	if (i32IncreaseDamage <= 0)
	{
		// 대미지 증가 수치가 <= 0 이면 기본 대미지
		i32SecondDamage = i32FirstDamage;
	}
	else
	{
		// 기본 대미지 * ( 1+ 대미지 증가 수치 * 랜덤값 0.01~0.05)
		i32SecondDamage = i32FirstDamage * (1 + i32IncreaseDamage * 0.01);
	}

	// 3차 대미지 계산 : 일격 필살
	// 일격 필살 발동 확률 계산 => 일격 필살 확률 >= 랜덤값 (1 ~ 100);
	bCriticalout = FALSE;
	INT32 i32CriticalRandom = g_GameManager.getRandomNumber(0, 100);
	DOUBLE i32ThirdDamageRandom = g_GameManager.getRandomNumber(1, 5) * 0.01f;
	if (i32CriticalRandom <= pPC->m_BalanceInfo._i32PartsArray[BALANCE_POWER_PARTS32::BALANCE_POWER_PARTS32_Critical_Percent])
	{
		bCriticalout = TRUE;
	}

	if (bCriticalout == TRUE)
	{
		// 일격 필살 대미지 증가 수치 : 공격자 일격 필살 대미지 증가 - 방어자 일격 필살 대미지 증가 무시
		DOUBLE i32IncreaseCritical = pPC->GetComputedIncreaseCritical() - pTargetPC->GetComputedIncreaseCriticalIgnore();

		if (i32IncreaseCritical <= 0)
		{
			// 대미지 증가 수치가 <= 0 이면 2차 대미지
			i32ThirdDamage = i32SecondDamage;
		}
		else
		{
			// 대미지 증가 수치가 > 0 이면  (2차 대미지) *(1.25 + (대미지 증가 수치 * 0.001) + 랜덤값(0.01~0.05))
			i32ThirdDamage = i32SecondDamage * (1.25 + (i32IncreaseCritical * 0.001) + i32ThirdDamageRandom);
		}
	}
	else
	{
		// 일격 필살 미 발동 시 대미지 : 2차 대미지
		i32ThirdDamage = i32SecondDamage;
	}

	// 최종 대미지 계산 PVP, PVE 공격력 증가, 방어력 증가 이용
	// 최종 대미지 : 3차 대미지 * ( 1 + (PVP 공격력 - PVP 방어력) * 0.01)
	INT32 i32PvPAttack = pPC->GetComputedPvpPower(); // PVP 공격력
	INT32 i32PvPDefense = pTargetPC->GetComputedPvpDef(); // PVP 방어력
	i32TotalDamage = i32ThirdDamage + (i32PvPAttack - i32PvPDefense);

	// 추가 대미지 증가 수치 : 공격자 추가 대미지 증가 - 방어자 추가 대미지 증가 무시
	i32AddDamage = pPC->GetComputedAddPowerIncrease(pPC->GetAttackType()) - pTargetPC->GetComputedAddPowerIncreaseIgnore(pPC->GetAttackType());
	if (i32AddDamage > 0)
	{
		// 추가 대미지 증가 수치가 > 0 이면 최종 대미지 *  ( 1 + 추가 대미지 증가 수치 * 0.01)
		// 아니면 그냥 최종 대미지
		i32TotalDamage = i32TotalDamage * (1 + i32AddDamage * 0.01);
	}

	if ((INT32)i32TotalDamage <= 0)
	{
		i32TotalDamage = 1;
	}

	return i32TotalDamage;
}

```
</details>

<details>
<summary>수정 코드</summary>

```ruby
// ===============================================================================
// AttackTarget_1003B2M의 개선된 대체 함수
// 유지보수성과 가독성을 향상시킨 버전
// MAX_COUNT_DAMAGE(5)와 클라이언트 패킷 호환성을 완전히 유지
// ===============================================================================

/// <summary>
/// AttackTarget_1003B2M의 개선된 대체 함수
/// 기존 함수의 모든 로직을 유지하면서 유지보수성을 대폭 개선
/// MAX_COUNT_DAMAGE(5) 및 클라이언트 패킷 구조 완전 호환
/// </summary>
/// <param name="pUnit">공격하는 유닛</param>
/// <param name="pTarget">공격받는 유닛</param>
void CMath::AttackTarget_Enhanced_V2(CUnit* pUnit, CUnit* pTarget)
{
	// 1단계: 컨텍스트 초기화
	AttackContext ctx;
	ctx.Initialize(pUnit, pTarget);
	
	if (!ctx.ValidateInput())
		return;
	
	// 2단계: 패킷 구조체 초기화 (클라이언트 호환성 보장)
	ctx.InitializePackets();
	
	// 3단계: 공격 유효성 검사 (거리, 타겟 상태 등)
	if (!ValidateAttackRequest(ctx))
		return;
	
	// 4단계: 공격 타입 및 애니메이션 설정
	if (ctx.pPC)
	{
		// 공격 타입 설정 (기존 로직 유지)
		for (int i = 0; i < MAX_COUNT_DAMAGE; i++)
		{
			INT32 i32AttackAnimation = ctx.pPC->GetAttAnimationIndex(FALSE);


			ctx.basicMsg._i32AttackType[i] = i32AttackAnimation;
			ctx.extraMsg._i32AttackType[i] = i32AttackAnimation;
			ctx.passiveExtraMsg._i32AttackType[i] = i32AttackAnimation;
			
		}
		

	}
	
	INT32 i32MaxDamageCount = 1;
	// 5단계: 공격 횟수 결정 (MAX_COUNT_DAMAGE 기반)
	if (ctx.pPC)
	{
		ctx.attackCount =  ((ctx.pPC->GetAttackSpeed()-1)/MAX_ATTACK_COUNT_SPEED)+1 ; // 공속에 따른 추가타
		ctx.extraPassive = CheckPassiveExtra(ctx.pPC);
		i32MaxDamageCount = GetAddAttackCount(ctx.pPC)+1; //추가 데미지 수
		
		// MAX_COUNT_DAMAGE(4) 제한 적용
		if (ctx.attackCount > MAX_ATTACK_COUNT)
		{
			ctx.attackCount = MAX_ATTACK_COUNT;
		}

		if (i32MaxDamageCount > MAX_COUNT_DAMAGE)
		{
			i32MaxDamageCount = MAX_COUNT_DAMAGE;
		}
	}
	
	// 6단계: 다중 공격 처리 (공격속도에 따른 패킷당 공격 횟수)
	for (int attackIndex = 0; attackIndex < ctx.attackCount; attackIndex++)
	{
		if (!ProcessHitCheck(ctx, attackIndex))
			continue; // 미스 발생 시 다음 공격으로
			
		// 7단계: 데미지 계산 및 적용
		CalculateMultiHitDamage(ctx, attackIndex, i32MaxDamageCount);
		
		// 8단계: 특수 효과 적용 (버프, 디버프 등)
		ApplySpecialEffects(ctx, attackIndex);
	}
	
	// 8.5단계: PvP 고급 PK 시스템 처리 - kill_unit
	/*if (ctx.pPC && ctx.pTargetPC)
	{
		ProcessAdvancedPKSystem(ctx);
	}*/
	
	// 8.6단계: 버프 및 아이템 옵션 처리
	ProcessBuffAndItemOptions(ctx);
	
	
	// 9단계: 결과 전송 및 후처리
	SendAttackResults(ctx);
	
	// 10단계: 타겟 사망 처리
	ProcessTargetDeath(ctx);
	
	// 11단계: 정리 작업
	CleanupAttack(ctx);
}

/// <summary>
/// 공격 요청 유효성 검사 (거리, 상태, 맵 등)
/// </summary>
BOOL CMath::ValidateAttackRequest(AttackContext& ctx)
{
	if (!ctx.pCurrentMap)
		return FALSE;
	
	// 거리 체크 (기존 로직 유지)
	if (!CheckAttackRange(ctx))
		return FALSE;
	
	if (ctx.pPC && ctx.pPC->GetWeightPercent() >= USABLE_WEIGHT_PERCENT)
	{
		return FALSE;
	}


	// 타겟 상태 체크
	if (ctx.pTargetNPC && ctx.pTargetNPC->GetNPC_CSVData()->Type == NPC)
	{
		if (ctx.pPC)
		{
			for (INT32 i32 = 0; i32 < MAX_COUNT_DAMAGE; i32++)
			{
				ctx.basicMsg._result[i32] = ENUM_ALL_ERROR_ATTACK_TOWN_NPC;
			}
			ctx.pPC->Write((BYTE*)&ctx.basicMsg, sizeof(SP_Attack), 0);
		}
		return FALSE;
	}
	
	return TRUE;
}

/// <summary>
/// 공격 거리 유효성 검사
/// </summary>
BOOL CMath::CheckAttackRange(AttackContext& ctx)
{
	if (!ctx.pPC)
		return TRUE; // NPC나 클론은 거리 체크 생략
		
	if (ctx.pPC->GetAccountType() == ACCTYPE_DUMMY)
		return TRUE; // 더미는 거리 체크 해제
	
	INT32 range = AttackRange(ctx.pPC->GetJobCode());
	BOOL inRange = FALSE;
	
	// PvP 거리 체크
	if (ctx.pTargetPC)
	{
		inRange = g_MapManager.IsRange(range, ctx.pAttacker->m_X, ctx.pAttacker->m_Y, 
									 ctx.pTarget->m_X, ctx.pTarget->m_Y);
	}
	// PvE 거리 체크 
	else if (ctx.pTargetNPC)
	{
		INT32 totalRange = range + ctx.pTargetNPC->GetNPC_CSVData()->TileSize/* + 
						  ctx.pTargetNPC->GetNPC_CSVData()->_i32HitSize*/;
		inRange = g_MapManager.IsRange(totalRange, ctx.pAttacker->m_X, ctx.pAttacker->m_Y,
									  ctx.pTarget->m_X, ctx.pTarget->m_Y);
	}
	
	if (!inRange)
	{
		for (INT32 i32 = 0; i32 < MAX_ATTACK_COUNT; i32++)
		{
			ctx.basicMsg._result[i32] = ENUM_ALL_ERROR_ATTACK_NOT_RANGE;
		}
		if (ctx.pTargetNPC)
		{
			ctx.basicMsg._HpPercent = ctx.pTargetNPC->GetHpPercent() ;
		}
		ctx.pPC->Write((BYTE*)&ctx.basicMsg, sizeof(SP_Attack), 0);
		return FALSE;
	}
	
	return TRUE;
}

/// <summary>
/// 명중 판정 처리 (공격 인덱스별)
/// </summary>
BOOL CMath::ProcessHitCheck(AttackContext& ctx, INT32 attackIndex)
{
	BOOL hitResult = !MissCheck(ctx.pAttacker, ctx.pTarget,false);
	
	if (!hitResult)
	{
		// 미스 처리 (기존 로직 유지)
		memset(ctx.basicMsg._ui32Damage[attackIndex], 0x00, sizeof(ctx.basicMsg._ui32Damage[attackIndex]));
		ctx.basicMsg._result[attackIndex] = ENUM_ALL_ERROR_ATAACK_MISS;
		ctx.basicMsg._ui32ExtraDamage[attackIndex] = 0;
		
		// 미스도 어그로 처리 (PvE에서)
		if (ctx.pTargetNPC && ctx.pTargetNPC->GetNPC_CSVData()->Type != NPC)
		{
			if (ctx.pTargetNPC->GetDamage_Unit()->dwUnitUnique <= 0)
			{
				ctx.pTargetNPC->SetDamageUnit(ctx.pPC->GetPoolArray(), 
											 ctx.pPC->GetFieldUnique(), 
											 ctx.pPC->m_X, ctx.pPC->m_Y);
			}
			
			// 합공 몬스터 어그로 처리
			if (ctx.pCurrentMap)
				ctx.pCurrentMap->m_BlockManager.SetJoinAttackDamageUnit(ctx.pPC, MAX_JOIN_ATTACK_SCOPE);
		}

		//공격자가 유저 , 타겟이 NPC일 때 -> 숙련도 상승
		if (ctx.pPC && ctx.pTargetNPC)
		{
			if (ctx.pPC->GetEquipWeapon() != NULL) //무기가 없을시 숙련도 상승 x
			{
				ctx.pPC->AddStatExp(ENUM_USER_STAT_TYPE::JOBLEVEL, ctx.pTargetNPC->GetNPC_CSVData()->_i32TrainingBonus);
			}
		}
		//공격자가 NPC , 타겟이 PC일 때 -> 민첩 상승
		else if (ctx.pNPC && ctx.pTargetPC)
		{
			ctx.pTargetPC->AddStatExp(ENUM_USER_STAT_TYPE::DEX, ctx.pNPC->GetNPC_CSVData()->_i32TrainingBonus);
		}

		
		return FALSE;
	}
	
	return TRUE;
}

/// <summary>
/// 다중 공격 데미지 계산 (MAX_COUNT_DAMAGE 배열 활용)
/// </summary>
void CMath::CalculateMultiHitDamage(AttackContext& ctx, INT32 attackIndex, INT32 i32MaxDamage)
{
	if (ctx.pTarget->GetCurrentHP() < 0)
	{
		return;
	}


	BOOL bCritical = FALSE;
	BOOL bExtraCritical = FALSE;
	
	// 기본 데미지 계산 (기존 데미지 계산 함수 활용)
	UINT32 basicDamage[MAX_COUNT_DAMAGE] = { 0, };
	
	// 유닛 타입별 데미지 계산 (기존 로직 유지)
	for (INT32 i32 = 0; i32 < i32MaxDamage; i32++)
	{
		basicDamage[i32] = AttackTarget_Damage(ctx.pAttacker, ctx.pTarget, bCritical);
	}
	
	// 특수 데미지 계산 (기존 로직 유지)
	INT32 specialDamage = 0;
	if (ctx.pPC)
	{
		specialDamage = GetAttackExtraDamage_1003B2M(ctx.pPC, ctx.pTarget, basicDamage[0], ctx.successSpecialCount);
	}
	
	// 패시브 추가타 데미지 계산
	INT32 extraDamage = 0;
	INT32 extraSpecialDamage = 0;
	if (ctx.extraPassive)
	{

		extraDamage = AttackTarget_Damage(ctx.pAttacker, ctx.pTarget, bExtraCritical);

		
		if (ctx.pPC)
		{
			extraSpecialDamage = GetAttackExtraDamage_1003B2M(ctx.pPC, ctx.pTarget, extraDamage, ctx.successSpecialExtraCount);
		}
	}
	
	// 패킷 배열에 데미지 저장 (MAX_COUNT_DAMAGE 인덱스 사용)
	memcpy(ctx.basicMsg._ui32Damage[attackIndex],basicDamage,sizeof(ctx.basicMsg._ui32Damage[attackIndex]));
	ctx.basicMsg._ui32ExtraDamage[attackIndex] = extraDamage;
	ctx.extraMsg._ui32Damage[attackIndex] = specialDamage;
	ctx.passiveExtraMsg._ui32Damage[attackIndex] = extraSpecialDamage;
	
	// 크리티컬 플래그 설정 (비트마스크 사용)
	ctx.basicMsg.i32Critical |= (bCritical << attackIndex);
	ctx.basicMsg.i32ExtraCritical |= (bExtraCritical << attackIndex);
	ctx.extraMsg.i32Critical |= (bCritical << attackIndex);
	ctx.passiveExtraMsg.i32Critical |= (bExtraCritical << attackIndex);
	
	INT64 i64TotalbasicDamage = 0;
	for (INT32 i32 = 0; i32 < MAX_COUNT_DAMAGE; i32++)
	{
		i64TotalbasicDamage += basicDamage[i32];
	}

	// 총 데미지로 타겟 HP 감소
	INT64 totalDamage = i64TotalbasicDamage + specialDamage + extraDamage + extraSpecialDamage;
	if (totalDamage > 0)
	{
		ctx.pTarget->AddHP(-totalDamage);

		// 최대 데미지 로깅 (PvE)
		if (ctx.pPC && ctx.pTarget->GetUnitTYPE() == eUnitType::NPC)
		{
			ctx.pPC->CheckMaxDamageLog(ctx.pTarget, totalDamage);
		}
		
		// PvE 경험치 처리 (몬스터 공격 시)
		if (ctx.pPC || ctx.pTargetPC)
		{
			ProcessExperienceGain(ctx, attackIndex, totalDamage);
		}
		
		// Hunt Event 처리 (기존 로직 유지)
		if (ctx.pPC && ctx.pTargetNPC && ctx.pTargetNPC->GetNPC_CSVData()->Type != NPC)
		{
			ctx.pTargetNPC->InsertHuntEventList(ctx.pPC, totalDamage);
		}

	}
	
	ctx.successAttackCount++;
}

/// <summary>
/// 특수 효과 적용 (버프, 디버프, 경직 등)
/// </summary>
void CMath::ApplySpecialEffects(AttackContext& ctx, INT32 attackIndex)
{
	// 혼불 소모 처리
	if (ctx.pPC)
	{
		ctx.pPC->_SoulfireList.UserSoulFire(ctx.pPC, ENUM_SOULFIRE_TYPE::NOMAL_ATTACK);
		
		// 패시브 추가타 시 추가 혼불 소모
		if (ctx.extraPassive)
		{
			ctx.pPC->_SoulfireList.UserSoulFire(ctx.pPC, ENUM_SOULFIRE_TYPE::NOMAL_ATTACK);
		}
	}
	
	// 반사 버프 처리 (PvP)
	//if (ctx.pTargetPC)
	//{
	//	stBuffData* reflectBuff = ctx.pTargetPC->GetReflectSkillData();
	//	if (reflectBuff)
	//	{
	//		g_BuffManager.AddBuff(reflectBuff, ctx.pTargetPC, ctx.pPC);
	//	}
	//}
	
	// 경직 처리
	if (ctx.pTarget->GetCurrentHP() > 0)
	{
		//if (StiffnssCheck(ctx.pAttacker, ctx.pTarget))
		//{
		//	if (ctx.pTargetPC && ctx.pTargetPC->GetStiffnessTime() <= 0)
		//	{
		//		ctx.pTargetPC->SetStiffnessTime();
		//		SP_STIFFNESS stiffnessMsg;
		//		stiffnessMsg._dwTargetFieldUnique = ctx.pTargetPC->GetFieldUnique();
		//		stiffnessMsg._dwFieldUnique = ctx.pAttacker->GetFieldUnique();
		//		if (ctx.pCurrentMap)
		//			ctx.pCurrentMap->m_BlockManager.BroadCast(ctx.pTarget, (BYTE*)&stiffnessMsg, sizeof(SP_STIFFNESS));
		//	}
		//	else if (ctx.pTargetNPC)
		//	{
		//		ctx.pTargetNPC->SetStiffnessTime();
		//	}
		//}
	}
	
	// PK 쿨타임 처리 (PvP)
	if (ctx.pPC && ctx.pTargetPC)
	{
		if (ctx.pPC->GetPKCoolTime() <= 0)
		{
			switch (ctx.pPC->GetMapType())
			{
				case ENUM_PVP_MAP_TYPE::ENUM_PVP_MAP_TYPE_SAFE:
					ctx.pPC->SetPKCoolTime(DEFINECSV("SAFE_PVP_MAP_COOL"));
					break;
				case ENUM_PVP_MAP_TYPE::ENUM_PVP_MAP_TYPE_NORMAL:
					ctx.pPC->SetPKCoolTime(DEFINECSV("NORMAL_PVP_MAP_COOL"));
					break;
				case ENUM_PVP_MAP_TYPE::ENUM_PVP_MAP_TYPE_DANGER:
					ctx.pPC->SetPKCoolTime(DANGER_PVP_MAP_COOL);
					ctx.pTargetPC->SetPKCoolTime(DANGER_PVP_MAP_COOL);
					break;
				case ENUM_PVP_MAP_TYPE::ENUM_PVP_MAP_TYPE_DISPUTE:
					ctx.pPC->SetPKCoolTime(DISPUTE_PVP_MAP_COOL);
					break;
				default:
					ctx.pPC->SetPKCoolTime(0);
					break;
			}
		}
	}
}

/// <summary>
/// 공격 결과 패킷 전송 (클라이언트 호환성 완전 보장)
/// </summary>
void CMath::SendAttackResults(AttackContext& ctx)
{

	// 공격 카운트 정보 설정 (기존 패킷 구조 유지)
	ctx.basicMsg._i32AttackCount =  ctx.successAttackCount;
	ctx.extraMsg._i32AttackCount =  ctx.successAttackCount;
	
	// HP 퍼센트 설정
	INT32 targetHpPercent = 0;
	if (ctx.pTargetPC)
		targetHpPercent = ctx.pTargetPC->GetHpPercent();
	else if (ctx.pTargetNPC)
		targetHpPercent = ctx.pTargetNPC->GetHpPercent();
	

	// 여기까지 들어오면 일단 공격에는 성공한 패킷임.
	if (ctx.successAttackCount > 0)
	{
		for (INT32 i32 = 0; i32 < MAX_ATTACK_COUNT; i32++)
		{
			if (ctx.basicMsg._result[i32] != ENUM_ALL_ERROR_ATAACK_MISS)
			{
				ctx.basicMsg._result[i32] = SUCCESS;
			}
		}
		ctx.basicMsg._HpPercent =targetHpPercent ;
	}
	else
	{
		for (INT32 i32 = 0; i32 < MAX_ATTACK_COUNT; i32++)
		{
			if (ctx.basicMsg._result[i32] != ENUM_ALL_ERROR_ATAACK_MISS)
			{
				ctx.basicMsg._result[i32] = SUCCESS;
			}
		}
		ctx.basicMsg._HpPercent = targetHpPercent;
	}
	
	// 패킷 전송 (기존 순서와 구조 완전 유지)
	//if (ctx.pPC != NULL)
	//{
	//	ctx.pPC->Write((BYTE*)&ctx.basicMsg, sizeof(SP_Attack));
	//}
	
	// 특수 데미지가 있을 때만 추가 패킷 전송
	INT32 totalExtraDamage = 0;
	for (int i = 0; i < MAX_COUNT_DAMAGE; i++)
	{
		totalExtraDamage += ctx.extraMsg._ui32Damage[i];
		totalExtraDamage += ctx.passiveExtraMsg._ui32Damage[i];
	}
	
	if (totalExtraDamage > 0)
	{
		if (ctx.pPC != NULL)
		{
			ctx.pPC->Write((BYTE*)&ctx.extraMsg, sizeof(SP_Attack_Extra));
			if (ctx.extraPassive)
				ctx.pPC->Write((BYTE*)&ctx.passiveExtraMsg, sizeof(SP_Attack_Extra));
		}
	}
	
	// 브로드캐스트 (주변 플레이어들에게 공격 정보 전송)
	if (ctx.pCurrentMap)
	{
		ctx.pCurrentMap->m_BlockManager.Attack_to_BroadCast(ctx.pAttacker, ctx.pTarget, &ctx.basicMsg);
	}
}

/// <summary>
/// 타겟 사망 처리 및 경험치/아이템 드랍
/// </summary>
void CMath::ProcessTargetDeath(AttackContext& ctx)
{
	if (ctx.pTarget->GetCurrentHP() <= 0)
	{
		//Pc가 Unit을 죽임
		if (ctx.pPC && ctx.pTarget)
		{
			Kill_Unit(ctx.pPC, ctx.pTarget, ctx.pCurrentMap, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, 0, 0);
		}
		//NPC가 Unit을 죽임
		else if (ctx.pNPC && ctx.pTarget)
		{
			Kill_Unit(ctx.pNPC, ctx.pTarget, ctx.pCurrentMap, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, 0, 0);
		}
		//Clone가 Unit을 죽임
		else if (ctx.pClone && ctx.pTarget)
		{
			Kill_Unit(ctx.pClone, ctx.pTarget, ctx.pCurrentMap, (INT32)ENUM_ATTACK_TYPE::ENUM_ATTACK_TYPE_ATTACK, 0, 0);
		}

	}
}

/// <summary>
/// 공격 후 정리 작업 (메모리 해제, 상태 리셋 등)
/// </summary>
void CMath::CleanupAttack(AttackContext& ctx)
{
	// 공격 후 상태 업데이트
	if (ctx.pPC)
	{
		ctx.pPC->ResetConExpTick(); // 연속 경험치 틱 리셋
		
		// 마지막 공격 대상 정보 저장
		if (ctx.pTargetNPC)
		{
			ctx.pPC->SetLastTargetNpcField(ctx.pTargetNPC->GetFieldUnique());
		}
	}
	
	// 어그로 관련 후처리
	if (ctx.pTargetNPC && ctx.pTargetNPC->GetAlive() ==TRUE && ctx.pPC)
	{
		if (ctx.pTargetNPC->GetDamage_Unit()->dwUnitUnique <= 0)
		{
			ctx.pTargetNPC->SetDamageUnit(ctx.pPC->GetPoolArray(), 
										 ctx.pPC->GetFieldUnique(),
										 ctx.pPC->m_X, ctx.pPC->m_Y);
		}
	}
}

/// <summary>
/// PvE 경험치 획득 처리 (기존 로직 완전 재현)
/// </summary>
void CMath::ProcessExperienceGain(AttackContext& ctx, INT32 attackIndex, INT32 totalDamage)
{
	// 25.10.16_몬스터를 공격할 때마다 경험치를 올려주는 처리인데 사용안해서 주석해둠 필요시 주석 제거 후 사용

	//공격자가 NPC , 타겟이 유저 일 때 -> 맷집 상승
	if (ctx.pNPC && ctx.pTargetPC)
	{
		ctx.pTargetPC->AddStatExp(ENUM_USER_STAT_TYPE::CON, ctx.pNPC->GetNPC_CSVData()->_i32TrainingBonus);
	}
	//공격자가 유저 , 타겟이 NPC일 때 -> 힘 상승
	else if (ctx.pPC && ctx.pTargetNPC)
	{
		ctx.pPC->AddStatExp(ENUM_USER_STAT_TYPE::STR, ctx.pTargetNPC->GetNPC_CSVData()->_i32TrainingBonus);
	}


	//if (!ctx.pPC || !ctx.pTargetNPC)
	//	return;
	//	
	//// 경험치 계산 (기존 로직 유지)
	//int exp = ctx.pTargetNPC->GetNpcAttackExp(totalDamage);
	//int myLevel = ctx.pPC->GetLevel();
	//int levelDiff = myLevel - ctx.pTargetNPC->GetLevel();
	//
	//// 레벨 차이에 따른 경험치 조정 (기존 로직 유지)
	//if (/*ctx.pTargetNPC->GetNPC_CSVData()->_i32LimitLevel == 0 && */levelDiff > 0)
	//{
	//	if (levelDiff < MAX_XP_LEVEL_GAP) // 1~199 레벨차
	//	{
	//		exp -= exp * levelDiff * 0.005;
	//		ctx.pPC->AddExp(exp, ctx.pTargetNPC->GetFieldUnique());
	//	}
	//	// 200 이상 레벨차는 경험치 없음
	//}
	//else
	//{
	//	// 케릭 레벨이 몬스터 레벨보다 낮거나 같으면
	//	ctx.pPC->AddExp(exp, ctx.pTargetNPC->GetFieldUnique());
	//}
}

void CMath::ProcessAdvancedPKSystem(AttackContext& ctx)
{
	// PK 시스템은 PC 대 PC 전투에서만 처리
	if (!ctx.pPC || !ctx.pTargetPC)
		return;

	// PK 모드 체크
	BOOL attackerPKMode = ctx.pPC->CheckPKMode();
	BOOL targetPKMode = ctx.pTargetPC->CheckPKMode();

	// 정당방위 시스템 처리
	if (attackerPKMode && !targetPKMode)
	{
		// 공격자가 PK 모드이고 피격자가 PK 모드가 아닌 경우
		// 피격자의 정당방위 리스트에 공격자 추가
		ctx.pTargetPC->AddSelfDefense(ctx.pPC);
	}

	// PK 등급 업데이트 (타겟이 죽었을 때만)
	if (ctx.pTargetPC->GetCurrentHP() <= 0)
	{
		// 살생부에 기록
		ctx.pPC->GetPkList()->SetAttacker(ctx.pTargetPC, ctx.basicMsg._ui32Damage[0][0]);

		// PK 기록 저장
		ctx.pPC->RecordPkData(ctx.pTargetPC);

		// PK 등급 업데이트
		ctx.pPC->UpdatePKGrade(ctx.pTargetPC);

		// PK 킬 카운트 증가
		ctx.pPC->AddPKKill();

		// 타겟의 데스 처리
		ctx.pTargetPC->AddPKDeath();

		//ctx.pTargetPC->GetPkList()->SetTarget(ctx.pPC, ctx.basicMsg._ui32Damage[0][0]);
	}
}

void CMath::ProcessBuffAndItemOptions(AttackContext& ctx)
{
	if (!ctx.pAttacker)
		return;

	for (INT32 i32 = 0; i32 < MAX_ATTACK_COUNT; i32++)
	{
		INT64 i64Damage = 0;
		for (INT32 j = 0; j < MAX_COUNT_DAMAGE; j++)
		{
			i64Damage += ctx.basicMsg._ui32Damage[i32][j];

		}

		//공격 후 데미지입히면 어태커가 받는 효과 
		if (g_BuffManager.SpeciailAttackBuffCheck(ctx.pAttacker) == TRUE)
		{
			g_BuffManager.NormalAttackSpecialEffect(ctx.pAttacker, ctx.pTarget, i64Damage); //평타 공격시 발동하는 버프들효과 적용
		}

		//반사는 살아있어야만 가능
		g_BuffManager.CounterBuffEffect(ctx.pAttacker, ctx.pTarget, i64Damage);
	}

	// 버프 시스템 처리
	if (ctx.pPC)
	{
		// 공격 시 버프 효과 적용
		// - 공격력 증가 버프
		// - 치명타 확률 증가 버프
		// - 스킬 데미지 증가 버프 등

		// 버프 지속시간 감소 (일부 버프는 공격 시 소모됨)
		ctx.pPC->BuffAbilityUpdate(false);
	}

	if (ctx.pTarget)
	{
		// 타겟에게 디버프 적용 가능성 체크
		// - 독 효과
		// - 기절 효과  
		// - 둔화 효과 등

		ctx.pTarget->BuffAbilityUpdate(false);
	}
}

```
</details>

개선점
1. 데미지 계산공식은 밸런스 구조체가 있는 CUnit의 함수에서 계산하게 만듬
2. 구간별로 명확하게 파트를 명확히 나누어 정의
3. 포인터와 참조를 구조체화 시켜서 NULL검증과 쓸대없는 분기를 격리
