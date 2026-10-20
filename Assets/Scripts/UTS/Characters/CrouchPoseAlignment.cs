using UnityEngine;
using Praktikum5.FSM;

namespace UTS.Characters
{
    // Retain the supplied foot trajectories while matching the deeper idle crouch.
    [RequireComponent(typeof(Animator))]
    public sealed class CrouchPoseAlignment : MonoBehaviour
    {
        [SerializeField, Range(0,.5f)] private float movingBodyLowering=.4f;
        private Animator animator;
        private SimplePlayerController player;
        private CharacterController capsule;
        private float lowering,velocity;
        private void Awake(){animator=GetComponent<Animator>();player=GetComponentInParent<SimplePlayerController>();capsule=player?player.GetComponent<CharacterController>():null;}
        private void OnAnimatorIK(int layer){
            if(!player||!animator.isHuman)return;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,0);animator.SetIKPositionWeight(AvatarIKGoal.RightFoot,0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,0);animator.SetIKRotationWeight(AvatarIKGoal.RightFoot,0);
            float target=0;
            if(player.IsCrouching&&capsule){
                float feet=player.transform.TransformPoint(capsule.center).y-capsule.height*.5f;
                float head=animator.GetBoneTransform(HumanBodyBones.Head).position.y;
                target=Mathf.Clamp(head-(feet+capsule.height-.22f),0,movingBodyLowering)*Mathf.Clamp01(player.CurrentSpeed/1.1f);
            }
            lowering=Mathf.SmoothDamp(lowering,target,ref velocity,.12f);
            if(lowering<.001f)return;
            var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var lp=left.position;var rp=right.position;var lr=left.rotation;var rr=right.rotation;
            animator.bodyPosition-=Vector3.up*lowering;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,1);animator.SetIKPositionWeight(AvatarIKGoal.RightFoot,1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,1);animator.SetIKRotationWeight(AvatarIKGoal.RightFoot,1);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot,lp);animator.SetIKPosition(AvatarIKGoal.RightFoot,rp);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot,lr);animator.SetIKRotation(AvatarIKGoal.RightFoot,rr);
        }
    }
}
