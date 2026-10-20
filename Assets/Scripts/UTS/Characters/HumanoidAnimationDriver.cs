using UnityEngine;
using UnityEngine.AI;
using Praktikum5.FSM;
using UTS.Zombies;

namespace UTS.Characters
{
    [DisallowMultipleComponent]
    public sealed class HumanoidAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private SimplePlayerController player;
        private CharacterController capsule;
        private NavMeshAgent agent;
        private ZombieSwarmAgent zombie;
        private EnemyFSM health;
        private bool dead;
        private Vector3 visualRestPosition;
        private SkinnedMeshRenderer[] deathRenderers;
        private Mesh deathContactMesh;
        private readonly System.Collections.Generic.List<Vector3> deathVertices=new System.Collections.Generic.List<Vector3>();
        private bool deathContactSettled;
        public Animator Animator => animator;
        static readonly int Speed=UnityEngine.Animator.StringToHash("Speed");
        static readonly int Crouching=UnityEngine.Animator.StringToHash("Crouching");
        static readonly int Dead=UnityEngine.Animator.StringToHash("Dead");
        static readonly int Punch=UnityEngine.Animator.StringToHash("Punch");
        static readonly int Attack=UnityEngine.Animator.StringToHash("Attack");
        private void Awake(){
            if(!animator)animator=GetComponentInChildren<Animator>();
            player=GetComponent<SimplePlayerController>();capsule=GetComponent<CharacterController>();
            agent=GetComponent<NavMeshAgent>();zombie=GetComponent<ZombieSwarmAgent>();health=GetComponent<EnemyFSM>();
            if(animator){visualRestPosition=animator.transform.localPosition;deathRenderers=animator.GetComponentsInChildren<SkinnedMeshRenderer>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}
        }
        private void OnEnable(){if(player)player.OnPunch+=PlayPunch;if(zombie)zombie.OnAttack+=PlayAttack;}
        private void OnDisable(){if(player)player.OnPunch-=PlayPunch;if(zombie)zombie.OnAttack-=PlayAttack;}
        private void PlayPunch(){if(animator&&!dead)animator.SetTrigger(Punch);}
        private void PlayAttack(){if(animator&&!dead)animator.SetTrigger(Attack);}
        private void LateUpdate(){
            if(!animator)return;
            if(player){
                if(player.IsDead&&!dead){dead=true;deathContactSettled=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.ResetTrigger(Punch);animator.SetBool(Crouching,false);animator.SetFloat(Speed,0);animator.SetBool(Dead,true);}
                else if(!player.IsDead&&dead){dead=false;deathContactSettled=false;animator.transform.localPosition=visualRestPosition;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;animator.SetBool(Dead,false);animator.Play("Standing",0,0);}
                if(dead){CorrectPlayerDeathContact();return;}
            }
            var velocity=capsule?capsule.velocity:agent&&agent.enabled&&agent.isOnNavMesh?agent.velocity:Vector3.zero;velocity.y=0;
            animator.SetFloat(Speed,velocity.magnitude,.12f,Time.deltaTime);
            if(player){animator.SetBool(Crouching,player.IsCrouching);return;}
            if(health&&health.IsDead&&!dead){dead=true;animator.ResetTrigger(Attack);animator.SetBool(Dead,true);foreach(var c in GetComponents<Collider>())c.enabled=false;}
        }
        // The reused death clip puts PlayerChar's skull below its support plane.
        // During death only, lift the entire visual by measured skin contact; retain all joint animation.
        private void CorrectPlayerDeathContact(){
            if(deathContactSettled&&capsule.velocity.sqrMagnitude<.0001f)return;
            animator.transform.localPosition=visualRestPosition;
            if(!deathContactMesh)deathContactMesh=new Mesh{name="Player death ground contact"};
            float lowest=float.PositiveInfinity;
            foreach(var r in deathRenderers){
                if(!r||!r.enabled||!r.sharedMesh)continue;
                r.BakeMesh(deathContactMesh);deathContactMesh.GetVertices(deathVertices);
                foreach(var v in deathVertices)lowest=Mathf.Min(lowest,r.transform.TransformPoint(v).y);
            }
            if(!float.IsInfinity(lowest)){
                float floor=transform.TransformPoint(capsule.center).y-capsule.height*.5f;
                float lift=Mathf.Max(0,floor+.006f-lowest);
                var parent=animator.transform.parent;
                animator.transform.localPosition=visualRestPosition+(parent?parent.InverseTransformVector(Vector3.up*lift):Vector3.up*lift);
            }
            var state=animator.GetCurrentAnimatorStateInfo(0);
            deathContactSettled=state.IsName("Death")&&state.normalizedTime>=1.05f;
        }
        private void OnDestroy(){if(deathContactMesh)Destroy(deathContactMesh);}
    }
}
