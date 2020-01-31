using UnityEngine;
using System.Collections;
using UnityEngine.AddressableAssets;
using System;

public class GameManager : MonoBehaviour
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

    private Settings loadedSettings;
    public Settings LoadedSettings
    {
        get
        {
            return loadedSettings;
        }
    }


    private void Awake()
    {
        instance = this;
    }


    // Use this for initialization
    void Start()
    {
        Addressables.LoadAssetsAsync<Settings>(settingsDataAddress, SettingsLoaded);

    }

    private void SettingsLoaded(Settings obj)
    {
        var settings = obj as Settings;

        if(settings == null)
        {
            throw new UnityException("Hammer time!");
        }

        loadedSettings = settings;
    }
}
