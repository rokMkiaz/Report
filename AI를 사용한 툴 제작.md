## AI를 사용한 툴 제작

### 1.Connect 파일의 반복작업
<img width="1470" height="727" alt="image" src="https://github.com/user-attachments/assets/4ea53992-bcc2-4002-883f-a53e2658e78e" />

1. 해당 파일들이 자주 수정되지는 않지만, 반복적인 호출이 많다. 해당 파일을 학습시켜 AI를 사용해 만들고자한다.
2. 반복학습 과정에서 접근해야할 부분에 대해 지정하고 다시 제시를 한다.
#### 1. 문서 반복 학습
1. GPT를 사용해 문서를 통째로 넣어 수정을 학습을 시도 하였으나, 실패하였다. 언어 보다는 학습의 접근 방법이 잘 못된 것 같다.
2. 내용을 복사 붙혀넣기 하여 학습을 시도 하였으나, 역시나 일정이상 가면 실패를 하였다. 용량을 적게 쓰면서 효율적인 학습 방법이 필요해 보인다.

#### 2. 문항을 통한 유도

<img width="556" height="719" alt="image" src="https://github.com/user-attachments/assets/eb06143e-a568-4d81-a54a-bf5eef4d904e" />

1. 최초 질문에 타겟 언어를 개발성과 유지보수를 생각 C#으로 지목하였다. 
2. 문항을 통해 내가 질문하고 싶은 점들을 정확하게 지정하여 나열해 보았다.
<details>
<summary>코드/펼치기</summary>

  
```ruby
using System;
using System.Linq;

namespace ProbabilityChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("확률들을 콤마(,)로 구분하여 입력하세요 (예: 0.2,0.5,0.3):");
            var input = Console.ReadLine();

            // 입력 파싱
            var tokens = input?
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => {
                    if (double.TryParse(t.Trim(), out var v)) return v;
                    throw new FormatException($"잘못된 숫자 형식: '{t}'");
                })
                .ToArray();

            if (tokens == null || tokens.Length == 0)
            {
                Console.WriteLine("확률을 하나 이상 입력해야 합니다.");
                return;
            }

            // 검사
            if (IsValidDistribution(tokens, out double sum, out int badIndex))
            {
                Console.WriteLine($"유효한 확률 분포입니다. 합 = {sum:F6}");
            }
            else
            {
                if (badIndex >= 0)
                    Console.WriteLine($"확률값 오류: 인덱스 {badIndex}의 값 {tokens[badIndex]}가 [0,1] 범위를 벗어났습니다.");
                else
                    Console.WriteLine($"합계 오류: 확률의 합이 1이 아닙니다. 합 = {sum:F6}");
            }
        }

        /// <summary>
        /// 확률 분포 유효성 검사
        /// </summary>
        /// <param name="probs">확률 배열</param>
        /// <param name="sum">합계 반환값</param>
        /// <param name="badIndex">잘못된 개별 확률의 인덱스 (-1 이면 합계 문제)</param>
        /// <param name="epsilon">합계 오차 한계(기본 1e-6)</param>
        static bool IsValidDistribution(double[] probs, out double sum, out int badIndex, double epsilon = 1e-6)
        {
            // 1) 0 ≤ p ≤ 1 검사
            for (int i = 0; i < probs.Length; i++)
            {
                if (probs[i] < 0.0 || probs[i] > 1.0)
                {
                    sum = probs.Sum();
                    badIndex = i;
                    return false;
                }
            }

            // 2) 합이 1에 근접한지 검사
            sum = probs.Sum();
            badIndex = -1;
            if (Math.Abs(sum - 1.0) > epsilon)
                return false;

            return true;
        }
    }
}
```
  
</details>

<img width="541" height="648" alt="image" src="https://github.com/user-attachments/assets/3c257908-6d55-40cf-a358-164cc8117381" />

