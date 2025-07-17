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
                tplLines = File.ReadAllLines(templatePath);
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

                // CSV 파일이므로 탭으로 분리하여 6개 항목 읽기
                var parts = line.Split('\t')
                                .Select(p => p.Trim())
                                .ToArray();

                if (parts.Length != 6)
                {
                    Console.WriteLine($"[파싱 오류] 6개 항목 필요(CSV 구분): \"{line}\"");
                    continue;
                }

                var csvRel = parts[0];
                var groupCol = parts[1];
                var subCol = parts[2];
                var probCol = parts[3];
                if (!double.TryParse(parts[4], out var targetSum))
                {
                    Console.WriteLine($"[파싱 오류] 최대확률 숫자 변환 실패: \"{parts[4]}\"");
                    continue;
                }
                var indexCol = parts[5];  // 중복 인덱스 검사 컬럼명 ("{없음}"일 경우 스킵)

                var csvPath = Path.Combine(exeDir, csvRel);
                if (!File.Exists(csvPath))
                {
                    Console.WriteLine($"[파일 없음] {csvPath}");
                    continue;
                }

                if (string.IsNullOrEmpty(subCol))
                    ProcessFile_GroupOnly(exeDir, csvPath, groupCol, probCol, targetSum, indexCol);
                else
                    ProcessFile_GroupAndSub(exeDir, csvPath, groupCol, subCol, probCol, targetSum, indexCol);
            }

            Console.WriteLine("모든 파일 처리 완료.");
        }

        static void ProcessFile_GroupOnly(string exeDir, string csvPath, string groupCol, string probCol, double targetSum, string indexCol)
        {
            var lines = File.ReadAllLines(csvPath);
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
            int ii = -1;
            bool doIndexCheck = indexCol != "{없음}";
            if (doIndexCheck)
            {
                ii = Array.IndexOf(headers, indexCol);
                if (ii < 0)
                {
                    Console.WriteLine($"[{Path.GetFileName(csvPath)}] 인덱스 컬럼 못 찾음: {indexCol}");
                    return;
                }
            }

            var sums = new Dictionary<string, double>();
            var groupIndices = new Dictionary<string, HashSet<string>>();
            var firstDup = new Dictionary<string, string>();

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

                if (doIndexCheck && cols.Length > ii)
                {
                    var idx = cols[ii];
                    if (!groupIndices.ContainsKey(g))
                        groupIndices[g] = new HashSet<string>();
                    if (groupIndices[g].Contains(idx))
                    {
                        if (!firstDup.ContainsKey(g))
                            firstDup[g] = idx;
                    }
                    else groupIndices[g].Add(idx);
                }
            }

            var outputDir = Path.Combine(exeDir, "output");
            Directory.CreateDirectory(outputDir);
            var outPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(csvPath) + ".txt");

            using (var sw = new StreamWriter(outPath))
            {
                foreach (var kv in sums)
                {
                    var err = new List<string>();
                    if (kv.Value != targetSum) err.Add("!!!!ERROR!!!!");
                    if (firstDup.TryGetValue(kv.Key, out var dupVal))
                        err.Add($"!!!!DUP INDEX {dupVal}!!!!");
                    sw.WriteLine($"{kv.Key}그룹 -> {kv.Value} / {targetSum}"
                        + (err.Any() ? "\t" + string.Join(" ", err) : string.Empty));
                }
            }

            Console.WriteLine($"[output/{Path.GetFileName(outPath)}] 생성 (그룹만):");
            foreach (var kv in sums)
            {
                var err = new List<string>();
                if (kv.Value != targetSum) err.Add("!!!!ERROR!!!!");
                if (firstDup.TryGetValue(kv.Key, out var dupVal))
                    err.Add($"!!!!DUP INDEX {dupVal}!!!!");
                Console.WriteLine($"  {kv.Key}그룹 -> {kv.Value} / {targetSum}"
                    + (err.Any() ? "\t" + string.Join(" ", err) : string.Empty));
            }
            Console.WriteLine();
        }

        static void ProcessFile_GroupAndSub(string exeDir, string csvPath, string groupCol, string subCol, string probCol, double targetSum, string indexCol)
        {
            var lines = File.ReadAllLines(csvPath);
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
            int ii = -1;
            bool doIndexCheck = indexCol != "{없음}";
            if (doIndexCheck)
            {
                ii = Array.IndexOf(headers, indexCol);
                if (ii < 0)
                {
                    Console.WriteLine($"[{Path.GetFileName(csvPath)}] 인덱스 컬럼 못 찾음: {indexCol}");
                    return;
                }
            }

            var sums = new Dictionary<(string grp, string sub), double>();
            var groupIndices = new Dictionary<string, HashSet<string>>();
            var firstDup = new Dictionary<string, string>();

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

                if (doIndexCheck && cols.Length > ii)
                {
                    var idx = cols[ii];
                    if (!groupIndices.ContainsKey(g))
                        groupIndices[g] = new HashSet<string>();
                    if (groupIndices[g].Contains(idx))
                    {
                        if (!firstDup.ContainsKey(g))
                            firstDup[g] = idx;
                    }
                    else groupIndices[g].Add(idx);
                }
            }

            var outputDir = Path.Combine(exeDir, "output");
            Directory.CreateDirectory(outputDir);
            var outPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(csvPath) + ".txt");

            using (var sw = new StreamWriter(outPath))
            {
                foreach (var kv in sums)
                {
                    var err = new List<string>();
                    if (kv.Value != targetSum) err.Add("!!!!ERROR!!!!");
                    if (firstDup.TryGetValue(kv.Key.grp, out var dupVal))
                        err.Add($"!!!!DUP INDEX {dupVal}!!!!");
                    sw.WriteLine($"{kv.Key.grp}그룹 {kv.Key.sub}서브그룹 -> {kv.Value} / {targetSum}"
                        + (err.Any() ? "\t" + string.Join(" ", err) : string.Empty));
                }
            }

            Console.WriteLine($"[output/{Path.GetFileName(outPath)}] 생성 (그룹+서브그룹):");
            foreach (var kv in sums)
            {
                var err = new List<string>();
                if (kv.Value != targetSum) err.Add("!!!!ERROR!!!!");
                if (firstDup.TryGetValue(kv.Key.grp, out var dupVal))
                    err.Add($"!!!!DUP INDEX {dupVal}!!!!");
                Console.WriteLine($"  {kv.Key.grp}그룹 {kv.Key.sub}서브그룹 -> {kv.Value} / {targetSum}"
                    + (err.Any() ? "\t" + string.Join(" ", err) : string.Empty));
            }
            Console.WriteLine();
        }
    }
}
