using UnityEngine;

public class MarkerProviderSelector : MonoBehaviour
{
    public GameObject fakeProvider;
    public GameObject qrProvider;

    void Awake()
    {
        bool inEditor = Application.isEditor;
        fakeProvider.SetActive(inEditor);
        qrProvider.SetActive(!inEditor);
    }
}
