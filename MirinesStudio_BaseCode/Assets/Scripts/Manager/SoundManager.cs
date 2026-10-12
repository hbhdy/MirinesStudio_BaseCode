using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MirinesStudio
{
    public enum SoundDataType
    {
        None = 0,
    }

    [System.Serializable]
    public class SoundEntry
    {
        public SoundDataType type;
        public AudioClip clip;
    }

    public class SoundManager : BaseManager
    {
        // ----- Param -----

        public bool Mute
        {
            get => MuteSFX && MuteBGM;
            set { MuteSFX = MuteBGM = value; UpdateAudioMutes(); }
        }

        public bool MuteSFX
        {
            get => muteSfx;
            set { muteSfx = value; UpdateAudioMutes(); }
        }

        public bool MuteBGM
        {
            get => muteBgm;
            set { muteBgm = value; UpdateAudioMutes(); }
        }

        [SerializeField]
        private float sameSoundIgnoreTime = 0.1f;    // 사운드별 마지막 재생 후 제한 시간 내 요청은 무시
        [SerializeField]
        private int maxAudioSources = 4;        // AudioSource 최대 개수

        [Header("Audio Sources")]
        public AudioSource bgmSource;

        [Header("Audio Clips")]
        public List<AudioClip> bgmClips;
        public List<SoundEntry> sfxEntries = new List<SoundEntry>();

        private Dictionary<SoundDataType, AudioClip> dicSoundData;
        private List<AudioSource> sfxSources = new List<AudioSource>();

        private readonly Dictionary<SoundDataType, float> lastPlaySoundTimes = new Dictionary<SoundDataType, float>();
        private bool isAdShowing;
        private bool resumeBgmAfterAd;
        private Coroutine bgmFade;

        private bool muteSfx;
        private bool muteBgm;

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            await base.Task_Init();
        }

        public override void Init()
        {
            dicSoundData = BuildSoundData();
            lastPlaySoundTimes.Clear();

            if (sfxSources?.Count < maxAudioSources)
            {
                for (int i = 0; i < maxAudioSources; i++)
                {
                    var source = gameObject.AddComponent<AudioSource>();
                    sfxSources.Add(source);
                }
            }

            ApplyFromStatic();

            PlayBGM(0);
            base.Init();
        }

        // ----- Set -----

        private Dictionary<SoundDataType, AudioClip> BuildSoundData()
        {
            var soundData = new Dictionary<SoundDataType, AudioClip>();
            if (sfxEntries == null)
                return soundData;

            for (int i = 0; i < sfxEntries.Count; i++)
            {
                SoundEntry entry = sfxEntries[i];
                if (entry == null || entry.type == SoundDataType.None || !System.Enum.IsDefined(typeof(SoundDataType), entry.type))
                {
                    Debug.LogWarning($"SFX entry [{i}] has no valid sound type. Entry skipped.", this);
                    continue;
                }
                if (entry.clip == null)
                {
                    Debug.LogWarning($"SFX entry [{i}] ({entry.type}) has no clip. Entry skipped.", this);
                    continue;
                }
                if (!soundData.TryAdd(entry.type, entry.clip))
                    Debug.LogWarning($"SFX entry [{i}] duplicates {entry.type}. First valid entry is used.", this);
            }
            return soundData;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            BuildSoundData();
        }
#endif

        public void ApplyFromStatic()
        {
            MuteBGM = !StaticGameData.OnBGM;
            MuteSFX = !StaticGameData.OnSFX;
            UpdateAudioMutes();
        }

        private void UpdateAudioMutes()
        {
            if (bgmSource != null)
                bgmSource.mute = MuteBGM;

            for (int i = 0; i < sfxSources.Count; i++)
                sfxSources[i].mute = MuteSFX;
        }

        // ----- Get -----

        private AudioSource GetAvailableAudioSource()
        {
            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                    return source;
            }

            MSLog.Log("Nothing Available Source");
            return null;
        }

        // ----- Main -----

        public void PlayBGM(int index, bool loop = true, float volume = 1f)
        {
            if (bgmClips == null || index < 0 || index >= bgmClips.Count)
                return;

            var clip = bgmClips[index];
            if (clip == null || bgmSource == null) return;

            CancelBGMFade();
            PlayBGMClip(clip, loop, volume);
        }

        public void PlayBGMWithFade(int index, float fadeDuration = 0.5f, bool loop = true, float volume = 1f)
        {
            if (bgmClips == null || index < 0 || index >= bgmClips.Count || bgmClips[index] == null || bgmSource == null)
                return;
            if (fadeDuration <= 0f)
            {
                PlayBGM(index, loop, volume);
                return;
            }

            CancelBGMFade();
            bgmFade = StartCoroutine(ChangeBGMWithFade(bgmClips[index], loop, Mathf.Clamp01(volume), fadeDuration));
        }

        private void PlayBGMClip(AudioClip clip, bool loop, float volume)
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = volume;

            // 음소거와 재생 상태는 별도로 유지합니다.
            bgmSource.Play();
            if (isAdShowing)
            {
                bgmSource.Pause();
                resumeBgmAfterAd = true;
            }
        }


        public void StopBGM()
        {
            CancelBGMFade();
            resumeBgmAfterAd = false;
            bgmSource?.Stop();
        }

        public void StopBGMWithFade(float fadeDuration = 0.5f)
        {
            if (bgmSource == null || !bgmSource.isPlaying || isAdShowing || fadeDuration <= 0f)
            {
                StopBGM();
                return;
            }

            CancelBGMFade();
            bgmFade = StartCoroutine(ChangeBGMWithFade(null, false, 0f, fadeDuration));
        }

        public void PlaySFX(SoundDataType type)
        {
            if (muteSfx)
                return;

            if (dicSoundData == null || !dicSoundData.TryGetValue(type, out AudioClip clip) || clip == null)
            {
                MSLog.Log($"SFX is Null : {type}");
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (lastPlaySoundTimes.TryGetValue(type, out float lastPlayTime) && now - lastPlayTime < sameSoundIgnoreTime)
                return;

            AudioSource source = GetAvailableAudioSource();
            if (source == null)
                return;

            source.volume = 1f;
            source.PlayOneShot(clip);
            lastPlaySoundTimes[type] = now;
        }

        public void StopSFX()
        {
            foreach (var source in sfxSources)
                source.Stop();
        }

        // ----- Logic -----

        private IEnumerator ChangeBGMWithFade(AudioClip nextClip, bool loop, float volume, float duration)
        {
            while (isAdShowing || AudioListener.pause)
                yield return null;

            if (bgmSource != null && bgmSource.isPlaying)
                yield return FadeBGMVolume(0f, duration);
            if (bgmSource == null)
            {
                bgmFade = null;
                yield break;
            }

            bgmSource.Stop();
            resumeBgmAfterAd = false;
            if (nextClip != null)
            {
                PlayBGMClip(nextClip, loop, 0f);
                yield return FadeBGMVolume(volume, duration);
            }
            bgmFade = null;
        }

        private IEnumerator FadeBGMVolume(float targetVolume, float duration)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration && bgmSource != null)
            {
                yield return null;
                if (bgmSource == null)
                    yield break;
                if (isAdShowing || AudioListener.pause)
                    continue;
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / duration));
            }
        }

        private void CancelBGMFade()
        {
            if (bgmFade == null)
                return;
            StopCoroutine(bgmFade);
            bgmFade = null;
        }

        public override void Shutdown()
        {
            StopBGM();
            base.Shutdown();
        }

        private void OnDisable()
        {
            StopBGM();
        }

        public void OnAdShown()
        {
            if (isAdShowing)
                return;

            isAdShowing = true;
            resumeBgmAfterAd = bgmSource != null && bgmSource.isPlaying;
            if (resumeBgmAfterAd)
                bgmSource.Pause();
        }

        public void OnAdClosed()
        {
            if (!isAdShowing)
                return;

            isAdShowing = false;
            if (resumeBgmAfterAd && bgmSource != null)
                bgmSource.UnPause();
            resumeBgmAfterAd = false;
            UpdateAudioMutes();
        }
    }
}
