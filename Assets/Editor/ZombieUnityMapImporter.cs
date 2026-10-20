using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

// Imports an immutable live-Studio snapshot and an official Studio OBJ conversion.
// Never opens a different scene, deletes existing content, or silently substitutes missing meshes.
public static class ZombieUnityMapImporter
{
    const string Base="Assets/RobloxMigration";
    const string RootName="ZombieUnity - Migrated Map";
    [Serializable] public class Attachment {public string @class,text,face,texture,kind,mesh,colorMap,normalMap,roughMap,metalMap,alphaMode;public float[] color,scale,offset;public float brightness,range,angle,alpha,tileU,tileV,offsetU,offsetV;public bool enabled,shadows;}
    [Serializable] public class Row {public int id,parent;public string name,@class,path,material,shape,mesh,texture;public float[] cf,size,color;public float alpha,reflectance;public bool collide,shadow;public Attachment[] attachments;}
    [Serializable] class Snapshot {public float studToMeter;public Row[] rows;}
    [Serializable] class ModelPivot {public int id;public string path;public float[] pivot;}
    [Serializable] class ModelPivots {public ModelPivot[] rows;}
    [Serializable] class SubMesh {public string material;public int[] triangles;}
    [Serializable] class Geometry {public string name;public float[] vertices,normals,uv;public SubMesh[] submeshes;}
    [Serializable] class MeshPart {public int id;public Geometry[] meshes;}
    [Serializable] class MeshFile {public MeshPart[] parts;}
    [Serializable] class SourceMaterial {public string name,texture,normal,specular,emission,alphaMode;public float[] color;public float alpha=1;public bool doubleSided;}
    [Serializable] class PackRecord {public long offset,length;public string name,kind;}
    [Serializable] class PackEntry {public int id;public PackRecord[] records;}
    [Serializable] class DeferredPart {public int id;public string reason;}
    [Serializable] class Manifest {public int format,objects,mapped,requiredComplexParts;public string geometryFile;public int[] missingComplexParts;public string[] meshFiles;public SourceMaterial[] materials;public PackEntry[] meshEntries;public DeferredPart[] deferredParts;}
    [Serializable] class Report {public string scene;public int sourceInstances,visibleParts,colliders,meshes,materials,labels,lights;public float studToMeter;public string[] originalRoots;public string backup;public string[] notes;public DeferredPart[] deferredParts;}
    static Dictionary<int,GameObject> objects;
    static Dictionary<int,Geometry[]> imported;
    static Dictionary<string,SourceMaterial> sources;
    static Dictionary<string,Material> materials;
    static Dictionary<string,Mesh> nativeMeshes;
    static Dictionary<int,PackEntry> packEntries;
    static BinaryReader packReader;
    static Dictionary<int,ModelPivot> pivots;
    static HashSet<int> deferred;
    static GameObject root;
    static Report report;
    static float scale;
    static bool urp;
    static bool busy;
    public static bool Busy=>busy;
    static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
    static Color C(float[] a)=>a!=null&&a.Length>=3?new Color(a[0],a[1],a[2],a.Length>3?a[3]:1):Color.white;
    static Vector3 Position(Row r)=>new Vector3(r.cf[0],r.cf[1],-r.cf[2])*scale;
    static Quaternion Rotation(Row r)=>Quaternion.LookRotation(new Vector3(-r.cf[5],-r.cf[8],r.cf[11]),new Vector3(r.cf[4],r.cf[7],-r.cf[10]));
    static string Hash(string s)=>Hash128.Compute(s).ToString();
    static void Folder(string p){Directory.CreateDirectory(p);}
    static string Control=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Migration");

