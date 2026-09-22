using System.Collections.Generic;
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
                    foreach (GameObject animal in GameObject.FindGameObjectsWithTag("Animal"))
                    {
                        Animal animalScript = animal.GetComponent<Animal>();
                        if (animalScript != null)
                        {
                            animalScript.Escape();
                        }
                    }
                    break;
            }
        }

        currentDialogueIndex++;
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
