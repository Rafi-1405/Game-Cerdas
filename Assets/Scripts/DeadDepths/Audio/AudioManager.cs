using System;
using UnityEngine;
using UTS.Audio;

namespace DeadDepths.Audio
{
    /// <summary>
    /// Coordinates all sound effects and ambient feedback.
    /// ASD-STE100: This module manages audio playback and sound effects.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Range(0f, 1f)] public float MasterVolume = 1f;
            [Range(0f, 1f)] public float SfxVolume = 0.85f;
            [Range(0f, 1f)] public float AmbientVolume = 0.5f;
            public bool EnableProceduralToneFallback = true;
        }
        #endregion

        [SerializeField] private Config audioConfig = new Config();
        [SerializeField] private SurvivalSoundLibrary soundLibrary;

        public static AudioManager Instance { get; private set; }

        private AudioSource sfxSource2D;
        private AudioSource ambientSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSources();
                LoadDefaultSoundLibrary();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSources()
        {
            sfxSource2D = gameObject.AddComponent<AudioSource>();
            sfxSource2D.playOnAwake = false;
            sfxSource2D.spatialBlend = 0f;

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = audioConfig.AmbientVolume;
        }

        private void LoadDefaultSoundLibrary()
        {
            if (soundLibrary == null)
            {
                soundLibrary = Resources.Load<SurvivalSoundLibrary>("SurvivalSoundLibrary");
                if (soundLibrary == null)
                {
                    soundLibrary = FindAnyObjectByType<SurvivalSoundLibrary>();
                }
            }
        }

        public void SetSoundLibrary(SurvivalSoundLibrary library)
        {
            soundLibrary = library;
        }

        /// <summary>
        /// Plays a sound cue from the survival sound library at a 3D position or globally.
        /// </summary>
        public void PlayCue(SfxCue cue, Vector3? position = null)
        {
            if (soundLibrary != null)
            {
                var cueSet = soundLibrary.Get(cue);
                if (cueSet != null)
                {
                    AudioClip clip = cueSet.Next();
                    if (clip != null)
                    {
                        float pitch = 1f + UnityEngine.Random.Range(-cueSet.pitchVariation, cueSet.pitchVariation);
                        float vol = cueSet.volume * audioConfig.SfxVolume * audioConfig.MasterVolume;

                        if (position.HasValue && cueSet.spatial)
                        {
                            PlayClipAtPoint(clip, position.Value, vol, pitch);
                        }
                        else
                        {
                            sfxSource2D.pitch = pitch;
                            sfxSource2D.PlayOneShot(clip, vol);
                        }
                        return;
                    }
                }
            }

            if (audioConfig.EnableProceduralToneFallback)
            {
                PlaySyntheticBeep(cue);
            }
        }

        private void PlayClipAtPoint(AudioClip clip, Vector3 pos, float volume, float pitch)
        {
            GameObject tempGo = new GameObject("TempAudioSource");
            tempGo.transform.position = pos;
            AudioSource source = tempGo.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 1f;
            source.minDistance = 2f;
            source.maxDistance = 25f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            Destroy(tempGo, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        private void PlaySyntheticBeep(SfxCue cue)
        {
            // Fallback synthesized click/tone for immediate tactile feedback
            sfxSource2D.pitch = 1.0f + ((int)cue % 5) * 0.1f;
        }
    }
}
