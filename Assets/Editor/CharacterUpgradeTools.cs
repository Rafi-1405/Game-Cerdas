using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CharacterUpgradeTools
{
    const string Base="Assets/CharacterUpgrade";
    static string Folder=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Characters");
    [Serializable] class Command{public string op,nonce;}
    static string last=SessionState.GetString("ZU.Characters.Command","");
    static CharacterUpgradeTools(){EditorApplication.update+=Tick;}
    static void Tick(){
        var f=Path.Combine(Folder,"command.json");if(!File.Exists(f)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        var txt=File.ReadAllText(f);if(txt==last)return;last=txt;SessionState.SetString("ZU.Characters.Command",txt);
        try{var c=JsonUtility.FromJson<Command>(txt);if(c.op=="inspect")Inspect();else if(c.op=="backup")Backup();else if(c.op=="imports")ConfigureImports();else if(c.op=="setup")CharacterUpgradeSetup.Setup();else if(c.op=="nav-audit")CharacterUpgradeNavigationQA.Audit();else if(c.op=="root-motion")NormalizeRootMotion();else if(c.op=="align-crouch")CharacterUpgradeSetup.AlignCrouch();else if(c.op=="observe-npcs")CharacterUpgradePlayQA.ObserveNPCs();else if(c.op=="finalize")CharacterUpgradeSetup.FinalizeChanges();else if(c.op=="play-test")CharacterUpgradePlayQA.Begin();}
        catch(Exception e){File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString());Debug.LogException(e);}
    }
    static string Hier(Transform t)=>t.parent?Hier(t.parent)+"/"+t.name:t.name;
    static void Write(string name,object data)=>File.WriteAllText(Path.Combine(Folder,name),JsonUtility.ToJson(data,true));
    [Serializable] class Obj{public string path;public Vector3 position,scale;public string[] components,visualChildren;public bool active;public float radius,height,baseOffset;public bool onNavMesh;}
    [Serializable] class Surface{public string path,data,collect,use;public bool active;public int agentType;}
    [Serializable] class Model{public string path,animationType;public int meshes,bones;public bool avatarValid,human;public string[] clips;}
    [Serializable] class Inspection{public string scene;public bool playing,dirty;public int triangles;public Obj[] characters;public Surface[] surfaces;public Model[] models;}
    static void Inspect(){
        var s=SceneManager.GetActiveScene();var r=new Inspection{scene=s.path,playing=Application.isPlaying,dirty=s.isDirty,triangles=NavMesh.CalculateTriangulation().indices.Length/3};
        r.characters=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.GetComponent<NavMeshAgent>()||t.GetComponent<Praktikum5.FSM.SimplePlayerController>()).Select(t=>{var a=t.GetComponent<NavMeshAgent>();return new Obj{path=Hier(t),position=t.position,scale=t.localScale,active=t.gameObject.activeInHierarchy,components=t.GetComponents<Component>().Select(x=>x?x.GetType().FullName:"Missing script").ToArray(),visualChildren=t.Cast<Transform>().Select(x=>x.name).ToArray(),radius=a?a.radius:0,height=a?a.height:0,baseOffset=a?a.baseOffset:0,onNavMesh=a&&a.enabled&&a.isOnNavMesh};}).ToArray();
        r.surfaces=UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(n=>new Surface{path=Hier(n.transform),data=AssetDatabase.GetAssetPath(n.navMeshData),active=n.isActiveAndEnabled,agentType=n.agentTypeID,collect=n.collectObjects.ToString(),use=n.useGeometry.ToString()}).ToArray();
        var paths=new[]{"Assets/Resources/Y Bot.fbx","Assets/Resources/Walking.fbx"}.Concat(AssetDatabase.FindAssets("t:Model",new[]{Base+"/Animations"}).Select(AssetDatabase.GUIDToAssetPath));
        r.models=paths.Select(p=>{var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);var mi=AssetImporter.GetAtPath(p)as ModelImporter;var av=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Avatar>().FirstOrDefault();return new Model{path=p,animationType=mi?mi.animationType.ToString():"none",meshes=go?go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length:0,bones=go?go.GetComponentsInChildren<Transform>(true).Length:0,avatarValid=av&&av.isValid,human=av&&av.isHuman,clips=AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).Select(c=>c.name+" | "+c.length+"s | legacy="+c.legacy).ToArray()};}).ToArray();Write("inspect.json",r);
    }
    static void ConfigureImports(){
        var files=new[]{Base+"/Models/Humanoid.fbx"}.Concat(AssetDatabase.FindAssets("t:Model",new[]{Base+"/Animations"}).Select(AssetDatabase.GUIDToAssetPath)).ToArray();
        foreach(string path in files){var mi=AssetImporter.GetAtPath(path)as ModelImporter;if(!mi)throw new Exception("Model not imported: "+path);mi.animationType=ModelImporterAnimationType.Human;mi.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;mi.importCameras=false;mi.importLights=false;mi.optimizeGameObjects=false;
            var clips=mi.defaultClipAnimations;foreach(var c in clips){c.name=Path.GetFileNameWithoutExtension(path);c.loopTime=!path.Contains("Punching")&&!path.Contains("attack")&&!path.Contains("death")&&!path.Contains("Humanoid");c.loopPose=c.loopTime;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionXZ=true;c.keepOriginalPositionY=true;}mi.clipAnimations=clips;mi.SaveAndReimport();
        }Inspect();File.WriteAllText(Path.Combine(Folder,"imports-complete.txt"),"Humanoid rigs imported");
    }
    static void NormalizeRootMotion(){
        var paths=AssetDatabase.FindAssets("t:Model",new[]{Base+"/Animations"}).Select(AssetDatabase.GUIDToAssetPath);
        foreach(string path in paths){var mi=(ModelImporter)AssetImporter.GetAtPath(path);var clips=mi.clipAnimations;foreach(var c in clips){
            bool locomotion=path.Contains("Walking")||path.Contains("Running")||path.Contains("zombie walk")||path.Contains("zombie run");
            c.lockRootPositionXZ=!locomotion;c.keepOriginalPositionXZ=false;c.keepOriginalPositionY=false;c.heightFromFeet=true;c.keepOriginalOrientation=false;
        }mi.clipAnimations=clips;mi.SaveAndReimport();}
        File.WriteAllText(Path.Combine(Folder,"root-motion-normalized.txt"),"Locomotion root trajectory extracted; controller/agent retains position authority");
    }
    static void Backup(){var s=SceneManager.GetActiveScene();if(Application.isPlaying||s.path!="Assets/Scenes/UTS.unity")throw new Exception("Expected UTS in Edit mode");Directory.CreateDirectory(Base+"/Backups");string f=Base+"/Backups/UTS_BeforeCharacterUpgrade_20261020.unity";if(!File.Exists(f))EditorSceneManager.SaveScene(s,f,true);Write("backup.json",new BackupResult{path=f});}
    [Serializable] class BackupResult{public string path;}
}
