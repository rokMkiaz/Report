using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/*
    수정전 읽어 볼 사항 
    ParseTemplate <- 문서 파싱
    ApplyPortRules <- 적정 포트 수정
    WriteConfigFile <- 출력
    
    간단하게 추가되는 포트면 그냥 그룹X 하고 Port를 따로 입력해도 만들어짐.
    헤더 파일 때문에 대소문자 구분 넣을꺼면 따로 하시고..

    개선 필요한 부분
    -COMMON_HEADER를 통해 주소비교하기?
 */

namespace ConnnectTool2
{
    class Program
    {
        static void Main(string[] args)
        {
            const string templateFile = "config_template.txt";
            if (!File.Exists(templateFile))
            {
                GenerateTemplate(templateFile);
                Console.WriteLine($"Template '{templateFile}' created. Please customize ports as needed and re-run.");
                return;
            }
            List<string> commonHeader;
            var configs = ParseTemplate(templateFile,out commonHeader);
            var servers = configs.ToDictionary(c => c.Name);

            ApplyPortRules(servers);
            AssignSoc(servers);

            foreach (var cfg in servers.Values)
            {
                var outputFile = $"{cfg.Name}.txt";
                WriteConfigFile(cfg, outputFile, commonHeader);
                Console.WriteLine($"Generated '{outputFile}'");
            }
        }

        // 포트 규칙 적용 (ACCOUNT⇔GAME/GAMEDB, GAME⇔GATE, GAME⇔GAMEDB)
        static void ApplyPortRules(Dictionary<string, ServerConfig> servers)
        {
            foreach (var cfg in servers.Values)
            {
                var srcName = cfg.Name;
                var srcType = Regex.Replace(srcName, @"\d+$", "");



                foreach (var conn in cfg.Connections)
                {
                    if (conn.Port != 0) continue; // 수동 지정 우선

                    var dstName = conn.ConnectIP.Split(':')[0];
                    var dstType = Regex.Replace(dstName, @"\d+$", "");

                    if(srcName == dstName)
                    {
                        dstName = conn.LocalIP.Split(':')[0];
                        dstType = Regex.Replace(dstName, @"\d+$", "");
                    }


                    int port = 0;

                    // 1) ACCOUNT ⇔ GAME/GAMEDB
                    if ((srcType == "ACCOUNT" && (dstType == "GAME")) ||
                        ((srcType == "GAME" ) && dstType == "ACCOUNT"))
                    {
                        port = 25000 + conn.GroupNumber;
                    }
                    // 2) GAME ⇔ GATE
                    else if ((srcType == "GAME" && dstType == "GATE") || (srcType == "GATE" && dstType == "GAME"))
                    {
                        int gateNum = srcType == "GATE"
                            ? int.Parse(Regex.Match(srcName, @"\d+").Value)
                            : int.Parse(Regex.Match(dstName, @"\d+").Value);
                        port = 10000 + gateNum * 100 + conn.GroupNumber;
                    }
                    // 3) GAME ⇔ GAMEDB
                    else if ((srcType == "GAME" && dstType == "GAMEDB") || (srcType == "GAMEDB" && dstType == "GAME"))
                    {
                        port = 30300 + conn.GroupNumber;
                    }
                    // 3) LOGIN ⇔ GAMEDB
                    else if ((srcType == "LOGIN" && dstType == "GAMEDB") || (srcType == "GAMEDB" && dstType == "LOGIN"))
                    {
                        port = 22000 + conn.GroupNumber;
                    }
                    // 3) LOGIN ⇔ GAME
                    else if ((srcType == "LOGIN" && dstType == "GAME") || (srcType == "GAME" && dstType == "LOGIN"))
                    {
                        port = 21000 + conn.GroupNumber;
                    }
                    // 3) TRADEDB ⇔ GAME
                    else if ((srcType == "TRADEDB" && dstType == "GAME") || (srcType == "GAME" && dstType == "TRADEDB"))
                    {
                        port = 25100 + conn.GroupNumber;
                    }
                    // 3) GAME ⇔ GAME 중복이 들어오는건 월드밖에 없다.
                    else if ((srcType == "GAME" && dstType == "GAME") )
                    {
                        port = 30000 + conn.GroupNumber;
                    }
                    // 3) UNION ⇔ GAME
                    else if ((srcType == "UNION" && dstType == "GAME") || (srcType == "GAME" && dstType == "UNION"))
                    {
                        port = 40100 + conn.GroupNumber;
                    }

                    if (port > 0)
                    {
                        conn.Port = port;
                    }
                }
            }
        }

