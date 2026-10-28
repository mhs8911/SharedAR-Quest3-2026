// Scripts/ARSceneBootstrapper.cs
// Role: Sits in Bootstrap.unity. Waits for platform init,
//       then loads the main AR scene. Keeps all DontDestroyOnLoad
//       managers alive across the scene transition.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ARSceneBootstrapper : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "SharedARMain";
    [SerializeField] private float  initWaitTime  = 1.5f; // give platform SDK time to init

    private void Start() => StartCoroutine(LoadMainScene());

    private IEnumerator LoadMainScene()
    {
        Debug.Log("[Bootstrap] Waiting for platform init...");
        yield return new WaitForSeconds(initWaitTime);

        // Optional: show a loading UI here

        Debug.Log($"[Bootstrap] Loading scene: {mainSceneName}");
        SceneManager.LoadScene(mainSceneName);
    }
}
