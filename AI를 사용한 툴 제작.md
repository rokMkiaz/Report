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
테스트 서버에서 문서파일 교체에 대해 자동으로 SVN업데이트, Data Convert, ServerStart가 가능하도록 bat파일을 만들어 보았다. 해당 bat파일을 실행할 수 있도록 이름과 경로를 지정하였다.

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

::실행 - 경로 자동화 추가
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


### 4. Unit 추적 툴
게임서버 Unit들을 추적하고 해당 객체가 하는 행동을 추적하기 위한 툴. imgui를 활용하여 만들었다.
<img width="640" height="382" alt="image" src="https://github.com/user-attachments/assets/4c118645-7903-41ab-8dcd-917a44219389" />

<img width="640" height="382" alt="image" src="https://github.com/user-attachments/assets/65c96be9-f7eb-4c10-8712-9b489946e2ac" />

<details>
<summary>코드/펼치기</summary>
	
```ruby
#pragma once
#include "Singleton.h"
#include <WinSock2.h> // 반드시 포함! (winsock 라이브러리 1순위 충돌방지를 위해 먼저 포함 필요)
#include "imgui.h"
#include "imgui_internal.h"
#include "backends/imgui_impl_win32.h"
#include "backends/imgui_impl_dx11.h"
#include <d3d11.h>
#include <tchar.h>
#include <vector>
#include <process.h>

//======================================
// 게임 서버 GUI 매니저
// 용도: 맵 상태 시각화 / 서버 상태 모니터
// - 자체 스레드를 소유하므로 main() 메시지루프에 영향 없음
// - StartThread() / StopThread() 로만 제어
//======================================

class GuiManager : public CSingleton <GuiManager>
{
public:
	GuiManager() {}
	~GuiManager() {}

	// ─── 외부 인터페이스 (main에서 이것만 호출) ───
	void StartThread();     // GUI 스레드 시작
	void StopThread();      // GUI 스레드 종료 요청 및 대기

private:
	// ─── 내부 스레드 진입점 ───
	static UINT WINAPI  GuiThreadEntry(LPVOID pVoid);
	void                RunGui();   // Initialize → SetupImGui → MainLoop → Cleanup

	// 단계별 초기화/해제/루프 함수
	BOOL    Initialize();       // 1. 윈도우 및 DirectX 초기화
	void    SetupImGui();       // 2. ImGui 컨텍스트 및 폰트 초기화
	void    MainLoop();         // 3. 전용 메시지 루프 및 프레임 렌더
	void    RenderUI();         // 4. 탭별 UI 렌더링 (탭바, 창 등)

	// 탭별 렌더링 함수
	void    RenderTabMapInfo(); // 탭1: 맵 타일 시각화 및 유닛 위치 표시

	void    RenderFrame();      // 5. DirectX로 최종 프레임 제출
	void    Cleanup();          // 6. 자원 해제 및 정리

	// OS에 의해 호출되는 WndProc 함수
	static LRESULT WINAPI   WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
	// 실제 메시지 처리의 멤버 함수
	LRESULT                 RealWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

	BOOL CreateDeviceD3D(HWND hWnd);
	void CleanupDeviceD3D();
	void CreateRenderTarget();
	void CleanupRenderTarget();

private:
	// ─── 스레드 제어 ───
	HANDLE          _hThread    = nullptr;
	volatile BOOL   _bStop      = FALSE;    // 외부에서 종료 요청 시 TRUE

	// ─── DirectX 11 ───
	HWND                        _hWnd                   = nullptr;
	ID3D11Device*               _pd3dDevice             = nullptr;
	ID3D11DeviceContext*        _pd3dDeviceContext       = nullptr;
	IDXGISwapChain*             _pSwapChain             = nullptr;
	BOOL                        _bSwapChainOccluded     = FALSE;
	UINT                        _uiResizeWidth          = 0;
	UINT                        _uiResizeHeight         = 0;
	ID3D11RenderTargetView*     _pMainRenderTargetView  = nullptr;

	// ─── 렌더 설정 ───
	ImVec4  _vClearColor = ImVec4(0.15f, 0.15f, 0.15f, 1.00f);

	// ─── 맵 시각화 상태 ───
	INT32   _i32SelectedMapIndex    = 0;
	float   _fMapZoom               = 2.0f;
	ImVec2  _vMapOffset             = ImVec2(0.0f, 0.0f);  // 드래그 팬 오프셋

	// ─── 선택 상태 ───
	INT32   _i32SelectedDummyIndex  = -1;
	DWORD   _dwSelectedCharUnique   = 0;    // 클릭 선택된 캐릭터 Unique

	// ─── 테스트 관련 입력값 ───
	INT32   _i32TestCount           = 1;
	INT32   _i32TestStartID         = 101;
	INT32   _i32TestGroupID         = 11;

	// ─── 워프 입력값 ───
	INT32   _i32WarpMapNum          = 0;
	INT32   _i32WarpX               = 0;
	INT32   _i32WarpY               = 0;
};


```

