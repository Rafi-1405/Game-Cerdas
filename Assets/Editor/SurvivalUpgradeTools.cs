using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.Rendering;
using Praktikum5.FSM;
using UTS.Inventory;
using UTS.Characters;
using UTS.Audio;

[InitializeOnLoad]
public static class SurvivalUpgradeTools
{
    const string Root="Assets/SurvivalUpgrade";
    static string Folder=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Survival");
    static string last=SessionState.GetString("ZU.Survival.Command","");
    [Serializable] class Command{public string op,nonce;}
    static SurvivalUpgradeTools(){Directory.CreateDirectory(Folder);EditorApplication.update+=Tick;}
    static void Tick(){var f=Path.Combine(Folder,"command.json");if(!File.Exists(f)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;var text=File.ReadAllText(f);if(text==last)return;last=text;SessionState.SetString("ZU.Survival.Command",text);try{var c=JsonUtility.FromJson<Command>(text);if(c.op=="inspect")Inspect();else if(c.op=="apply")Apply();else if(c.op=="preview")Preview();else if(c.op=="tune-night")TuneNight();else if(c.op=="play-test")SurvivalUpgradeQA.Begin();else if(c.op=="stop-test")EditorApplication.isPlaying=false;else if(c.op=="refresh")AssetDatabase.Refresh();}catch(Exception e){File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString());Debug.LogException(e);}}
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    static void Inspect(){
        var s=SceneManager.GetActiveScene();var lines=new System.Collections.Generic.List<string>();
        lines.Add("Scene="+s.path+" playing="+Application.isPlaying+" dirty="+s.isDirty);
        if(!Application.isPlaying&&s.path=="Assets/Scenes/UTS.unity"){
            var backup=Root+"/Backups/Survival_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(s,backup+"/UTS_BeforeSurvival.unity",true);File.WriteAllText(Path.Combine(Folder,"backup.txt"),backup);
            var pc=UnityEngine.Object.FindAnyObjectByType<SimplePlayerController>();var a=pc.GetComponentInChildren<Animator>();File.Copy(AssetDatabase.GetAssetPath(a.runtimeAnimatorController),backup+"/Player_BeforeSurvival.controller");
            foreach(var file in new[]{"Assets/Scripts/UTS/Inventory/ScavengeItemPickup.cs","Assets/Scripts/UTS/Inventory/ItemSpawner.cs","Assets/Scripts/UTS/Characters/HumanoidAnimationDriver.cs","Assets/Scripts/UTS/Player/SimplePlayerController.cs"})File.Copy(file,backup+"/"+System.IO.Path.GetFileName(file)+".txt");
            lines.Add("Backup="+backup);
        }
        foreach(var item in UnityEngine.Object.FindObjectsByType<ScavengeItemPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None))lines.Add("ITEM "+PathOf(item.transform)+" type="+item.ItemType+" pos="+item.transform.position+" scale="+item.transform.lossyScale+" active="+item.gameObject.activeInHierarchy);
        foreach(var spawner in UnityEngine.Object.FindObjectsByType<ItemSpawner>(FindObjectsInactive.Include,FindObjectsSortMode.None))lines.Add("SPAWNER "+PathOf(spawner.transform)+" "+EditorJsonUtility.ToJson(spawner));
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))lines.Add("LIGHT "+PathOf(light.transform)+" type="+light.type+" intensity="+light.intensity+" color="+light.color+" range="+light.range);
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))lines.Add("CAMERA "+PathOf(camera.transform)+" clear="+camera.clearFlags+" position="+camera.transform.position+" euler="+camera.transform.eulerAngles);
        foreach(var audio in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))lines.Add("AUDIO "+PathOf(audio.transform)+" clip="+(audio.clip?audio.clip.name:"null"));
        lines.Add("SKY="+AssetDatabase.GetAssetPath(RenderSettings.skybox)+" ambient="+RenderSettings.ambientMode+" intensity="+RenderSettings.ambientIntensity+" fog="+RenderSettings.fog+" fogdensity="+RenderSettings.fogDensity);
        File.WriteAllLines(Path.Combine(Folder,"inspect.txt"),lines);AssetDatabase.Refresh();
    }
    [Serializable] class CueFile{public string cue;public float volume,pitchVariation;public bool spatial;public string[] clips;}
    [Serializable] class CueManifest{public CueFile[] cues;}
    static void SetObject(UnityEngine.Object target,string key,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(key).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);}
    static void Apply(){
        var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.path!="Assets/Scenes/UTS.unity")throw new Exception("Expected UTS in Edit mode");
        if(!File.Exists(Path.Combine(Folder,"backup.txt")))throw new Exception("Inspect and back up current scene first");
        if(UnityEngine.Object.FindAnyObjectByType<SurvivalAudioDirector>())throw new Exception("Already applied; inspect and patch instead of regenerating");
        var notes=new System.Collections.Generic.List<string>();
        var prefabs=new ScavengeItemPickup[3];var names=new[]{"Bandage","FirstAidKit","Medkit"};var textures=new[]{"Bandage1_diff","Firstaidkit1_diff","Medkit1_diff"};var widths=new[]{.18f,.30f,.38f};
        for(int i=0;i<names.Length;i++){
            string modelPath=Root+"/Items/Models/"+names[i]+".obj";var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);importer.globalScale=1;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
            string materialPath=Root+"/Items/Materials/"+names[i]+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,materialPath);}mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Items/Textures/"+textures[i]+".png"));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",i==0?.16f:.3f);mat.SetFloat("_Metallic",i==0?0:.1f);EditorUtility.SetDirty(mat);
            var go=new GameObject(names[i]+" Pickup");var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath),go.transform);visual.name="Textured "+names[i];
            foreach(var r in visual.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
            var bounds=BoundsOf(visual);visual.transform.localScale*=widths[i]/bounds.size.x;bounds=BoundsOf(visual);visual.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            bounds=BoundsOf(visual);var col=go.AddComponent<BoxCollider>();col.isTrigger=true;col.center=new Vector3(0,Mathf.Max(bounds.size.y*.5f,.14f),0);col.size=new Vector3(Mathf.Max(bounds.size.x+.18f,.42f),Mathf.Max(bounds.size.y+.12f,.3f),Mathf.Max(bounds.size.z+.18f,.42f));
            var pickup=go.AddComponent<ScavengeItemPickup>();var so=new SerializedObject(pickup);so.FindProperty("useModelVisuals").boolValue=true;so.FindProperty("enableAnimation").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();go.transform.localScale=Vector3.one;pickup.SetItemType((ScavengeItemType)i);
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Items/Prefabs/"+names[i]+".prefab");prefabs[i]=prefab.GetComponent<ScavengeItemPickup>();notes.Add(names[i]+" size="+bounds.size+" trigger="+col.size);UnityEngine.Object.DestroyImmediate(go);
        }
        foreach(var spawner in UnityEngine.Object.FindObjectsByType<ItemSpawner>(FindObjectsInactive.Exclude)){
            var so=new SerializedObject(spawner);var array=so.FindProperty("itemPrefabs");array.arraySize=3;for(int i=0;i<3;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=prefabs[i];so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(spawner);
        }
        // The inspected scene had no persistent pickups; only its runtime spawner.
        if(UnityEngine.Object.FindObjectsByType<ScavengeItemPickup>(FindObjectsInactive.Exclude).Length>0)throw new Exception("Persistent pickup placements changed since inspection; patch them individually");
        var player=UnityEngine.Object.FindAnyObjectByType<SimplePlayerController>();var animator=player.GetComponent<HumanoidAnimationDriver>().Animator;var controller=(AnimatorController)animator.runtimeAnimatorController;
        var death=AssetDatabase.LoadAllAssetsAtPath("Assets/CharacterUpgrade/Animations/Zombie/zombie death.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));if(death.isLooping)throw new Exception("Death clip must not loop");
        if(!controller.parameters.Any(x=>x.name=="Dead"))controller.AddParameter("Dead",AnimatorControllerParameterType.Bool);
        var sm=controller.layers[0].stateMachine;if(sm.states.Any(x=>x.state.name=="Death"))throw new Exception("Existing Death state: inspect before editing");var state=sm.AddState("Death",new Vector3(610,240,0));state.motion=death;state.writeDefaultValues=true;state.iKOnFeet=false;var tr=sm.AddAnyStateTransition(state);tr.hasExitTime=false;tr.duration=.12f;tr.canTransitionToSelf=false;tr.AddCondition(AnimatorConditionMode.If,0,"Dead");EditorUtility.SetDirty(controller);notes.Add("Player death motion="+death.name+" duration="+death.length+" loop="+death.isLooping);
        var manifest=JsonUtility.FromJson<CueManifest>(File.ReadAllText(Root+"/Reports/CueAssignments.json"));
        var library=ScriptableObject.CreateInstance<SurvivalSoundLibrary>();library.cues=manifest.cues.Select(c=>new SurvivalSoundLibrary.CueSet{cue=(SfxCue)Enum.Parse(typeof(SfxCue),c.cue),clips=c.clips.Select(path=>AssetDatabase.LoadAssetAtPath<AudioClip>(path)).ToArray(),volume=c.volume,pitchVariation=c.pitchVariation,spatial=c.spatial}).ToArray();
        foreach(var cue in manifest.cues)foreach(var path in cue.clips){var ai=(AudioImporter)AssetImporter.GetAtPath(path);ai.forceToMono=cue.spatial;var settings=ai.defaultSampleSettings;bool stream=cue.cue=="Wind"||cue.cue=="Tension";settings.loadType=stream?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;settings.preloadAudioData=!stream;ai.defaultSampleSettings=settings;ai.SaveAndReimport();}
        if(library.cues.Any(c=>c.clips.Any(clip=>!clip)))throw new Exception("Missing imported audio clip");AssetDatabase.CreateAsset(library,Root+"/Audio/SurvivalSoundLibrary.asset");
        var atmosphere=new GameObject("Survival Atmosphere");var director=atmosphere.AddComponent<SurvivalAudioDirector>();SetObject(director,"library",library);
        var map=scene.GetRootGameObjects().Single(g=>g.name=="ZombieUnity - Migrated Map");var anchors=new System.Collections.Generic.List<Transform>();
        foreach(var t in map.GetComponentsInChildren<Transform>().Where(t=>t.name.IndexOf("gate",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("wire panel",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("timber fence",StringComparison.OrdinalIgnoreCase)>=0)){
            if(anchors.Any(a=>Vector3.Distance(a.position,t.position)<25))continue;var a=new GameObject("Creak near "+t.name).transform;a.SetParent(atmosphere.transform);a.position=t.position;anchors.Add(a);if(anchors.Count==6)break;
        }
        var ds=new SerializedObject(director);var anchorProperty=ds.FindProperty("environmentalAnchors");anchorProperty.arraySize=anchors.Count;for(int i=0;i<anchors.Count;i++)anchorProperty.GetArrayElementAtIndex(i).objectReferenceValue=anchors[i];ds.ApplyModifiedPropertiesWithoutUndo();
        foreach(var driver in UnityEngine.Object.FindObjectsByType<HumanoidAnimationDriver>(FindObjectsInactive.Exclude))if(!driver.GetComponent<ActorSurvivalAudio>())driver.gameObject.AddComponent<ActorSurvivalAudio>();
        const string zombiePrefab="Assets/CharacterUpgrade/Prefabs/Zombie.prefab";var contents=PrefabUtility.LoadPrefabContents(zombiePrefab);try{if(!contents.GetComponent<ActorSurvivalAudio>())contents.AddComponent<ActorSurvivalAudio>();PrefabUtility.SaveAsPrefabAsset(contents,zombiePrefab);}finally{PrefabUtility.UnloadPrefabContents(contents);}
        var sky=new Material(Shader.Find("ZombieUnity/Night Sky"));AssetDatabase.CreateAsset(sky,Root+"/Sky/NightSky.mat");RenderSettings.skybox=sky;
        var moon=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).FirstOrDefault(l=>l.type==LightType.Directional);if(moon){moon.color=new Color(.52f,.65f,1);moon.intensity=.16f;moon.shadows=LightShadows.Soft;sky.SetVector("_MoonDirection",-moon.transform.forward);RenderSettings.sun=moon;EditorUtility.SetDirty(moon);}
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.035f,.048f,.082f);RenderSettings.ambientEquatorColor=new Color(.018f,.024f,.038f);RenderSettings.ambientGroundColor=new Color(.009f,.011f,.016f);RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.16f;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.017f,.024f,.04f);RenderSettings.fogDensity=.0055f;DynamicGI.UpdateEnvironment();
        EditorUtility.SetDirty(sky);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);notes.Add("Audio: "+library.cues.Length+" cues / "+library.cues.Sum(c=>c.clips.Length)+" clips; anchors="+anchors.Count);notes.Add("Night sky + .16 moon light + low ambient/fog; existing practical lights retained.");File.WriteAllLines(Path.Combine(Folder,"applied.txt"),notes);Preview();
    }
    static void TuneNight(){
        var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.path!="Assets/Scenes/UTS.unity"||!RenderSettings.skybox||RenderSettings.skybox.shader.name!="ZombieUnity/Night Sky")throw new Exception("Expected current night UTS in Edit mode");
        RenderSettings.ambientSkyColor=new Color(.10f,.125f,.17f);RenderSettings.ambientEquatorColor=new Color(.055f,.066f,.09f);RenderSettings.ambientGroundColor=new Color(.025f,.03f,.04f);
        if(RenderSettings.sun){
            var nc=RenderSettings.sun.GetComponent<UTS.Environment.NightEnvironmentController>();
            if(nc){var settings=new SerializedObject(nc);settings.FindProperty("previewNightInEditor").boolValue=true;settings.FindProperty("nightIntensity").floatValue=.20f;settings.FindProperty("nightLightColor").colorValue=new Color(.52f,.65f,1);settings.ApplyModifiedPropertiesWithoutUndo();nc.ApplyLighting();EditorUtility.SetDirty(nc);}
            RenderSettings.sun.intensity=.20f;EditorUtility.SetDirty(RenderSettings.sun);
        }
        DynamicGI.UpdateEnvironment();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Path.Combine(Folder,"night-tuned.txt"),"Ambient lifted for readable silhouettes; moon .20; original practical lights and transforms retained.");Preview();
    }
    static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static void Preview(){
        var camera=Camera.main;if(!camera)throw new Exception("No game camera");Render(camera,Path.Combine(Folder,"NightGameView.png"),1280,720);
        var previewScene=EditorSceneManager.NewPreviewScene();try{
            var cameraGo=new GameObject("Item preview camera");SceneManager.MoveGameObjectToScene(cameraGo,previewScene);var cam=cameraGo.AddComponent<Camera>();cam.scene=previewScene;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.09f,.11f);cam.fieldOfView=36;cam.nearClipPlane=.01f;
            var lightGo=new GameObject("Preview light");SceneManager.MoveGameObjectToScene(lightGo,previewScene);var l=lightGo.AddComponent<Light>();l.type=LightType.Directional;l.intensity=2;l.transform.rotation=Quaternion.Euler(35,-25,0);
            int i=0;foreach(var name in new[]{"Bandage","FirstAidKit","Medkit"}){var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Items/Prefabs/"+name+".prefab"));SceneManager.MoveGameObjectToScene(go,previewScene);go.transform.position=new Vector3((i-1)*.52f,0,0);i++;}
            cam.transform.position=new Vector3(.85f,.65f,-1.65f);cam.transform.LookAt(new Vector3(0,.11f,0));Render(cam,Path.Combine(Folder,"ItemModels.png"),1280,720);
        }finally{EditorSceneManager.ClosePreviewScene(previewScene);}
    }
    public static void Render(Camera camera,string file,int width=1280,int height=720){var old=camera.targetTexture;var rt=new RenderTexture(width,height,24);var active=RenderTexture.active;try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}
