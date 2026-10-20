using UnityEngine;
using Praktikum5.FSM;
using UTS.Zombies;

namespace UTS.Audio
{
    [DisallowMultipleComponent]
    public sealed class ActorSurvivalAudio : MonoBehaviour
    {
        private SimplePlayerController player;
        private EnemyFSM enemy;
        private ZombieSwarmAgent zombie;
        private CharacterController capsule;
        private UTS.Inventory.PlayerInventory inventory;
        private UTS.Player.PlayerFlashlight flashlight;
        private Vector3 previousPosition;
        private float distance,previousHealth,nextGroan,groanReady,nextHurt,nextPackWarning;
        private bool previousFlashlight,previousDead;
        private int previousItems;
        private readonly RaycastHit[] groundHits=new RaycastHit[20];
        private void Awake(){player=GetComponent<SimplePlayerController>();enemy=GetComponent<EnemyFSM>();zombie=GetComponent<ZombieSwarmAgent>();capsule=GetComponent<CharacterController>();inventory=GetComponent<UTS.Inventory.PlayerInventory>();flashlight=GetComponent<UTS.Player.PlayerFlashlight>();}
        private void OnEnable(){
            previousPosition=transform.position;previousHealth=player?player.CurrentHealth:enemy?enemy.CurrentHealth:100;
            previousDead=false;groanReady=Time.time+Random.Range(1,4);nextGroan=groanReady;
            if(player){player.OnPunch+=Swing;player.OnPunchHit+=Hit;player.OnHealthChanged+=HealthChanged;}
            if(enemy)enemy.OnHealthChanged+=HealthChanged;
            if(zombie)zombie.OnAttack+=ZombieAttack;
            if(inventory){previousItems=TotalItems();inventory.OnInventoryChanged+=InventoryChanged;}
            if(flashlight)previousFlashlight=flashlight.IsOn;
        }
        private void Start(){
            previousHealth=player?player.CurrentHealth:enemy?enemy.CurrentHealth:100;
            // SimplePlayerController can add these helpers during its Awake, after this component's Awake.
            if(player&&!inventory){inventory=GetComponent<UTS.Inventory.PlayerInventory>();if(inventory){previousItems=TotalItems();inventory.OnInventoryChanged+=InventoryChanged;}}
            if(player&&!flashlight){flashlight=GetComponent<UTS.Player.PlayerFlashlight>();if(flashlight)previousFlashlight=flashlight.IsOn;}
        }
        private void OnDisable(){
            if(player){player.OnPunch-=Swing;player.OnPunchHit-=Hit;player.OnHealthChanged-=HealthChanged;}
            if(enemy)enemy.OnHealthChanged-=HealthChanged;
            if(zombie)zombie.OnAttack-=ZombieAttack;
            if(inventory)inventory.OnInventoryChanged-=InventoryChanged;CancelInvoke();
        }
        private int TotalItems()=>inventory.BandageCount+inventory.FirstAidCount+inventory.MedkitCount;
        private void InventoryChanged(){int count=TotalItems();if(count>previousItems)SurvivalAudioDirector.Emit(SfxCue.Pickup,transform.position);previousItems=count;}
        public void InventoryFull(){if(Time.unscaledTime<nextPackWarning)return;nextPackWarning=Time.unscaledTime+1.5f;SurvivalAudioDirector.Emit(SfxCue.InventoryFull,transform.position);}
        private void Swing(){SurvivalAudioDirector.Emit(SfxCue.PunchSwing,transform.position+Vector3.up*.4f);}
        private void Hit(EnemyFSM target){if(target)SurvivalAudioDirector.Emit(SfxCue.PunchHit,target.transform.position+Vector3.up*.25f);}
        private void ZombieAttack(){nextGroan=Time.time+Random.Range(4,7);SurvivalAudioDirector.Emit(SfxCue.ZombieAttack,transform.position+Vector3.up*.5f);}
        private void HealthChanged(float current,float max){
            if(current<previousHealth){
                if(current<=0&&!previousDead){previousDead=true;SurvivalAudioDirector.Emit(player?SfxCue.PlayerDeath:SfxCue.ZombieDeath,transform.position+Vector3.up*.25f,1,player?1:.85f);Invoke(nameof(Fall),1.7f);}
                else if(current>0&&Time.time>=nextHurt){nextHurt=Time.time+.65f;SurvivalAudioDirector.Emit(player?SfxCue.PlayerHurt:SfxCue.ZombieAttack,transform.position+Vector3.up*.4f,.8f,player?1:.86f);}
            }
            if(current>0&&previousDead){previousDead=false;CancelInvoke(nameof(Fall));}
            previousHealth=current;
        }
        private void Fall(){SurvivalAudioDirector.Emit(SfxCue.BodyFall,transform.position-Vector3.up*.65f,.9f);}
        private void Update(){
            var position=transform.position;var delta=position-previousPosition;delta.y=0;previousPosition=position;
            bool dead=player?player.IsDead:enemy&&enemy.IsDead;
            if(flashlight&&flashlight.IsOn!=previousFlashlight){previousFlashlight=flashlight.IsOn;SurvivalAudioDirector.Emit(SfxCue.Flashlight,position);}
            if(dead){distance=0;return;}
            bool moving=delta.sqrMagnitude>.00001f&&delta.sqrMagnitude<4;
            if(player&&capsule&&!capsule.isGrounded)moving=false;
            if(moving){
                distance+=delta.magnitude;float step=player?(player.IsCrouching?.68f:player.CurrentSpeed>3.2f?1.55f:1.0f):1.2f;
                if(distance>=step){distance%=step;var cue=GroundCue(out var foot);float gain=player?(player.IsCrouching?.24f:player.CurrentSpeed>3.2f?1f:.72f):.32f;SurvivalAudioDirector.Emit(cue,foot,gain,player?1:.86f);}
            }else distance=0;
            if(zombie&&Time.time>=nextGroan){nextGroan=Time.time+Random.Range(6,12);if(zombie.CurrentState!=ZombieSwarmAgent.PZZombieState.Attacking)SurvivalAudioDirector.Emit(SfxCue.ZombieGroan,position+Vector3.up*.5f,.85f,Random.Range(.88f,1.08f));}
        }
        private SfxCue GroundCue(out Vector3 contact){
            contact=transform.position-Vector3.up*.9f;float nearest=10;Collider floor=null;
            int n=Physics.RaycastNonAlloc(transform.position+Vector3.up*.35f,Vector3.down,groundHits,3,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++){var h=groundHits[i];if(h.collider.transform.IsChildOf(transform)||h.collider.GetComponentInParent<EnemyFSM>()||h.normal.y<.65f||h.distance>=nearest)continue;floor=h.collider;nearest=h.distance;contact=h.point;}
            if(!floor)return SfxCue.FootConcrete;
            string name=floor.name.ToLowerInvariant();var r=floor.GetComponent<Renderer>();if(r&&r.sharedMaterial)name+=" "+r.sharedMaterial.name.ToLowerInvariant();
            if(name.Contains("wood")||name.Contains("plank")||name.Contains("timber"))return SfxCue.FootWood;
            if(name.Contains("carpet")||name.Contains("rug"))return SfxCue.FootCarpet;
            if(name.Contains("grass")||name.Contains("soil")||name.Contains("earth")||name.Contains("dirt")||name.Contains("terrain")||name.Contains("lawn"))return SfxCue.FootGrass;
            return SfxCue.FootConcrete;
        }
    }
}
