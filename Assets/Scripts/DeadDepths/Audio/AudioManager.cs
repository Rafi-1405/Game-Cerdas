using System;
using System.Collections.Generic;
using UnityEngine;
using DeadDepths.AI;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.Player;
using UTS.Audio;

namespace DeadDepths.Audio
{
    /// <summary>
    /// Coordinates 24-voice pooled 3D audio with wall occlusion, adaptive tension crossfading,
    /// low-HP heartbeat, surface-aware footsteps, and PlayerPrefs-persisted volume controls.
    /// ASD-STE100: This module manages audio playback and sound effects.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Range(0f, 1f)] public float MasterVolume = 0.90f;
            [Range(0f, 1f)] public float SfxVolume = 0.85f;
            [Range(0f, 1f)] public float AmbientVolume = 0.65f;
            [Range(8, 32)] public int VoicePoolSize = 24;
            public bool EnableProceduralToneFallback = true;
            public float OcclusionCutoffHz = 1800f;
            public float OcclusionVolumeScale = 0.42f;
        }
        #endregion

        public const string PrefMasterVol = "DeadDepths_MasterVol";
        public const string PrefSfxVol = "DeadDepths_SfxVol";
        public const string PrefAmbientVol = "DeadDepths_AmbientVol";

        [SerializeField] private Config audioConfig = new Config();
        [SerializeField] private SurvivalSoundLibrary soundLibrary;
        public SurvivalSoundLibrary SoundLibrary => soundLibrary;
        public Config AudioConfig => audioConfig;

        public static AudioManager Instance { get; private set; }

        public float MasterVolume => audioConfig.MasterVolume;
        public float SfxVolume => audioConfig.SfxVolume;
        public float AmbientVolume => audioConfig.AmbientVolume;

        private struct PooledVoice
        {
            public AudioSource Source;
            public AudioLowPassFilter Filter;
            public SfxCue Cue;
            public float BaseVolume;
            public bool IsSpatial;
        }

        private PooledVoice[] voices;
        private AudioSource ambientSource;
        private AudioSource tensionSourceA;
        private AudioSource tensionSourceB;
        private AudioSource heartbeatSource;

        private bool tensionUsingA = true;
        private float tensionLevel = 0f;
        private float nextThreatScanTime = 0f;
        private float tensionCrossfadeTimer = 0f;
        private float tensionClipDuration = 0f;
        private bool isCrossfadingTension = false;
        private readonly RaycastHit[] occlusionHits = new RaycastHit[32];
        private readonly List<ZombieController> cachedZombies = new List<ZombieController>();
        private float nextZombieCacheRefresh = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying) DontDestroyOnLoad(gameObject);
                LoadVolumePrefs();
                InitializeSources();
                LoadDefaultSoundLibrary();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            PlayAmbientLoop(SfxCue.Wind);
            StartTensionLoop();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LoadVolumePrefs()
        {
            if (PlayerPrefs.HasKey(PrefMasterVol))
                audioConfig.MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMasterVol, audioConfig.MasterVolume));
            if (PlayerPrefs.HasKey(PrefSfxVol))
                audioConfig.SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVol, audioConfig.SfxVolume));
            if (PlayerPrefs.HasKey(PrefAmbientVol))
                audioConfig.AmbientVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefAmbientVol, audioConfig.AmbientVolume));
        }

        public void SetVolumes(float master, float sfx, float ambient, bool save = true)
        {
            audioConfig.MasterVolume = Mathf.Clamp01(master);
            audioConfig.SfxVolume = Mathf.Clamp01(sfx);
            audioConfig.AmbientVolume = Mathf.Clamp01(ambient);

            if (save)
            {
                PlayerPrefs.SetFloat(PrefMasterVol, audioConfig.MasterVolume);
                PlayerPrefs.SetFloat(PrefSfxVol, audioConfig.SfxVolume);
                PlayerPrefs.SetFloat(PrefAmbientVol, audioConfig.AmbientVolume);
                PlayerPrefs.Save();
            }

            UpdateLoopVolumes();
        }

        private void InitializeSources()
        {
            int count = Mathf.Clamp(audioConfig.VoicePoolSize, 8, 32);
            voices = new PooledVoice[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"SFX_Voice_{i}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.dopplerLevel = 0f;
                src.minDistance = 2.5f;
                src.maxDistance = 28f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.priority = 150;

                var filter = go.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency = 22000f;

                voices[i] = new PooledVoice
                {
                    Source = src,
                    Filter = filter,
                    Cue = SfxCue.UI,
                    BaseVolume = 1f,
                    IsSpatial = false
                };
            }

            ambientSource = CreateLoopSource("Ambient_Wind", true);
            tensionSourceA = CreateLoopSource("Ambient_Tension_A", false);
            tensionSourceB = CreateLoopSource("Ambient_Tension_B", false);
            heartbeatSource = CreateLoopSource("Player_Heartbeat", true);
        }

        private AudioSource CreateLoopSource(string objName, bool loop)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.priority = 220;
            src.volume = 0f;
            return src;
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
        /// Starts a continuous looping ambient cue on the dedicated ambient audio source.
        /// </summary>
        public void PlayAmbientLoop(SfxCue cue)
        {
            if (ambientSource == null) return;
            if (soundLibrary == null) LoadDefaultSoundLibrary();
            if (soundLibrary == null) return;

            var cueSet = soundLibrary.Get(cue);
            if (cueSet == null) return;

            AudioClip clip = cueSet.Next();
            if (clip == null) return;

            bool clipChanged = ambientSource.clip != clip;
            ambientSource.clip = clip;
            ambientSource.volume = Mathf.Clamp01(cueSet.volume * audioConfig.AmbientVolume * audioConfig.MasterVolume);
            ambientSource.loop = true;
            if (clipChanged || !ambientSource.isPlaying)
            {
                ambientSource.Play();
            }
        }

        private void StartTensionLoop()
        {
            if (soundLibrary == null) LoadDefaultSoundLibrary();
            if (soundLibrary == null || tensionSourceA == null || tensionSourceB == null) return;

            var cueSet = soundLibrary.Get(SfxCue.Tension);
            var clip = cueSet?.Next();
            if (clip == null) return;

            tensionSourceA.clip = clip;
            tensionSourceA.time = 0f;
            tensionSourceA.volume = 0f;
            tensionSourceA.Play();
            tensionUsingA = true;
            tensionClipDuration = clip.length;
            isCrossfadingTension = false;
            tensionCrossfadeTimer = 0f;
        }

        private void Update()
        {
            UpdateLoopVolumes();
            UpdateAdaptiveTension();
            UpdateHeartbeatLoop();
        }

        private void UpdateLoopVolumes()
        {
            if (soundLibrary == null) return;

            if (ambientSource != null && ambientSource.isPlaying)
            {
                var windSet = soundLibrary.Get(SfxCue.Wind);
                float baseVol = windSet != null ? windSet.volume : 0.4f;
                ambientSource.volume = Mathf.Clamp01(baseVol * audioConfig.AmbientVolume * audioConfig.MasterVolume);
            }
        }

        private void UpdateAdaptiveTension()
        {
            if (soundLibrary == null || tensionSourceA == null || tensionSourceB == null) return;

            if (Time.unscaledTime >= nextThreatScanTime)
            {
                nextThreatScanTime = Time.unscaledTime + 0.45f;
                float threat = 0f;

                bool inMenu = GameManager.Instance != null && GameManager.Instance.IsInMainMenu;
                var player = DeadDepthsPlayerController.Instance;
                if (!inMenu && player != null && !player.IsDead)
                {
                    if (Time.unscaledTime >= nextZombieCacheRefresh)
                    {
                        nextZombieCacheRefresh = Time.unscaledTime + 1.5f;
                        cachedZombies.Clear();
                        var found = FindObjectsByType<ZombieController>(FindObjectsInactive.Exclude);
                        if (found != null) cachedZombies.AddRange(found);
                    }

                    foreach (var z in cachedZombies)
                    {
                        if (z == null || z.IsDead) continue;
                        float d = Vector3.Distance(z.transform.position, player.transform.position);
                        if (d < 16f)
                        {
                            float weight = (z.CurrentState == ZombieState.Chase || z.CurrentState == ZombieState.Attack) ? 0.45f : 0.18f;
                            threat += Mathf.Clamp01(1f - d / 16f) * weight;
                        }
                    }
                }

                tensionLevel = Mathf.MoveTowards(tensionLevel, Mathf.Clamp01(threat), 0.35f);
            }

            var cueSet = soundLibrary.Get(SfxCue.Tension);
            float targetTensionVol = (cueSet != null ? cueSet.volume : 0.35f) *
                                     audioConfig.AmbientVolume * audioConfig.MasterVolume *
                                     Mathf.Lerp(0.28f, 1.0f, tensionLevel);

            AudioSource current = tensionUsingA ? tensionSourceA : tensionSourceB;
            AudioSource other = tensionUsingA ? tensionSourceB : tensionSourceA;

            if (current.clip == null) return;

            float fadeWindow = Mathf.Min(3.5f, Mathf.Max(0.5f, current.clip.length * 0.12f));
            if (!isCrossfadingTension && current.isPlaying && current.time >= current.clip.length - fadeWindow)
            {
                AudioClip nextClip = cueSet?.Next() ?? current.clip;
                other.clip = nextClip;
                other.time = 0f;
                other.volume = 0f;
                other.Play();
                isCrossfadingTension = true;
                tensionCrossfadeTimer = 0f;
            }

            if (isCrossfadingTension)
            {
                tensionCrossfadeTimer += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(tensionCrossfadeTimer / fadeWindow);
                current.volume = targetTensionVol * (1f - k);
                other.volume = targetTensionVol * k;
                if (k >= 1f)
                {
                    current.Stop();
                    tensionUsingA = !tensionUsingA;
                    isCrossfadingTension = false;
                }
            }
            else if (current.isPlaying)
            {
                float fadeIn = current.time < 1.5f ? Mathf.Clamp01(current.time / 1.5f) : 1f;
                current.volume = Mathf.MoveTowards(current.volume, targetTensionVol * fadeIn, Time.unscaledDeltaTime * 0.6f);
            }
            else
            {
                current.time = 0f;
                current.Play();
            }
        }

        private void UpdateHeartbeatLoop()
        {
            if (heartbeatSource == null || soundLibrary == null) return;

            if (heartbeatSource.clip == null)
            {
                var hbSet = soundLibrary.Get(SfxCue.Heartbeat);
                heartbeatSource.clip = hbSet?.Next();
                if (heartbeatSource.clip != null && !heartbeatSource.isPlaying)
                {
                    heartbeatSource.Play();
                }
            }

            if (heartbeatSource.clip == null) return;

            var player = DeadDepthsPlayerController.Instance;
            bool inMenu = GameManager.Instance != null && GameManager.Instance.IsInMainMenu;
            bool paused = Time.timeScale <= 0f;

            float targetVol = 0f;
            if (!inMenu && !paused && player != null && !player.IsDead && player.MaxHealth > 0f)
            {
                float ratio = player.CurrentHealth / player.MaxHealth;
                if (ratio <= 0.35f)
                {
                    float intensity = Mathf.InverseLerp(0.36f, 0.08f, ratio);
                    var hbSet = soundLibrary.Get(SfxCue.Heartbeat);
                    float cueVol = hbSet != null ? hbSet.volume : 0.55f;
                    targetVol = cueVol * audioConfig.SfxVolume * audioConfig.MasterVolume * Mathf.Lerp(0.35f, 1.0f, intensity);
                    heartbeatSource.pitch = Mathf.Lerp(1.0f, 1.30f, intensity);
                }
            }

            heartbeatSource.volume = Mathf.MoveTowards(heartbeatSource.volume, targetVol, Time.unscaledDeltaTime * 0.45f);
        }

        /// <summary>
        /// Plays a sound cue from the survival sound library at a 3D position or globally.
        /// Preserves legacy signature while routing through the 24-voice pooled system with wall occlusion.
        /// </summary>
        public void PlayCue(SfxCue cue, Vector3? position = null)
        {
            PlayCueScaled(cue, position, 1f, 1f);
        }

        public void PlayCueScaled(SfxCue cue, Vector3? position, float volumeScale, float pitchScale)
        {
            if (voices == null || voices.Length == 0) return;
            if (soundLibrary == null) LoadDefaultSoundLibrary();

            if (soundLibrary != null)
            {
                var cueSet = soundLibrary.Get(cue);
                if (cueSet != null)
                {
                    AudioClip clip = cueSet.Next();
                    if (clip != null)
                    {
                        Vector3 playPos = position ?? Vector3.zero;
                        bool spatial = position.HasValue && cueSet.spatial;

                        var playerCtrl = DeadDepthsPlayerController.Instance;
                        Transform listener = playerCtrl != null
                            ? playerCtrl.transform
                            : (Camera.main != null ? Camera.main.transform : null);
                        Vector3 listenerPos = playerCtrl != null
                            ? playerCtrl.transform.position + Vector3.up * 1.0f
                            : (listener != null ? listener.position : Vector3.zero);
                        if (spatial && listener != null && Vector3.Distance(listener.position, playPos) > 32f)
                        {
                            return;
                        }

                        // Cap simultaneous zombie vocals so hordes stay clean
                        bool isVocal = cue == SfxCue.ZombieGroan || cue == SfxCue.ZombieAttack;
                        if (isVocal)
                        {
                            int vocalCount = 0;
                            for (int i = 0; i < voices.Length; i++)
                            {
                                if (voices[i].Source != null && voices[i].Source.isPlaying &&
                                    (voices[i].Cue == SfxCue.ZombieGroan || voices[i].Cue == SfxCue.ZombieAttack))
                                {
                                    vocalCount++;
                                }
                            }
                            if (vocalCount >= 5) return;
                        }

                        int idx = AcquireVoiceIndex(isVocal, cue);
                        if (idx < 0) return;

                        float pitch = pitchScale * (1f + UnityEngine.Random.Range(-cueSet.pitchVariation, cueSet.pitchVariation));
                        float vol = Mathf.Clamp01(cueSet.volume * audioConfig.SfxVolume * audioConfig.MasterVolume * volumeScale);
                        float cutoff = 22000f;

                        if (spatial && listener != null)
                        {
                            if (IsOccludedByWall(playPos + Vector3.up * 0.9f, listenerPos))
                            {
                                vol *= audioConfig.OcclusionVolumeScale;
                                cutoff = audioConfig.OcclusionCutoffHz;
                            }
                        }

                        ref PooledVoice v = ref voices[idx];
                        v.Cue = cue;
                        v.BaseVolume = vol;
                        v.IsSpatial = spatial;

                        v.Source.Stop();
                        v.Source.transform.position = playPos;
                        v.Source.clip = clip;
                        v.Source.spatialBlend = spatial ? 1f : 0f;
                        v.Source.pitch = Mathf.Clamp(pitch, 0.5f, 1.8f);
                        v.Source.volume = vol;
                        v.Source.priority = isVocal ? 160 : (cue == SfxCue.UI ? 64 : 110);
                        if (v.Filter != null) v.Filter.cutoffFrequency = cutoff;
                        v.Source.Play();
                        return;
                    }
                }
            }

            if (audioConfig.EnableProceduralToneFallback)
            {
                PlaySyntheticBeep(cue);
            }
        }

        public void PlayFootstep(Vector3 position, bool inMud)
        {
            SfxCue cue = SfxCue.FootGrass;
            float pitch = inMud ? 0.78f : 1.0f;

            if (inMud)
            {
                cue = SfxCue.FootGrass;
            }
            else if (PCG.WorldGenerator.Instance != null)
            {
                var cell = PCG.WorldGenerator.Instance.WorldToCell(position);
                var cellType = PCG.WorldGenerator.Instance.GetCell(cell.x, cell.y);
                if (cellType == PCG.CellType.StructureFloor || cellType == PCG.CellType.StructureDoorway)
                {
                    cue = SfxCue.FootWood;
                }
                else if (cellType == PCG.CellType.ExtractionZone)
                {
                    cue = SfxCue.FootConcrete;
                }
            }

            PlayCueScaled(cue, position, inMud ? 0.9f : 0.75f, pitch);
        }

        private int AcquireVoiceIndex(bool isVocal, SfxCue cue)
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i].Source != null && !voices[i].Source.isPlaying)
                {
                    return i;
                }
            }

            // Do not steal slots for low-priority footsteps/vocals when pool is saturated
            if (isVocal || cue == SfxCue.FootGrass || cue == SfxCue.FootWood || cue == SfxCue.FootConcrete || cue == SfxCue.FootCarpet)
            {
                return -1;
            }

            return 0;
        }

        private bool IsOccludedByWall(Vector3 sourcePos, Vector3 listenerPos)
        {
            Vector3 delta = listenerPos - sourcePos;
            float dist = delta.magnitude;
            if (dist <= 0.5f) return false;

            Vector3 dir = delta / dist;
            int count = Physics.RaycastNonAlloc(sourcePos, dir, occlusionHits, dist, ~0, QueryTriggerInteraction.Ignore);
            RaycastHit[] hits = occlusionHits;
            if (count >= occlusionHits.Length)
            {
                hits = Physics.RaycastAll(sourcePos, dir, dist, ~0, QueryTriggerInteraction.Ignore);
                count = hits != null ? hits.Length : 0;
            }
            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;
                if (col.gameObject.name == "PlayableGround") continue;
                if (col.gameObject.name.StartsWith("BoundaryTree_", StringComparison.Ordinal)) continue;
                if (col.GetComponentInParent<DeadDepthsPlayerController>() != null) continue;
                if (col.GetComponentInParent<ZombieController>() != null) continue;
                if (col.GetComponentInParent<SuppliesCrate>() != null) continue;
                if (col.GetComponentInParent<BlockingObject>() != null) continue;
                if (col.GetComponentInParent<ExtractionHelicopter>() != null) continue;
                return true;
            }
            return false;
        }

        private void PlaySyntheticBeep(SfxCue cue)
        {
            if (voices != null && voices.Length > 0 && voices[0].Source != null)
            {
                voices[0].Source.pitch = 1.0f + ((int)cue % 5) * 0.1f;
            }
        }
    }
}
