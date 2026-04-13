using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ButtonSceneLoader
{
    public enum LoadMode
    {
        Synchronous,
        Asynchronous,
        LevelLoader
    }

    [SerializeField] private Button button;
    [SerializeField] private string sceneName;
    [SerializeField] private LoadMode loadMode = LoadMode.Synchronous;
    [SerializeField] private GameObject levelLoaderPrefab;

    public void Subscribe()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(LoadScene);
        }
    }

    private void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        switch (loadMode)
        {
            case LoadMode.Asynchronous:
                SceneManager.LoadSceneAsync(sceneName);
                break;

            case LoadMode.LevelLoader:
                if (levelLoaderPrefab != null)
                {
                    Object.Instantiate(levelLoaderPrefab);
                }
                else
                {
                    Debug.LogWarning("[ButtonSceneLoader] LevelLoader prefab is not assigned. Falling back to synchronous load.");
                    SceneManager.LoadScene(sceneName);
                }
                break;

            default:
                SceneManager.LoadScene(sceneName);
                break;
        }
    }
}
