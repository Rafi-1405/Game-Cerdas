using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public static class RobloxMigrationControl
{
    static string Folder => Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Migration");
    static string last=SessionState.GetString("ZombieUnity.LastMigrationCommand","");
    [Serializable] class Command { public string op; public string nonce; }
    [Serializable] class RootInfo { public string name; public bool active; public Vector3 position; public Vector3 scale; public Vector3 boundsCenter; public Vector3 boundsSize; }
    [Serializable] class Status { public string scene; public string path; public bool dirty; public bool playing; public RootInfo[] roots; public string pipeline; public string message; }
    static RobloxMigrationControl(){ EditorApplication.update+=Tick; EditorApplication.delayCall+=Probe; }
    static void Tick(){
        string p=Path.Combine(Folder,"command.json"); if(!File.Exists(p)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        string txt=File.ReadAllText(p); if(txt==last)return; last=txt;SessionState.SetString("ZombieUnity.LastMigrationCommand",txt);
        try {var c=JsonUtility.FromJson<Command>(txt); if(c.op=="probe")Probe(); else if(c.op=="backup")Backup(); else if(c.op=="save")SaveFinal(); else if(c.op=="import")ZombieUnityMapImporter.Import(); else if(c.op=="resume")ZombieUnityMapImporter.ResumeVerifiedImport(); else if(c.op=="supplement")ZombieUnityMapImporter.ImportSupplemental(); else if(c.op=="audit")ZombieUnityMigrationQA.AuditMap();else if(c.op=="finish")ZombieUnityMigrationPolish.Apply();else if(c.op=="render")ZombieUnityMigrationQA.Render();else if(c.op=="stop"){EditorApplication.isPlaying=false;EditorApplication.delayCall+=Probe;}}
        catch(Exception e){Directory.CreateDirectory(Folder); File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString()); Debug.LogException(e);}
    }
    static void Probe(){
        var s=SceneManager.GetActiveScene(); var status=new Status {scene=s.name,path=s.path,dirty=s.isDirty,playing=EditorApplication.isPlaying,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name:"BuiltIn"};
        status.roots=s.GetRootGameObjects().Select(g=>{var rs=g.GetComponentsInChildren<Renderer>(true);var b=new Bounds(g.transform.position,Vector3.zero);if(rs.Length>0){b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);}return new RootInfo{name=g.name,active=g.activeSelf,position=g.transform.position,scale=g.transform.localScale,boundsCenter=b.center,boundsSize=b.size};}).ToArray();
        Directory.CreateDirectory(Folder); File.WriteAllText(Path.Combine(Folder,"status.json"),JsonUtility.ToJson(status,true));
    }
    static void SaveFinal(){
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/UTS.unity"||EditorApplication.isPlaying)throw new Exception("Expected UTS in Edit mode");
        var root=scene.GetRootGameObjects().Single(g=>g.name=="ZombieUnity - Migrated Map");if(!root.activeSelf)throw new Exception("Map is inactive");
        Selection.activeGameObject=root;var view=SceneView.lastActiveSceneView;if(view)view.LookAt(new Vector3(0,0,15),Quaternion.Euler(55,35,0),85,false,true);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Probe();File.WriteAllText(Path.Combine(Folder,"final-saved.txt"),scene.path);
    }
    static void Backup(){
        Directory.CreateDirectory("Assets/RobloxMigration/Backups");
        var s=SceneManager.GetActiveScene();
        if(s.path!="Assets/Scenes/UTS.unity")throw new Exception("Active scene changed: "+s.path);
        string p="Assets/RobloxMigration/Backups/UTS_BeforeRoblox_20261020.unity";
        if(!File.Exists(p))EditorSceneManager.SaveScene(s,p,true);
        Probe(); File.WriteAllText(Path.Combine(Folder,"backup.txt"),p);
    }
}
