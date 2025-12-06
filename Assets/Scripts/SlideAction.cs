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

        public override void Awake()
        {
            base.Awake();

            originalCenter = cc.center;
            originalHeight = cc.height;
        }
    }
}
