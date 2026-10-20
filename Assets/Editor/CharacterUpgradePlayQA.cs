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
using UTS.Zombies;
using UTS.Characters;

[InitializeOnLoad]
public static class CharacterUpgradePlayQA
{
    static string Folder=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Characters");
    [Serializable] class Sample{public string name,state;public float speed,height,health;public bool crouching;public string[] clips;public Vector3 head;}
    [Serializable] class Result{public int spawned,onNavMesh,naturallyMoved;public Sample[] samples;public bool walk,run,crouchIdle,crouchWalk,crouchFitsCollider,crouchHeadroomBlocked,stand,punchAnimation,punchHit,punchOccluded,chase,attack,attackHit,death;public float chaseDistance,punchDamage,sceneEnemyMovedDistance;public string[] errors;}
    static Result result;
    static List<Sample> samples;
    static List<string> errors;
    static Dictionary<ZombieSwarmAgent,Vector3> initial;
    static Keyboard keys,previousKeys;
    static Mouse mouse,previousMouse;
    static SimplePlayerController player;
    static CharacterController capsule;
    static Animator animator;
    static ZombieSwarmAgent dummy;
    static EnemyFSM dummyHealth;
    static NavMeshAgent dummyAgent;
    static float phaseStart,runStart,healthBefore,playerHealthBefore;
    static int phase;
    static bool active,captured,npcOnly;
    static Vector3 playerHome,chaseStart;
    static GameObject wall,ceiling;
    static InputSettings.EditorInputBehaviorInPlayMode oldInputBehavior;
    static CharacterUpgradePlayQA(){EditorApplication.playModeStateChanged+=Mode;EditorApplication.update+=Tick;}
    public static void Begin(){if(EditorApplication.isPlaying)throw new Exception("Start test in Edit mode");UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());SessionState.SetBool("ZU.Characters.TestPending",true);EditorApplication.isPlaying=true;}
    public static void ObserveNPCs(){SessionState.SetBool("ZU.Characters.NPCOnly",true);Begin();}
    static void Mode(PlayModeStateChange s){if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("ZU.Characters.TestPending",false))return;SessionState.SetBool("ZU.Characters.TestPending",false);try{
        npcOnly=SessionState.GetBool("ZU.Characters.NPCOnly",false);SessionState.SetBool("ZU.Characters.NPCOnly",false);result=new Result();samples=new List<Sample>();errors=new List<string>();initial=new Dictionary<ZombieSwarmAgent,Vector3>();phase=0;phaseStart=runStart=Time.time;active=true;captured=false;
        Application.logMessageReceived+=Log;player=UnityEngine.Object.FindAnyObjectByType<SimplePlayerController>();capsule=player.GetComponent<CharacterController>();animator=player.GetComponent<HumanoidAnimationDriver>().Animator;
        previousKeys=Keyboard.current;previousMouse=Mouse.current;oldInputBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keys=InputSystem.AddDevice<Keyboard>("Character QA Keyboard");mouse=InputSystem.AddDevice<Mouse>("Character QA Mouse");
        var cam=Camera.main;var orbit=cam?cam.GetComponent<GTACameraController>():null;if(orbit)orbit.enabled=false;if(cam)cam.transform.rotation=Quaternion.Euler(12,0,0);
    }catch(Exception e){Fail(e);}}
    static void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)errors?.Add(message+" "+stack.Split('\n')[0]);}
    static void Input(params Key[] held){keys.MakeCurrent();mouse.MakeCurrent();InputSystem.QueueStateEvent(keys,new KeyboardState(held));InputSystem.QueueStateEvent(mouse,new MouseState());Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    static void Next(){phase++;phaseStart=Time.time;captured=false;}
    static Sample Capture(string name){var info=animator.GetCurrentAnimatorStateInfo(0);var head=animator.GetBoneTransform(HumanBodyBones.Head);var s=new Sample{name=name,state=info.IsName("Punch")?"Punch":info.IsName("Crouching")?"Crouching":"Standing",speed=player.CurrentSpeed,height=capsule.height,crouching=player.IsCrouching,health=player.CurrentHealth,clips=animator.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name).ToArray(),head=head?head.position:Vector3.zero};samples.Add(s);Shot(name,player.transform);return s;}
    static void Tick(){if(!active||!EditorApplication.isPlaying||EditorApplication.isPaused)return;try{
        if(Time.time-runStart>90)throw new Exception("Runtime verification timed out in phase "+phase);
        float elapsed=Time.time-phaseStart;
        if(phase==2)Input(Key.W);else if(phase==3)Input(Key.W,Key.LeftShift);else if(phase==4)Input(Key.LeftCtrl);else if(phase==5)Input(Key.W,Key.LeftCtrl);else if((phase==7||phase==8)&&elapsed<.15f)Input(Key.Space);else Input();
        if(phase==0){foreach(var z in ZombieSwarmAgent.ActiveAgents.ToArray())if(z&&!initial.ContainsKey(z))initial[z]=z.transform.position;if(elapsed<(npcOnly?15:9))return;
            var zs=ZombieSwarmAgent.ActiveAgents.Where(z=>z).ToArray();result.spawned=zs.Length;result.onNavMesh=zs.Count(z=>{var a=z.GetComponent<NavMeshAgent>();return a.enabled&&a.isOnNavMesh;});result.naturallyMoved=zs.Count(z=>initial.TryGetValue(z,out var p)&&Vector3.Distance(p,z.transform.position)>.3f);
            var sceneEnemy=GameObject.Find("Enemy").GetComponent<ZombieSwarmAgent>();if(initial.TryGetValue(sceneEnemy,out var original))result.sceneEnemyMovedDistance=Vector3.Distance(original,sceneEnemy.transform.position);if(npcOnly){Finish();return;}
            foreach(var z in zs){z.enabled=false;var a=z.GetComponent<NavMeshAgent>();if(a.enabled&&a.isOnNavMesh)a.isStopped=true;}
            var surface=UnityEngine.Object.FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();var q=new NavMeshQueryFilter{agentTypeID=surface.agentTypeID,areaMask=NavMesh.AllAreas};if(!NavMesh.SamplePosition(new Vector3(5.6f,0,0),out var hit,2,q))throw new Exception("No NavMesh at test street");playerHome=hit.position+Vector3.up*1.03f;capsule.enabled=false;player.transform.SetPositionAndRotation(playerHome,Quaternion.identity);capsule.enabled=true;
            var pf=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CharacterUpgrade/Prefabs/Zombie.prefab");var go=UnityEngine.Object.Instantiate(pf,playerHome+Vector3.forward*20,Quaternion.Euler(0,180,0));dummy=go.GetComponent<ZombieSwarmAgent>();dummy.enabled=false;dummyHealth=go.GetComponent<EnemyFSM>();dummyAgent=go.GetComponent<NavMeshAgent>();if(dummyAgent.isOnNavMesh)dummyAgent.isStopped=true;healthBefore=dummyHealth.CurrentHealth;Next();return;
        }
        if(phase==1){if(elapsed<.7f)return;Capture("player_idle");Next();return;}
        if(phase==2){if(elapsed<1.3f)return;var s=Capture("player_walk");result.walk=s.speed>1&&s.clips.Contains("Walking");Next();return;}
        if(phase==3){if(elapsed<1.2f)return;var s=Capture("player_run");result.run=s.speed>4&&s.clips.Contains("Running");Next();return;}
        if(phase==4){if(elapsed<.8f)return;var s=Capture("player_crouch_idle");result.crouchIdle=s.crouching&&s.height<1.5f&&s.clips.Contains("Crouching Idle");Next();return;}
        if(phase==5){if(elapsed<1.1f)return;var s=Capture("player_crouch_walk");result.crouchWalk=s.crouching&&s.speed>0&&s.speed<1.5f&&s.clips.Contains("Crouched Walking");result.crouchFitsCollider=s.head.y+.1f<=player.transform.TransformPoint(capsule.center).y+capsule.height*.5f+.06f;
            ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.name="QA temporary low ceiling";ceiling.transform.position=player.transform.position+Vector3.up*.6f;ceiling.transform.localScale=new Vector3(2,.12f,2);Physics.SyncTransforms();Next();return;}
        if(phase==6){if(elapsed<.6f)return;result.crouchHeadroomBlocked=player.IsCrouching&&capsule.height<1.5f;Capture("player_crouch_under_ceiling");UnityEngine.Object.Destroy(ceiling);phase=16;phaseStart=Time.time;return;}
        if(phase==16){if(elapsed<.6f)return;var s=Capture("player_stand");result.stand=!s.crouching&&s.height>1.9f;var p=player.transform.position+player.transform.forward*1.2f;dummyAgent.Warp(p);dummyAgent.isStopped=true;healthBefore=dummyHealth.CurrentHealth;phase=7;phaseStart=Time.time;captured=false;return;}
        if(phase==7){if(!captured&&elapsed>.5f){var s=Capture("player_punch");result.punchAnimation=s.state=="Punch";captured=true;}if(elapsed<1.5f)return;result.punchDamage=healthBefore-dummyHealth.CurrentHealth;result.punchHit=Mathf.Abs(result.punchDamage-20)<.1f;
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA temporary punch wall";wall.transform.position=player.transform.position+player.transform.forward*.65f;wall.transform.localScale=new Vector3(2,2,.12f);Physics.SyncTransforms();healthBefore=dummyHealth.CurrentHealth;Next();return;}
        if(phase==8){if(elapsed<1.5f)return;result.punchOccluded=Mathf.Abs(dummyHealth.CurrentHealth-healthBefore)<.01f;UnityEngine.Object.Destroy(wall);capsule.enabled=false;player.transform.SetPositionAndRotation(playerHome,Quaternion.identity);capsule.enabled=true;dummyAgent.Warp(playerHome+Vector3.forward*6);dummy.transform.rotation=Quaternion.Euler(0,180,0);dummy.enabled=true;dummy.ReceiveHuntOrder(12);chaseStart=dummy.transform.position;playerHealthBefore=player.CurrentHealth;Next();return;}
        if(phase==9){if(elapsed<1.6f)return;result.chaseDistance=Vector3.Distance(chaseStart,dummy.transform.position);result.chase=result.chaseDistance>.5f;Shot("zombie_chase",dummy.transform);Next();return;}
        if(phase==10){if(dummy.CurrentState==ZombieSwarmAgent.PZZombieState.Attacking&&!captured){result.attack|=dummy.GetComponent<HumanoidAnimationDriver>().Animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="zombie attack");if(elapsed>.7f&&result.attack){Shot("zombie_attack",dummy.transform);captured=true;}}if(elapsed<5)return;result.attackHit=player.CurrentHealth<playerHealthBefore;dummyHealth.TakeDamage(1000);Next();return;}
        if(phase==11){if(elapsed<2.95f)return;var a=dummy.GetComponent<HumanoidAnimationDriver>().Animator;result.death=a.GetCurrentAnimatorStateInfo(0).IsName("Death");Shot("zombie_death",dummy.transform);Finish();}
    }catch(Exception e){Fail(e);}}
    static void Shot(string name,Transform target){Directory.CreateDirectory(Path.Combine(Folder,"Previews"));var go=new GameObject("QA portrait camera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.fieldOfView=42;c.nearClipPlane=.03f;c.farClipPlane=400;c.transform.position=target.position+target.right*2.8f+target.forward*3.4f+Vector3.up*.6f;c.transform.LookAt(target.position+Vector3.up*-.05f);var rt=new RenderTexture(1000,900,24);c.targetTexture=rt;c.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1000,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1000,900),0,0);t.Apply();File.WriteAllBytes(Path.Combine(Folder,"Previews/"+name+".png"),t.EncodeToPNG());RenderTexture.active=prev;c.targetTexture=null;UnityEngine.Object.Destroy(t);rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);}
    static void Fail(Exception e){errors?.Add(e.ToString());Finish();}
    static void Finish(){active=false;Application.logMessageReceived-=Log;if(keys!=null)InputSystem.RemoveDevice(keys);if(mouse!=null)InputSystem.RemoveDevice(mouse);previousKeys?.MakeCurrent();previousMouse?.MakeCurrent();InputSystem.settings.editorInputBehaviorInPlayMode=oldInputBehavior;result.samples=samples?.ToArray();result.errors=errors?.ToArray();File.WriteAllText(Path.Combine(Folder,npcOnly?"npc-observation.json":"play-test.json"),JsonUtility.ToJson(result,true));EditorApplication.isPlaying=false;}
}
