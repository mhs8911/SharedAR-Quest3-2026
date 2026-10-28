// Scripts/Objects/SpawnTrigger.cs
// Role: A thin input bridge. Listens for controller button presses
//       or hand pinch gestures and calls SharedObjectSpawner.
//       Attach to any active GameObject in SharedARMain.unity.

using UnityEngine;
using UnityEngine.InputSystem;   // requires com.unity.inputsystem

public class SpawnTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SharedObjectSpawner spawner;

    [Header("Input — Controller (OVR)")]
    [Tooltip("Uses OVR input directly as a fallback if Input System not configured")]
    [SerializeField] private OVRInput.Button spawnButton      = OVRInput.Button.One; // A button
    [SerializeField] private OVRInput.Button despawnButton    = OVRInput.Button.Two; // B button

    [Header("Input — Hand Tracking")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;
    [Tooltip("Pinch on both hands simultaneously to spawn")]
    [SerializeField] private bool requireBothHandsPinch = false;

    private bool _spawnCooldown   = false;
    private bool _despawnCooldown = false;
    private const float COOLDOWN  = 0.5f;

    private void Update()
    {
        HandleControllerInput();
        HandleHandTrackingInput();
    }

    // ── Controller Input ─────────────────────────────────────────────────────

    private void HandleControllerInput()
    {
        if (!_spawnCooldown && OVRInput.GetDown(spawnButton))
        {
            spawner.SpawnSharedObject();
            StartCoroutine(Cooldown(c => _spawnCooldown = c));
        }

        if (!_despawnCooldown && OVRInput.GetDown(despawnButton))
        {
            spawner.DespawnSharedObject();
            StartCoroutine(Cooldown(c => _despawnCooldown = c));
        }
    }

    // ── Hand Tracking Input ──────────────────────────────────────────────────

    private void HandleHandTrackingInput()
    {
        if (leftHand == null || rightHand == null) return;

        bool leftPinch  = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool rightPinch = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        bool shouldSpawn = requireBothHandsPinch
            ? (leftPinch && rightPinch)
            : (leftPinch || rightPinch);

        if (!_spawnCooldown && shouldSpawn)
        {
            spawner.SpawnSharedObject();
            StartCoroutine(Cooldown(c => _spawnCooldown = c));
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator Cooldown(System.Action<bool> setter)
    {
        setter(true);
        yield return new WaitForSeconds(COOLDOWN);
        setter(false);
    }
}
