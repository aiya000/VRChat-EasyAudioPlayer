using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon.ClientBindings;
using VRC.Udon.ClientBindings.Interfaces;
using VRC.Udon.Common.Interfaces;
using Object = UnityEngine.Object;

namespace VRC.Udon
{
    [AddComponentMenu("")]
    [ExecuteInEditMode]
    public class UdonManager : MonoBehaviour, IUdonClientInterface
    {
        public UdonBehaviour currentlyExecuting;
        
        private static UdonManager _instance;
        private bool _isUdonEnabled = true;
        private readonly Dictionary<Scene, Dictionary<GameObject, HashSet<UdonBehaviour>>> _sceneUdonBehaviourDirectories = new Dictionary<Scene, Dictionary<GameObject, HashSet<UdonBehaviour>>>();

        public static UdonManager Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                GameObject udonManagerGameObject = new GameObject("UdonManager");
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(udonManagerGameObject);
                }

                _instance = udonManagerGameObject.AddComponent<UdonManager>();
                return _instance;
            }
        }

        private IUdonClientInterface _udonClientInterface;

        private IUdonClientInterface UdonClientInterface
        {
            get
            {
                if (_udonClientInterface != null)
                {
                    return _udonClientInterface;
                }

                _udonClientInterface = new UdonClientInterface();

                return _udonClientInterface;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if(loadSceneMode == LoadSceneMode.Single)
            {
                _sceneUdonBehaviourDirectories.Clear();
            }

            Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory = new Dictionary<GameObject, HashSet<UdonBehaviour>>();
            List<Transform> transformsTempList = new List<Transform>();
            foreach(GameObject rootGameObject in scene.GetRootGameObjects())
            {
                rootGameObject.GetComponentsInChildren(true, transformsTempList);
                foreach(Transform currentTransform in transformsTempList)
                {
                    List<UdonBehaviour> currentGameObjectUdonBehaviours = new List<UdonBehaviour>();
                    GameObject currentGameObject = currentTransform.gameObject;
                    currentGameObject.GetComponents(currentGameObjectUdonBehaviours);

                    if(currentGameObjectUdonBehaviours.Count > 0)
                    {
                        sceneUdonBehaviourDirectory.Add(currentGameObject, new HashSet<UdonBehaviour>(currentGameObjectUdonBehaviours));
                    }
                }
            }

            if(!_isUdonEnabled)
            {
                VRC.Core.Logger.LogWarning("Udon is disabled globally, Udon components will be removed from the scene.");
                foreach(HashSet<UdonBehaviour> udonBehaviours in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviours)
                    {
                        Destroy(udonBehaviour);
                    }
                }

                return;
            }

            _sceneUdonBehaviourDirectories.Add(scene, sceneUdonBehaviourDirectory);

            // Initialize all UdonBehaviours in the scene so their Public Variables are populated.
            foreach (HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
            {
                foreach (UdonBehaviour udonBehaviour in udonBehaviourList)
                {
                    udonBehaviour.InitializeUdonContent();
                }
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if(_sceneUdonBehaviourDirectories.ContainsKey(scene))
            {
                _sceneUdonBehaviourDirectories.Remove(scene);
            }
        }

        public void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }

            DebugLogging = Application.isEditor;

            if (this == Instance)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(this);
            }
            else
            {
                DestroyImmediate(this);
            }

            PrimitiveType[] primitiveTypes = (PrimitiveType[])Enum.GetValues(typeof(PrimitiveType));
            foreach(PrimitiveType primitiveType in primitiveTypes)
            {
                GameObject go = GameObject.CreatePrimitive(primitiveType);
                Mesh primitiveMesh = go.GetComponent<MeshFilter>().sharedMesh;
                Destroy(go);
                Blacklist(primitiveMesh);
            }
        }

        [PublicAPI]
        public static void SetUdonEnabled(bool isEnabled)
        {
            _instance._isUdonEnabled = isEnabled;
        }

        public IUdonVM ConstructUdonVM()
        {
            return !_isUdonEnabled ? null : UdonClientInterface.ConstructUdonVM();
        }

        public void FilterBlacklisted<T>(ref T objectToFilter) where T : class
        {
            UdonClientInterface.FilterBlacklisted(ref objectToFilter);
        }


        public void Blacklist(UnityEngine.Object objectToBlacklist)
        {
            UdonClientInterface.Blacklist(objectToBlacklist);
        }

        public void Blacklist(IEnumerable<UnityEngine.Object> objectsToBlacklist)
        {
            UdonClientInterface.Blacklist(objectsToBlacklist);
        }

        public void FilterBlacklisted(ref UnityEngine.Object objectToFilter)
        {
            UdonClientInterface.FilterBlacklisted(ref objectToFilter);
        }

        public bool IsBlacklisted(Object objectToCheck)
        {
            return UdonClientInterface.IsBlacklisted(objectToCheck);
        }

        public void ClearBlacklist()
        {
            UdonClientInterface.ClearBlacklist();
        }

        public bool IsBlacklisted<T>(T objectToCheck)
        {
            return UdonClientInterface.IsBlacklisted(objectToCheck);
        }

        public IUdonWrapper GetWrapper()
        {
            return UdonClientInterface.GetWrapper();
        }

        [PublicAPI]
        public void RegisterUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            GameObject udonBehaviourGameObject = udonBehaviour.gameObject;
            Scene udonBehaviourScene = udonBehaviourGameObject.scene;
            if(!_sceneUdonBehaviourDirectories.TryGetValue(udonBehaviourScene, out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                return;
            }

            if(!sceneUdonBehaviourDirectory.TryGetValue(udonBehaviourGameObject, out HashSet<UdonBehaviour> gameObjectUdonBehaviours))
            {
                gameObjectUdonBehaviours = new HashSet<UdonBehaviour>();
                sceneUdonBehaviourDirectory.Add(udonBehaviourGameObject, gameObjectUdonBehaviours);
                return;
            }

            if(!gameObjectUdonBehaviours.Contains(udonBehaviour))
            {
                gameObjectUdonBehaviours.Add(udonBehaviour);
            }
        }

        //Run an udon event on all objects
        [PublicAPI]
        public void RunEvent(string eventName, params (string symbolName, object value)[] programVariables)
        {
            foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in _sceneUdonBehaviourDirectories.Values)
            {
                foreach (HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach (UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        if(udonBehaviour != null)
                        {
                            udonBehaviour.RunEvent(eventName, programVariables);
                        }
                    }
                }
            }
        }

        //Run an udon event on a specific gameObject
        [PublicAPI]
        public void RunEvent(GameObject eventReceiverObject, string eventName, params (string symbolName, object value)[] programVariables)
        {
            if(!_sceneUdonBehaviourDirectories.TryGetValue(eventReceiverObject.scene, out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                return;
            }

            if(!sceneUdonBehaviourDirectory.TryGetValue(eventReceiverObject, out HashSet<UdonBehaviour> eventReceiverBehaviourList))
            {
                return;
            }

            foreach(UdonBehaviour udonBehaviour in eventReceiverBehaviourList)
            {
                udonBehaviour.RunEvent(eventName, programVariables);
            }
        }

        public bool DebugLogging
        {
            get => UdonClientInterface.DebugLogging;
            set => UdonClientInterface.DebugLogging = value;
        }
    }
}
