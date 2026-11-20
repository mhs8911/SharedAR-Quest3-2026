// ============================================================================
// Mariam code addition
// File: Scripts/Anchors/ARSceneBootstrapper.cs
// ONE CHANGE: initWaitTime default 1.5 -> 0.5 seconds. It used to wait for the Oculus
// Platform SDK to initialize. That script is deleted, so there is nothing to wait for.
// (A value already saved in the scene's Inspector overrides this default.)
// Keep it in Bootstrap.unity next to the NetworkManager.
// ============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ARSceneBootstrapper : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "SharedARMain";
    [SerializeField] private float  initWaitTime  = 0.5f;   // [CHANGED] was 1.5f

    private void Start() => StartCoroutine(LoadMainScene());

    private IEnumerator LoadMainScene()
    {
        Debug.Log("[Bootstrap] Waiting for init...");
        yield return new WaitForSeconds(initWaitTime);

        // Optional: show a loading UI here

        Debug.Log($"[Bootstrap] Loading scene: {mainSceneName}");
        SceneManager.LoadScene(mainSceneName);
    }
}
