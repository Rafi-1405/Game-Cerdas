using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class ZombieUnityMigrationQA
{
    const string Base="Assets/RobloxMigration";
    static string Control=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Migration");
    [Serializable] class Snapshot{public float studToMeter;public ZombieUnityMapImporter.Row[] rows;}
    [Serializable] class DoorTest{public string name;public int samples;public string[] hits;}
    [Serializable] class Audit{public int expectedVisible,actualVisible,renderers,colliders,missingMaterials,unsupportedShaders;public float maxPositionError,maxRotationErrorDegrees;public int[] missingIds;public DoorTest[] doors;public int stairTreadsSupported;public string[] originalRoots;public string[] playerOverlaps;public string playerGround;public float playerGroundY;}
    static Vector3 Convert(Vector3 p)=>new Vector3(p.x,p.y,-p.z)*.28f;
    public static void AuditMap(){
        var root=SceneManager.GetActiveScene().GetRootGameObjects().First(g=>g.name=="ZombieUnity - Migrated Map");
        var data=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Base+"/Source/map.json"));
        var found=new Dictionary<int,Transform>();foreach(var f in root.GetComponentsInChildren<MeshFilter>(true)){var m=Regex.Match(f.name,@"^Mesh - ZU_(\d+)_");if(m.Success)found[int.Parse(m.Groups[1].Value)]=f.transform.parent;}
        var result=new Audit{expectedVisible=data.rows.Count(r=>r.cf!=null&&r.alpha<1),actualVisible=found.Count,renderers=root.GetComponentsInChildren<Renderer>(true).Length,colliders=root.GetComponentsInChildren<Collider>(true).Length,originalRoots=SceneManager.GetActiveScene().GetRootGameObjects().Select(g=>g.name).ToArray()};
        var missing=new List<int>();foreach(var r in data.rows){if(r.cf==null||r.alpha>=1)continue;if(!found.TryGetValue(r.id,out var t)){missing.Add(r.id);continue;}var p=Convert(new Vector3(r.cf[0],r.cf[1],r.cf[2]));var q=Quaternion.LookRotation(new Vector3(-r.cf[5],-r.cf[8],r.cf[11]),new Vector3(r.cf[4],r.cf[7],-r.cf[10]));result.maxPositionError=Mathf.Max(result.maxPositionError,Vector3.Distance(t.position,p));result.maxRotationErrorDegrees=Mathf.Max(result.maxRotationErrorDegrees,Quaternion.Angle(q,t.rotation));}
        result.missingIds=missing.ToArray();foreach(var r in root.GetComponentsInChildren<Renderer>()){foreach(var m in r.sharedMaterials){if(!m||!m.shader)result.missingMaterials++;else if(!m.shader.isSupported)result.unsupportedShaders++;}}
        Physics.SyncTransforms();var tests=new List<DoorTest>();
        tests.Add(Sweep(root,"Guardhouse direct doorway",new Vector3(11.5f,3.5f,-203),new Vector3(5.5f,3.5f,-203)));
        tests.Add(Sweep(root,"Sleeping tent direct doorway",new Vector3(-37,3.5f,-257),new Vector3(-37,3.5f,-263)));
        tests.Add(Sweep(root,"Medical tent direct doorway",new Vector3(-16,3.5f,-275),new Vector3(-16,3.5f,-281)));
        var jambs=data.rows.Where(r=>r.name=="Pedestrian gate steel jamb"&&r.cf!=null).ToArray();if(jambs.Length==2){var a=jambs[0].cf;var b=jambs[1].cf;var p=new Vector3((a[0]+b[0])/2,3.5f,(a[2]+b[2])/2);var n=new Vector3(a[5],0,a[11]);tests.Add(Sweep(root,"Wicket direct doorway",p+n*3,p-n*3));}
        foreach(var r in data.rows.Where(r=>r.name=="Door head"&&r.cf!=null)){
            var p=new Vector3(r.cf[0],4.1f,r.cf[2]);var n=new Vector3(r.cf[5],0,r.cf[11]);tests.Add(Sweep(root,"Building doorway "+r.id,p+n*2,p-n*2));
        }
        var player=SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g=>g.name=="Player");if(player){
            var cc=player.GetComponent<CharacterController>();var center=player.transform.TransformPoint(cc?cc.center:Vector3.zero);float radius=cc?cc.radius:.5f,height=cc?cc.height:2;
            var delta=Vector3.up*Mathf.Max(0,height*.5f-radius);result.playerOverlaps=Physics.OverlapCapsule(center-delta,center+delta,radius*.94f,~0,QueryTriggerInteraction.Ignore).Where(c=>c.transform.IsChildOf(root.transform)).Select(c=>c.name).Distinct().ToArray();
            var ground=Physics.RaycastAll(new Ray(center+Vector3.up*10,Vector3.down),30,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(root.transform)&&h.point.y<=center.y+.05f).OrderByDescending(h=>h.point.y).ToArray();if(ground.Length>0){result.playerGround=ground[0].collider.name;result.playerGroundY=ground[0].point.y;}
        }
        result.doors=tests.ToArray();for(int i=1;i<=22;i++){float y=.08f+i*(14.6f-.08f)/22;var ray=new Ray(Convert(new Vector3(-4,y+1,-274.8f-(i-.5f))),Vector3.down);if(Physics.RaycastAll(ray,1,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.transform.IsChildOf(root.transform)&&(h.collider.name=="Rampart steel stair tread"||h.collider.transform.parent.name=="Rampart steel stair tread")))result.stairTreadsSupported++;}
        File.WriteAllText(Path.Combine(Control,"audit.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Base+"/Reports/Audit.json",JsonUtility.ToJson(result,true));
    }
    static DoorTest Sweep(GameObject root,string name,Vector3 a,Vector3 b){var hits=new HashSet<string>();int samples=0;for(int j=0;j<=16;j++){var p=Convert(Vector3.Lerp(a,b,j/16f));foreach(var c in Physics.OverlapBox(p,Vector3.one*.42f+new Vector3(0,.42f,0),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)){if(c.transform.IsChildOf(root.transform))hits.Add(c.name+" @ "+(c.transform.parent?c.transform.parent.name:"root"));}samples++;}return new DoorTest{name=name,samples=samples,hits=hits.ToArray()};}
    public static void Render(){
        var go=new GameObject("Migration QA camera"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.fieldOfView=55;cam.farClipPlane=1500;cam.nearClipPlane=.05f;cam.clearFlags=CameraClearFlags.Skybox;cam.allowHDR=false;
        Directory.CreateDirectory(Path.Combine(Control,"Renders"));Shot(cam,"overview",new Vector3(130,100,-160),new Vector3(0,2,0));Shot(cam,"fortress_gate",new Vector3(10,5,40),new Vector3(5.6f,3.4f,51.8f));Shot(cam,"town_street",new Vector3(5.6f,1.7f,-8),new Vector3(5.6f,1.7f,35));Shot(cam,"fastfood_interior",new Vector3(-3.92f,1.624f,-16.8f),new Vector3(-8.54f,1.1f,-22.2f));Shot(cam,"minimarket_interior",new Vector3(-19.88f,1.624f,-15.68f),new Vector3(-25.2f,1.1f,-19.32f));UnityEngine.Object.DestroyImmediate(go);
    }
    static void Shot(Camera c,string name,Vector3 pos,Vector3 look){c.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(look-pos));var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);c.targetTexture=rt;c.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Control,"Renders/"+name+".png"),tex.EncodeToPNG());RenderTexture.active=prev;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
}
