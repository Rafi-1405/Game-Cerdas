using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Praktikum5.FSM;
using UTS.Characters;
using UTS.Zombies;

[InitializeOnLoad]
public static class CharacterModelSwap
{
    const string Base="Assets/CharacterUpgrade";
    const string PlayerModel=Base+"/Models/PlayerChar.fbx",ZombieModel=Base+"/Models/ZombieChar.fbx",Walk=Base+"/Animations/Player/Walking2.fbx";
    static string Folder=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_ModelSwap");
    static string last=SessionState.GetString("ZU.ModelSwap.Command","");
    [Serializable] class Command{public string op,nonce;}
    static CharacterModelSwap(){EditorApplication.update+=Tick;}
    static void Tick(){var f=Path.Combine(Folder,"command.json");if(!File.Exists(f)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;var txt=File.ReadAllText(f);if(txt==last)return;last=txt;SessionState.SetString("ZU.ModelSwap.Command",txt);try{var c=JsonUtility.FromJson<Command>(txt);if(c.op=="prepare")Prepare();else if(c.op=="inspect")Inspect();else if(c.op=="apply")Apply();else if(c.op=="verify")Verify();else if(c.op=="refresh")AssetDatabase.Refresh();}catch(Exception e){File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString());Debug.LogException(e);}}
    static void Write(string f,object obj)=>File.WriteAllText(Path.Combine(Folder,f),JsonUtility.ToJson(obj,true));
    static Scene Scene(){var s=SceneManager.GetActiveScene();if(Application.isPlaying||s.path!="Assets/Scenes/UTS.unity")throw new Exception("Expected current UTS scene in Edit mode");return s;}
    static string Hash(GameObject root){var b=new StringBuilder();foreach(var t in root.GetComponentsInChildren<Transform>(true))b.Append(t.name).Append(t.position.ToString("F6")).Append(t.rotation.ToString("F6")).Append(t.lossyScale.ToString("F6"));using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()))).Replace("-","");}
    [Serializable] class TextureInfo{public string property,path,name;public Vector2 scale,offset;}
    [Serializable] class MatInfo{public string name,shader;public Color color;public TextureInfo[] textures;}
    [Serializable] class ModelInfo{public string path,type;public bool avatarValid,human;public int meshes,vertices;public Vector3 size;public string[] bones,clips;public MatInfo[] materials;}
    [Serializable] class ActorInfo{public string name,model,controller;public Vector3 position,scale;public Quaternion rotation;}
    [Serializable] class Inspection{public string scene,pipeline,mapHash,prefab;public ActorInfo[] actors;public ModelInfo[] models;}
    static GameObject Map()=>Scene().GetRootGameObjects().Single(g=>g.name=="ZombieUnity - Migrated Map");
    static HumanoidAnimationDriver[] Actors()=>Scene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HumanoidAnimationDriver>(true)).ToArray();
    static GameObject ZombiePrefab(){var mgr=UnityEngine.Object.FindAnyObjectByType<ZombieHordeManager>();var so=new SerializedObject(mgr);return (GameObject)so.FindProperty("zombiePrefab").objectReferenceValue;}
    static ModelInfo Model(string p){var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);var av=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Avatar>().FirstOrDefault();var rs=go.GetComponentsInChildren<SkinnedMeshRenderer>(true);var bounds=rs.Length>0?rs[0].bounds:new Bounds();foreach(var r in rs)bounds.Encapsulate(r.bounds);return new ModelInfo{path=p,type=((ModelImporter)AssetImporter.GetAtPath(p)).animationType.ToString(),avatarValid=av&&av.isValid,human=av&&av.isHuman,meshes=rs.Length,vertices=rs.Sum(r=>r.sharedMesh?r.sharedMesh.vertexCount:0),size=bounds.size,bones=go.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToArray(),clips=AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(a=>!a.name.StartsWith("__preview")).Select(a=>a.name).ToArray(),materials=rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Select(m=>new MatInfo{name=m.name,shader=m.shader.name,color=m.color,textures=m.GetTexturePropertyNames().Select(n=>new TextureInfo{property=n,path=AssetDatabase.GetAssetPath(m.GetTexture(n)),name=m.GetTexture(n)?m.GetTexture(n).name:"",scale=m.GetTextureScale(n),offset=m.GetTextureOffset(n)}).ToArray()}).ToArray()};}
    static Inspection Current(){return new Inspection{scene=Scene().path,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name,mapHash=Hash(Map()),prefab=AssetDatabase.GetAssetPath(ZombiePrefab()),actors=Actors().Select(d=>new ActorInfo{name=d.name,position=d.transform.position,rotation=d.transform.rotation,scale=d.transform.localScale,model=d.Animator?PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(d.Animator.gameObject):"",controller=d.Animator?AssetDatabase.GetAssetPath(d.Animator.runtimeAnimatorController):""}).ToArray(),models=new[]{PlayerModel,ZombieModel,Walk}.Select(Model).ToArray()};}
    static void Inspect()=>Write("inspect.json",Current());
    static void Prepare(){
        var scene=Scene();string backup=Base+"/Backups/ModelSwap_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);AssetDatabase.Refresh();EditorSceneManager.SaveScene(scene,backup+"/UTS_BeforeModels.unity",true);AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(ZombiePrefab()),backup+"/Zombie_BeforeModels.prefab");var pc=Actors().Single(d=>d.GetComponent<SimplePlayerController>()).Animator.runtimeAnimatorController;AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(pc),backup+"/Player_BeforeWalking2.controller");
        foreach(string p in new[]{PlayerModel,ZombieModel,Walk}){
            var mi=(ModelImporter)AssetImporter.GetAtPath(p);mi.animationType=ModelImporterAnimationType.Human;mi.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;mi.importCameras=false;mi.importLights=false;mi.optimizeGameObjects=false;
            if(p==Walk){var cs=mi.defaultClipAnimations;foreach(var c in cs){c.name="Walking2";c.loopTime=true;c.loopPose=true;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=false;c.keepOriginalPositionXZ=false;c.keepOriginalPositionY=false;c.heightFromFeet=true;c.keepOriginalOrientation=false;}mi.clipAnimations=cs;}
            mi.SaveAndReimport();if(p!=Walk){string folder=Base+"/Textures/"+Path.GetFileNameWithoutExtension(p);Directory.CreateDirectory(folder);AssetDatabase.Refresh();if(mi.ExtractTextures(folder))mi.SaveAndReimport();}
        }
        AssetDatabase.SaveAssets();Write("before.json",Current());File.WriteAllText(Path.Combine(Folder,"backup.txt"),backup);Inspect();
    }
    static Texture2D Tex(string who,string file,bool normal=false,bool linear=false){string p=Base+"/Textures/"+who+"/"+file+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(p);if(!ti)throw new Exception("Missing texture "+p);bool changed=ti.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default)||ti.sRGBTexture==linear;if(changed){ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=!linear;ti.SaveAndReimport();}return AssetDatabase.LoadAssetAtPath<Texture2D>(p);}
    static void Material(string who,string name,string diffuse,string normal,string spec,bool cutout){
        string folder=Base+"/Materials/"+who;Directory.CreateDirectory(folder);AssetDatabase.Refresh();string p=folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(p);if(mat)throw new Exception("Material already exists; inspect instead: "+p);
        mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",Tex(who,diffuse));mat.SetFloat("_Smoothness",.28f);mat.SetFloat("_Metallic",0);mat.SetFloat("_WorkflowMode",0);mat.EnableKeyword("_SPECULAR_SETUP");mat.SetColor("_SpecColor",new Color(.2f,.2f,.2f));
        if(normal!=null){mat.SetTexture("_BumpMap",Tex(who,normal,true,true));mat.SetFloat("_BumpScale",1);mat.EnableKeyword("_NORMALMAP");}
        if(spec!=null){mat.SetTexture("_SpecGlossMap",Tex(who,spec,false,true));mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",who=="PlayerChar"?1:.28f);}
        if(cutout){mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.35f);mat.SetFloat("_Cull",0);mat.EnableKeyword("_ALPHATEST_ON");mat.SetOverrideTag("RenderType","TransparentCutout");mat.renderQueue=2450;}
        AssetDatabase.CreateAsset(mat,p);var mi=(ModelImporter)AssetImporter.GetAtPath(Base+"/Models/"+who+".fbx");mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);EditorUtility.SetDirty(mi);
    }
    static Bounds MeshBounds(GameObject model,Transform frame){bool first=true;var result=new Bounds();foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var m=new Mesh();r.BakeMesh(m);foreach(var v in m.vertices){var p=frame.InverseTransformPoint(r.transform.TransformPoint(v));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}UnityEngine.Object.DestroyImmediate(m);}if(first)throw new Exception("Model has no skinned vertices");return result;}
    static void Swap(HumanoidAnimationDriver driver,bool player){
        var actor=driver.gameObject;var previous=driver.Animator;if(!previous||previous.transform.parent!=actor.transform||previous.name!="Character Model")throw new Exception("Inspect unexpected actor hierarchy: "+actor.name);string path=player?PlayerModel:ZombieModel;
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault(a=>a.isHuman&&a.isValid);if(!avatar)throw new Exception("Invalid avatar: "+path);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(model);go.name="New Character Model";go.transform.SetParent(actor.transform,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
        try{
            var a=go.GetComponent<Animator>();if(!a)a=go.AddComponent<Animator>();a.avatar=avatar;a.runtimeAnimatorController=previous.runtimeAnimatorController;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            var b=MeshBounds(go,actor.transform);if(b.size.y<.5f||b.size.y>5)throw new Exception("Unexpected authored height: "+b.size.y);go.transform.localScale=Vector3.one*(1.85f/b.size.y);b=MeshBounds(go,actor.transform);
            float floor=-1;var cc=actor.GetComponent<CharacterController>();var col=actor.GetComponent<CapsuleCollider>();if(cc)floor=cc.center.y-cc.height*.5f;else if(col)floor=col.center.y-col.height*.5f;go.transform.localPosition+=Vector3.up*(floor-b.min.y);
            if(player){var align=go.AddComponent<CrouchPoseAlignment>();var old=previous.GetComponent<CrouchPoseAlignment>();if(old){var os=new SerializedObject(old);var ns=new SerializedObject(align);ns.FindProperty("movingBodyLowering").floatValue=os.FindProperty("movingBodyLowering").floatValue;ns.ApplyModifiedPropertiesWithoutUndo();}}
            var so=new SerializedObject(driver);so.FindProperty("animator").objectReferenceValue=a;so.ApplyModifiedPropertiesWithoutUndo();UnityEngine.Object.DestroyImmediate(previous.gameObject);go.name="Character Model";
            if(PrefabUtility.IsPartOfPrefabInstance(driver))PrefabUtility.RecordPrefabInstancePropertyModifications(driver);EditorUtility.SetDirty(driver);
        }catch{if(go)UnityEngine.Object.DestroyImmediate(go);throw;}
    }
    static void ReplaceWalk(BlendTree tree,AnimationClip clip,ref int count){var children=tree.children;for(int i=0;i<children.Length;i++){if(children[i].motion is BlendTree nested)ReplaceWalk(nested,clip,ref count);else if(children[i].motion&&AssetDatabase.GetAssetPath(children[i].motion)==Base+"/Animations/Player/Walking.fbx"){children[i].motion=clip;count++;}}tree.children=children;EditorUtility.SetDirty(tree);}
    static void WalkStates(AnimatorStateMachine sm,AnimationClip clip,ref int count){foreach(var child in sm.states){if(child.state.motion is BlendTree tree)ReplaceWalk(tree,clip,ref count);}foreach(var sub in sm.stateMachines)WalkStates(sub.stateMachine,clip,ref count);}
    static void Apply(){
        var scene=Scene();var before=JsonUtility.FromJson<Inspection>(File.ReadAllText(Path.Combine(Folder,"before.json")));if(before.mapHash!=Hash(Map()))throw new Exception("Map changed since snapshot; re-inspect before applying");if(Actors().Any(d=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(d.Animator.gameObject)!=Base+"/Models/Humanoid.fbx"))throw new Exception("Actor visual changed since inspection; inspect current scene");
        Material("PlayerChar","Ch31_body","Ch31_1001_Diffuse","Ch31_1001_Normal","Ch31_SpecGloss",false);Material("PlayerChar","Ch31_hair","Ch31_1002_Diffuse",null,null,true);
        Material("ZombieChar","yaku_zombie_material","Yakuzombie_","Yakuzombie_normal","Yakuzombie_specular",false);Material("ZombieChar","yifu","Yakuzombie_diffuse","Yakuzombie_normal","Yakuzombie_specular",true);
        foreach(var p in new[]{PlayerModel,ZombieModel})((ModelImporter)AssetImporter.GetAtPath(p)).SaveAndReimport();
        var player=Actors().Single(d=>d.GetComponent<SimplePlayerController>());var controller=player.Animator.runtimeAnimatorController as AnimatorController;if(!controller)throw new Exception("Player controller is not editable AnimatorController");var clip=AssetDatabase.LoadAllAssetsAtPath(Walk).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));int count=0;foreach(var l in controller.layers)WalkStates(l.stateMachine,clip,ref count);if(count!=1)throw new Exception("Expected exactly one existing walk motion, got "+count);EditorUtility.SetDirty(controller);
        foreach(var d in Actors())Swap(d,d.GetComponent<SimplePlayerController>()!=null);
        string prefabPath=AssetDatabase.GetAssetPath(ZombiePrefab());var prefab=PrefabUtility.LoadPrefabContents(prefabPath);try{Swap(prefab.GetComponent<HumanoidAnimationDriver>(),false);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Verify();File.WriteAllText(Path.Combine(Folder,"applied.txt"),"PlayerChar + ZombieChar + Walking2 saved");
    }
    [Serializable] class Verification{public bool mapPreserved,actorTransformsPreserved,avatarsValid,allMaterialsTextured,prefabUsesZombieChar,walking2;public string mapHash;public int navTriangles;public ActorInfo[] actors;}
    static bool UsesWalk(Motion m){if(m is BlendTree t)return t.children.Any(c=>UsesWalk(c.motion));return m&&AssetDatabase.GetAssetPath(m)==Walk;}
    static void Verify(){
        var current=Current();var before=JsonUtility.FromJson<Inspection>(File.ReadAllText(Path.Combine(Folder,"before.json")));var ds=Actors();var pc=(AnimatorController)ds.Single(d=>d.GetComponent<SimplePlayerController>()).Animator.runtimeAnimatorController;var pf=ZombiePrefab();var pa=pf.GetComponentInChildren<Animator>();
        var v=new Verification{mapPreserved=current.mapHash==before.mapHash,mapHash=current.mapHash,actorTransformsPreserved=current.actors.All(a=>before.actors.Any(b=>a.name==b.name&&a.position==b.position&&a.rotation==b.rotation&&a.scale==b.scale)),avatarsValid=ds.All(d=>d.Animator.isHuman&&d.Animator.avatar.isValid),allMaterialsTextured=ds.All(d=>d.Animator.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader.isSupported&&m.GetTexture("_BaseMap")))),prefabUsesZombieChar=pa&&pa.avatar==AssetDatabase.LoadAllAssetsAtPath(ZombieModel).OfType<Avatar>().First(),walking2=pc.layers.SelectMany(l=>l.stateMachine.states).Any(s=>UsesWalk(s.state.motion)),navTriangles=NavMesh.CalculateTriangulation().indices.Length/3,actors=current.actors};Write("verification.json",v);if(!v.mapPreserved||!v.actorTransformsPreserved||!v.avatarsValid||!v.allMaterialsTextured||!v.prefabUsesZombieChar||!v.walking2)throw new Exception("Model swap verification failed");Directory.CreateDirectory(Base+"/Reports");File.WriteAllText(Base+"/Reports/ModelSwap.json",JsonUtility.ToJson(v,true));Inspect();
    }
}
