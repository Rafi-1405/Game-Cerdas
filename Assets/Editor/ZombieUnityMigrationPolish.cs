using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ZombieUnityMigrationPolish
{
    const string Base="Assets/RobloxMigration";
    [Serializable] class Result{public string pipeline;public int glassMaterials,cutoutMaterials,lights;public Vector3 playerBefore,playerAfter;}
    public static void Apply(){
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/UTS.unity"||EditorApplication.isPlaying)throw new Exception("Expected UTS in Edit mode");
        string control=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Migration");if(File.Exists(Path.Combine(control,"finish-complete.json")))throw new Exception("Display adaptation already applied");
        var root=scene.GetRootGameObjects().Single(g=>g.name=="ZombieUnity - Migrated Map");if(!root.activeSelf)throw new Exception("Map import is incomplete");
        var result=new Result{pipeline=GraphicsSettings.currentRenderPipeline?GraphicsSettings.currentRenderPipeline.name:"BuiltIn"};
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4509804f,.4666667f,.427451f);RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.25f;
        var sun=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Directional Light")?.GetComponent<Light>();if(sun){sun.transform.rotation=Quaternion.LookRotation(new Vector3(.84726375f,-.3509478f,-.3987228f));sun.intensity=1.15f;RenderSettings.sun=sun;}
        var glass=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"/Source/Geometry/Textures/glass_albedo_unity.png");if(!glass)throw new Exception("Glass translation texture not imported yet");
        string[] cutoutTextures={"7e016e0477d758ba","f6dfe255c18e8a32","baa745991ab834bf","29d4dbb49b8bc01d","dfac1f385da6cf29","d8101d94a6298216","3779adb9968759f7","6a3886c5d201b905"};
        foreach(var mat in root.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m&&AssetDatabase.GetAssetPath(m).StartsWith(Base+"/Materials/")).Distinct()){
            bool urp=mat.shader.name.StartsWith("Universal Render Pipeline/");
            if(urp&&mat.HasProperty("_Surface")&&mat.GetFloat("_Surface")>.5f){mat.SetFloat("_BlendModePreserveSpecular",0);BaseShaderGUI.SetupMaterialBlendMode(mat);EditorUtility.SetDirty(mat);}
            if(mat.name.StartsWith("ZU_Glass_")&&mat.color.a<.99f){
                var color=mat.color;color.a=Mathf.Clamp(color.a*.32f,.08f,.28f);mat.color=color;mat.SetTexture(urp?"_BaseMap":"_MainTex",glass);mat.SetFloat("_BumpScale",.08f);mat.SetFloat(urp?"_Smoothness":"_Glossiness",.65f);mat.SetFloat("_Cull",2);
                if(urp){mat.SetFloat("_EnvironmentReflections",0);mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");BaseShaderGUI.SetupMaterialBlendMode(mat);}EditorUtility.SetDirty(mat);result.glassMaterials++;
            }
            var tex=mat.GetTexture(urp?"_BaseMap":"_MainTex");string texture=tex?AssetDatabase.GetAssetPath(tex):"";
            if(urp&&cutoutTextures.Any(t=>texture.Contains(t))){mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.35f);BaseShaderGUI.SetupMaterialBlendMode(mat);mat.SetFloat("_Cull",0);EditorUtility.SetDirty(mat);result.cutoutMaterials++;}
        }
        foreach(var light in root.GetComponentsInChildren<Light>()){light.intensity*=4;result.lights++;}
        var player=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Player");if(player){result.playerBefore=player.transform.position;var cc=player.GetComponent<CharacterController>();if(cc){var c=player.transform.TransformPoint(cc.center);Physics.SyncTransforms();var hits=Physics.RaycastAll(c+Vector3.up*2,Vector3.down,5,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(root.transform)&&h.point.y<c.y).OrderByDescending(h=>h.point.y).ToArray();if(hits.Length>0){float bottom=c.y-cc.height*.5f;float lift=hits[0].point.y+.03f-bottom;if(lift>0&&lift<.25f){Undo.RecordObject(player.transform,"Align player above imported ground");player.transform.position+=Vector3.up*lift;}}}result.playerAfter=player.transform.position;}
        PrefabUtility.SaveAsPrefabAsset(root,Base+"/Prefabs/ZombieUnity_Map.prefab");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Physics.SyncTransforms();
        File.WriteAllText(Path.Combine(control,"finish-complete.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Base+"/Reports/DisplayAdaptation.json",JsonUtility.ToJson(result,true));
    }
}
