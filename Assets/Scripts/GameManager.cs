using System.Collections.Generic;
using Pathfinding;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Player player;
    public PoolManager pool;
    public DialogueManager dialogueManager;
    public int dialogueIndex;

    public TypeEffect typeEffect;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    private int currentDialogueId = 0;
    private int currentDialogueIndex = 0;

    public Animator animator;
    public GameObject scanObject;
    public bool isAction;
    private bool isInitialDialogue = true;
    public DayNightCycle dayNightCycle;
    public AudioClip greetingSound;
    private AudioSource greetingAudioSource;

    public RectTransform uiElement;

    // 튜토리얼에서 동물들이 울타리를 뛰쳐나가 흩어지는 범위
    public Rect scatterArea = new Rect(-18f, -18f, 36f, 36f); // 맵 안쪽 (가장자리 조금 안)
    public float minScatterDistance = 8f;  // 울타리 가운데에서 적어도 이만큼 떨어진 곳으로
    public float maxScatterDistance = 18f;
    public float minScatterSpacing = 3f;   // 동물끼리 이만큼은 떨어진 곳으로
    public float maxEscapeDelay = 1.5f;    // 동물마다 출발을 0~이 시간 사이로 엇갈린다

    private void Awake()
    {
        instance = this;
        GameResult.Reset();
        WebBridge.CaptureKeyboard();
        greetingAudioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        ShowDialogue(0);
    }

    private void Update()
    {
        if (dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextDialogue();
        }

        if (!isInitialDialogue && AllAnimalsCaptured())
        {
            GameResult.Finish(true);
            SceneManager.LoadScene("ClearScene", LoadSceneMode.Single);
        }
    }

    public void ShowDialogue(int id)
    {
        //Debug.Log($"ShowDialogue 호출됨, id: {id}");
        currentDialogueId = id;
        currentDialogueIndex = 0;

        string dialogueData = dialogueManager.GetDialogue(id, currentDialogueIndex);

        if (dialogueData == null)
        {
            Debug.LogWarning("대화 데이터가 존재하지 않습니다.");
            currentDialogueIndex = 0;
            return;
        }

        string dialogueTextContent = dialogueData.Split(':')[0];

        dialoguePanel.SetActive(true);
        typeEffect.SetMsg(dialogueTextContent);

        if (!typeEffect.gameObject.activeSelf)
        {
            typeEffect.gameObject.SetActive(true);
        }

        if (IsInitialDialogue())
        {
            greetingAudioSource.PlayOneShot(greetingSound);
        }


        string trigger = dialogueData.Split(':')[1];
        Debug.Log("애니메이션 트리거 설정: " + trigger);
        animator.SetTrigger(trigger);

        currentDialogueIndex++;
    }

    public void ShowNextDialogue()
    {
        if (typeEffect.isTyping)
        {
            typeEffect.CompleteTyping();
            return;
        }

        string dialogueData = dialogueManager.GetDialogue(currentDialogueId, currentDialogueIndex);

        if (dialogueData == null)
        {
            Debug.Log("다음 대화 데이터가 존재하지 않습니다.");
            dialoguePanel.SetActive(false);
            currentDialogueIndex = 0;

            if (isInitialDialogue)
            {
                isInitialDialogue = false;
            }

            return;
        }

        string dialogueTextContent = dialogueData.Split(':')[0];
        typeEffect.SetMsg(dialogueTextContent);

        if (!typeEffect.gameObject.activeSelf)
        {
            typeEffect.gameObject.SetActive(true);
        }

        string trigger = dialogueData.Split(':')[1];
        //Debug.Log("애니메이션 트리거 설정: " + trigger);
        animator.SetTrigger(trigger);

        if (currentDialogueId == 0)
        {
            Debug.Log(currentDialogueIndex);
            switch (currentDialogueIndex)
            {
                case 8:
                    ReleaseAnimals();
                    break;
            }
        }

        currentDialogueIndex++;
    }

    // 울타리 안의 동물들을 맵 곳곳으로 흩어지게 풀어놓는다. 둘레를 동물 수만큼 고르게 나눠
    // 저마다 다른 방향으로 멀리 달아나게 해서, 처음에 한곳에서 한꺼번에 잡히지 않게 한다.
    private void ReleaseAnimals()
    {
        List<Animal> animals = new List<Animal>();
        foreach (GameObject animal in GameObject.FindGameObjectsWithTag("Animal"))
        {
            Animal animalScript = animal.GetComponent<Animal>();
            if (animalScript != null)
            {
                animals.Add(animalScript);
            }
        }

        // 같은 종류끼리 몰리지 않게 순서를 섞은 뒤 방향을 나눠 준다
        for (int i = animals.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (animals[i], animals[j]) = (animals[j], animals[i]);
        }

        Vector2 penCenter = player.penArea.center;
        GraphNode playerNode = AstarPath.active != null ? AstarPath.active.GetNearest(player.transform.position, NNConstraint.Default).node : null;
        List<Vector2> taken = new List<Vector2>();
        float startAngle = Random.Range(0f, 360f);
        for (int i = 0; i < animals.Count; i++)
        {
            float angle = startAngle + (i + Random.Range(0.2f, 0.8f)) * 360f / animals.Count;
            Vector2 destination = PickScatterPoint(penCenter, angle, playerNode, taken);
            taken.Add(destination);
            animals[i].Escape(destination, Random.Range(0f, maxEscapeDelay));
        }
    }

    // angle 방향으로 울타리에서 멀리 떨어진, 플레이어가 걸어갈 수 있는 빈 곳을 고른다
    private Vector2 PickScatterPoint(Vector2 penCenter, float angle, GraphNode playerNode, List<Vector2> taken)
    {
        House house = FindAnyObjectByType<House>();
        Vector2 fallback = penCenter;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            // 처음에는 정해진 방향 근처에서 찾고, 잘 안 되면 점점 넓게 찾는다
            float spread = 20f + attempt * 6f;
            float radians = (angle + Random.Range(-spread, spread)) * Mathf.Deg2Rad;
            float distance = Random.Range(minScatterDistance, maxScatterDistance);
            Vector2 point = penCenter + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * distance;
            point.x = Mathf.Clamp(point.x, scatterArea.xMin, scatterArea.xMax);
            point.y = Mathf.Clamp(point.y, scatterArea.yMin, scatterArea.yMax);

            if (AstarPath.active != null)
            {
                NNInfo nearest = AstarPath.active.GetNearest(point, NNConstraint.Default);
                if (nearest.node == null || (playerNode != null && !PathUtilities.IsPathPossible(playerNode, nearest.node)))
                    continue;
                point = nearest.position;
            }

            if (attempt == 0)
            {
                fallback = point;
            }

            bool tooClose = Vector2.Distance(point, penCenter) < minScatterDistance
                || Vector2.Distance(point, player.transform.position) < minScatterDistance;
            foreach (Vector2 other in taken)
            {
                tooClose |= Vector2.Distance(point, other) < minScatterSpacing;
            }
            if (tooClose || player.penArea.Contains(point) || (house != null && house.IsUnderRoof(point)))
                continue;

            return point;
        }
        return fallback;
    }

    public void ScanAction(GameObject scanObj)
    {
        scanObject = scanObj;
        ObjectData objectData = scanObject.GetComponent<ObjectData>();
        if (objectData != null)
        {
            ShowDialogue(objectData.id);
        }

        dialoguePanel.SetActive(isAction);
    }

    public void CloseScanAction()
    {
        isAction = false;
        scanObject = null;
        dialoguePanel.SetActive(isAction);
    }

    public bool HasFollowingAnimals()
    {
        foreach (GameObject animal in GameObject.FindGameObjectsWithTag("Animal"))
        {
            Animal animalScript = animal.GetComponent<Animal>();
            if (animalScript != null && animalScript.isFollowing)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsInitialDialogue()
    {
        return isInitialDialogue;
    }

    private bool AllAnimalsCaptured()
    {
        foreach (GameObject animal in GameObject.FindGameObjectsWithTag("Animal"))
        {
            Animal animalScript = animal.GetComponent<Animal>();
            if (animalScript != null && animalScript.isCaptured == false)
            {
                return false;
            }
        }
        return true;
    }
}
