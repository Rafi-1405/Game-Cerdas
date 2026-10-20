using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;

public static class CharacterUpgradeNavigationQA
{
    [Serializable] class Route{public string name,status;public bool sampled,complete;public float length;public Vector3 start,end;}
    [Serializable] class Result{public int triangles,agentType;public string surface,data;public Route[] routes,doorways;public bool humanoidPlayer,humanoidZombie;}
    static Route Path(string name,Vector3 a,Vector3 b,NavMeshQueryFilter q,float sample=1.5f){var r=new Route{name=name};var path=new NavMeshPath();r.sampled=NavMesh.SamplePosition(a,out var h1,sample,q)&&NavMesh.SamplePosition(b,out var h2,sample,q);if(!r.sampled){r.status="No nearby NavMesh";return r;}NavMesh.SamplePosition(b,out var end,sample,q);r.start=h1.position;r.end=end.position;NavMesh.CalculatePath(r.start,r.end,q,path);r.status=path.status.ToString();r.complete=path.status==NavMeshPathStatus.PathComplete;for(int i=1;i<path.corners.Length;i++)r.length+=Vector3.Distance(path.corners[i-1],path.corners[i]);return r;}
    public static void Audit(){
        var n=UnityEngine.Object.FindAnyObjectByType<NavMeshSurface>();if(!n)throw new Exception("No active navigation surface");var q=new NavMeshQueryFilter{agentTypeID=n.agentTypeID,areaMask=NavMesh.AllAreas};var result=new Result{triangles=NavMesh.CalculateTriangulation().indices.Length/3,agentType=n.agentTypeID,surface=n.name,data=AssetDatabase.GetAssetPath(n.navMeshData)};
        var routes=new List<Route>();var origin=new Vector3(5.6f,0,0);routes.Add(Path("Main street to fortress camp",origin,new Vector3(5.6f,0,65),q));routes.Add(Path("Main street to minimarket",origin,new Vector3(-20,0,-16),q));routes.Add(Path("Main street to townhall",origin,new Vector3(-18,0,28),q));routes.Add(Path("Main street to east service station",origin,new Vector3(48,0,-19),q));routes.Add(Path("Main street to west neighborhood",origin,new Vector3(-65,0,0),q));routes.Add(Path("Main street to east neighborhood",origin,new Vector3(80,0,0),q));routes.Add(Path("Scene Enemy to main street",GameObject.Find("Enemy").transform.position,origin,q,2));result.routes=routes.ToArray();
        var map=GameObject.Find("ZombieUnity - Migrated Map");var doors=new List<Route>();foreach(var t in map.GetComponentsInChildren<Transform>().Where(t=>t.name=="Door head")){var p=t.position-Vector3.up*2.15f;doors.Add(Path("Direct doorway: "+t.parent.parent.name+" "+t.position,p+t.forward*.5f,p-t.forward*.5f,q,.4f));}result.doorways=doors.ToArray();
        var player=GameObject.Find("Player").GetComponentInChildren<Animator>();var zombie=GameObject.Find("Enemy").GetComponentInChildren<Animator>();result.humanoidPlayer=player&&player.isHuman;result.humanoidZombie=zombie&&zombie.isHuman;
        string folder=System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Library/ZU_Characters");File.WriteAllText(System.IO.Path.Combine(folder,"navigation-audit.json"),JsonUtility.ToJson(result,true));
    }
}
