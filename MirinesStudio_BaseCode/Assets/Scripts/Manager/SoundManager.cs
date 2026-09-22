using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace MirinesStudio
{
    public enum SoundDataType
    {
        None = 0,
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
        private float sameSoundIgnoreTime = 0.1f;    // 마지막 재생한 사운드와 같은게 특정시간 내에오면 무시
        [SerializeField]
        private int maxAudioSources = 4;        // AudioSource 최대 개수

        [Header("Audio Sources")]
        public AudioSource bgmSource;

        [Header("Audio Clips")]
        public List<AudioClip> bgmClips;
        public List<AudioClip> sfxClips;

        private Dictionary<SoundDataType, AudioClip> dicSoundData;
        private List<AudioSource> sfxSources = new List<AudioSource>();

        private float lastPlaySoundTime;
        private string lastPlaySoundName;

        private bool muteSfx;
        private bool muteBgm;

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            await base.Task_Init();
        }

        public override void Init()
        {
            dicSoundData = new Dictionary<SoundDataType, AudioClip>();

            for (int i = 0; i < sfxClips.Count; i++)
                dicSoundData[(SoundDataType)(i + 1)] = sfxClips[i];

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

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = volume;

            // 주석 한 이유 : bgm 껏다가 키면 bgm 안나옴
            //if (!MuteBGM)
                bgmSource.Play();
        }


        public void StopBGM() => bgmSource?.Stop();

        public void PlaySFX(SoundDataType type)
        {
            if (muteSfx)
                return;

            if (dicSoundData.TryGetValue(type, out AudioClip clip))
            {
                AudioSource source = GetAvailableAudioSource();
                if (source != null)
                {
                    if (clip.name == lastPlaySoundName)
                    {
                        if (Time.realtimeSinceStartup - lastPlaySoundTime < sameSoundIgnoreTime)
                            return;
                    }

                    lastPlaySoundName = clip.name;
                    lastPlaySoundTime = Time.realtimeSinceStartup;

                    source.volume = 1f;
                    source.PlayOneShot(clip);
                }
            }
            else
                MSLog.Log($"SFX is Null : {type}");
        }

        public void StopSFX()
        {
            foreach (var source in sfxSources)
                source.Stop();
        }

        // ----- Logic -----

        private void TryResumeBGM()
        {
            // 광고 SDK나 OS 일시정지 시 AudioListener.pause이 True로 남는 경우
            AudioListener.pause = false;

            if (bgmSource == null) return;

            // 음소거 아니면 자동 복구
            if (!MuteBGM)
            {
                if (bgmSource.clip == null)
                {
                    PlayBGM(0);
                    return;
                }

                if (bgmSource.clip != null && !bgmSource.isPlaying)
                    bgmSource.Play();
            }

            UpdateAudioMutes();
        }

        public void OnAdShown() => bgmSource?.Pause();
        public void OnAdClosed() => TryResumeBGM();
    }
}
