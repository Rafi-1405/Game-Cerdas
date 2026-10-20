using System;
using UnityEngine;

namespace UTS.Audio
{
    public enum SfxCue { FootConcrete,FootGrass,FootWood,FootCarpet,PunchHit,BodyFall,PunchSwing,Pickup,Flashlight,Creak,ZombieGroan,ZombieAttack,ZombieDeath,PlayerHurt,PlayerDeath,Wind,Tension,UI,InventoryFull,ExtractionTick,ExtractionSuccess,Heartbeat }
    [CreateAssetMenu(menuName="ZombieUnity/Survival Sound Library")]
    public sealed class SurvivalSoundLibrary : ScriptableObject
    {
        [Serializable] public class CueSet
        {
            public SfxCue cue;
            public AudioClip[] clips;
            [Range(0,1)] public float volume=.5f;
            [Range(0,.3f)] public float pitchVariation=.05f;
            public bool spatial=true;
            [NonSerialized] public int previous=-1;
            public AudioClip Next(){if(clips==null||clips.Length==0)return null;int index=UnityEngine.Random.Range(0,clips.Length);if(clips.Length>1&&index==previous)index=(index+1)%clips.Length;previous=index;return clips[index];}
        }
        public CueSet[] cues;
        public CueSet Get(SfxCue cue){if(cues!=null)foreach(var entry in cues)if(entry.cue==cue)return entry;return null;}
    }
}
