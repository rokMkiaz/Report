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
/'''
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
'''
  
<!-- summary 아래 한칸 공백 두어야함 -->
## 접은 제목
접은 내용
</details>
출처: https://young-cow.tistory.com/44 [어린소:티스토리]
<img width="541" height="648" alt="image" src="https://github.com/user-attachments/assets/3c257908-6d55-40cf-a358-164cc8117381" />

2. 추가적으로 수정이 필요한 질문에도 꼭 문항을 나누어 정확하게 지적을 하였더니 내가 목적으로 하는 툴에 가까워 졌다.
3. 코드 완성 후 개별적인 튜닝을 통해 완성도를 높혔다.
[링크](https://github.com/rokMkiaz/GunBooster_TeamRocket_iwnam_summary/blob/main/Coroutine.md)
4. 커넥트 파일의 제목을 입력하는 기능을 추가하였다.

### 2.확률 검사 툴
기획자들의 실수를 줄여줄 툴을 만들었다. 실제로 액셀에 있는 기능이지만, 여전히 .csv만 다뤄 힘든 팀원들을 위하여 만들어주었다.
