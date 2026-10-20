using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Praktikum5.FSM;
using UTS.Characters;
using UTS.Zombies;

public static class CharacterUpgradeSetup
{
    const string Base="Assets/CharacterUpgrade";
    static string Folder=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Characters");
    [Serializable] class SetupResult{public int agentType,navTriangles,roofModifiers;public string geometryBefore,geometryAfter;public bool mapTransformsUnchanged;public string playerController,zombieController,prefab;public Vector3 enemyBefore,enemyAfter;}
    static void Set(UnityEngine.Object target,string field,float value){var s=new SerializedObject(target);var p=s.FindProperty(field);if(p==null)throw new Exception("Missing field "+field);p.floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static AnimationClip Clip(string folder,string name){string path=Base+"/Animations/"+folder+"/"+name+".fbx";return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));}
    static void Transition(AnimatorState a,AnimatorState b,string parameter,bool value){var t=a.AddTransition(b);t.hasExitTime=false;t.duration=.16f;t.AddCondition(value?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,parameter);}
    static BlendTree Tree(AnimatorController controller,string name,params (AnimationClip,float)[] motions){var t=new BlendTree{name=name,blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(t,controller);foreach(var p in motions)t.AddChild(p.Item1,p.Item2);return t;}
    static AnimatorController Controller(bool player){
        string path=Base+"/Controllers/"+(player?"Player":"Zombie")+".controller";var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(existing)return existing;
        var c=AnimatorController.CreateAnimatorControllerAtPath(path);c.AddParameter("Speed",AnimatorControllerParameterType.Float);var sm=c.layers[0].stateMachine;
        var locomotion=sm.AddState(player?"Standing":"Locomotion");sm.defaultState=locomotion;
        if(player){
            c.AddParameter("Crouching",AnimatorControllerParameterType.Bool);c.AddParameter("Punch",AnimatorControllerParameterType.Trigger);
            locomotion.motion=Tree(c,"Standing blend",(Clip("Player","Idle"),0),(Clip("Player","Walking"),2.3f),(Clip("Player","Running"),5.2f));
            var crouch=sm.AddState("Crouching");crouch.motion=Tree(c,"Crouch blend",(Clip("Player","Crouching Idle"),0),(Clip("Player","Crouched Walking"),1.1f));Transition(locomotion,crouch,"Crouching",true);Transition(crouch,locomotion,"Crouching",false);
            var punch=sm.AddState("Punch");punch.motion=Clip("Player","Punching");var entry=sm.AddAnyStateTransition(punch);entry.hasExitTime=false;entry.duration=.08f;entry.canTransitionToSelf=false;entry.AddCondition(AnimatorConditionMode.If,0,"Punch");
            foreach(var target in new[]{locomotion,crouch}){var t=punch.AddTransition(target);t.hasExitTime=true;t.exitTime=.94f;t.duration=.08f;t.AddCondition(target==crouch?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Crouching");}
        }else{
            c.AddParameter("Attack",AnimatorControllerParameterType.Trigger);c.AddParameter("Dead",AnimatorControllerParameterType.Bool);
            locomotion.motion=Tree(c,"Zombie locomotion",(Clip("Zombie","zombie idle"),0),(Clip("Zombie","zombie walk"),.9f),(Clip("Zombie","zombie run"),2.3f));
            var attack=sm.AddState("Attack");attack.motion=Clip("Zombie","zombie attack");var t=sm.AddAnyStateTransition(attack);t.duration=.1f;t.hasExitTime=false;t.canTransitionToSelf=false;t.AddCondition(AnimatorConditionMode.If,0,"Attack");t.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");
            t=attack.AddTransition(locomotion);t.hasExitTime=true;t.exitTime=.94f;t.duration=.12f;
            var death=sm.AddState("Death");death.motion=Clip("Zombie","zombie death");t=sm.AddAnyStateTransition(death);t.duration=.12f;t.hasExitTime=false;t.canTransitionToSelf=false;t.AddCondition(AnimatorConditionMode.If,0,"Dead");
        }EditorUtility.SetDirty(c);return c;
    }
    static void Body(GameObject actor,bool player,AnimatorController controller){
        if(actor.GetComponent<HumanoidAnimationDriver>())throw new Exception("Character already upgraded: "+actor.name);
        foreach(var comp in actor.GetComponents<MonoBehaviour>().Where(c=>c&&(c.GetType().Name=="CharacterModelVisual"||c.GetType().Name=="CharacterVisualFSM")).ToArray())UnityEngine.Object.DestroyImmediate(comp);
        foreach(Transform child in actor.transform.Cast<Transform>().Where(t=>new[]{"YBot_Visual","FSM_YBot_Visual","Walking_Visual"}.Contains(t.name)).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"/Models/Humanoid.fbx");var avatar=AssetDatabase.LoadAllAssetsAtPath(Base+"/Models/Humanoid.fbx").OfType<Avatar>().FirstOrDefault();if(!avatar||!avatar.isValid||!avatar.isHuman)throw new Exception("Body humanoid Avatar invalid");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(model);go.name="Character Model";go.transform.SetParent(actor.transform,false);var animation=go.GetComponent<Animation>();if(animation)UnityEngine.Object.DestroyImmediate(animation);
        var a=go.GetComponent<Animator>();if(!a)a=go.AddComponent<Animator>();a.avatar=avatar;a.runtimeAnimatorController=controller;a.applyRootMotion=false;
        var idle=Clip(player?"Player":"Zombie",player?"Idle":"zombie idle");idle.SampleAnimation(go,0);
        var renderers=go.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=1.85f/bounds.size.y;go.transform.localScale=Vector3.one*scale;go.transform.localPosition=new Vector3(0,-1-bounds.min.y*scale+actor.transform.position.y*scale,0);
        // Bounds above are world-space; compute foot placement in the actor frame after rescaling.
        Physics.SyncTransforms();bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);go.transform.position+=Vector3.up*(actor.transform.position.y-1-bounds.min.y);
        string matPath=Base+"/Materials/"+(player?"Player":"Zombie")+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=player?"Player clothing":"Zombie skin"};mat.color=player?new Color(.22f,.36f,.48f):new Color(.37f,.42f,.29f);mat.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(mat,matPath);}foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
        foreach(var r in actor.GetComponents<Renderer>())r.enabled=false;var driver=actor.AddComponent<HumanoidAnimationDriver>();Ref(driver,"animator",a);
    }
    static int AgentType(){
        const string name="ZombieUnity Human";int id=-1;for(int i=0;i<NavMesh.GetSettingsCount();i++){var st=NavMesh.GetSettingsByIndex(i);if(NavMesh.GetSettingsNameFromID(st.agentTypeID)==name)id=st.agentTypeID;}if(id<0)id=NavMesh.CreateSettings().agentTypeID;
        var settings=new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings"));var list=settings.FindProperty("m_Settings");var names=settings.FindProperty("m_SettingNames");for(int i=0;i<list.arraySize;i++){var p=list.GetArrayElementAtIndex(i);if(p.FindPropertyRelative("agentTypeID").intValue!=id)continue;p.FindPropertyRelative("agentRadius").floatValue=.3f;p.FindPropertyRelative("agentHeight").floatValue=1.85f;p.FindPropertyRelative("agentClimb").floatValue=.3f;p.FindPropertyRelative("agentSlope").floatValue=45;names.GetArrayElementAtIndex(i).stringValue=name;}settings.ApplyModifiedPropertiesWithoutUndo();return id;
    }
    static string Hash(GameObject root){var b=new StringBuilder();foreach(var t in root.GetComponentsInChildren<Transform>(true)){b.Append(t.name).Append(t.position.ToString("F6")).Append(t.rotation.ToString("F6")).Append(t.lossyScale.ToString("F6"));}using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()))).Replace("-","");}
    static void ConfigureZombie(GameObject go,int type,AnimatorController controller){
        Body(go,false,controller);var a=go.GetComponent<NavMeshAgent>();a.agentTypeID=type;a.radius=.3f;a.height=1.85f;a.baseOffset=1;
        var c=go.GetComponent<CapsuleCollider>();if(c){c.radius=.3f;c.height=1.85f;c.center=new Vector3(0,-.075f,0);}
        var z=go.GetComponent<ZombieSwarmAgent>();if(!z)z=go.AddComponent<ZombieSwarmAgent>();Set(z,"eyeHeight",.6f);Set(z,"targetChestHeight",.1f);Set(z,"targetHeadHeight",.65f);Set(z,"attackCooldown",2.7f);Set(z,"attackImpactDelay",.65f);
    }
    public static void FinalizeChanges(){
        var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.path!="Assets/Scenes/UTS.unity")throw new Exception("Expected UTS in Edit mode");
        var map=scene.GetRootGameObjects().Single(g=>g.name=="ZombieUnity - Migrated Map");var previous=JsonUtility.FromJson<SetupResult>(File.ReadAllText(Path.Combine(Folder,"setup.json")));if(Hash(map)!=previous.geometryBefore)throw new Exception("Map transform fingerprint changed");
        CharacterUpgradeNavigationQA.Audit();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText(Path.Combine(Folder,"final-saved.txt"),scene.path+" | map transforms unchanged | nav triangles "+NavMesh.CalculateTriangulation().indices.Length/3);
    }
    public static void AlignCrouch(){
        var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.path!="Assets/Scenes/UTS.unity")throw new Exception("Expected UTS in Edit mode");
        var player=GameObject.Find("Player");Set(player.GetComponent<SimplePlayerController>(),"crouchHeight",1.4f);
        var animator=player.GetComponentInChildren<Animator>();if(!animator.GetComponent<CrouchPoseAlignment>())animator.gameObject.AddComponent<CrouchPoseAlignment>();
        var c=animator.runtimeAnimatorController as AnimatorController;var layers=c.layers;layers[0].iKPass=true;c.layers=layers;EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Path.Combine(Folder,"crouch-aligned.txt"),"Crouch capsule 1.4m; animated foot paths preserved with humanoid IK");
    }
    [MenuItem("Tools/ZombieUnity/Configure character animations and navigation")]
    public static void Setup(){
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/UTS.unity"||Application.isPlaying)throw new Exception("Expected UTS in Edit mode");if(File.Exists(Path.Combine(Folder,"setup.json")))throw new Exception("Already configured; inspect existing objects instead of regenerating");
        foreach(var f in new[]{"Controllers","Materials","Prefabs","Navigation","Reports"})Directory.CreateDirectory(Base+"/"+f);
        var map=scene.GetRootGameObjects().Single(g=>g.name=="ZombieUnity - Migrated Map");var result=new SetupResult{geometryBefore=Hash(map)};int type=AgentType();result.agentType=type;
        var player=scene.GetRootGameObjects().Single(g=>g.name=="Player");var enemy=scene.GetRootGameObjects().Single(g=>g.name=="Enemy");result.enemyBefore=enemy.transform.position;
        var pc=Controller(true);var zc=Controller(false);Body(player,true,pc);var playerController=player.GetComponent<SimplePlayerController>();Set(playerController,"walkSpeed",2.3f);Set(playerController,"runSpeed",5.2f);Set(playerController,"sneakSpeed",1.1f);Set(playerController,"aimWalkSpeed",1.7f);Set(playerController,"attackDistance",1.6f);
        var ignore=player.GetComponent<NavMeshModifier>();if(!ignore)ignore=player.AddComponent<NavMeshModifier>();ignore.ignoreFromBuild=true;
        ConfigureZombie(enemy,type,zc);
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Scenes/UTS/Enemy.prefab");try{ConfigureZombie(prefab,type,zc);result.prefab=Base+"/Prefabs/Zombie.prefab";PrefabUtility.SaveAsPrefabAsset(prefab,result.prefab);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var manager=scene.GetRootGameObjects().Single(g=>g.name=="ZombieHordeManager").GetComponent<ZombieHordeManager>();Ref(manager,"zombiePrefab",AssetDatabase.LoadAssetAtPath<GameObject>(result.prefab));Set(manager,"navMeshSampleDistance",1.5f);
        var fortress=map.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="FortressCamp");Ref(manager,"protectedSpawnCenter",fortress);Set(manager,"protectedSpawnRadius",20);
        var so=new SerializedObject(manager);var zones=so.FindProperty("spawnZones");zones.arraySize=7;var centers=new[]{new Vector3(-64,0,-20),new Vector3(-20,0,-42),new Vector3(64,0,-22),new Vector3(10,0,-18),new Vector3(17,0,25),new Vector3(-60,0,26),new Vector3(90,0,20)};var extents=new[]{new Vector3(18,1,18),new Vector3(22,1,12),new Vector3(18,1,16),new Vector3(25,1,10),new Vector3(18,1,15),new Vector3(20,1,16),new Vector3(12,1,18)};var counts=new[]{4,5,5,6,5,4,3};var names=new[]{"West homes","South homes","East homes","Commercial strip","Civic and park","Northwest homes","East outskirts"};for(int i=0;i<7;i++){var z=zones.GetArrayElementAtIndex(i);z.FindPropertyRelative("zoneName").stringValue=names[i];z.FindPropertyRelative("center").vector3Value=centers[i];z.FindPropertyRelative("extents").vector3Value=extents[i];z.FindPropertyRelative("zombieCount").intValue=counts[i];}so.ApplyModifiedPropertiesWithoutUndo();
        var roots=new HashSet<Transform>();foreach(var c in map.GetComponentsInChildren<Collider>()){var t=c.name.StartsWith("Mesh - ZU_")?c.transform.parent:c.transform;string n=t.name.ToLowerInvariant();if(n.Contains("roof")||n.Contains("ceiling")||n.Contains("canopy"))roots.Add(t);}foreach(var t in roots){var mod=t.GetComponent<NavMeshModifier>();if(!mod)mod=t.gameObject.AddComponent<NavMeshModifier>();mod.overrideArea=true;mod.area=1;result.roofModifiers++;}
        var nav=new GameObject("Gameplay Navigation");var surface=nav.AddComponent<NavMeshSurface>();surface.agentTypeID=type;surface.collectObjects=CollectObjects.Volume;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.08f;surface.overrideTileSize=true;surface.tileSize=128;surface.minRegionArea=2;
        var grade=map.GetComponentsInChildren<Collider>().First(c=>c.name=="Continuous grade").bounds;surface.center=new Vector3(grade.center.x,5,grade.center.z);surface.size=new Vector3(grade.size.x,14,grade.size.z);Physics.SyncTransforms();surface.BuildNavMesh();if(!surface.navMeshData)throw new Exception("NavMesh build produced no data");AssetDatabase.CreateAsset(surface.navMeshData,Base+"/Navigation/TownNavMesh.asset");
        var agent=enemy.GetComponent<NavMeshAgent>();var query=new NavMeshQueryFilter{agentTypeID=type,areaMask=NavMesh.AllAreas};if(!NavMesh.SamplePosition(enemy.transform.position,out var hit,3,query))throw new Exception("Scene enemy is not near the rebuilt NavMesh");agent.enabled=false;enemy.transform.position=hit.position+Vector3.up*agent.baseOffset;agent.enabled=true;result.enemyAfter=enemy.transform.position;
        result.navTriangles=NavMesh.CalculateTriangulation().indices.Length/3;result.geometryAfter=Hash(map);result.mapTransformsUnchanged=result.geometryBefore==result.geometryAfter;if(!result.mapTransformsUnchanged)throw new Exception("Map transforms unexpectedly changed");result.playerController=AssetDatabase.GetAssetPath(pc);result.zombieController=AssetDatabase.GetAssetPath(zc);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Path.Combine(Folder,"setup.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Base+"/Reports/Setup.json",JsonUtility.ToJson(result,true));
    }
}