        // Soc 순차 배정
        static void AssignSoc(Dictionary<string, ServerConfig> servers)
        {
            //var counters = new Dictionary<string, int>();
     
            foreach (var cfg in servers.Values.OrderBy(c => c.Name))
            {
                var type = Regex.Replace(cfg.Name, @"\d+$", "");
               // if (!counters.ContainsKey(type)) counters[type] = 0;
                int i = 0;
                foreach (var conn in cfg.Connections)
                {
                    conn.Soc = i++;

                }
            }
        }

        // 템플릿 자동 생성
        static void GenerateTemplate(string path)
        {
            var lines = new List<string>
            {
                "; 서버 설정 템플릿 ",
                "; 0) COMMON_HEADER 섹션: 이 아래에 적은 줄을 모든 파일 상단에 붙입니다.",
                "COMMON_HEADER",
                "@   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                ";	NAME	PUBLIC		LOCAL		EXPLAIN",
                "SERVER  LOGIN   127.0.0.1  127.0.0.1    Update, Login",
                "SERVER  LOGIN   127.0.0.1  127.0.0.1    Update, Login",
                "",
                ";상단과 띄워줘야함",
                "; 1) 서버 목록 지정: SERVER_LIST 섹션에 Type:인덱스리스트(콤마 및 범위 허용) 입력",
                ";    예시: ACCOUNT:1   GAME:11,12,13,21,22   GAMEDB:10-12   GATE:1-3",
                "SERVER_LIST",
                "ACCOUNT:1",
                "GAME:11,12,13,21,22",
                "GAMEDB:10-12",
                "GATE:1-3",
                "",
                "; 2) 서버별 연결 설정 블록: 아래 $SERVER:<Name> 블록을 복제하여 각 서버 이름으로 작성",
                "; - 기본 규칙(GroupNumber)을 사용하려면 GroupNumber만 입력(100 이하 숫자)",
                "; -직접 포트를 지정하려면 GroupNumber 대신 'X'를 쓰고 Port에 숫자 입력",
                "; -서버이름과 연결할 서버이름을 비교하므로, COMMON_HEADER에 선언된 이름과 맞추는게 좋음",
                "$SERVER:SERVER_NAME",
                "; LisOrConn Soc Comment LocalIP ConnectIP [GroupNumber] Port RecvBuf SendBuf ReadQ SendQueue ",
                "1 0 TOOL1 SERVER_NAME:LOCAL OTHER:LOCAL 5 1024 1024 0 0",
                "1 1 TOOL2 SERVER_NAME:LOCAL OTHER:LOCAL X 65000 160000 160000 60000000 60000000",
                "",
            };
            File.WriteAllLines(path, lines);
        }

