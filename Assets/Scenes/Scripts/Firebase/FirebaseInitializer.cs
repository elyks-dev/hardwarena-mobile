using System;
using Firebase;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    public static FirebaseInitializer Instance;

    public bool IsInitialized { get; private set; }

    public event Action OnFirebaseInitialized;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeFirebase();
    }

    async void InitializeFirebase()
    {
        try
        {
            var dependencyStatus =
                await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                IsInitialized = true;

                Debug.Log("✅ Firebase initialized successfully!");

                OnFirebaseInitialized?.Invoke();
            }
            else
            {
                Debug.LogError(
                    "Firebase could not resolve dependencies: " +
                    dependencyStatus
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Firebase initialization failed: " +
                ex
            );
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}