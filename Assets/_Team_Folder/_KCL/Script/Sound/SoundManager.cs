using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum BGMTrackName
{
    None = 0,
    Lobby,
    LabIdle,
    ExperimentPhase,
    Success,
    Failure,
    Discovery,
    Ending
}

[System.Serializable]
public class MusicTrack
{
    public BGMTrackName trackName;
    public AudioClip audioClip;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("BGM")]
    [SerializeField] private List<MusicTrack> musicTracks;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private float crossfadeDuration = 1.5f;
    [SerializeField] private BGMTrackName autoStartTrack = BGMTrackName.LabIdle;

    [Header("SFX")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    private AudioSource bgmA;
    private AudioSource bgmB;
    private AudioSource sfxSource;
    private bool isPlaying = true; // true=A 활성 / false=B 활성

    private Dictionary<BGMTrackName, AudioClip> musicClipDict;

    // ---- 볼륨 스케일 ----
    private float masterVolume = 1.0f;  // 전체(마스터) 0~1
    private float bgmMasterVolume = 0.8f; // BGM 0~1
    private float sfxMasterVolume = 0.8f; // SFX 0~1

    // BGM 현재 '정규화 볼륨'(0~1). 실제 소스 볼륨은 norm * (master*bgm)
    private float bgmANorm = 0f;
    private float bgmBNorm = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSound();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (autoStartTrack != BGMTrackName.None)
        {
            PlayBGM(autoStartTrack, true);
        }
    }

    private void InitializeSound()
    {
        // BGM 소스 2개 (교차 페이드)
        bgmA = gameObject.AddComponent<AudioSource>();
        bgmB = gameObject.AddComponent<AudioSource>();
        bgmA.outputAudioMixerGroup = bgmMixerGroup;
        bgmB.outputAudioMixerGroup = bgmMixerGroup;
        bgmA.playOnAwake = false; bgmB.playOnAwake = false;
        bgmA.loop = true; bgmB.loop = true;
        bgmA.volume = 0f; bgmB.volume = 0f;

        // SFX 소스
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        sfxSource.playOnAwake = false; sfxSource.loop = false;
        sfxSource.volume = sfxMasterVolume * masterVolume;

        // Enum → Clip 맵
        musicClipDict = new Dictionary<BGMTrackName, AudioClip>();
        foreach (var track in musicTracks)
        {
            if (track != null && track.audioClip != null)
                musicClipDict[track.trackName] = track.audioClip;
        }
    }

    public void PlayBGM(BGMTrackName trackName, bool loop = true)
    {
        if (!musicClipDict.ContainsKey(trackName)) return;

        var clipToPlay = musicClipDict[trackName];
        var current = isPlaying ? bgmA : bgmB;

        if (current.isPlaying && current.clip == clipToPlay) return;

        StopAllCoroutines();
        StartCoroutine(Crossfade(clipToPlay, loop));
    }

    private IEnumerator Crossfade(AudioClip newClip, bool loop)
    {
        var active = isPlaying ? bgmA : bgmB;
        var inactive = isPlaying ? bgmB : bgmA;

        inactive.clip = newClip;
        inactive.loop = loop;
        inactive.Play();

        float timer = 0f;
        while (timer < crossfadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / crossfadeDuration);

            // 정규화 볼륨(0~1) 갱신
            float activeNorm   = Mathf.Lerp(1f, 0f, t);
            float inactiveNorm = Mathf.Lerp(0f, 1f, t);

            // 저장(슬라이더 변경 시 즉시 재적용용)
            if (isPlaying) { bgmANorm = activeNorm; bgmBNorm = inactiveNorm; }
            else           { bgmBNorm = activeNorm; bgmANorm = inactiveNorm; }

            // 실제 소스 볼륨 = norm * (마스터 * BGM)
            float scale = masterVolume * bgmMasterVolume;
            active.volume   = activeNorm   * scale;
            inactive.volume = inactiveNorm * scale;

            yield return null;
        }

        active.Stop();
        active.clip = null;

        // 최종 상태(새 트랙 1.0, 이전 트랙 0.0)
        if (isPlaying) { bgmANorm = 0f; bgmBNorm = 1f; }
        else           { bgmBNorm = 0f; bgmANorm = 1f; }

        ApplyBgmScaledVolumes(); // 현재 스케일 반영
        isPlaying = !isPlaying;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.volume = sfxMasterVolume * masterVolume;
        sfxSource.PlayOneShot(clip);
    }

    // ----- 볼륨 API (UI에서 호출) -----
    public void SetMasterVolume01(float v01)
    {
        masterVolume = Mathf.Clamp01(v01);
        ApplyBgmScaledVolumes();
        sfxSource.volume = sfxMasterVolume * masterVolume;
        AudioListener.volume = 1f; // 혹시 건드렸다면 원복(우린 내부 스케일로만 제어)
    }

    public void SetBGMVolume01(float v01)
    {
        bgmMasterVolume = Mathf.Clamp01(v01);
        ApplyBgmScaledVolumes();
    }

    public void SetSFXVolume01(float v01)
    {
        sfxMasterVolume = Mathf.Clamp01(v01);
        sfxSource.volume = sfxMasterVolume * masterVolume;
    }

    public void SetCrossfadeSeconds(float sec)
    {
        crossfadeDuration = Mathf.Max(0f, sec);
    }

    private void ApplyBgmScaledVolumes()
    {
        float scale = masterVolume * bgmMasterVolume;
        bgmA.volume = bgmANorm * scale;
        bgmB.volume = bgmBNorm * scale;
    }
}
