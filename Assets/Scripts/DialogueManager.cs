using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    // 대화 데이터를 저장할 Dictionary 변수
    public Dictionary<int, string[]> dialogueData;

    // MonoBehaviour의 Awake() 메서드. 오브젝트가 활성화될 때 호출됩니다.
    void Awake()
    {
        // 대화 데이터를 저장할 Dictionary 초기화
        dialogueData = new Dictionary<int, string[]>();
        // 대화 데이터를 생성하는 메서드 호출
        GenerateData();
    }

    // 대화 데이터를 생성하는 메서드
    void GenerateData()
    {
        // ID가 0인 대화 데이터를 추가합니다.
        dialogueData.Add(0, new string[] { // 대사와 애니메이션 트리거
            "안녕~ 새싹 농장에 온 것을 환영해 ∩'ㅅ'∩\n다음 대화로 넘어가려면 Space 바를 눌러줘!♥:Loving",
            "새싹 농장에서 너만의 작은 농장을 가꿀 시간이야! 그런데 농장에는 몇 가지 규칙이 있어. 같이 살펴볼까?:Hooray1",
            "이 농장에서는 시간이 빠르게 흐르고 있어! 이 농장에서의 1시간은 현실 세계의 시간으로 계산하면......:MovingEars",
            "......$^%!%^&#@%&#&*(:MovingEars",
            "1분 12초야! 이곳에서의 시간은 금방 가니까 할 일이 있다면 서둘러야 해! >ㅅ< /:Hooray1",
            "그리고 9시간 동안 계속 일하면 체력이 고갈되어 죽을 수 있어!:Sleeping",
            "움직이거나 뛰면 체력이 더 빨리 닳으니까 상황에 따라 전략적으로 체력을 관리해야 해!:Sleeping",
            "체력이 부족하다면 곳곳에 열린 빨간 열매를 먹어봐! 집에 들어가 침대에서 푹 자면 체력이 가득 찰 거야.:Hooray2",
            "앗! 우리가 한 눈을 판 사이에 소와 닭들이 우리 밖으로 도망쳤어. ε=ε=(⊃≧□≦)⊃:Angry",
            "어서 하루가 다 가기 전에 도망친 소와 닭들을 잡아서 울타리에 집어 넣어야 해!!:Angry",
            "내가 작은 팁을 줄게~! 오른쪽 위를 보면 우리와 가장 가까운 동물이 있는 방향을 알 수 있어. 체력이 부족할 땐 집을 가리켜 줄게!:Sunglasses",
            "방향키로 움직일 수 있고 Shift 키로 달릴 수 있어－＝≡ヘ(*・ω・)ノ:Sunglasses",
            "그럼, 이제 시작해볼까? 화이팅! ⌒(o＾▽＾o)ノ:Hooray2"
        });

        // ID가 1인 대화 데이터를 추가합니다.
        dialogueData.Add(1, new string[] {
            "Space 바를 누르면 데려온 동물을 울타리 안에 넣을 수 있어!:Blinking"
        });

        dialogueData.Add(2, new string[] {
            "더는 동물을 데려갈 수 없어! 먼저 울타리에 넣어 줘.:Blinking"
        });
    }

    // 대화 데이터를 반환하는 메서드
    // id: 대화 데이터의 ID
    // dialogueIndex: 대화의 인덱스
    public string GetDialogue(int id, int dialogueIndex)
    {
        // 해당 ID의 대화 데이터가 존재하는지 확인합니다.
        if (dialogueData.ContainsKey(id))
        {
            // 인덱스 범위 내에 있는지 확인합니다.
            if (dialogueIndex < dialogueData[id].Length)
            {
                // 대화 데이터를 반환합니다.
                return dialogueData[id][dialogueIndex];
            }
        }
        // 해당 ID나 인덱스 범위에 존재하지 않으면 null을 반환합니다.
        return null;
    }
}
