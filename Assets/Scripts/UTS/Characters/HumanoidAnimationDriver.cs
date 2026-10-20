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
            if(animator){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}
        }
        private void OnEnable(){if(player)player.OnPunch+=PlayPunch;if(zombie)zombie.OnAttack+=PlayAttack;}
        private void OnDisable(){if(player)player.OnPunch-=PlayPunch;if(zombie)zombie.OnAttack-=PlayAttack;}
        private void PlayPunch(){if(animator&&!dead)animator.SetTrigger(Punch);}
        private void PlayAttack(){if(animator&&!dead)animator.SetTrigger(Attack);}
        private void LateUpdate(){
            if(!animator)return;
            var velocity=capsule?capsule.velocity:agent&&agent.enabled&&agent.isOnNavMesh?agent.velocity:Vector3.zero;velocity.y=0;
            animator.SetFloat(Speed,velocity.magnitude,.12f,Time.deltaTime);
            if(player){animator.SetBool(Crouching,player.IsCrouching);return;}
            if(health&&health.IsDead&&!dead){dead=true;animator.ResetTrigger(Attack);animator.SetBool(Dead,true);foreach(var c in GetComponents<Collider>())c.enabled=false;}
        }
    }
}