```ruby

#include "StdAfx.h"
#include "GuiManager.h"
#include "DataManager.h"
#include "MapManager.h"
#include "GameManager.h"
#include "ConsoleCommands.h"
#include "UnitPC.h"
#include "UnitNPC.h"
#include <string>


//============================================================================================================
// Win32 / DirectX 헬퍼
//============================================================================================================
// Forward declare message handler from imgui_impl_win32.cpp
extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

LRESULT WINAPI GuiManager::WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    return GuiManager::GetInstance()->RealWndProc(hWnd, msg, wParam, lParam);
}

LRESULT GuiManager::RealWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
        return true;

    switch (msg)
    {
    case WM_SIZE:
        if (wParam == SIZE_MINIMIZED) return 0;
        _uiResizeWidth  = (UINT)LOWORD(lParam);
        _uiResizeHeight = (UINT)HIWORD(lParam);
        return 0;
    case WM_SYSCOMMAND:
        if ((wParam & 0xfff0) == SC_KEYMENU) return 0;
        break;
    case WM_DESTROY:
        ::PostQuitMessage(0);
        return 0;
    }
    return ::DefWindowProcA(hWnd, msg, wParam, lParam);
}

BOOL GuiManager::CreateDeviceD3D(HWND hWnd)
{
    DXGI_SWAP_CHAIN_DESC sd = {};
    sd.BufferCount        = 2;
    sd.BufferDesc.Format  = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.Flags              = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
    sd.BufferUsage        = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow       = hWnd;
    sd.SampleDesc.Count   = 1;
    sd.Windowed           = TRUE;
    sd.SwapEffect         = DXGI_SWAP_EFFECT_DISCARD;

    const D3D_FEATURE_LEVEL featureLevelArray[2] = { D3D_FEATURE_LEVEL_11_0, D3D_FEATURE_LEVEL_10_0 };
    D3D_FEATURE_LEVEL featureLevel;

    HRESULT res = D3D11CreateDeviceAndSwapChain(
        nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0,
        featureLevelArray, 2, D3D11_SDK_VERSION,
        &sd, &_pSwapChain, &_pd3dDevice, &featureLevel, &_pd3dDeviceContext);

    if (res == DXGI_ERROR_UNSUPPORTED)
        res = D3D11CreateDeviceAndSwapChain(
            nullptr, D3D_DRIVER_TYPE_WARP, nullptr, 0,
            featureLevelArray, 2, D3D11_SDK_VERSION,
            &sd, &_pSwapChain, &_pd3dDevice, &featureLevel, &_pd3dDeviceContext);

    if (res != S_OK) return FALSE;

    CreateRenderTarget();
    return TRUE;
}

void GuiManager::CleanupDeviceD3D()
{
    CleanupRenderTarget();
    if (_pSwapChain)        { _pSwapChain->Release();        _pSwapChain        = nullptr; }
    if (_pd3dDeviceContext) { _pd3dDeviceContext->Release(); _pd3dDeviceContext = nullptr; }
    if (_pd3dDevice)        { _pd3dDevice->Release();        _pd3dDevice        = nullptr; }
}

void GuiManager::CreateRenderTarget()
{
    ID3D11Texture2D* pBack = nullptr;
    _pSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBack));
    if (pBack)
    {
        _pd3dDevice->CreateRenderTargetView(pBack, nullptr, &_pMainRenderTargetView);
        pBack->Release();
    }
}

void GuiManager::CleanupRenderTarget()
{
    if (_pMainRenderTargetView) { _pMainRenderTargetView->Release(); _pMainRenderTargetView = nullptr; }
}

//============================================================================================================
// 초기화 / 메인 루프 / 정리
//============================================================================================================


//============================================================================================================
// 스레드 관리
//============================================================================================================

void GuiManager::StartThread()
{
    _bStop = FALSE;
    UINT dwId = 0;
    _hThread = (HANDLE)_beginthreadex(nullptr, 0, GuiThreadEntry, this, 0, &dwId);
    SetThreadPriority(_hThread, THREAD_PRIORITY_BELOW_NORMAL);
}

void GuiManager::StopThread()
{
    _bStop = TRUE;
    if (_hWnd)
        ::PostMessageA(_hWnd, WM_CLOSE, 0, 0);

    if (_hThread)
    {
        ::WaitForSingleObject(_hThread, 5000);
        ::CloseHandle(_hThread);
        _hThread = nullptr;
    }
}

UINT WINAPI GuiManager::GuiThreadEntry(LPVOID pVoid)
{
    reinterpret_cast<GuiManager*>(pVoid)->RunGui();
    return 0;
}

BOOL GuiManager::Initialize()
{
    ImGui_ImplWin32_EnableDpiAwareness();

    WNDCLASSEXA wc = { sizeof(wc), CS_CLASSDC, WndProc, 0L, 0L,
        GetModuleHandleA(nullptr), nullptr, nullptr, nullptr, nullptr, "Game Server GUI", nullptr };
    ::RegisterClassExA(&wc);

    _hWnd = ::CreateWindowA(wc.lpszClassName, "Game Server GUI",
        WS_OVERLAPPEDWINDOW, 100, 100, 1280, 800,
        nullptr, nullptr, wc.hInstance, nullptr);

    if (!CreateDeviceD3D(_hWnd))
    {
        CleanupDeviceD3D();
        ::UnregisterClassA(wc.lpszClassName, wc.hInstance);
        return FALSE;
    }

    ::ShowWindow(_hWnd, SW_SHOWDEFAULT);
    ::UpdateWindow(_hWnd);
    return TRUE;
}

void GuiManager::SetupImGui()
{
    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;
    ImGui::StyleColorsDark();

    ImGui_ImplWin32_Init(_hWnd);
    ImGui_ImplDX11_Init(_pd3dDevice, _pd3dDeviceContext);

    // 한국어 폰트 로드
    static const ImWchar ranges[] = {
        0x0020, 0x00FF,  // Basic Latin + Latin Supplement
        0x3131, 0x3163,  // 한글 자모
        0xAC00, 0xD7A3,  // 한글 음절
        0,
    };
    ImFontConfig cfg;
    cfg.OversampleH = 1; cfg.OversampleV = 1; cfg.PixelSnapH = true;

    const char* korFonts[] = {
        "C:/Windows/Fonts/malgun.ttf",
        "C:/Windows/Fonts/NanumGothic.ttf",
    };
    bool loaded = false;
    for (auto& path : korFonts)
    {
        if (GetFileAttributesA(path) != INVALID_FILE_ATTRIBUTES)
        {
            io.Fonts->AddFontFromFileTTF(path, 16.0f, &cfg, ranges);
            loaded = true;
            break;
        }
    }
    if (!loaded)
        io.Fonts->AddFontDefault();
}

void GuiManager::RunGui()
{
    Initialize();
    //if (!Initialize()) return -1;
    SetupImGui();
    MainLoop();
    Cleanup();
    //return 0;
}

void GuiManager::MainLoop()
{
    bool done = false;
    while (!done)
    {
        MSG msg;
        while (::PeekMessageA(&msg, nullptr, 0U, 0U, PM_REMOVE))
        {
            ::TranslateMessage(&msg);
            ::DispatchMessageA(&msg);
            if (msg.message == WM_QUIT) done = true;
        }
        if (done || _bStop) break;

        // 최소화 중이면 렌더링 생략하고 대기
        if (::IsIconic(_hWnd))
        {
            ::Sleep(100);
            continue;
        }

        // 창이 가려진 경우 대기
        if (_bSwapChainOccluded &&
            _pSwapChain->Present(0, DXGI_PRESENT_TEST) == DXGI_STATUS_OCCLUDED)
        {
            ::Sleep(10);
            continue;
        }
        _bSwapChainOccluded = FALSE;

        // 리사이즈 처리
        if (_uiResizeWidth != 0 && _uiResizeHeight != 0)
        {
            CleanupRenderTarget();
            _pSwapChain->ResizeBuffers(0, _uiResizeWidth, _uiResizeHeight, DXGI_FORMAT_UNKNOWN, 0);
            _uiResizeWidth = _uiResizeHeight = 0;
            CreateRenderTarget();
        }

        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();

        RenderUI();
        RenderFrame();
    }
}

void GuiManager::RenderFrame()
{
    ImGui::Render();
    const float clear[4] = { _vClearColor.x, _vClearColor.y, _vClearColor.z, _vClearColor.w };
    _pd3dDeviceContext->OMSetRenderTargets(1, &_pMainRenderTargetView, nullptr);
    _pd3dDeviceContext->ClearRenderTargetView(_pMainRenderTargetView, clear);
    ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
    HRESULT hr = _pSwapChain->Present(1, 0);
    _bSwapChainOccluded = (hr == DXGI_STATUS_OCCLUDED);
}

void GuiManager::Cleanup()
{
    ImGui_ImplDX11_Shutdown();
    ImGui_ImplWin32_Shutdown();
    ImGui::DestroyContext();
    CleanupDeviceD3D();
    ::DestroyWindow(_hWnd);
    ::UnregisterClassA("GameServerGUI", GetModuleHandleA(nullptr));
}

//============================================================================================================
// UI 렌더링
//============================================================================================================

void GuiManager::RenderUI()
{
    ImGuiIO& io = ImGui::GetIO();
    ImGui::SetNextWindowPos(ImVec2(0, 0));
    ImGui::SetNextWindowSize(io.DisplaySize);
    ImGui::Begin("##Main", nullptr,
        ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize |
        ImGuiWindowFlags_NoMove     | ImGuiWindowFlags_NoScrollbar |
        ImGuiWindowFlags_NoBringToFrontOnFocus);

    if (ImGui::BeginTabBar("##Tabs"))
    {
        if (ImGui::BeginTabItem(u8"맵 정보"))   { RenderTabMapInfo();      ImGui::EndTabItem(); }
 
        ImGui::EndTabBar();
    }
    ImGui::End();
}

//============================================================================================================
// 탭1 - 맵 정보 시각화
//============================================================================================================

void GuiManager::RenderTabMapInfo()
{
    auto* pMapInfos = g_DataManager.GetMapData();
    if (!pMapInfos || pMapInfos->empty())
    {
        ImGui::TextDisabled("No map data");
        return;
    }

    static std::vector<std::pair<WORD, CMapInfo*>> s_MapList;
    if (s_MapList.empty())
        for (auto& kv : *pMapInfos)
            s_MapList.emplace_back(kv.first, kv.second);

    // Map select combo
    {
        char preview[32] = "None";
        if (_i32SelectedMapIndex < (int)s_MapList.size())
            sprintf_s(preview, "Map %d", (int)s_MapList[_i32SelectedMapIndex].first);
        ImGui::SetNextItemWidth(160);
        if (ImGui::BeginCombo("Map", preview))
        {
            for (int i = 0; i < (int)s_MapList.size(); ++i)
            {
                char label[32];
                sprintf_s(label, "Map %d", (int)s_MapList[i].first);
                if (ImGui::Selectable(label, _i32SelectedMapIndex == i))
                {
                    _i32SelectedMapIndex  = i;
                    _vMapOffset           = ImVec2(0.0f, 0.0f);
                    _dwSelectedCharUnique = 0;
                }
            }
            ImGui::EndCombo();
        }
    }
    ImGui::SameLine();
    ImGui::SetNextItemWidth(150);
    float prevZoom = _fMapZoom;
    ImGui::SliderFloat("Zoom", &_fMapZoom, 0.5f, 8.0f, "%.1f");
    ImGui::SameLine();
    if (ImGui::SmallButton("Reset"))
        _vMapOffset = ImVec2(0.0f, 0.0f);

    if (_i32SelectedMapIndex >= (int)s_MapList.size()) return;
    CMapInfo* pMapInfo = s_MapList[_i32SelectedMapIndex].second;
    CGameMap* pMap     = g_MapManager.FindMapIndex(s_MapList[_i32SelectedMapIndex].first);
    if (!pMap || !pMapInfo || pMapInfo->_i32MaxSize <= 0) return;

    const int   mapSize  = pMapInfo->_i32MaxSize;
    const float tileSize = _fMapZoom;

    // Unit position collection 
    struct UserPos { int x, y; DWORD dwAccunique;  DWORD charUnique; int level; };
    struct NpcPos  { int x, y; };
    static std::vector<UserPos> s_UserPositions;
    static std::vector<NpcPos>  s_NpcPositions;
  
    // 맵 데이터는 1초에 1회만 갱신, 렌더링은 매 프레임 유지
    static double s_LastUpdateTime = 0.0;
    const  double now              = ImGui::GetTime();
    if (now - s_LastUpdateTime >= 1.0 &&
        TryEnterCriticalSection(&pMap->m_MonitToolLock))
    {
        s_LastUpdateTime = now;
        s_UserPositions.clear();
        s_NpcPositions.clear();

        for (const auto& iter : pMap->m_UnitPos)
        {
            CUnit* pUnit = iter.second;
            if (!pUnit) continue;                           

            if (pUnit->GetUnitTYPE() == eUnitType::PC)
            {
                CUnitPC* pPC = static_cast<CUnitPC*>(pUnit);
                if (!pPC) continue;                       

                UserPos up = {};
                up.x = pPC->m_X;
                up.y = pPC->m_Y;
                up.charUnique = pPC->GetCharacterUnique();
                up.level = pPC->GetLevel();
                up.dwAccunique = pPC->GetAccountUnique();
                s_UserPositions.push_back(up);
            }
            else if (pUnit->GetUnitTYPE() == eUnitType::NPC)
                s_NpcPositions.push_back({ pUnit->m_X, pUnit->m_Y });
        }
        LeaveCriticalSection(&pMap->m_MonitToolLock);
    }

    // Canvas
    ImVec2 canvasPos  = ImGui::GetCursorScreenPos();
    ImVec2 canvasSize = ImGui::GetContentRegionAvail();
    canvasSize.y -= 24.0f;
    if (canvasSize.x < 10 || canvasSize.y < 10) return;

    const float totalMapW = mapSize * tileSize;
    const float totalMapH = mapSize * tileSize;
    if (prevZoom != _fMapZoom)
    {
        _vMapOffset.x = ImClamp(_vMapOffset.x, ImMin(-(totalMapW - canvasSize.x), 0.0f), 0.0f);
        _vMapOffset.y = ImClamp(_vMapOffset.y, ImMin(-(totalMapH - canvasSize.y), 0.0f), 0.0f);
    }

    ImDrawList* dl = ImGui::GetWindowDrawList();
    dl->AddRectFilled(canvasPos,
        ImVec2(canvasPos.x + canvasSize.x, canvasPos.y + canvasSize.y),
        IM_COL32(25, 25, 25, 255));
    dl->AddRect(canvasPos,
        ImVec2(canvasPos.x + canvasSize.x, canvasPos.y + canvasSize.y),
        IM_COL32(80, 80, 80, 255));
    dl->PushClipRect(canvasPos,
        ImVec2(canvasPos.x + canvasSize.x, canvasPos.y + canvasSize.y), true);

    if (!pMapInfo->m_MapData.tileInfo)
    {
        dl->PopClipRect();
        ImGui::InvisibleButton("##mapcanvas", canvasSize);
        ImGui::TextDisabled("tileInfo not loaded");
        return;
    }

    // Visible tile range (performance optimization)
    const int startX = ImMax(0, (int)(-_vMapOffset.x / tileSize));
    const int startY = ImMax(0, (int)(-_vMapOffset.y / tileSize));
    const int endX   = ImMin(mapSize, startX + (int)(canvasSize.x / tileSize) + 2);
    const int endY   = ImMin(mapSize, startY + (int)(canvasSize.y / tileSize) + 2);

    // Tile rendering
    for (int y = startY; y < endY; ++y)
        for (int x = startX; x < endX; ++x)
        {
            if (!pMapInfo->m_MapData.tileInfo[x]) continue;
            const int tileType = pMapInfo->m_MapData.tileInfo[x][y].type;
            if (tileType == MAP_TILE_TYPE::MAP_TILE_NOMAL) continue;
            ImU32 col = IM_COL32(160, 50, 50, 210);
            if (tileType >= MAP_TILE_TYPE::MAP_TILE_INWARP_0 &&
                tileType <= MAP_TILE_TYPE::MAP_TILE_OUTWARP_9)
                col = IM_COL32(80, 120, 220, 200);
            else if (tileType == MAP_TILE_TYPE::MAP_TILE_PVP ||
                     tileType == MAP_TILE_TYPE::MAP_TILE_CASTLE_PVP)
                col = IM_COL32(220, 160, 40, 200);
            const float px = canvasPos.x + x * tileSize + _vMapOffset.x;
            const float py = canvasPos.y + y * tileSize + _vMapOffset.y;
            dl->AddRectFilled(ImVec2(px, py), ImVec2(px + tileSize, py + tileSize), col);
        }

    // NPC dots (grey)
    const float dotR = ImMax(2.0f, tileSize * 0.55f);
    for (auto& dp : s_NpcPositions)
    {
        const float px = canvasPos.x + dp.x * tileSize + _vMapOffset.x;
        const float py = canvasPos.y + dp.y * tileSize + _vMapOffset.y;
        dl->AddCircleFilled(ImVec2(px, py), dotR, IM_COL32(200, 200, 200, 180));
    }

    // Player dots (green) + selected highlight ring
    for (auto& up : s_UserPositions)
    {
        const float px = canvasPos.x + up.x * tileSize + _vMapOffset.x;
        const float py = canvasPos.y + up.y * tileSize + _vMapOffset.y;
        dl->AddCircleFilled(ImVec2(px, py), dotR, IM_COL32(60, 220, 60, 230));
        if (up.charUnique == _dwSelectedCharUnique)
            dl->AddCircle(ImVec2(px, py), dotR + 3.0f, IM_COL32(255, 220, 0, 255), 0, 2.0f);
    }
    dl->PopClipRect();

    // Canvas input
    ImGui::InvisibleButton("##mapcanvas", canvasSize);

    if (ImGui::IsItemHovered())
        ImGui::SetMouseCursor(ImGuiMouseCursor_Hand);

    // Drag pan
    if (ImGui::IsItemActive() && ImGui::IsMouseDragging(ImGuiMouseButton_Left, 0.0f))
    {
        ImGuiIO& io = ImGui::GetIO();
        _vMapOffset.x += io.MouseDelta.x;
        _vMapOffset.y += io.MouseDelta.y;
    }

    // Click: select nearest player dot
    if (ImGui::IsItemClicked(ImGuiMouseButton_Left))
    {
        ImVec2 mousePos = ImGui::GetMousePos();
        float  bestDist = dotR * 4.0f;
        DWORD  bestUniq = 0;
        for (auto& up : s_UserPositions)
        {
            const float px = canvasPos.x + up.x * tileSize + _vMapOffset.x;
            const float py = canvasPos.y + up.y * tileSize + _vMapOffset.y;
            const float dx = mousePos.x - px;
            const float dy = mousePos.y - py;
            const float dist = sqrtf(dx * dx + dy * dy);
            if (dist < bestDist) { bestDist = dist; bestUniq = up.charUnique; }
        }
        _dwSelectedCharUnique = bestUniq;
    }

    // Offset clamp
    if (totalMapW > canvasSize.x)
        _vMapOffset.x = ImClamp(_vMapOffset.x, -(totalMapW - canvasSize.x), 0.0f);
    else _vMapOffset.x = 0.0f;
    if (totalMapH > canvasSize.y)
        _vMapOffset.y = ImClamp(_vMapOffset.y, -(totalMapH - canvasSize.y), 0.0f);
    else _vMapOffset.y = 0.0f;

    // Status bar
    const int viewX = (int)(-_vMapOffset.x / tileSize);
    const int viewY = (int)(-_vMapOffset.y / tileSize);
    ImGui::Text("Map User: %d | Total User : %d  |  NPC: %d  |  ",
        
        (int)s_UserPositions.size(),g_GameManager.mUserManager.m_pUsers.size(), (int)s_NpcPositions.size());

    // ── Character info popup (ForegroundDrawList = always on top) ────
    if (_dwSelectedCharUnique != 0)
    {
        const UserPos* pSel = nullptr;
        for (auto& up : s_UserPositions)
            if (up.charUnique == _dwSelectedCharUnique) { pSel = &up; break; }

        if (pSel)
        {
            const float popW  = 200.0f;
            const float popH  =  105.0f;
            const float dotPx = canvasPos.x + pSel->x * tileSize + _vMapOffset.x;
            const float dotPy = canvasPos.y + pSel->y * tileSize + _vMapOffset.y;
            float popX = dotPx + dotR + 8.0f;
            float popY = dotPy - popH * 0.5f;
            popX = ImClamp(popX, canvasPos.x + 4.0f, canvasPos.x + canvasSize.x - popW - 4.0f);
            popY = ImClamp(popY, canvasPos.y + 4.0f, canvasPos.y + canvasSize.y - popH - 4.0f);

            ImDrawList* fg = ImGui::GetForegroundDrawList();
            ImFont*     ft = ImGui::GetFont();
            const float fs = ImGui::GetFontSize();

            // Background + border
            fg->AddRectFilled(ImVec2(popX, popY), ImVec2(popX + popW, popY + popH),
                IM_COL32(28, 28, 28, 240), 4.0f);
            fg->AddRect(ImVec2(popX, popY), ImVec2(popX + popW, popY + popH),
                IM_COL32(80, 200, 80, 220), 4.0f);

            // Connector line: dot -> popup
            fg->AddLine(ImVec2(dotPx, dotPy),
                ImVec2(popX, popY + popH * 0.5f),
                IM_COL32(255, 220, 0, 100));

            // Title
            float ty = popY + 6.0f;
            fg->AddText(ft, fs, ImVec2(popX + 8.0f, ty),
                IM_COL32(80, 220, 80, 255), "[ Char Info ]");
            ty += fs + 2.0f;

            // Separator
            fg->AddLine(ImVec2(popX + 4.0f, ty), ImVec2(popX + popW - 4.0f, ty),
                IM_COL32(70, 70, 70, 220));
            ty += 4.0f;

            // Content rows
            char buf[64];
            sprintf_s(buf, "Acc : %u", pSel->dwAccunique);
            fg->AddText(ft, fs, ImVec2(popX + 8.0f, ty), IM_COL32(220, 220, 220, 255), buf);
            ty += fs + 2.0f;

            sprintf_s(buf, "CharUnique : %u", pSel->charUnique);
            fg->AddText(ft, fs, ImVec2(popX + 8.0f, ty), IM_COL32(220, 220, 220, 255), buf);
            ty += fs + 2.0f;

            sprintf_s(buf, "Level  : %d", pSel->level);
            fg->AddText(ft, fs, ImVec2(popX + 8.0f, ty), IM_COL32(220, 220, 220, 255), buf);
            ty += fs + 2.0f;

            sprintf_s(buf, "Pos    : (%d, %d)", pSel->x, pSel->y);
            fg->AddText(ft, fs, ImVec2(popX + 8.0f, ty), IM_COL32(220, 220, 220, 255), buf);

            // X close button
            const float bx = popX + popW - 20.0f;
            const float by = popY + 5.0f;
            const float bw = 15.0f, bh = 15.0f;
            const bool  hov = ImGui::IsMouseHoveringRect(
                ImVec2(bx, by), ImVec2(bx + bw, by + bh));
            fg->AddRectFilled(ImVec2(bx, by), ImVec2(bx + bw, by + bh),
                hov ? IM_COL32(200, 60, 60, 230) : IM_COL32(100, 40, 40, 200), 2.0f);
            fg->AddText(ft, fs, ImVec2(bx + 3.0f, by + 1.0f),
                IM_COL32(255, 255, 255, 255), "X");
            if (hov && ImGui::IsMouseClicked(ImGuiMouseButton_Left))
                _dwSelectedCharUnique = 0;
        }
        else { _dwSelectedCharUnique = 0; }
    }
}


```
</details>
- Normal Map 유저 분포 확인 기능 및 1인 입장 가능한 Instance Map 역시 활성화된 맵들을 추적할 수 있게되었다.
