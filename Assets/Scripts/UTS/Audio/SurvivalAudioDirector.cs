using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Praktikum5.FSM;
using UTS.Zombies;

namespace UTS.Audio
{
    [DisallowMultipleComponent]
    public sealed class SurvivalAudioDirector : MonoBehaviour
    {
        public static SurvivalAudioDirector Instance {get;private set;}
        [SerializeField] private SurvivalSoundLibrary library;
        [SerializeField,Range(0,1)] private float masterVolume=.85f;
        [SerializeField,Range(0,1)] private float effectsVolume=.9f;
        [SerializeField,Range(0,1)] private float ambienceVolume=.75f;
        [SerializeField,Range(8,40)] private int maxVoices=24;
        [SerializeField] private Transform[] environmentalAnchors;
        private struct Voice { public AudioSource source; public AudioLowPassFilter filter; public SfxCue cue; }
        private Voice[] voices;
        private AudioSource wind,droneA,droneB,heartbeat;
        private SimplePlayerController player;
        private ZombieHordeManager horde;
        private UTS.Environment.GreenSmokeExtractionZone extraction;
        private GTACameraController gameCamera;
        private float nextThreatCheck,nextCreak,nextExtractionTick;
        private float tension;
        private bool enteredExtraction,wasPaused;
        private readonly Dictionary<SfxCue,int> played=new Dictionary<SfxCue,int>();
        private readonly RaycastHit[] hits=new RaycastHit[16];
        public IReadOnlyDictionary<SfxCue,int> Played=>played;
        public int Count(SfxCue cue)=>played.TryGetValue(cue,out int n)?n:0;
        public int ActiveVoices {get{int n=0;if(voices!=null)foreach(var v in voices)if(v.source&&v.source.isPlaying)n++;return n;}}
        public bool AmbiencePlaying=>wind&&wind.isPlaying&&((droneA&&droneA.isPlaying)||(droneB&&droneB.isPlaying));
        public bool HasLibrary=>library!=null;
        private void Awake(){
            if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;
            voices=new Voice[maxVoices];
            for(int i=0;i<voices.Length;i++){
                var go=new GameObject("SFX voice "+i);go.transform.SetParent(transform,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.dopplerLevel=0;s.rolloffMode=AudioRolloffMode.Custom;s.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.25f,.45f),new Keyframe(1,0)));s.minDistance=2.5f;s.maxDistance=28;s.priority=160;
                var filter=go.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=22000;voices[i]=new Voice{source=s,filter=filter};
            }
            wind=LoopSource("Night wind",SfxCue.Wind);droneA=LoopSource("Tension A",SfxCue.Tension,false);droneB=LoopSource("Tension B",SfxCue.Tension,false);heartbeat=LoopSource("Low health pulse",SfxCue.Heartbeat);heartbeat.volume=0;
        }
        private void Start(){
            player=FindAnyObjectByType<SimplePlayerController>();horde=FindAnyObjectByType<ZombieHordeManager>();extraction=FindAnyObjectByType<UTS.Environment.GreenSmokeExtractionZone>();gameCamera=FindAnyObjectByType<GTACameraController>();
            if(horde){horde.OnObjectiveReached+=Objective;horde.OnExtractionReached+=ExtractionSuccess;}
            nextCreak=Time.time+Random.Range(18,32);StartCoroutine(TensionLoop());
        }
        private void OnDestroy(){if(horde){horde.OnObjectiveReached-=Objective;horde.OnExtractionReached-=ExtractionSuccess;}if(Instance==this)Instance=null;}
        private AudioSource LoopSource(string name,SfxCue cue,bool loop=true){
            var go=new GameObject(name);go.transform.SetParent(transform,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.loop=loop;s.priority=220;var entry=library?library.Get(cue):null;s.clip=entry?.Next();s.volume=0;if(s.clip&&loop)s.Play();return s;
        }
        private IEnumerator TensionLoop(){
            var current=droneA;var other=droneB;
            if(!current||!current.clip)yield break;
            current.time=0;current.Play();
            while(current&&current.clip){
                float length=current.clip.length;float fade=Mathf.Min(4,length*.1f);float t=0;
                while(current.time<length-fade&&current.isPlaying){current.volume=TensionVolume()*Mathf.Min(1,current.time/fade);yield return null;}
                other.time=0;other.volume=0;other.Play();t=0;
                while(t<fade){t+=Time.deltaTime;float k=t/fade;current.volume=TensionVolume()*(1-k);other.volume=TensionVolume()*k;yield return null;}
                current.Stop();var swap=current;current=other;other=swap;
            }
        }
        private float TensionVolume(){var e=library?library.Get(SfxCue.Tension):null;return (e?.volume??0)*masterVolume*ambienceVolume*Mathf.Lerp(.55f,1.35f,tension);}
        private void Update(){
            if(wind){var e=library?library.Get(SfxCue.Wind):null;wind.volume=(e?.volume??0)*masterVolume*ambienceVolume;}
            if(Time.time>=nextThreatCheck){nextThreatCheck=Time.time+.5f;float threat=0;if(player&&!player.IsDead)foreach(var z in ZombieSwarmAgent.ActiveAgents){if(!z||z.CurrentState==ZombieSwarmAgent.PZZombieState.Dead)continue;float distance=Vector3.Distance(z.transform.position,player.transform.position);if(distance<13)threat+=Mathf.Clamp01(1-distance/13f)*.45f;}tension=Mathf.Clamp01(threat);}
            if(heartbeat){float target=player&&!player.IsDead?Mathf.InverseLerp(.36f,.08f,player.CurrentHealth/player.MaxHealth)*.3f*masterVolume*effectsVolume:0;heartbeat.volume=Mathf.MoveTowards(heartbeat.volume,target,Time.unscaledDeltaTime*.18f);heartbeat.pitch=Mathf.Lerp(1,1.25f,tension);}
            if(gameCamera&&gameCamera.IsPaused!=wasPaused){wasPaused=gameCamera.IsPaused;Play(SfxCue.UI,Vector3.zero);}
            if(player&&!player.IsDead&&extraction&&!extraction.IsExtracted){
                if(extraction.IsPlayerInside&&!enteredExtraction){Play(SfxCue.ExtractionTick,player.transform.position);nextExtractionTick=Time.time+1;}
                enteredExtraction=extraction.IsPlayerInside;
                if(enteredExtraction&&Time.time>=nextExtractionTick){Play(SfxCue.ExtractionTick,player.transform.position,.6f);nextExtractionTick=Time.time+1;}
            }
            if(player&&!player.IsDead&&Time.time>=nextCreak){nextCreak=Time.time+Random.Range(22,42);if(environmentalAnchors!=null&&environmentalAnchors.Length>0){var anchor=environmentalAnchors[Random.Range(0,environmentalAnchors.Length)];if(anchor&&Vector3.Distance(anchor.position,player.transform.position)<24)Play(SfxCue.Creak,anchor.position);}}
        }
        private void Objective(){Play(SfxCue.ExtractionTick,Vector3.zero);}
        private void ExtractionSuccess(){Play(SfxCue.ExtractionSuccess,Vector3.zero);}
        public static bool Emit(SfxCue cue,Vector3 position,float volume=1,float pitch=1)=>Instance&&Instance.Play(cue,position,volume,pitch);
        public bool Play(SfxCue cue,Vector3 position,float volume=1,float pitch=1){
            var e=library?library.Get(cue):null;var clip=e?.Next();if(!clip||voices==null)return false;
            var listener=Camera.main?Camera.main.transform:player?player.transform:null;
            if(e.spatial&&listener&&Vector3.Distance(listener.position,position)>30)return false;
            bool vocal=cue==SfxCue.ZombieGroan||cue==SfxCue.ZombieAttack;
            if(vocal){int n=0;foreach(var v in voices)if(v.source.isPlaying&&(v.cue==SfxCue.ZombieGroan||v.cue==SfxCue.ZombieAttack))n++;if(n>=5)return false;}
            int index=-1;for(int i=0;i<voices.Length;i++)if(!voices[i].source.isPlaying){index=i;break;}
            if(index<0){if(vocal||cue==SfxCue.FootConcrete||cue==SfxCue.FootGrass||cue==SfxCue.FootWood||cue==SfxCue.FootCarpet)return false;index=0;}
            var voice=voices[index];voice.cue=cue;voices[index]=voice;var s=voice.source;s.Stop();s.transform.position=position;s.clip=clip;s.spatialBlend=e.spatial?1:0;s.pitch=pitch*(1+Random.Range(-e.pitchVariation,e.pitchVariation));s.volume=e.volume*masterVolume*effectsVolume*volume;s.priority=vocal?150:90;voice.filter.cutoffFrequency=22000;
            if(e.spatial&&listener){int n=Physics.RaycastNonAlloc(position,listener.position-position,hits,Vector3.Distance(position,listener.position),~0,QueryTriggerInteraction.Ignore);for(int i=0;i<n;i++){var t=hits[i].collider.transform;if(t.GetComponentInParent<SimplePlayerController>()||t.GetComponentInParent<EnemyFSM>()||t.IsChildOf(transform))continue;s.volume*=.38f;voice.filter.cutoffFrequency=1800;break;}}
            s.Play();played[cue]=Count(cue)+1;return true;
        }
    }
}