2. 추가적으로 수정이 필요한 질문에도 꼭 문항을 나누어 정확하게 지적을 하였더니 내가 목적으로 하는 툴에 가까워 졌다.
3. 코드 완성 후 개별적인 튜닝을 통해 완성도를 높혔다.
[링크](https://github.com/rokMkiaz/GunBooster_TeamRocket_iwnam_summary/blob/main/Coroutine.md)
4. 커넥트 파일의 제목을 입력하는 기능을 추가하였다.



### 2.확률 검사 툴
기획자들의 실수를 줄여줄 툴을 만들었다. 실제로 액셀에 있는 기능이지만, 여전히 .csv만 다뤄 힘든 팀원들을 위하여 만들어주었다.

<img width="581" height="468" alt="image" src="https://github.com/user-attachments/assets/dfe69039-3356-45cd-aeb1-f0a0a9fef126" />
교육방식은 위와 같다
코드 초안은 아래와 같지만, 아직 많이 부족하였다.

<details>
<summary>코드/펼치기</summary>

  
```ruby
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CsvGroupProbabilityChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1) 처리할 파일들 입력
            Console.WriteLine("처리할 CSV 파일명을 콤마(,)로 구분하여 입력하세요 (예: A.csv,B.csv):");
            var fileNames = Console.ReadLine()?
                .Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .ToArray();

            if (fileNames == null || fileNames.Length == 0)
            {
                Console.WriteLine("파일을 하나 이상 입력해야 합니다.");
                return;
            }

            // 2) 그룹 칼럼 이름
            Console.WriteLine("그룹 칼럼 이름을 입력하세요 (예: 그룹):");
            var groupCol = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(groupCol))
            {
                Console.WriteLine("그룹 칼럼 이름을 입력해야 합니다.");
                return;
            }

            // 3) 확률 칼럼 이름
            Console.WriteLine("확률 칼럼 이름을 입력하세요 (예: 확률):");
            var probCol = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(probCol))
            {
                Console.WriteLine("확률 칼럼 이름을 입력해야 합니다.");
                return;
            }

            // 4) 최종 확률 합계
            Console.WriteLine("비교할 최종 확률(합계)을 입력하세요 (예: 1000000000):");
            if (!double.TryParse(Console.ReadLine(), out var targetSum))
            {
                Console.WriteLine("유효한 숫자를 입력하세요.");
                return;
            }

            foreach (var inputFile in fileNames)
            {
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"파일을 찾을 수 없습니다: {inputFile}");
                    continue;
                }

                var lines = File.ReadAllLines(inputFile);
                // 1) 헤더 행 찾기
                int hdrIdx = Array.FindIndex(lines, 
                    l => l.Contains(groupCol) && l.Contains(probCol));
                if (hdrIdx < 0)
                {
                    Console.WriteLine($"헤더({groupCol}, {probCol})를 찾을 수 없습니다: {inputFile}");
                    continue;
                }

                // 2) 헤더 분해 (';', '\t' 제거하고 칼럼 명만 남기기)
                var headers = lines[hdrIdx]
                    .Split(new[]{';','\t'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
                    .ToArray();

                int gi = Array.IndexOf(headers, groupCol);
                int pi = Array.IndexOf(headers, probCol);
                if (gi < 0 || pi < 0)
                {
                    Console.WriteLine($"칼럼 인덱스 파싱 오류: {inputFile}");
                    continue;
                }

                // 3) 데이터 라인 파싱 & 그룹별 합산
                var groupSums = new Dictionary<string,double>();
                for (int i = hdrIdx + 1; i < lines.Length; i++)
                {
                    var cols = lines[i]
                        .Split(new[]{';','\t'}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .ToArray();
                    if (cols.Length <= Math.Max(gi, pi))
                        continue;

                    var g = cols[gi];
                    if (string.IsNullOrEmpty(g))
                        continue;

                    if (!double.TryParse(cols[pi], out var p))
                        continue;   // 확률 파싱 실패 시 건너뜀

                    if (groupSums.ContainsKey(g)) groupSums[g] += p;
                    else groupSums[g] = p;
                }

                // 4) 결과 파일 쓰기 (.csv → .txt)
                var outputFile = Path.ChangeExtension(inputFile, ".txt");
                using (var sw = new StreamWriter(outputFile))
                {
                    foreach (var kv in groupSums)
                    {
                        sw.WriteLine($"{kv.Key}그룹 -> {kv.Value} / {targetSum}");
                    }
                }

                // 5) 콘솔에도 출력
                Console.WriteLine($"{outputFile} 를 출력:");
                foreach (var kv in groupSums)
                {
                    Console.WriteLine($"  {kv.Key}그룹 -> {kv.Value} / {targetSum}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("모든 작업이 완료되었습니다.");
        }
    }
}

```
</details>

<img width="512" height="312" alt="image" src="https://github.com/user-attachments/assets/ec095406-8901-4625-9564-e2cdf1608ab5" />

위와같은 질문을 추가하며, 컬럼과 코드를 수정하였다.

<details>
<summary>코드/펼치기</summary>

  
```ruby
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CsvGroupProbabilityChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            // 실행파일(.exe)과 같은 디렉터리에서 InputFile.csv 로드
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatePath = Path.Combine(exeDir, "InputFile.csv");

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"템플릿이 없습니다: {templatePath}");
                return;
            }

            string[] tplLines;
            try
            {
                using (var fs = new FileStream(templatePath,
                                               FileMode.Open,
                                               FileAccess.Read,
                                               FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    var temp = new List<string>();
                    string row;
                    while ((row = sr.ReadLine()) != null)
                        temp.Add(row);
                    tplLines = temp.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"템플릿 읽기 오류: {ex.Message}");
                return;
            }

            foreach (var raw in tplLines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") )
                    continue;

                // 탭으로 분리하여 7개 항목 읽기: 파일, 그룹컬럼, 서브그룹컬럼, 확률컬럼, 최대합계, 그룹 인덱스컬럼, 전체 인덱스컬럼
                var parts = line.Split('\t')
                                .Select(p => p.Trim())
                                .ToArray();

                if (parts.Length != 7)
                {
                    Console.WriteLine($"[파싱 오류] 6개 항목 필요(CSV 구분): \"{line}\"");
                    continue;
                }

                var csvRel = "_Data/"+parts[0];
                var groupCol = parts[1];
                var subCol = parts[2];
                var isSubCol = subCol == "0" ? string.Empty : subCol;

                var probCol = parts[3];
                if (!double.TryParse(parts[4], out var targetSum))
                {
                    Console.WriteLine($"[파싱 오류] 최대확률 숫자 변환 실패: \"{parts[4]}\"");
                    continue;
                }
                var rawGroupIndex = parts[5];  // 중복 인덱스 검사 컬럼명 ("{없음}"일 경우 스킵)
                var rawGlobalIndex = parts[6];
                var groupIndexCol = rawGroupIndex == "0" ? string.Empty : rawGroupIndex;
                var globalIndexCol = rawGlobalIndex == "0" ? string.Empty : rawGlobalIndex;


                var csvPath = Path.Combine(exeDir, csvRel);
                if (!File.Exists(csvPath))
                {
                    Console.WriteLine($"[파일 없음] {csvPath}");
                    continue;
                }
                if (targetSum == 0)
                    ProcessFile_NotPercent(exeDir, csvPath, groupCol, groupIndexCol, globalIndexCol);
                else if (string.IsNullOrEmpty(isSubCol))
                    ProcessFile_GroupOnly(exeDir, csvPath, groupCol, probCol, targetSum, groupIndexCol, globalIndexCol);
                else
                    ProcessFile_GroupAndSub(exeDir, csvPath, groupCol, subCol, probCol, targetSum, groupIndexCol, globalIndexCol);
            }

            Console.WriteLine("모든 파일 처리 완료.");
        }
        static void ProcessFile_NotPercent(string exeDir, string csvPath,string groupCol,string groupIndexCol,string globalIndexCol)
        {
            // 파일 공유 모드로 읽기
            string[] lines;
            using (var fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                var temp = new List<string>();
                string row;
                while ((row = sr.ReadLine()) != null)
                    temp.Add(row);
                lines = temp.ToArray();
            }

            // 헤더 찾기: 구분자별로 컬럼을 분리한 뒤 정확히 매칭
            int hdr = Array.FindIndex(lines, l =>
            {
                var cols = l.Split(new[] { ',', ';', '	' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(c => c.Trim());
                return cols.Contains(groupCol);
            });
            if (hdr < 0)
            {
                Console.WriteLine($"[{Path.GetFileName(csvPath)}] 헤더({groupCol}) 못 찾음");
                return;
            }

            // 헤더에서 컬럼 인덱스 구하기
            var headers = lines[hdr].Split(new[] { ',', ';', '	' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(h => h.Trim()).ToArray();
            int gi = Array.IndexOf(headers, groupCol);
            int gii = string.IsNullOrEmpty(groupIndexCol) ? -1 : Array.IndexOf(headers, groupIndexCol);
            int globii = string.IsNullOrEmpty(globalIndexCol) ? -1 : Array.IndexOf(headers, globalIndexCol);

            var groupIndices = new Dictionary<string, HashSet<string>>();
            var firstGroupDup = new Dictionary<string, string>();
            var globalSeen = new HashSet<string>();
            string firstGlobalDup = null;

            for (int i = hdr + 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(new[] { ',', ';', '	' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(c => c.Trim()).ToArray();
                if (cols.Length <= gi) continue;

                var g = cols[gi];
                if (string.IsNullOrEmpty(g)) continue;

                // 그룹별 중복 인덱스 체크
                if (gii >= 0 && cols.Length > gii)
                {
                    var idx = cols[gii];
                    if (!groupIndices.ContainsKey(g))
                        groupIndices[g] = new HashSet<string>();
                    if (groupIndices[g].Contains(idx))
                    {
                        if (!firstGroupDup.ContainsKey(g))
                            firstGroupDup[g] = idx;
                    }
                    else
                        groupIndices[g].Add(idx);
                }

                // 전체 중복 인덱스 체크
                if (globii >= 0 && cols.Length > globii)
                {
                    var gidx = cols[globii];
                    if (globalSeen.Contains(gidx))
                    {
                        if (firstGlobalDup == null)
                            firstGlobalDup = gidx;
                    }
                    else
                        globalSeen.Add(gidx);
                }
            }

            var outputDir = Path.Combine(exeDir, "output");
            Directory.CreateDirectory(outputDir);
            var outPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(csvPath) + "_no_percnt.txt");

            using (var sw = new StreamWriter(outPath))
            {
                // 전체 인덱스 첫 중복을 상단에
                if (!string.IsNullOrEmpty(firstGlobalDup))
                    sw.WriteLine($"{globalIndexCol} 중복 INDEX: {firstGlobalDup}");
                // 그룹별 첫 중복만 출력
                foreach (var kv in firstGroupDup)
                    sw.WriteLine($"{groupCol} '{kv.Key}' 중복 INDEX: {kv.Value}");
                if (firstGroupDup.Count == 0 && string.IsNullOrEmpty(firstGlobalDup))
                    sw.WriteLine("No duplicates found.");
            }

            // 콘솔 출력
            Console.WriteLine($"[output/{Path.GetFileName(outPath)}] 생성 (중복만):");
            if (!string.IsNullOrEmpty(firstGlobalDup))
                Console.WriteLine($"{globalIndexCol} DUP INDEX: {firstGlobalDup}");
            foreach (var kv in firstGroupDup)
                Console.WriteLine($"'{groupCol}' '{kv.Key}' DUP INDEX: {kv.Value}");
            if (firstGroupDup.Count == 0 && string.IsNullOrEmpty(firstGlobalDup))
                Console.WriteLine("No duplicates found.");
            Console.WriteLine();
        }
        static void ProcessFile_GroupOnly(string exeDir, string csvPath, string groupCol, string probCol, double targetSum, string indexCol, string globalIndexCol)
        {
            string[] lines;
            using (var fs = new FileStream(csvPath,
                                           FileMode.Open,
                                           FileAccess.Read,
                                           FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                var temp = new List<string>();
                string row;
                while ((row = sr.ReadLine()) != null)
                    temp.Add(row);
                lines = temp.ToArray();
            }

            int hdr = Array.FindIndex(lines, l => l.Contains(groupCol) && l.Contains(probCol));
            if (hdr < 0)
            {
                Console.WriteLine($"[{Path.GetFileName(csvPath)}] 헤더({groupCol},{probCol}) 못 찾음");
                return;
            }

            var headers = lines[hdr]
                .Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim())
                .ToArray();

            int gi = Array.IndexOf(headers, groupCol);
            int pi = Array.IndexOf(headers, probCol);
            int gii = -1, globii = -1;

            bool doGroupIndex = !string.IsNullOrEmpty(indexCol);
            bool doGlobalIndex = !string.IsNullOrEmpty(globalIndexCol);
            if (doGroupIndex)
            {
                gii = Array.IndexOf(headers, indexCol);
                if (gii < 0)
                {
                    Console.WriteLine($"[{Path.GetFileName(csvPath)}] 인덱스 컬럼 못 찾음: {indexCol}");
                    return;
                }
            }
            if (doGlobalIndex)
            {
                globii = Array.IndexOf(headers, globalIndexCol);
                if (globii < 0)
                {
                    Console.WriteLine($"[{Path.GetFileName(csvPath)}] 전체 인덱스 컬럼 못 찾음: {globalIndexCol}");
                    return;
                }
            }

            var sums = new Dictionary<string, double>();
            var groupIndices = new Dictionary<string, HashSet<string>>();
            var firstGroupDup = new Dictionary<string, string>();
            var globalSeen = new HashSet<string>();
            string firstGlobalDup = null;

            for (int i = hdr + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";")) continue;

                var cols = lines[i]
                    .Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToArray();
                if (cols.Length <= Math.Max(gi, pi)) continue;

                var g = cols[gi];
                if (string.IsNullOrEmpty(g)) continue;
                if (!double.TryParse(cols[pi], out var p)) continue;

                if (sums.ContainsKey(g)) sums[g] += p;
                else sums[g] = p;

                if (doGroupIndex && cols.Length > gii)
                {
                    var idx = cols[gii];
                    if (!groupIndices.ContainsKey(g))
                        groupIndices[g] = new HashSet<string>();
                    if (groupIndices[g].Contains(idx))
                    {
                        if (!firstGroupDup.ContainsKey(g))
                            firstGroupDup[g] = idx;
                    }
                    else groupIndices[g].Add(idx);
                }
                if (doGlobalIndex && cols.Length > globii)
                {
                    var gidx = cols[globii];
                    if (globalSeen.Contains(gidx))
                    {
                        if (firstGlobalDup == null) firstGlobalDup = gidx;
                    }
                    else globalSeen.Add(gidx);
                }
            }

            var outputDir = Path.Combine(exeDir, "output");
            Directory.CreateDirectory(outputDir);
            var outPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(csvPath) + ".txt");
            using (var sw = new StreamWriter(outPath))
            {

                if (!string.IsNullOrEmpty(firstGlobalDup))
                    sw.WriteLine($"'{globalIndexCol}' 중복 INDEX: {firstGlobalDup}");
                foreach (var kv in sums)
                {
                    var err = new List<string>();
                    if (kv.Value != targetSum) err.Add("!!!!ERROR!!!!");
                    if (firstGroupDup.TryGetValue(kv.Key, out var dupVal))
                        err.Add($"!!!!{indexCol} 중복 INDEX : {dupVal}!!!!");
                    sw.WriteLine($"{groupCol} {kv.Key} -> {kv.Value} / {targetSum}"
                        + (err.Any() ? "\t" + string.Join(" ", err) : string.Empty));
                }
            }

            Console.WriteLine($"[output/{Path.GetFileName(outPath)}] 생성 (그룹만):");
            foreach (var kv in sums)
            {
                var err = new List<string>();
                if (kv.Value != targetSum) err.Add("!!!!ERROR!!!!");
                if (firstGroupDup.TryGetValue(kv.Key, out var dupVal))
                    err.Add($"!!!!DUP INDEX {dupVal}!!!!");
                Console.WriteLine($"{kv.Key} 그룹 -> {kv.Value} / {targetSum}"
                    + (err.Any() ? "\t" + string.Join(" ", err) : string.Empty));
            }
            Console.WriteLine();
        }
        static void ProcessFile_GroupAndSub(string exeDir, string csvPath, string groupCol, string subCol, string probCol, double targetSum, string indexCol ,string globalIndexCol)
        {
            string[] lines;
            using (var fs = new FileStream(csvPath,
                                           FileMode.Open,
                                           FileAccess.Read,
                                           FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                var temp = new List<string>();
                string row;
                while ((row = sr.ReadLine()) != null)
                    temp.Add(row);
                lines = temp.ToArray();
            }
            int hdr = Array.FindIndex(lines, l => l.Contains(groupCol) && l.Contains(subCol) && l.Contains(probCol));
            if (hdr < 0)
            {
                Console.WriteLine($"[{Path.GetFileName(csvPath)}] 헤더({groupCol},{subCol},{probCol}) 못 찾음");
                return;
            }

            var headers = lines[hdr]
                .Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim())
                .ToArray();

            int gi = Array.IndexOf(headers, groupCol);
            int si = Array.IndexOf(headers, subCol);
            int pi = Array.IndexOf(headers, probCol);
            int gii = -1, globii = -1;

            bool doGroupIndex = !string.IsNullOrEmpty(indexCol);
            bool doGlobalIndex = !string.IsNullOrEmpty(globalIndexCol);

        
            if (doGroupIndex)
            {
                gii = Array.IndexOf(headers, indexCol);
                if (gii < 0)
                {
                    Console.WriteLine($"[{Path.GetFileName(csvPath)}] 인덱스 컬럼 못 찾음: {indexCol}");
                    return;
                }
            }
            if (doGlobalIndex)
            {
                globii = Array.IndexOf(headers, globalIndexCol);
                if (globii < 0)
                {
                    Console.WriteLine($"[{Path.GetFileName(csvPath)}] 전체 인덱스 컬럼 못 찾음: {globalIndexCol}");
                    return;
                }
            }

            var sums = new Dictionary<(string grp, string sub), double>();
            var groupIndices = new Dictionary<string, HashSet<string>>();
            
            var firstGroupDup = new Dictionary<string, string>();
            var globalSeen = new HashSet<string>();
            string firstGlobalDup = null;

            for (int i = hdr + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";")) continue;

                var cols = lines[i]
                    .Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToArray();
                if (cols.Length <= Math.Max(Math.Max(gi, si), pi)) continue;

                var g = cols[gi];
                var s = cols[si];
                if (string.IsNullOrEmpty(g) || string.IsNullOrEmpty(s)) continue;
                if (!double.TryParse(cols[pi], out var p)) continue;

                var key = (g, s);
                if (sums.ContainsKey(key)) sums[key] += p;
                else sums[key] = p;

                if (doGroupIndex && cols.Length > gii)
                {
                    var idx = cols[gii];
                    if (!groupIndices.ContainsKey(g))
                        groupIndices[g] = new HashSet<string>();
                    if (groupIndices[g].Contains(idx))
                    {
                        if (!firstGroupDup.ContainsKey(g))
                            firstGroupDup[g] = idx;
                    }
                    else groupIndices[g].Add(idx);
                }
                if (doGlobalIndex && cols.Length > globii)
                {
                    var gidx = cols[globii];
                    if (globalSeen.Contains(gidx))
                    {
                        if (firstGlobalDup == null) firstGlobalDup = gidx;
                    }
                    else globalSeen.Add(gidx);
                }
            }

            var outputDir = Path.Combine(exeDir, "output");
            Directory.CreateDirectory(outputDir);
            var outPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(csvPath) + ".txt");


            using (var sw = new StreamWriter(outPath))
            {
                if (!string.IsNullOrEmpty(firstGlobalDup))
                    sw.WriteLine($"{globalIndexCol} 중복 INDEX: {firstGlobalDup}");
                foreach (var kv in sums)
                {
                    var err = new List<string>();
                    if (kv.Value != targetSum) err.Add("!!!!ERROR!!!!");
                    if (firstGroupDup.TryGetValue(kv.Key.grp, out var dupVal))
                        err.Add($"!!!!{indexCol} 중복 INDEX {dupVal}!!!!");
                    sw.WriteLine($"{groupCol} {kv.Key.grp}/  {subCol} {kv.Key.sub}서브그룹 -> {kv.Value} / {targetSum}"
                        + (err.Any() ? "\t" + string.Join(" ", err) : string.Empty));
                }
            }

            Console.WriteLine($"[output/{Path.GetFileName(outPath)}] 생성 (그룹+서브그룹):");
            foreach (var kv in sums)
            {
                var err = new List<string>();
                if (kv.Value != targetSum) err.Add("!!!!ERROR!!!!");
                if (firstGroupDup.TryGetValue(kv.Key.grp, out var dupVal))
                    err.Add($"!!!!DUP INDEX {dupVal}!!!!");
                Console.WriteLine($"  {kv.Key.grp}그룹 {kv.Key.sub}서브그룹 -> {kv.Value} / {targetSum}"
                    + (err.Any() ? "\t" + string.Join(" ", err) : string.Empty));
            }
            Console.WriteLine();
        }
    }
}


```
</details>

완성된 코드에서는 간단한 파일명 입력, 그룹선택, 전체적인 중복인덱스를 검사할 수 있는 기능을 추가했으며, 출력파일들을 보기 좋게 수정하였다.
<img width="1101" height="665" alt="image" src="https://github.com/user-attachments/assets/ad62fac3-e3d1-452b-a944-0832a07fd46f" />

### 3.자동 SVN 업데이트 툴
테스트 서버에서 문서파일 교체에 대해 자동으로 SVN업데이트, Data Convert, ServerStart가 가능하도록 bat파일을 만들어 보았다.

<details>
<summary>코드/펼치기</summary>

```ruby
::경로 저장
for %%I in ("%~dp0..") do set "PARENT=%%~fI"

::KillServer
start "KillServer"/b "%PARENT%/Run64\KillServer.bat" 
timeout -t 5 /nobreak

::이전 버전과 비교를 위한 MAP 폴더 백업 생성
set ORIGINAL_MAP=%PARENT%\Run64\_Data\MAP
set BACKUP_MAP=%PARENT%\Run64\MAP_backup
robocopy "%ORIGINAL_MAP%" "%BACKUP_MAP%" /E /COPY:DAT

::CleanUp
 CD C:\Program Files\TortoiseSVN\bin\
 START TortoiseProc.exe /command:cleanup /cleanup /noui /breaklocks /revert /fixtimestaps /vacuum /path:"%PARENT%/Run64\_Data\" /closeonend:0
 
 timeout -t 3 /nobreak
 
 
::CleanUp 상태의 현재 MAP 폴더 복사본 생성
set COMPARE_REVISION=%PARENT%\Run64\MAP_compare
robocopy "%ORIGINAL_MAP%" "%COMPARE_REVISION%" /E /COPY:DAT


::Update
START TortoiseProc.exe /command:update /path:"%PARENT%/Run64\_Data" /closeonend:1

timeout -t 5 /nobreak


::Update 내역 있는지 비교
robocopy %ORIGINAL_MAP% %COMPARE_REVISION% /L /NFL /NDL /NJH /NJS
if %errorlevel% equ 0 (
    echo MAP NO UPDATED.
	robocopy "%BACKUP_MAP%" "%ORIGINAL_MAP%" /E /COPY:DAT
	timeout -t 1 /nobreak
) else (
    echo MAP UPDATED. CONVERT TOOL EXCUTE!
	start /wait "DataConvert" "%PARENT%/Run64/DataConvertTool\DataConvertTool.exe" 
    echo CONVERT TOOL COMPLETE!
	timeout -t 1 /nobreak
)
::비교 폴더 삭제
rmdir /s /q "%BACKUP_MAP%"
rmdir /s /q "%COMPARE_REVISION%"

::실행
 start "" 	 cmd /c	"%PARENT%\Run64\Game\server_Game.exe"
 ::start "" 	 cmd /c	"%PARENT%\Run64\Game12\server_Game.exe"
 start "" 	 cmd /c	"%PARENT%\Run64\AccountDB\server_AccountDB.exe"
 start "" 	 cmd /c	"%PARENT%\Run64\CashDB\server_CashDB.exe"
 ::start "" 	 cmd /c	"%PARENT%\Run64\GameDB\server_GameDB.exe"
 ::start "" 	 cmd /c	"%PARENT%\Run64\GameDB12\server_GameDB.exe"
 start "" 	 cmd /c	"%PARENT%\Run64\Gate\server_Gate.exe"
 start "" 	 cmd /c	"%PARENT%\Run64\Login\server_Login.exe"
 start "" 	 cmd /c	"%PARENT%\Run64\TradeDB\server_TradeDB.exe"
 ::start "" 	 cmd /c	"%PARENT%\Run64\TradeDB10\server_TradeDB.exe"
 ::start "" 	 cmd /c	"%PARENT%\Run64\Game10(World)\server_Game.exe"
 ::start "" 	 cmd /c	"%PARENT%\Run64\WorldDB10\server_GameDB.exe"
 ::start "" 	 cmd /c	"%PARENT%\Run64\Union\server_Game.exe"
 ::start "" 	 cmd /c	"%PARENT%\Run64\UnionDB\server_GameDB.exe"
:: start "" 	 cmd /c	"%PARENT%\Run64\ServerMoveDB\server_ServerMoveDB.exe"

goto 1

exit /b


```

</details>
