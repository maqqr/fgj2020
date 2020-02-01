using UnityEngine;
using System.Collections;
using UnityEngine.AddressableAssets;
using System;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

public class GameManager
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            return instance;
        }
    }
    private const string settingsDataAddress = "Settings";

    public const float TILE_SIZE = 1.5f;
    public const int HAULING_SPEED = 5;
    public const float COOLDOWN_LENGTH = 0.8f;
    public const float MOVEMENT_SPEED = 2.1f;

    private Settings loadedSettings;
    public Settings LoadedSettings
    {
        get
        {
            return loadedSettings;
        }
    }
    public bool IsSettingsLoaded
    {
        get
        {
            return loadedSettings != null;
        }
    }


    public Action<Settings> OnSettingsLoaded;

    public GameManager()
    {
        instance = this;
    }


    public void Initialize()
    {
        var operation = Addressables.LoadAssetAsync<Settings>(settingsDataAddress);
        operation.Completed += SettingsLoaded;
    }




    private void SettingsLoaded(AsyncOperationHandle<Settings> obj)
    {
        var settings = obj.Result;

        if(settings == null)
        {
            throw new UnityException("Hammer time!");
        }

        loadedSettings = settings;

        OnSettingsLoaded?.Invoke(loadedSettings);
    }
}
