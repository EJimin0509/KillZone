using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip mainBgm;   // 로비/메인
    public AudioClip storeBgm;  // 인벤토리/뽑기
    public AudioClip forestBgm; // Stage 1
    public AudioClip desertBgm; // Stage 2
    public AudioClip winterBgm; // Stage 3

    [Header("SFX Clips")]
    public AudioClip arrowSound;
    public AudioClip knifeSound;
    public AudioClip enemyDeathSound;
    public AudioClip maleDeathSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // --- 씬 번호(Build Index)에 따른 BGM 자동 전환 ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Build Settings (Ctrl+Shift+B)에 등록된 순서 번호입니다.
        switch (scene.buildIndex)
        {
            case 0: // 보통 로비나 타이틀 화면
                PlayBGM(mainBgm);
                break;

            case 1: // 인벤토리 혹은 가챠 씬 (본인 설정에 맞게 수정)
            case 2:
            case 3:
            case 4:
                PlayBGM(storeBgm);
                break;

            case 5: // 1스테이지 (Forest)
                PlayBGM(forestBgm);
                break;

            case 6: // 2스테이지 (Desert)
                PlayBGM(desertBgm);
                break;

            case 7: // 3스테이지 (Winter)
                PlayBGM(winterBgm);
                break;
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}