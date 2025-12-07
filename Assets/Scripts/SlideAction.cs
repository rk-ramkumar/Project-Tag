using System.Collections;
using UnityEngine;

namespace TPP
{
    public class SlideAction : ActionComponent
    {
        public SlideDefinition def;

        private bool sliding = false;
        private float slideEndTime;
        private float currentSpeed;
        private Vector3 slideDirection;
        private float originalHeight;
        private Vector3 originalCenter;
        private GameObject vfxInstance;
        private CrouchAction crouchAction;

        public override void Awake()
        {
            base.Awake();

            crouchAction = GetComponent<CrouchAction>();

            originalCenter = cc.center;
            originalHeight = cc.height;
        }


        private void Update()
        {
            if (def == null)
            {
                return;
            }

            if (!sliding && input != null)
            {
                        Debug.Log(input.crouch);
                if(input.crouch && (!def.requiresSprint || input.sprint))
                {
                    if(VelocityMagnitude >= def.minSpeed)
                    {
                        TryUse();
                    }
                }

            }

            if (sliding)
            {
                RunSlide();
            }
        }

        public override bool CanUse()
        {
            if(!base.CanUse()) return false;
            if(!player.Grounded) return false;
            if (def.requiresSprint && !input.sprint) return false;
            if (VelocityMagnitude < def.minForwardSpeed) return false;
            return true;
        }

        protected override void Use()
        {
            base.Use();

            // start slide
            sliding = true;
            slideEndTime = Time.time + def.duration;

            slideDirection = transform.forward;

            currentSpeed = Mathf.Max(def.minSpeed, VelocityMagnitude * def.initialSpeedMultiplier);

            cc.height = def.slideHeight;
            cc.center = new Vector3(originalCenter.x, def.slideHeight / 2f, originalCenter.z);

            // block normal movement
            player.SetMovementBlocked(true, this);

            // vfx/sfx/anim
            if (def.vfxPrefab) vfxInstance = Instantiate(def.vfxPrefab, transform);
            if (def.sfx) AudioSource.PlayClipAtPoint(def.sfx, transform.position);
            if (!string.IsNullOrEmpty(def.animTrigger) && player.AnimatorExists)
            {
                player.Animator.SetBool(def.animTrigger, true);
            }
        }
        public void RunSlide()
        {
            // Move forward with currentSpeed; decay with friction
            cc.Move(slideDirection * currentSpeed * Time.deltaTime);

            // apply friction to reduce speed over time (momentum feel)
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, def.friction * Time.deltaTime);

            // end conditions
            if (Time.time >= slideEndTime || currentSpeed <= 0.1f)
            {
                StopAction();
            }
        }
        public override void StopAction()
        {
            base.StopAction();
            sliding = false;

            // spawn fade of vfx if needed
            if (vfxInstance) Destroy(vfxInstance, 0.6f);

            // unblock movement
            player.SetMovementBlocked(false, this);

            // decide end state: if crouch still held and def says so, activate crouch action
            if (def.endInCrouchIfHeld && input != null && input.crouch)
            {
                
                if (crouchAction != null && !crouchAction.IsCrouched)
                {
                    // toggle crouch on (we don't consume input here)
                    crouchAction.ForceEnterCrouch();
                }
            }
            if (!string.IsNullOrEmpty(def.animTrigger) && player.AnimatorExists)
            {
                player.Animator.SetBool(def.animTrigger, false);
            }
            // smooth recover capsule back to standing (we lerp in coroutine to keep Update simple)
            StartCoroutine(RecoverCapsule());
        }

        private IEnumerator RecoverCapsule()
        {
            float t = 0f;
            float startHeight = cc.height;
            float duration = 1f / Mathf.Max(0.1f, def.recoverSpeed);

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float h = Mathf.Lerp(startHeight, originalHeight, t);
                cc.height = h;
                cc.center = new Vector3(originalCenter.x, h / 2f, originalCenter.z);
                yield return null;
            }

            cc.height = originalHeight;
            cc.center = originalCenter;
        }

        public bool IsSliding() => sliding;

        private float VelocityMagnitude => new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
    }
}
