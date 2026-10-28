// Scripts/OculusPlatformInit.cs
// Role: Must run FIRST before any Oculus Platform calls (Meta User ID, sharing).
//       Place this on a GameObject in Bootstrap.unity and mark it DontDestroyOnLoad.

using Oculus.Platform;
using UnityEngine;

public class OculusPlatformInit : MonoBehaviour
{
    [Tooltip("Your Meta App ID from developer.oculus.com")]
    [SerializeField] private string appId = "YOUR_META_APP_ID_HERE";

    public static bool IsInitialized { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitializePlatform();
    }

    private async void InitializePlatform()
    {
        try
        {
            Core.AsyncInitialize(appId);
            Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCallback);
            Debug.Log("[Platform] Oculus Platform SDK initialized.");
            IsInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Platform] Init failed: {e.Message}");
        }
    }

    private void EntitlementCallback(Message msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("[Platform] Entitlement check FAILED — user may not own the app.");
            // In production: quit or show an error screen
            // Application.Quit();
        }
        else
        {
            Debug.Log("[Platform] Entitlement check passed.");
        }
    }
}
