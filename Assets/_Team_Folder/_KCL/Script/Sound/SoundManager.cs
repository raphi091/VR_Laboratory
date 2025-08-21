using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum BGMTrackName
{
    None = 0,
    Tutorial,
    Lobby
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
    [SerializeField] private BGMTrackName autoStartTrack = BGMTrackName.Lobby;

    [Header("SFX")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    private AudioSource bgmA;
    private AudioSource bgmB;
    private AudioSource sfxSource;
    private bool isPlaying = true; // true = A 활성, false = B 활성

    private Dictionary<BGMTrackName, AudioClip> musicClipDict;

    // ---- 볼륨 스케일 ----
    private float masterVolume = 1.0f;   // 전체(마스터) 0~1
    private float bgmMasterVolume = 0.8f; // BGM 0~1
    private float sfxMasterVolume = 0.8f; // SFX 0~1

    // BGM 정규화 볼륨(0~1). 실제 소스 볼륨은 norm * (master*bgm)
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
        // BGM 소스 2개 (교차 페이드용)
        bgmA = gameObject.AddComponent<AudioSource>();
        bgmB = gameObject.AddComponent<AudioSource>();
        if (bgmMixerGroup != null)
        {
            bgmA.outputAudioMixerGroup = bgmMixerGroup;
            bgmB.outputAudioMixerGroup = bgmMixerGroup;
        }
        bgmA.playOnAwake = false; bgmB.playOnAwake = false;
        bgmA.loop = true; bgmB.loop = true;
        bgmA.volume = 0f; bgmB.volume = 0f;

        // SFX 소스
        sfxSource = gameObject.AddComponent<AudioSource>();
        if (sfxMixerGroup != null)
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

        // 같은 트랙이면 무시
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
        inactive.volume = 0f;
        inactive.Play();

        // 활성 소스가 현재 실제로 재생 중인지(첫 시작이면 false)
        bool activeHasAudio = active.isPlaying && active.clip != null && active.volume > 0.0001f;

        float timer = 0f;
        float dur = Mathf.Max(0f, crossfadeDuration);

        while (timer < dur)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / dur);
            float scale = masterVolume * bgmMasterVolume;

            if (activeHasAudio)
            {
                // 정상적인 교차 페이드
                float activeNorm = 1f - t;
                float inactiveNorm = t;

                if (isPlaying) { bgmANorm = activeNorm; bgmBNorm = inactiveNorm; }
                else           { bgmBNorm = activeNorm; bgmANorm = inactiveNorm; }

                active.volume   = activeNorm   * scale;
                inactive.volume = inactiveNorm * scale;
            }
            else
            {
                // 첫 시작: 비활성(inactive)만 부드럽게 페이드 인
                float inactiveNorm = t;

                if (isPlaying) { bgmBNorm = inactiveNorm; bgmANorm = 0f; }
                else           { bgmANorm = inactiveNorm; bgmBNorm = 0f; }

                inactive.volume = inactiveNorm * scale;
            }

            yield return null;
        }

        if (activeHasAudio)
        {
            active.Stop();
            active.clip = null;
        }

        // 최종 상태(새 트랙 1.0, 이전 트랙 0.0)
        if (isPlaying) { bgmANorm = 0f; bgmBNorm = 1f; }
        else           { bgmBNorm = 0f; bgmANorm = 1f; }

        ApplyBgmScaledVolumes();
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
        AudioListener.volume = 1f; // 외부에서 건드렸을 수 있어 원복
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
