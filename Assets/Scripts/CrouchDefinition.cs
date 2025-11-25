using UnityEngine;

[CreateAssetMenu(fileName = "CrouchAction",menuName = "TPP/Action/CrouchAction")]
public class CrouchDefinition : ScriptableObject
{
    [Header("Crouch dimensions")]
    public float crouchHeight = 1f;
    public float transitionSpeed = 10f;

    [Header("Movement")]
    [Range(0f, 1f)]
    public float speedMultiplier = 0.55f;

    [Header("Animation")]
    public string animBool = "Crouched"; // animator bool parameter

    [Header("Rules")]
    public bool toggleMode = true;   // true = press to toggle, false = hold
    public bool disableSprint = true;
    public bool disableJump = false;

}