        // 템플릿 파싱
        static List<ServerConfig> ParseTemplate(string path ,out List<string> commonHeader)
        {
            var lines = File.ReadAllLines(path);
            var configs = new List<ServerConfig>();

            commonHeader = new List<string>();
            int idx = 0;

            // COMMON_HEADER 읽기
            idx = Array.IndexOf(lines, "COMMON_HEADER");
            
            while (idx < lines.Length && !string.IsNullOrWhiteSpace(lines[idx]))
            {
                idx++;
                commonHeader.Add(lines[idx]);
            }
            

            idx = Array.IndexOf(lines, "SERVER_LIST");
            var serverNames = new List<string>();

            if (idx >= 0)
            {
                for (int i = idx + 1; i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]); i++)
                {
                    var raw = lines[i].Trim();
                    if (raw.StartsWith(";")) continue;
                    var parts = raw.Split(':');
                    if (parts.Length != 2) continue;
                    var type = parts[0];
                    foreach (var seg in parts[1].Split(','))
                    {
                        if (seg.Contains("-"))
                        {
                            var range = seg.Split('-').Select(int.Parse).ToArray();
                            for (int v = range[0]; v <= range[1]; v++)
                                serverNames.Add(type + (type == "ACCOUNT" ? "" : v.ToString("D2")));
                        }
                        else if (int.TryParse(seg, out var v))
                        {
                            serverNames.Add(type + (type == "ACCOUNT" ? "" : v.ToString("D2")));
                        }
                    }
                }
            }

            ServerConfig current = null;
            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i].Trim();
                if (!raw.StartsWith("$SERVER:")) continue;
                if (current != null) configs.Add(current);
                var name = raw.Substring(8).Trim();
                current = new ServerConfig { Name = name };

                for (int j = i + 1; j < lines.Length; j++)
                {
                    var line = lines[j].Trim();
                    if (string.IsNullOrEmpty(line)) break;
                    if (line.StartsWith(";") || line.StartsWith("$SERVER:")) continue;

                    var parts = Regex.Split(line, @"\s+");
                    if (parts.Length < 10) continue;

                    bool custom = parts[5] == "X";
                    int group;
                    if (!custom)
                    {
                        group = int.Parse(parts[5]);
                        if (group < 0 || group > 100)
                            throw new ArgumentOutOfRangeException($"GroupNumber must be between 0 and 100: {group}");
                    }
                    else
                    {
                        group = 0;
                    }

                    int port = custom ? int.Parse(parts[6]) : 0;
                    int recv = custom ? int.Parse(parts[7]) : int.Parse(parts[6]);
                    int send = custom ? int.Parse(parts[8]) : int.Parse(parts[7]);
                    int rq = custom ? int.Parse(parts[9]) : int.Parse(parts[8]);
                    int sq = custom ? int.Parse(parts[10]) : int.Parse(parts[9]);

                    current.Connections.Add(new Connection
                    {
                        LisOrConn = int.Parse(parts[0]),
                        Soc = int.Parse(parts[1]),
                        Comment = parts[2],
                        LocalIP = parts[3],
                        ConnectIP = parts[4],
                        GroupNumber = group,
                        Port = port,
                        RecvBuffer = recv,
                        SendBuffer = send,
                        ReadQueueBuffer = rq,
                        SendQueueBuffer = sq,
                    });
                }
            }
            if (current != null) configs.Add(current);
            return configs;
        }

        // 결과 파일 작성
        static void WriteConfigFile(ServerConfig cfg, string path, List<string> commonHeader)
        {
            var lines = new List<string>
            {
                $"$\t{cfg.Name}\n"
            };

            // 2) 공통 헤더 삽입
            lines.AddRange(commonHeader);

            // 3) 구분선 및 컬럼 설명
            lines.Add(";---------------------------------------------------------------------------------------------------------------------------------------------------------");
            lines.Add(";Lis or conn\tSoc#\t\tComments\tLocalIP\t\tConnectIP\tPort\tRecvBuffer\tSendBuffer\tReadQueueBuffer\tSendQueueBuffer");
            lines.Add(";---------------------------------------------------------------------------------------------------------------------------------------------------------");


            lines.AddRange(cfg.Connections.Select(c =>
                $"{c.LisOrConn}\t{c.Soc}\t{c.Comment}\t{c.LocalIP}\t{c.ConnectIP}\t{c.Port}\t{c.RecvBuffer}\t{c.SendBuffer}\t{c.ReadQueueBuffer}\t{c.SendQueueBuffer}"));
            File.WriteAllLines(path, lines);
        }
    }


}
