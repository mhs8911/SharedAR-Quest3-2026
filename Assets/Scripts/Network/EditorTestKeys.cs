using UnityEngine;
using UnityEngine.InputSystem;

public class EditorTestKeys : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current == null) return;

        var net = SharedARNetworkManager.Instance;
        if (net != null)
        {
            if (Keyboard.current.hKey.wasPressedThisFrame) net.StartAsHost();
            if (Keyboard.current.jKey.wasPressedThisFrame) net.StartAsClient("127.0.0.1");
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            var spawner = SharedObjectSpawner.Instance;
            if (spawner == null)
            {
                Debug.LogError("[EditorTestKeys] S pressed, but SharedObjectSpawner.Instance is null. " +
                               "The component is disabled, missing, or on an inactive GameObject.");
            }
            else
            {
                Debug.Log("[EditorTestKeys] S pressed. Calling SpawnSharedObject().");
                spawner.SpawnSharedObject();
            }
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            var spawner = SharedObjectSpawner.Instance;
            if (spawner != null) spawner.DespawnSharedObject();
        }
    }
#endif
}
