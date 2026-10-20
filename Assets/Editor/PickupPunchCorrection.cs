using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Praktikum5.FSM;
using UTS.Inventory;
using UTS.Audio;

[InitializeOnLoad]
public static class PickupPunchCorrection
{
    const string Root="Assets/SurvivalUpgrade";
    static string Folder=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_PickupPunch");
    static string last=SessionState.GetString("ZU.PickupPunch.Command","");
    [Serializable] class Command{public string op,nonce;}
    static bool active,oldBackground;
    static float start;
    static int punches;
    static bool airVoiceObserved;
    static double editorStart;
    static SimplePlayerController player;
    static SurvivalAudioDirector audio;
    static Dictionary<ScavengeItemPickup,Sample> samples;
    static List<string> errors;
    class Sample{public float minY=float.PositiveInfinity,maxY=float.NegativeInfinity,rotation;public Quaternion previous;public Vector3 scale;public string name;}
    static PickupPunchCorrection(){Directory.CreateDirectory(Folder);EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Mode;}
    static void Tick(){
        if(active){TestTick();return;}
        var file=Path.Combine(Folder,"command.json");if(!File.Exists(file)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        var text=File.ReadAllText(file);if(text==last)return;last=text;SessionState.SetString("ZU.PickupPunch.Command",text);
        try{var c=JsonUtility.FromJson<Command>(text);if(c.op=="apply-and-test"){Apply();SessionState.SetBool("ZU.PickupPunch.QA",true);EditorApplication.isPlaying=true;}}
        catch(Exception e){File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString());Debug.LogException(e);}
    }
    static void Enable(ScavengeItemPickup pickup){var so=new SerializedObject(pickup);so.FindProperty("enableAnimation").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pickup);}
    static void Apply(){
        var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.path!="Assets/Scenes/UTS.unity")throw new Exception("Expected current UTS scene in Edit mode");
        var backup=File.ReadAllText(Path.Combine(Folder,"backup.txt"));EditorSceneManager.SaveScene(scene,backup+"/UTS_BeforePickupPunch.unity",true);
        var library=AssetDatabase.LoadAssetAtPath<SurvivalSoundLibrary>(Root+"/Audio/SurvivalSoundLibrary.asset");var cue=library.Get(SfxCue.PunchSwing);
        if(!cue.clips.All(c=>c&&(c.name=="knifeSlice"||c.name=="knifeSlice2")))throw new Exception("PunchSwing changed since inspection; inspect current clips before replacing");
        var clips=new AudioClip[2];for(int i=0;i<2;i++){
            var path=Root+"/Audio/PunchSwing/fist_air_whoosh_"+(i+1)+".wav";var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.SaveAndReimport();clips[i]=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(!clips[i])throw new Exception("Missing whoosh "+path);
        }
        cue.clips=clips;EditorUtility.SetDirty(library);
        foreach(var name in new[]{"Bandage","FirstAidKit","Medkit"}){
            var path=Root+"/Items/Prefabs/"+name+".prefab";var go=PrefabUtility.LoadPrefabContents(path);try{Enable(go.GetComponent<ScavengeItemPickup>());PrefabUtility.SaveAsPrefabAsset(go,path);}finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        foreach(var pickup in UnityEngine.Object.FindObjectsByType<ScavengeItemPickup>(FindObjectsInactive.Include))if(pickup.UsesModelVisuals)Enable(pickup);
        AssetDatabase.SaveAssets();if(scene.isDirty)EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Path.Combine(Folder,"applied.txt"),"PunchSwing now uses fist_air_whoosh_1/2. Item model prefabs animate again; original spawn position/elevation restored; original 60 deg/s and .12 m / 2.5 bob settings retained. Other sound cues and scene placement preserved.");
    }
    static void Mode(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("ZU.PickupPunch.QA",false))return;SessionState.SetBool("ZU.PickupPunch.QA",false);
        active=true;start=Time.time;editorStart=EditorApplication.timeSinceStartup;punches=0;airVoiceObserved=false;oldBackground=Application.runInBackground;Application.runInBackground=true;samples=new Dictionary<ScavengeItemPickup,Sample>();errors=new List<string>();Application.logMessageReceived+=Log;player=UnityEngine.Object.FindAnyObjectByType<SimplePlayerController>();audio=UnityEngine.Object.FindAnyObjectByType<SurvivalAudioDirector>();
    }
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
    static void TestTick(){try{
        EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying)throw new Exception("Play test interrupted");if(EditorApplication.timeSinceStartup-editorStart>30)throw new Exception("Test timeout");float elapsed=Time.time-start;
        foreach(var item in UnityEngine.Object.FindObjectsByType<ScavengeItemPickup>(FindObjectsInactive.Exclude)){
            if(!samples.TryGetValue(item,out var sample)){sample=new Sample{name=item.ItemType.ToString(),previous=item.transform.rotation,scale=item.transform.localScale};samples.Add(item,sample);}
            sample.minY=Mathf.Min(sample.minY,item.transform.position.y);sample.maxY=Mathf.Max(sample.maxY,item.transform.position.y);sample.rotation+=Quaternion.Angle(sample.previous,item.transform.rotation);sample.previous=item.transform.rotation;
        }
        if(elapsed>.6f&&punches==0){player.TryAttackEnemy();punches++;}else if(elapsed>1.9f&&punches==1){player.TryAttackEnemy();punches++;}
        airVoiceObserved|=audio.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying&&s.clip&&s.clip.name.StartsWith("fist_air_whoosh_"));
        if(elapsed<3.3f)return;
        var baseline=typeof(ScavengeItemPickup).GetField("initialPosition",BindingFlags.NonPublic|BindingFlags.Instance);
        var records=samples.Where(kv=>kv.Key).Select(kv=>new{name=kv.Value.name,yTravel=kv.Value.maxY-kv.Value.minY,rotationDegrees=kv.Value.rotation,scaleUnchanged=kv.Key.transform.localScale==kv.Value.scale,usesTexturedModel=kv.Key.UsesModelVisuals&&kv.Key.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterial&&r.sharedMaterial.mainTexture),spawnY=((Vector3)baseline.GetValue(kv.Key)).y}).ToArray();
        var spawner=UnityEngine.Object.FindAnyObjectByType<ItemSpawner>();var so=new SerializedObject(spawner);float expectedY=so.FindProperty("areaCenter").vector3Value.y+so.FindProperty("itemElevation").floatValue;
        var library=AssetDatabase.LoadAssetAtPath<SurvivalSoundLibrary>(Root+"/Audio/SurvivalSoundLibrary.asset");var names=library.Get(SfxCue.PunchSwing).clips.Select(c=>c.name).ToArray();
        var result=new{items=records,allThreeTypes=records.Select(r=>r.name).Distinct().Count()==3,allItemsRotate=records.Length>0&&records.All(r=>r.rotationDegrees>150),allItemsBob=records.Length>0&&records.All(r=>r.yTravel>.20f&&r.yTravel<.25f),modelScalesAndTexturesPreserved=records.All(r=>r.scaleUnchanged&&r.usesTexturedModel),originalSpawnElevation=records.All(r=>Mathf.Abs(r.spawnY-expectedY)<.001f),expectedSpawnY=expectedY,punchSwingClips=names,noWeaponSliceAssigned=names.All(n=>n.StartsWith("fist_air_whoosh_")),punchSwingTriggered=audio.Count(SfxCue.PunchSwing)>=2,airWhooshActuallyPlaying=airVoiceObserved,errors=errors.ToArray()};
        File.WriteAllText(Path.Combine(Folder,"play-test.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));Finish();
    }catch(Exception e){errors.Add(e.ToString());File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString());Finish();}}
    static void Finish(){active=false;Application.logMessageReceived-=Log;Application.runInBackground=oldBackground;EditorApplication.isPlaying=false;}
}
