using UnityEngine;

namespace TPP
{
    [RequireComponent(typeof(CharacterController))]
    public class SlideAction : ActionComponent
    {
        public SlideDefinition def;

        public override void Awake()
        {
            base.Awake();
        }
    }
}