    [MenuItem("Tools/ZombieUnity/Import verified Studio map")]
    public static void Import(){
        if(busy)throw new Exception("Migration already running");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before importing");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/UTS.unity")throw new Exception("Expected the user's UTS scene, got "+scene.path);
        if(scene.GetRootGameObjects().Any(g=>g.name==RootName))throw new Exception("Map already exists: inspect it; never regenerate over user changes.");
        string manifestPath=Base+"/Source/Geometry/manifest.json";
        if(!File.Exists(manifestPath))throw new Exception("Official Studio OBJ export is still required. No scene changes made.");
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
        deferred=new HashSet<int>((manifest.deferredParts??Array.Empty<DeferredPart>()).Select(d=>d.id));
        if(manifest.missingComplexParts==null||!new HashSet<int>(manifest.missingComplexParts).SetEquals(deferred))throw new Exception("Undocumented source meshes are missing. No scene changes made.");
        var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Base+"/Source/map.json"));scale=snapshot.studToMeter;
        pivots=JsonUtility.FromJson<ModelPivots>(File.ReadAllText(Base+"/Source/model-pivots.json")).rows.ToDictionary(p=>p.id,p=>p);
        foreach(var row in snapshot.rows)if(row.@class=="Model"&&(!pivots.TryGetValue(row.id,out var mp)||mp.path!=row.path))throw new Exception("Model hierarchy changed: "+row.path);
        if(Mathf.Abs(scale-.28f)>.0001f)throw new Exception("Unexpected source scale");
        imported=new Dictionary<int,Geometry[]>();packEntries=null;
        if(manifest.format==1){packEntries=manifest.meshEntries.ToDictionary(e=>e.id,e=>e);foreach(var row in snapshot.rows)if(row.cf!=null&&row.alpha<1&&!packEntries.ContainsKey(row.id)&&!deferred.Contains(row.id))throw new Exception("Mesh missing: "+row.path);}
        else {foreach(string f in manifest.meshFiles)foreach(var p in JsonUtility.FromJson<MeshFile>(File.ReadAllText(Base+"/Source/Geometry/"+f)).parts)imported.Add(p.id,p.meshes);foreach(var row in snapshot.rows)if(row.cf!=null&&row.alpha<1&&!imported.ContainsKey(row.id))throw new Exception("Mesh missing: "+row.path);}
        Folder(Base+"/Backups");Folder(Base+"/Materials");Folder(Base+"/Meshes");Folder(Base+"/Reports");Folder(Base+"/Prefabs");
        string backup=Base+"/Backups/UTS_BeforeRoblox_20261020.unity";if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
        report=new Report{scene=scene.path,sourceInstances=snapshot.rows.Length,studToMeter=scale,originalRoots=scene.GetRootGameObjects().Select(g=>g.name).ToArray(),backup=backup,notes=new[]{"Built-in Roblox materials use Unity equivalents. Imported source image textures use the official OBJ export.","Roblox scripts are not imported. Existing UTS gameplay scripts are preserved.","Old Environment is retained inactive to avoid overlapping map surfaces; no old objects are deleted."}};
        report.deferredParts=manifest.deferredParts;
        objects=new Dictionary<int,GameObject>();materials=new Dictionary<string,Material>();nativeMeshes=new Dictionary<string,Mesh>();sources=manifest.materials.ToDictionary(m=>m.name,m=>m);
        PrepareTextures(manifest.materials);
        urp=GraphicsSettings.currentRenderPipeline!=null;
        root=new GameObject(RootName);root.SetActive(false);Undo.RegisterCreatedObjectUndo(root,"Import ZombieUnity map");
        if(packEntries!=null){packReader=new BinaryReader(File.OpenRead(Base+"/Source/Geometry/"+manifest.geometryFile));if(new string(packReader.ReadChars(4))!="ZUM1")throw new Exception("Invalid geometry pack");}
        Schedule(snapshot,scene,0);
    }
    static void Schedule(Snapshot snapshot,Scene scene,int index){
        busy=true;
        EditorApplication.CallbackFunction build=null;
        build=()=>{
            try{
                int stop=Math.Min(index+150,snapshot.rows.Length);
                AssetDatabase.StartAssetEditing();try{for(;index<stop;index++)Build(snapshot.rows[index]);}finally{AssetDatabase.StopAssetEditing();}
                File.WriteAllText(Path.Combine(Control,"progress.json"),"{\"rows\":"+index+",\"total\":"+snapshot.rows.Length+"}");
                EditorUtility.DisplayProgressBar("ZombieUnity migration",index+" / "+snapshot.rows.Length,(float)index/snapshot.rows.Length);
                if(index==snapshot.rows.Length){EditorApplication.update-=build;Finish(scene);}
            }catch(Exception e){EditorApplication.update-=build;packReader?.Dispose();packReader=null;busy=false;EditorUtility.ClearProgressBar();Directory.CreateDirectory(Control);File.WriteAllText(Path.Combine(Control,"error.txt"),e.ToString());Debug.LogException(e);}
        };EditorApplication.update+=build;
    }
    public static void ResumeVerifiedImport(){
        if(busy||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Import already running or not in Edit mode");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/UTS.unity")throw new Exception("Active scene changed");
        root=scene.GetRootGameObjects().Single(g=>g.name==RootName);if(root.activeSelf)throw new Exception("Completed map must not be resumed");
        var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Base+"/Source/map.json"));scale=snapshot.studToMeter;
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Base+"/Source/Geometry/manifest.json"));
        deferred=new HashSet<int>(manifest.deferredParts.Select(d=>d.id));packEntries=manifest.meshEntries.ToDictionary(e=>e.id,e=>e);
        pivots=JsonUtility.FromJson<ModelPivots>(File.ReadAllText(Base+"/Source/model-pivots.json")).rows.ToDictionary(p=>p.id,p=>p);
        objects=new Dictionary<int,GameObject>();materials=new Dictionary<string,Material>();nativeMeshes=new Dictionary<string,Mesh>();sources=manifest.materials.ToDictionary(m=>m.name,m=>m);urp=GraphicsSettings.currentRenderPipeline!=null;
        var ordinal=new Dictionary<string,int>();int index=0;int visible=0;
        foreach(var row in snapshot.rows){
            if(row.parent!=0&&!objects.ContainsKey(row.parent))break;
            var parent=row.parent==0?root.transform:objects[row.parent].transform;string key=row.parent+":"+row.name;ordinal.TryGetValue(key,out int k);
            var children=parent.Cast<Transform>().Where(t=>t.name==row.name).ToArray();if(k>=children.Length)break;var go=children[k].gameObject;ordinal[key]=k+1;
            if(row.cf!=null){if(Vector3.Distance(go.transform.position,Position(row))>.001f||Quaternion.Angle(go.transform.rotation,Rotation(row))>.1f)throw new Exception("Generated node transform changed: "+row.path);
                if(row.alpha<1&&!deferred.Contains(row.id)){var filters=go.GetComponentsInChildren<MeshFilter>(true);if(filters.Length==0)throw new Exception("Incomplete generated node: "+row.path);visible++;
                    if(row.@class=="MeshPart"){var entries=packEntries[row.id].records;for(int j=0;j<entries.Length;j++)if(entries[j].kind=="base"){var filter=go.transform.Find("Mesh - ZU_"+row.id+"_"+j)?.GetComponent<MeshFilter>();if(filter&&filter.sharedMesh){string mk=row.mesh+":"+row.material+":"+string.Join(",",row.size.Select(v=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))+":"+(filter.sharedMesh.vertexCount*3);nativeMeshes[mk]=filter.sharedMesh;}}}
                }
            }
            objects.Add(row.id,go);index++;
        }
        if(index==0||index>=snapshot.rows.Length)throw new Exception("Unexpected resume boundary: "+index);
        // Prefix reconstruction is exact; preserve every completed node and its assets.
        report=new Report{scene=scene.path,sourceInstances=snapshot.rows.Length,studToMeter=scale,backup=Base+"/Backups/UTS_BeforeRoblox_20261020.unity",originalRoots=scene.GetRootGameObjects().Where(g=>g!=root).Select(g=>g.name).ToArray(),visibleParts=visible,colliders=root.GetComponentsInChildren<Collider>(true).Length,meshes=Directory.GetFiles(Base+"/Meshes","*.asset").Length,materials=Directory.GetFiles(Base+"/Materials","*.mat").Length,labels=root.GetComponentsInChildren<TMPro.TextMeshPro>(true).Length,lights=root.GetComponentsInChildren<Light>(true).Length,deferredParts=manifest.deferredParts,notes=new[]{"Official Studio OBJ geometry, UVs and exported image maps imported at .28 meters/stud; renderer appearance may differ between engines.","Roblox scripts are not imported. Existing UTS gameplay scripts are preserved.","Old Environment retained inactive; source map unchanged."}};
        packReader=new BinaryReader(File.OpenRead(Base+"/Source/Geometry/"+manifest.geometryFile));if(new string(packReader.ReadChars(4))!="ZUM1")throw new Exception("Invalid pack");
        File.WriteAllText(Path.Combine(Control,"resume.txt"),"Verified and preserved "+index+" completed source nodes");Schedule(snapshot,scene,index);
    }
    public static void ImportSupplemental(){
        if(busy||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Wait for the main import in Edit mode");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/UTS.unity")throw new Exception("Active scene changed");
        root=scene.GetRootGameObjects().First(g=>g.name==RootName);
        var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Base+"/Source/map.json"));scale=snapshot.studToMeter;
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Base+"/Source/Geometry/supplement.json"));
        var rows=snapshot.rows.ToDictionary(r=>r.id,r=>r);var targets=new Dictionary<int,GameObject>();
        foreach(var entry in manifest.meshEntries){
            var chain=new Stack<Row>();var row=rows[entry.id];for(var cur=row;;cur=rows[cur.parent]){chain.Push(cur);if(cur.parent==0)break;}
            var t=root.transform;while(chain.Count>0){var r=chain.Pop();var children=t.Cast<Transform>().Where(c=>c.name==r.name).ToArray();if(children.Length!=1)throw new Exception("Ambiguous source node: "+r.path);t=children[0];}
            if(t.GetComponentsInChildren<MeshFilter>(true).Length!=0)throw new Exception("Supplement already imported: "+row.path);
            if(Vector3.Distance(t.position,Position(row))>.001f||Quaternion.Angle(t.rotation,Rotation(row))>.1f)throw new Exception("Target was moved after snapshot: "+row.path);
            targets.Add(entry.id,t.gameObject);
        }
        report=JsonUtility.FromJson<Report>(File.ReadAllText(Base+"/Reports/ImportReport.json"));
        packEntries=manifest.meshEntries.ToDictionary(e=>e.id,e=>e);sources=manifest.materials.ToDictionary(m=>m.name,m=>m);materials=new Dictionary<string,Material>();nativeMeshes=new Dictionary<string,Mesh>();deferred=new HashSet<int>();urp=GraphicsSettings.currentRenderPipeline!=null;
        PrepareTextures(manifest.materials);
        using(packReader=new BinaryReader(File.OpenRead(Base+"/Source/Geometry/"+manifest.geometryFile))){
            if(new string(packReader.ReadChars(4))!="ZUM1")throw new Exception("Invalid supplemental pack");
            AssetDatabase.StartAssetEditing();try{foreach(var target in targets){foreach(var c in target.Value.GetComponents<Collider>()){UnityEngine.Object.DestroyImmediate(c);report.colliders--;}Populate(rows[target.Key],target.Value);}}finally{AssetDatabase.StopAssetEditing();}
        }packReader=null;
        report.deferredParts=report.deferredParts.Where(d=>!targets.ContainsKey(d.id)).ToArray();
        PrefabUtility.SaveAsPrefabAsset(root,Base+"/Prefabs/ZombieUnity_Map.prefab");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Path.Combine(Control,"import-report.json"),JsonUtility.ToJson(report,true));File.WriteAllText(Base+"/Reports/ImportReport.json",JsonUtility.ToJson(report,true));
        File.WriteAllText(Path.Combine(Control,"supplement-complete.txt"),"Truck003 and Tire Stack imported at their original transforms");
    }
    static bool NeedsOBJ(Row r)=>r.cf!=null&&r.alpha<1;
    static void PrepareTextures(SourceMaterial[] mats){
        var roles=new Dictionary<string,int>();foreach(var m in mats){if(!string.IsNullOrEmpty(m.texture))roles[m.texture]=0;if(!string.IsNullOrEmpty(m.emission)&&!roles.ContainsKey(m.emission))roles[m.emission]=0;if(!string.IsNullOrEmpty(m.specular)&&!roles.ContainsKey(m.specular))roles[m.specular]=1;}foreach(var m in mats)if(!string.IsNullOrEmpty(m.normal))roles[m.normal]=2;
        foreach(var role in roles){string p=Base+"/Source/Geometry/"+role.Key;var ti=AssetImporter.GetAtPath(p)as TextureImporter;if(ti==null)throw new Exception("Image not yet imported: "+p);bool normal=role.Value==2;bool srgb=role.Value==0;var kind=normal?TextureImporterType.NormalMap:TextureImporterType.Default;if(ti.textureType!=kind||ti.sRGBTexture!=srgb||!ti.mipmapEnabled){ti.textureType=kind;ti.sRGBTexture=srgb;ti.mipmapEnabled=true;ti.alphaIsTransparency=!normal;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.SaveAndReimport();}}
    }
    static float[] ReadFloats(int n){byte[] b=packReader.ReadBytes(n*4);if(b.Length!=n*4)throw new EndOfStreamException();var a=new float[n];Buffer.BlockCopy(b,0,a,0,b.Length);return a;}
    static Geometry[] LoadGeometry(int id){
        if(packEntries==null)return imported[id];var geoms=new List<Geometry>();
        foreach(var rec in packEntries[id].records){packReader.BaseStream.Position=rec.offset;int n=packReader.ReadInt32();int flags=packReader.ReadInt32();if(n<1||n>4000000)throw new Exception("Invalid vertex count: "+id);
            var g=new Geometry{name=rec.kind,vertices=ReadFloats(n*3),normals=ReadFloats(n*3),uv=ReadFloats(n*2)};int count=packReader.ReadInt32();g.submeshes=new SubMesh[count];
            for(int j=0;j<count;j++){string m=packReader.ReadString();int len=packReader.ReadInt32();var t=new int[len];byte[] b=packReader.ReadBytes(len*4);Buffer.BlockCopy(b,0,t,0,b.Length);g.submeshes[j]=new SubMesh{material=m,triangles=t};}geoms.Add(g);
        }return geoms.ToArray();
    }
    static void Build(Row r){
        var go=new GameObject(r.name);go.transform.SetParent(r.parent!=0&&objects.ContainsKey(r.parent)?objects[r.parent].transform:root.transform,false);objects.Add(r.id,go);
        if(r.cf==null){if(pivots.TryGetValue(r.id,out var model)){var p=model.pivot;go.transform.position=new Vector3(p[0],p[1],-p[2])*scale;go.transform.rotation=Quaternion.LookRotation(new Vector3(-p[5],-p[8],p[11]),new Vector3(p[4],p[7],-p[10]));}return;}
        go.transform.position=Position(r);go.transform.rotation=Rotation(r);go.isStatic=true;
        Populate(r,go);
    }
    static void Populate(Row r,GameObject go){
        if(deferred.Contains(r.id)){if(r.collide){go.AddComponent<BoxCollider>().size=V(r.size)*scale;report.colliders++;}return;}
        if(r.alpha<1){
            if(NeedsOBJ(r)){
                bool box=r.@class=="Part"&&r.shape=="Block"&&!(r.attachments??Array.Empty<Attachment>()).Any(a=>new[]{"SpecialMesh","FileMesh","CylinderMesh"}.Contains(a.@class));
                if(box&&r.collide){go.AddComponent<BoxCollider>().size=V(r.size)*scale;report.colliders++;}
                int geometryIndex=0;
                foreach(var geom in LoadGeometry(r.id)){
                    var child=new GameObject("Mesh - ZU_"+r.id+"_"+geometryIndex);child.transform.SetParent(go.transform,false);child.isStatic=true;
                    var mesh=new Mesh{name="ZU_"+r.id+"_"+report.meshes,indexFormat=geom.vertices.Length/3>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                    mesh.vertices=Enumerable.Range(0,geom.vertices.Length/3).Select(i=>new Vector3(geom.vertices[i*3],geom.vertices[i*3+1],geom.vertices[i*3+2])).ToArray();
                    mesh.uv=Enumerable.Range(0,geom.uv.Length/2).Select(i=>new Vector2(geom.uv[i*2],geom.uv[i*2+1])).ToArray();
                    mesh.subMeshCount=geom.submeshes.Length;for(int j=0;j<geom.submeshes.Length;j++)mesh.SetTriangles(geom.submeshes[j].triangles,j);
                    if(geom.normals.Length==geom.vertices.Length)mesh.normals=Enumerable.Range(0,geom.normals.Length/3).Select(i=>new Vector3(geom.normals[i*3],geom.normals[i*3+1],geom.normals[i*3+2])).ToArray();else mesh.RecalculateNormals();
                    mesh.RecalculateBounds();mesh.RecalculateTangents();
                    string meshKey=r.@class=="MeshPart"&&geom.name=="base"?r.mesh+":"+r.material+":"+string.Join(",",r.size.Select(v=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))+":"+geom.vertices.Length:r.id+":"+geometryIndex;
                    if(nativeMeshes.TryGetValue(meshKey,out var shared)){UnityEngine.Object.DestroyImmediate(mesh);mesh=shared;}else{AssetDatabase.CreateAsset(mesh,Base+"/Meshes/"+mesh.name+".asset");nativeMeshes[meshKey]=mesh;report.meshes++;}geometryIndex++;
                    child.AddComponent<MeshFilter>().sharedMesh=mesh;var rend=child.AddComponent<MeshRenderer>();rend.sharedMaterials=geom.submeshes.Select(s=>Mat(r,s.material)).ToArray();rend.shadowCastingMode=r.shadow?ShadowCastingMode.On:ShadowCastingMode.Off;
                    if(!box&&r.collide&&geom.name!="surface"){child.AddComponent<MeshCollider>().sharedMesh=mesh;report.colliders++;}
                }
            }else{
                var mesh=NativeMesh(r);go.AddComponent<MeshFilter>().sharedMesh=mesh;var rend=go.AddComponent<MeshRenderer>();rend.sharedMaterial=Mat(r,null);rend.shadowCastingMode=r.shadow?ShadowCastingMode.On:ShadowCastingMode.Off;
                if(r.collide){if(r.@class=="Part"&&r.shape=="Block"){go.AddComponent<BoxCollider>().size=V(r.size)*scale;}else go.AddComponent<MeshCollider>().sharedMesh=mesh;report.colliders++;}
            }report.visibleParts++;
        }else if(r.collide){go.AddComponent<BoxCollider>().size=V(r.size)*scale;report.colliders++;}
        foreach(var a in r.attachments??Array.Empty<Attachment>()){
            if(a.@class=="TextLabel"&&!string.IsNullOrWhiteSpace(a.text))Label(go,r,a);
            else if(new[]{"PointLight","SpotLight","SurfaceLight"}.Contains(a.@class))Light(go,r,a);
        }
    }
    static Mesh NativeMesh(Row r){
        string key=r.@class+":"+r.shape+":"+string.Join(",",r.size.Select(v=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
        if(nativeMeshes.TryGetValue(key,out var existing))return existing;
        Mesh mesh;Vector3 size=V(r.size)*scale;
        if(r.@class=="WedgePart"){
            Vector3[] v={new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f)};
            int[][] faces={new[]{0,1,3,2},new[]{2,3,5,4},new[]{0,2,4},new[]{1,5,3},new[]{0,4,5,1}};var vs=new List<Vector3>();var ts=new List<int>();
            foreach(var face in faces){int start=vs.Count;foreach(int k in face)vs.Add(Vector3.Scale(v[k],new Vector3(size.x,size.y,-size.z)));for(int i=1;i<face.Length-1;i++){ts.Add(start);ts.Add(start+i+1);ts.Add(start+i);}}
            mesh=new Mesh();mesh.SetVertices(vs);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();
        }else{
            var temp=GameObject.CreatePrimitive(r.shape=="Cylinder"?PrimitiveType.Cylinder:r.shape=="Ball"?PrimitiveType.Sphere:PrimitiveType.Cube);mesh=UnityEngine.Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(temp);
            var vs=mesh.vertices;for(int i=0;i<vs.Length;i++){var v=vs[i];if(r.shape=="Cylinder")v=new Vector3(-v.y*.5f,v.x,v.z);vs[i]=Vector3.Scale(v,size);}mesh.vertices=vs;mesh.RecalculateNormals();
        }
        var vv=mesh.vertices;var nn=mesh.normals;var uv=new Vector2[vv.Length];for(int i=0;i<uv.Length;i++){var n=nn[i];var v=vv[i]/(scale*4);if(Mathf.Abs(n.y)>.7f)uv[i]=new Vector2(v.x,v.z);else if(Mathf.Abs(n.x)>.7f)uv[i]=new Vector2(v.z,v.y);else uv[i]=new Vector2(v.x,v.y);}mesh.uv=uv;mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name="Native_"+Hash(key);AssetDatabase.CreateAsset(mesh,Base+"/Meshes/"+mesh.name+".asset");nativeMeshes.Add(key,mesh);report.meshes++;return mesh;
    }
    static Material Mat(Row row,string sourceName){
        SourceMaterial sm=null;if(sourceName!=null)sources.TryGetValue(sourceName,out sm);
        Color color=sm!=null?new Color(Mathf.Pow(sm.color[0],1/2.2f),Mathf.Pow(sm.color[1],1/2.2f),Mathf.Pow(sm.color[2],1/2.2f)):C(row.color);color.a=sm!=null?sm.alpha:1-row.alpha;
        string texture=sm?.texture??"";string normal=sm?.normal??"";
        string key=row.material+":"+color.ToString("F4")+":"+texture+":"+normal+":"+sm?.specular+":"+sm?.emission+":"+sm?.alphaMode+":"+sm?.doubleSided;
        if(materials.TryGetValue(key,out var old))return old;
        var shader=Shader.Find(urp?"Universal Render Pipeline/Lit":"Standard");if(shader==null)throw new Exception("Required render pipeline shader unavailable");
        var mat=new Material(shader){name="ZU_"+row.material+"_"+Hash(key)};mat.color=color;
        float smooth=row.material=="Glass"?.8f:row.material=="Metal"?.3f:row.material=="SmoothPlastic"?.25f:.08f;
        mat.SetFloat(urp?"_Smoothness":"_Glossiness",smooth);mat.SetFloat("_Metallic",row.material.Contains("Metal")?.45f:0);
        if(!string.IsNullOrEmpty(texture)){var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"/Source/Geometry/"+texture);mat.SetTexture(urp?"_BaseMap":"_MainTex",tex);if(tex==null)throw new Exception("Texture missing: "+texture);}
        else{
            string equivalent=null;
            switch(row.material){case"Brick":equivalent="ArchitecturalMaterials/Textures/T_YFAM_BricksGray";break;case"Wood":case"WoodPlanks":equivalent="ArchitecturalMaterials/Textures/T_YFAM_WoodFlooring";break;case"CeramicTiles":equivalent="ArchitecturalMaterials/Textures/T_YFAM_TilesWorn";break;case"Plaster":equivalent="ArchitecturalMaterials/Textures/T_YFAM_Plasterboard";break;case"Grass":equivalent="GroundMaterials/Textures/T_YFGM_Grass01";break;case"Ground":equivalent="GroundMaterials/Textures/T_YFGM_Ground01";break;case"Sand":equivalent="GroundMaterials/Textures/T_YFGM_SandySoil";break;}
            if(equivalent!=null){var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/YughuesFree"+equivalent+"_d.tga");var bump=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/YughuesFree"+equivalent+"_n.tga");if(albedo)mat.SetTexture(urp?"_BaseMap":"_MainTex",albedo);if(bump){mat.SetTexture("_BumpMap",bump);mat.SetFloat("_BumpScale",.35f);mat.EnableKeyword("_NORMALMAP");}}
        }
        if(!string.IsNullOrEmpty(normal)){var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"/Source/Geometry/"+normal);mat.SetTexture("_BumpMap",tex);mat.EnableKeyword("_NORMALMAP");}
        // Imported vegetation and mesh sheets require double-sided rendering.
        mat.SetFloat("_Cull",0);mat.doubleSidedGI=true;
        bool foliage=row.path.IndexOf("tree",StringComparison.OrdinalIgnoreCase)>=0||row.path.IndexOf("foliage",StringComparison.OrdinalIgnoreCase)>=0||row.path.IndexOf("shrub",StringComparison.OrdinalIgnoreCase)>=0||(row.attachments??Array.Empty<Attachment>()).Any(a=>a.@class=="SurfaceAppearance"&&a.alphaMode=="Transparency");
        if((foliage||(sm?.doubleSided??false))&&!urp&&!string.IsNullOrEmpty(texture)){var twoSided=Shader.Find("ZombieUnity/Foliage");if(twoSided)mat.shader=twoSided;}
        bool overlay=sm?.alphaMode=="overlay";mat.SetFloat("_RobloxOverlay",overlay?1:0);
        if(foliage&&!string.IsNullOrEmpty(texture)){mat.EnableKeyword("_ALPHATEST_ON");mat.SetFloat("_Cutoff",overlay?0:.35f);mat.SetFloat(urp?"_AlphaClip":"_Mode",urp?1:1);mat.renderQueue=2450;}
        else if(color.a<.99f){mat.SetOverrideTag("RenderType","Transparent");mat.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);if(urp){mat.SetFloat("_Surface",1);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");}else{mat.SetFloat("_Mode",3);mat.EnableKeyword("_ALPHABLEND_ON");}mat.renderQueue=3000;}
        if(row.material=="Neon"){mat.SetColor("_EmissionColor",color*1.2f);mat.EnableKeyword("_EMISSION");}
        if(!string.IsNullOrEmpty(sm?.emission)){mat.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"/Source/Geometry/"+sm.emission));mat.SetColor("_EmissionColor",Color.white);mat.EnableKeyword("_EMISSION");}
        if(!string.IsNullOrEmpty(sm?.specular))mat.SetTexture("_SpecGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"/Source/Geometry/"+sm.specular));
        string path=Base+"/Materials/"+mat.name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing){UnityEngine.Object.DestroyImmediate(mat);materials.Add(key,existing);return existing;}
        AssetDatabase.CreateAsset(mat,path);materials.Add(key,mat);report.materials++;return mat;
    }
    static Vector3 Face(string s){switch(s){case"Back":return Vector3.back;case"Left":return Vector3.left;case"Right":return Vector3.right;case"Top":return Vector3.up;case"Bottom":return Vector3.down;default:return Vector3.forward;}}
    static void Label(GameObject parent,Row r,Attachment a){
        var g=new GameObject("Label - "+a.text);g.transform.SetParent(parent.transform,false);var f=Face(a.face);g.transform.localPosition=Vector3.Scale(f,V(r.size))*scale*.5f+f*.002f;
        g.transform.localRotation=Quaternion.LookRotation(-f,Mathf.Abs(f.y)>.9f?Vector3.forward:Vector3.up);
        var text=g.AddComponent<TMPro.TextMeshPro>();text.text=a.text;text.font=TMPro.TMP_Settings.defaultFontAsset;text.color=C(a.color);text.alignment=TMPro.TextAlignmentOptions.Center;text.enableAutoSizing=true;text.fontSizeMin=.01f;text.fontSizeMax=12;text.overflowMode=TMPro.TextOverflowModes.Truncate;
        var rect=text.rectTransform;rect.sizeDelta=new Vector2(r.size[0]*scale*.94f,r.size[1]*scale*.86f);report.labels++;
    }
    static void Light(GameObject parent,Row r,Attachment a){
        var g=new GameObject(a.@class);g.transform.SetParent(parent.transform,false);var f=Face(a.face);g.transform.localPosition=Vector3.Scale(f,V(r.size))*scale*.5f;g.transform.localRotation=Quaternion.LookRotation(f,Mathf.Abs(f.y)>.9f?Vector3.forward:Vector3.up);var l=g.AddComponent<Light>();l.enabled=a.enabled;l.color=C(a.color);l.intensity=Mathf.Min(a.brightness,2);l.range=a.range*scale;l.type=a.@class=="PointLight"?LightType.Point:LightType.Spot;l.spotAngle=Mathf.Clamp(a.angle,1,170);l.shadows=a.shadows?LightShadows.Soft:LightShadows.None;report.lights++;
    }
    static void Finish(Scene scene){
        packReader?.Dispose();packReader=null;
        root.SetActive(true);
        var environment=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Environment");if(environment){Undo.RecordObject(environment,"Suspend old map geometry");environment.SetActive(false);}
        PrefabUtility.SaveAsPrefabAsset(root,Base+"/Prefabs/ZombieUnity_Map.prefab");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Control);File.WriteAllText(Path.Combine(Control,"import-report.json"),JsonUtility.ToJson(report,true));File.WriteAllText(Base+"/Reports/ImportReport.json",JsonUtility.ToJson(report,true));
        Selection.activeGameObject=root;var view=SceneView.lastActiveSceneView;if(view){view.LookAt(new Vector3(0,0,0),Quaternion.Euler(55,35,0),170,false,true);}
        EditorUtility.ClearProgressBar();busy=false;Debug.Log("ZombieUnity map imported: "+report.visibleParts+" visible parts; original scene preserved in "+report.backup);
    }
}
