using UnityEngine;

[CreateAssetMenu(fileName ="SlideAction", menuName = "TPP/Action/SlideAction")]
public class SlideDefinition : ScriptableObject
{
    [Header("Timing and Movement")]
    public float duration = 0.5f;           // total slide time
    public float initialSpeedMultiplier = 1.3f; // multiply current velocity at start
    public float minSpeed = 6f;             // minimum slide speed if player was slow
    public float friction = 6f;             // how quickly slide speed decays (higher => faster slow down)

    [Header("Capsule")]
    public float slideHeight = 0.9f;        // immediate height set at start
    public float recoverSpeed = 8f;         // lerp speed to recover capsule to stand

    [Header("Requirements")]
    public float minForwardSpeed = 6f;      // minimum forward velocity to allow slide
    public bool requiresSprint = true;      // only allow slide while sprinting

    [Header("Animation / VFX")]
    public string animTrigger = "Slide";
    public GameObject vfxPrefab;
    public AudioClip sfx;

    [Header("Rules")]
    [Tooltip("If true, when slide ends player will return to crouch if crouch input still held")]
    public bool endInCrouchIfHeld = true;
}
