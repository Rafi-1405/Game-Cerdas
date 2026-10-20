using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using Praktikum5.FSM;
using UTS.Characters;
using UTS.Inventory;
using UTS.Audio;
using UTS.Zombies;

[InitializeOnLoad]
public static class SurvivalUpgradeQA
{
    static string Folder=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Survival");
    static bool active,oldBackground,captured,oldMute;
    static Keyboard keyboard,previousKeyboard;static Mouse mouse,previousMouse;
    static InputSettings.EditorInputBehaviorInPlayMode oldBehavior;
    static SimplePlayerController player;static Animator animator;static CharacterController capsule;static SurvivalAudioDirector audio;static ItemSpawner spawner;static EnemyFSM target;
    static Vector3 home;static int phase;static float start,phaseStart,nextTrace,deathLength,walkStepCount,runStepCount;static double editorStart;
    static Dictionary<string,object> report;static List<string> errors,trace;
    static SurvivalUpgradeQA(){EditorApplication.playModeStateChanged+=Mode;EditorApplication.update+=Tick;}
    public static void Begin(){if(EditorApplication.isPlaying)throw new Exception("Start in Edit mode");SessionState.SetBool("ZU.Survival.QA",true);EditorApplication.isPlaying=true;}
    static void Mode(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("ZU.Survival.QA",false))return;SessionState.SetBool("ZU.Survival.QA",false);try{
        active=true;phase=0;phaseStart=start=Time.time;editorStart=EditorApplication.timeSinceStartup;report=new Dictionary<string,object>();errors=new List<string>();trace=new List<string>();Application.logMessageReceived+=Log;oldBackground=Application.runInBackground;Application.runInBackground=true;oldMute=EditorUtility.audioMasterMute;EditorUtility.audioMasterMute=false;report["editorPreviouslyMuted"]=oldMute;
        previousKeyboard=Keyboard.current;previousMouse=Mouse.current;oldBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>("Survival QA Keyboard");mouse=InputSystem.AddDevice<Mouse>("Survival QA Mouse");InputSystem.onAfterUpdate+=Current;
        player=UnityEngine.Object.FindAnyObjectByType<SimplePlayerController>();capsule=player.GetComponent<CharacterController>();animator=player.GetComponent<HumanoidAnimationDriver>().Animator;audio=UnityEngine.Object.FindAnyObjectByType<SurvivalAudioDirector>();spawner=UnityEngine.Object.FindAnyObjectByType<ItemSpawner>();
        var orbit=Camera.main.GetComponent<GTACameraController>();if(orbit)orbit.enabled=false;Camera.main.transform.rotation=Quaternion.Euler(20,0,0);
        deathLength=((UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController).layers[0].stateMachine.states.First(s=>s.state.name=="Death").state.motion.averageDuration;
    }catch(Exception e){Fail(e);}}
    static void Current(){if(active){keyboard?.MakeCurrent();mouse?.MakeCurrent();}}
    static void Input(params Key[] keys){keyboard.MakeCurrent();mouse.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState());}
    static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors?.Add(text+" "+stack.Split('\n')[0]);}
    static void Next(){phase++;phaseStart=Time.time;captured=false;}
    static float Steps()=>audio.Count(SfxCue.FootConcrete)+audio.Count(SfxCue.FootGrass)+audio.Count(SfxCue.FootWood)+audio.Count(SfxCue.FootCarpet);
    static void Warp(Vector3 position){capsule.enabled=false;player.transform.SetPositionAndRotation(position,Quaternion.identity);capsule.enabled=true;Physics.SyncTransforms();}
    static void Tick(){if(!active)return;EditorApplication.QueuePlayerLoopUpdate();try{
        if(!EditorApplication.isPlaying){Finish();return;}if(EditorApplication.timeSinceStartup-editorStart>120)throw new Exception("QA timeout");float t=Time.time-phaseStart;
        if(phase==1)Input(Key.W);else if(phase==2)Input(Key.W,Key.LeftShift);else if(phase==10)Input(Key.LeftCtrl);else Input();
        if(phase==0){if(t<2.5)return;var h=UnityEngine.Object.FindAnyObjectByType<ZombieHordeManager>();if(h){h.StopAllCoroutines();h.enabled=false;}
            foreach(var z in ZombieSwarmAgent.ActiveAgents.ToArray()){z.enabled=false;var a=z.GetComponent<NavMeshAgent>();if(a&&a.isOnNavMesh)a.isStopped=true;foreach(var c in z.GetComponents<Collider>())c.enabled=false;}
            var surface=UnityEngine.Object.FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();var filter=new NavMeshQueryFilter{agentTypeID=surface.agentTypeID,areaMask=NavMesh.AllAreas};if(!NavMesh.SamplePosition(new Vector3(5.6f,0,0),out var hit,2,filter))throw new Exception("QA street missing");home=hit.position+Vector3.up*1.03f;Warp(home);
            var items=UnityEngine.Object.FindObjectsByType<ScavengeItemPickup>(FindObjectsInactive.Exclude);report["runtimeItemsSpawned"]=items.Length;report["allItemsUseTexturedModels"]=items.Length>0&&items.All(i=>i.UsesModelVisuals&&i.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterial&&r.sharedMaterial.mainTexture));
            report["itemsGrounded"]=items.All(i=>Physics.Raycast(i.transform.position+Vector3.up*.04f,Vector3.down,out var ground,.12f,~0,QueryTriggerInteraction.Ignore)&&Mathf.Abs(ground.point.y-i.transform.position.y)<.03f);
            report["nightSky"]=RenderSettings.skybox&&RenderSettings.skybox.shader.name=="ZombieUnity/Night Sky";report["moonIntensity"]=RenderSettings.sun?RenderSettings.sun.intensity:-1;
            report["ambiencePlaying"]=audio&&audio.HasLibrary&&audio.AmbiencePlaying;walkStepCount=Steps();
            for(int i=0;i<3;i++)spawner.SpawnItem((ScavengeItemType)i,home+Vector3.forward*(1.1f+i*.3f));
            SurvivalUpgradeTools.Render(Camera.main,Path.Combine(Folder,"NightInPlay.png"));Next();return;
        }
        if(phase==1){if(t<1.45)return;report["walkFootsteps"]=Steps()>walkStepCount;report["allThreePickupsCollected"]=player.Inventory.BandageCount>0&&player.Inventory.FirstAidCount>0&&player.Inventory.MedkitCount>0;report["pickupAudio"]=audio.Count(SfxCue.Pickup)>=3;runStepCount=Steps();Warp(home);Next();return;}
        if(phase==2){if(t<1.1)return;report["runFootsteps"]=Steps()>runStepCount;Warp(home);Next();return;}
        if(phase==3){if(t<.35)return;var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CharacterUpgrade/Prefabs/Zombie.prefab"),home+Vector3.forward*1.2f,Quaternion.Euler(0,180,0));go.name="Survival QA target";target=go.GetComponent<EnemyFSM>();target.enabled=false;go.GetComponent<ZombieSwarmAgent>().enabled=false;var agent=go.GetComponent<NavMeshAgent>();if(agent.isOnNavMesh)agent.isStopped=true;player.TryAttackEnemy();Next();return;}
        if(phase==4){if(t<1.35)return;report["punchSwingAudio"]=audio.Count(SfxCue.PunchSwing)>0;report["punchImpactAudio"]=audio.Count(SfxCue.PunchHit)>0&&target.CurrentHealth<100;player.TakeDamage(20);Next();return;}
        if(phase==5){if(t<.4)return;report["playerHurtAudio"]=audio.Count(SfxCue.PlayerHurt)>0;player.Flashlight.ToggleFlashlight();Next();return;}
        if(phase==6){if(t<.35)return;report["flashlightAudio"]=audio.Count(SfxCue.Flashlight)>0;while(player.Inventory.CanAddItem(ScavengeItemType.Medkit))player.Inventory.TryAddItem(ScavengeItemType.Medkit);spawner.SpawnItem(ScavengeItemType.Medkit,player.transform.position);target.TakeDamage(1000);Next();return;}
        if(phase==7){if(t<1.9)return;report["fullBackpackAudio"]=audio.Count(SfxCue.InventoryFull)>0;report["zombieDeathAudio"]=audio.Count(SfxCue.ZombieDeath)>0;report["bodyFallAudio"]=audio.Count(SfxCue.BodyFall)>0;report["standingHeadBeforeDeath"]=HeadHeight();player.TakeDamage(1000);nextTrace=0;Next();return;}
        if(phase==8){DeathTrace("standing",t);if(t<deathLength+.4f)return;report["playerDeathState"]=animator.GetCurrentAnimatorStateInfo(0).IsName("Death");report["deathStopsMoving"]=player.CurrentSpeed==0;report["standingDeathFinalHeadHeight"]=HeadHeight();report["standingDeathLiesDown"]=HeadHeight()<.8f;report["deathMeshFloorOffset"]=MeshFloorOffset();report["corpseRestsOnFloor"]=MeshFloorOffset()>=-.01f&&MeshFloorOffset()<.025f;report["playerDeathAudio"]=audio.Count(SfxCue.PlayerDeath)>0;Portrait("PlayerDeathStanding.png");player.ResetHealth();Next();return;}
        if(phase==9){if(t<.5)return;report["resetHealthRestoresStanding"]=!player.IsDead&&animator.GetCurrentAnimatorStateInfo(0).IsName("Standing");Next();return;}
        if(phase==10){if(t<.8)return;report["crouchBeforeDeath"]=player.IsCrouching;player.TakeDamage(1000);nextTrace=0;Next();return;}
        if(phase==11){DeathTrace("crouching",t);if(t<deathLength+.4f)return;report["crouchDeathLiesDown"]=HeadHeight()<.8f&&animator.GetCurrentAnimatorStateInfo(0).IsName("Death");report["crouchingDeathFinalHeadHeight"]=HeadHeight();Portrait("PlayerDeathCrouching.png");player.ResetHealth();player.TakeDamage(80);var zone=UnityEngine.Object.FindAnyObjectByType<UTS.Environment.GreenSmokeExtractionZone>();Warp(zone.transform.position+Vector3.up*1.03f);Next();return;}
        if(phase==12){if(t>1.5f){var pulse=audio.GetComponentsInChildren<AudioSource>().First(a=>a.name=="Low health pulse");report["lowHealthHeartbeat"]=pulse.isPlaying&&pulse.volume>.03f;}var zone=UnityEngine.Object.FindAnyObjectByType<UTS.Environment.GreenSmokeExtractionZone>();if(t<zone.RequiredHoldTime+1)return;report["extractionCountdownAudio"]=audio.Count(SfxCue.ExtractionTick)>0;report["extractionSuccessAudio"]=zone.IsExtracted&&audio.Count(SfxCue.ExtractionSuccess)>0;report["voicePoolBounded"]=audio.ActiveVoices<=24;report["audioEvents"]=Enum.GetValues(typeof(SfxCue)).Cast<SfxCue>().ToDictionary(c=>c.ToString(),c=>audio.Count(c));var buffer=new float[1024];AudioListener.GetOutputData(buffer,0);report["outputRms"]=Math.Sqrt(buffer.Average(v=>(double)v*v));Finish();}
    }catch(Exception e){Fail(e);}}
    static float HeadHeight(){float floor=player.transform.TransformPoint(capsule.center).y-capsule.height*.5f;return animator.GetBoneTransform(HumanBodyBones.Head).position.y-floor;}
    static float MeshFloorOffset(){float min=float.PositiveInfinity;foreach(var r in animator.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();r.BakeMesh(mesh);foreach(var v in mesh.vertices)min=Mathf.Min(min,r.transform.TransformPoint(v).y);UnityEngine.Object.Destroy(mesh);}float floor=player.transform.TransformPoint(capsule.center).y-capsule.height*.5f;return min-floor;}
    static void DeathTrace(string from,float elapsed){if(elapsed<nextTrace)return;nextTrace+=.15f;trace.Add(Newtonsoft.Json.JsonConvert.SerializeObject(new{from,elapsed,state=animator.GetCurrentAnimatorStateInfo(0).fullPathHash,normalizedTime=animator.GetCurrentAnimatorStateInfo(0).normalizedTime,headHeight=HeadHeight(),hips=animator.GetBoneTransform(HumanBodyBones.Hips).position.ToString("F5"),root=player.transform.position.ToString("F5")}));}
    static void Portrait(string name){var go=new GameObject("Death QA camera");var c=go.AddComponent<Camera>();c.CopyFrom(Camera.main);c.transform.position=player.transform.position+new Vector3(2.7f,1.4f,3.1f);c.transform.LookAt(player.transform.position+Vector3.down*.55f);var lg=new GameObject("QA pose inspection light");var l=lg.AddComponent<Light>();l.type=LightType.Directional;l.intensity=2.5f;lg.transform.rotation=Quaternion.Euler(50,30,0);SurvivalUpgradeTools.Render(c,Path.Combine(Folder,name));UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(lg);}
    static void Fail(Exception e){errors?.Add(e.ToString());Finish();}
    static void Finish(){if(!active)return;active=false;report["audioListeners"]=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Count(l=>l.enabled);report["audioListenerVolume"]=AudioListener.volume;report["audioListenerPaused"]=AudioListener.pause;report["dspTime"]=AudioSettings.dspTime;report["errors"]=errors.ToArray();File.WriteAllText(Path.Combine(Folder,"play-test.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));File.WriteAllLines(Path.Combine(Folder,"death-trace.jsonl"),trace);Application.logMessageReceived-=Log;InputSystem.onAfterUpdate-=Current;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);previousKeyboard?.MakeCurrent();previousMouse?.MakeCurrent();InputSystem.settings.editorInputBehaviorInPlayMode=oldBehavior;Application.runInBackground=oldBackground;EditorUtility.audioMasterMute=oldMute;EditorApplication.isPlaying=false;}
}
