using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;
using VRC.Udon.VM;
#if VRC_CLIENT
using VRC.Udon.Security;
#endif
#if UNITY_EDITOR && !VRC_CLIENT
using UnityEditor.SceneManagement;

#endif

namespace VRC.Udon
{
    public class UdonBehaviour : VRC.SDKBase.VRC_Interactable, IUdonBehaviour, ISerializationCallbackReceiver, VRC.SDKBase.INetworkID
    {
        #region Odin Serialized Fields

        public IUdonVariableTable publicVariables = new UdonVariableTable();

        #endregion

        #region Serialized Public Fields

        public bool SynchronizePosition;
        public readonly bool SynchronizeAnimation = false; //We don't support animation sync yet, coming soon.
        public bool AllowCollisionOwnershipTransfer = true;

        #endregion

        #region Serialized Private Fields

        [SerializeField]
        private AbstractSerializedUdonProgramAsset serializedProgramAsset;

        #if UNITY_EDITOR && !VRC_CLIENT
        [SerializeField]
        public AbstractUdonProgramSource programSource;

        #endif

        #endregion

        #region Public Fields and Properties

        [PublicAPI]
        public static System.Action<UdonBehaviour, IUdonProgram> OnInit { get; set; } = null;

        [PublicAPI]
        public static System.Action<UdonBehaviour, NetworkEventTarget, string> SendCustomNetworkEventHook { get; set; } = null;

        [PublicAPI]
        public bool HasInteractiveEvents { get; private set; }

        public override bool IsInteractive => HasInteractiveEvents;
        
        public int NetworkID { get; set; }

        #endregion

        #region Private Fields

        private IUdonProgram _program;
        private IUdonVM _udonVM;
        private bool _isNetworkReady;
        private int _debugLevel;
        private bool _hasError;
        private bool _hasDoneStart;
        private bool _initialized;
        private readonly Dictionary<string, List<uint>> _eventTable = new Dictionary<string, List<uint>>();
        private readonly Dictionary<(string eventName, string symbolName), string> _symbolNameCache = new Dictionary<(string, string), string>();

        #endregion

        #region Editor Only

        #if UNITY_EDITOR && !VRC_CLIENT

        public void RunEditorUpdate(ref bool dirty)
        {
            if(programSource == null)
            {
                return;
            }

            programSource.RunEditorUpdate(this, ref dirty);

            if(!dirty)
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        #endif

        #endregion

        #region Private Methods

        private bool LoadProgram()
        {
            if(serializedProgramAsset == null)
            {
                return false;
            }

            _program = serializedProgramAsset.RetrieveProgram();

            IUdonSymbolTable symbolTable = _program?.SymbolTable;
            IUdonHeap heap = _program?.Heap;
            if(symbolTable == null || heap == null)
            {
                return false;
            }

            foreach(string variableSymbol in publicVariables.VariableSymbols)
            {
                if(!symbolTable.HasAddressForSymbol(variableSymbol))
                {
                    continue;
                }

                uint symbolAddress = symbolTable.GetAddressFromSymbol(variableSymbol);

                if(!publicVariables.TryGetVariableType(variableSymbol, out Type declaredType))
                {
                    continue;
                }

                publicVariables.TryGetVariableValue(variableSymbol, out object value);
                if(declaredType == typeof(GameObject) || declaredType == typeof(UdonBehaviour) ||
                   declaredType == typeof(Transform))
                {
                    if(value == null)
                    {
                        value = new UdonGameObjectComponentHeapReference(declaredType);
                        declaredType = typeof(UdonGameObjectComponentHeapReference);
                    }
                }

                heap.SetHeapVariable(symbolAddress, value, declaredType);
            }

            return true;
        }

        private void ProcessEntryPoints()
        {
            string[] exportedSymbols = _program.EntryPoints.GetExportedSymbols();
            if (exportedSymbols.Contains("_interact"))
            {
                HasInteractiveEvents = true;
            }

            _eventTable.Clear();
            foreach(string entryPoint in exportedSymbols)
            {
                uint address = _program.EntryPoints.GetAddressFromSymbol(entryPoint);

                if (!_eventTable.ContainsKey(entryPoint))
                {
                    _eventTable.Add(entryPoint, new List<uint>());
                }

                _eventTable[entryPoint].Add(address);
            }
        }

        private bool ResolveUdonHeapReferences(IUdonSymbolTable symbolTable, IUdonHeap heap)
        {
            bool success = true;
            foreach(string symbolName in symbolTable.GetSymbols())
            {
                uint symbolAddress = symbolTable.GetAddressFromSymbol(symbolName);
                object heapValue = heap.GetHeapVariable(symbolAddress);
                if(!(heapValue is UdonBaseHeapReference udonBaseHeapReference))
                {
                    continue;
                }

                if(!ResolveUdonHeapReference(heap, symbolAddress, udonBaseHeapReference))
                {
                    success = false;
                }
            }

            return success;
        }

        private bool ResolveUdonHeapReference(IUdonHeap heap, uint symbolAddress, UdonBaseHeapReference udonBaseHeapReference)
        {
            switch(udonBaseHeapReference)
            {
                case UdonGameObjectComponentHeapReference udonGameObjectComponentHeapReference:
                {
                    Type referenceType = udonGameObjectComponentHeapReference.type;
                    if(referenceType == typeof(GameObject))
                    {
                        heap.SetHeapVariable(symbolAddress, gameObject);
                        return true;
                    }
                    else if(referenceType == typeof(Transform))
                    {
                        heap.SetHeapVariable(symbolAddress, gameObject.transform);
                        return true;
                    }
                    else if(referenceType == typeof(UdonBehaviour))
                    {
                        heap.SetHeapVariable(symbolAddress, this);
                        return true;
                    }
                    else if(referenceType == typeof(UnityEngine.Object))
                    {
                        heap.SetHeapVariable(symbolAddress, this);
                        return true;
                    }
                    else
                    {
                        Core.Logger.Log(
                            $"Unsupported GameObject/Component reference type: {udonBaseHeapReference.GetType().Name}. Only GameObject, Transform, and UdonBehaviour are supported.",
                            _debugLevel,
                            this);

                        return false;
                    }
                }
                default:
                {
                    Core.Logger.Log($"Unknown heap reference type: {udonBaseHeapReference.GetType().Name}", _debugLevel, this);
                    return false;
                }
            }
        }
        
        #endregion

        #region Unity Events

        public override void Start()
        {
            InitializeUdonContent();

            RunOnInit();
        }

        private void Update()
        {
            if(!_hasDoneStart && _isNetworkReady)
            {
                _hasDoneStart = true;
                RunEvent("_start");
            }
            
            RunEvent("_update");
        }
        
        private void LateUpdate()
        {
            RunEvent("_lateUpdate");
        }

        public void FixedUpdate()
        {
            RunEvent("_fixedUpdate");
        }

        public void OnAnimatorIK(int layerIndex)
        {
            RunEvent("_onAnimatorIk", ("index", layerIndex));
        }

        public void OnAnimatorMove()
        {
            RunEvent("_onAnimatorMove");
        }

        public void OnAudioFilterRead(float[] data, int channels)
        {
            RunEvent("_onAudioFilterRead", ("data", data), ("channels", channels));
        }

        public void OnBecameInvisible()
        {
            RunEvent("_onBecameInvisible");
        }

        public void OnBecameVisible()
        {
            RunEvent("_onBecameVisible");
        }

        public void OnCollisionEnter(Collision other)
        {
            RunEvent("_onCollisionEnter", ("other", other));
        }

        public void OnCollisionEnter2D(Collision2D other)
        {
            RunEvent("_onCollisionEnter2D", ("other", other));
        }

        public void OnCollisionExit(Collision other)
        {
            RunEvent("_onCollisionExit", ("other", other));
        }

        public void OnCollisionExit2D(Collision2D other)
        {
            RunEvent("_onCollisionExit2D", ("other", other));
        }

        public void OnCollisionStay(Collision other)
        {
            RunEvent("_onCollisionStay", ("other", other));
        }

        public void OnCollisionStay2D(Collision2D other)
        {
            RunEvent("_onCollisionStay2D", ("other", other));
        }

        public void OnControllerColliderHit(ControllerColliderHit hit)
        {
            RunEvent("_onControllerColliderHit", ("hit", hit));
        }

        public void OnDestroy()
        {
            RunEvent("_onDestroy");
        }

        public void OnDisable()
        {
            RunEvent("_onDisable");
        }

        public void OnDrawGizmos()
        {
            RunEvent("_onDrawGizmos");
        }

        public void OnDrawGizmosSelected()
        {
            RunEvent("_onDrawGizmosSelected");
        }

        public void OnEnable()
        {
            RunEvent("_onEnable");
        }

        public void OnGUI()
        {
            RunEvent("_onGUI");
        }

        public void OnJointBreak(float breakForce)
        {
            RunEvent("_onJointBreak", ("force", breakForce));
        }

        public void OnJointBreak2D(Joint2D brokenJoint)
        {
            RunEvent("_onJointBreak2D", ("joint", brokenJoint));
        }

        public void OnMouseDown()
        {
            RunEvent("_onMouseDown");
        }

        public void OnMouseDrag()
        {
            RunEvent("_onMouseDrag");
        }

        public void OnMouseEnter()
        {
            RunEvent("_onMouseEnter");
        }

        public void OnMouseExit()
        {
            RunEvent("_onMouseExit");
        }

        public void OnMouseOver()
        {
            RunEvent("_onMouseOver");
        }

        public void OnMouseUp()
        {
            RunEvent("_onMouseUp");
        }

        public void OnMouseUpAsButton()
        {
            RunEvent("_onMouseUpAsButton");
        }

        public void OnParticleCollision(GameObject other)
        {
            RunEvent("_onParticleCollision", ("other", other));
        }

        public void OnParticleTrigger()
        {
            RunEvent("_onParticleTrigger");
        }

        public void OnPostRender()
        {
            RunEvent("_onPostRender");
        }

        public void OnPreCull()
        {
            RunEvent("_onPreCull");
        }

        public void OnPreRender()
        {
            RunEvent("_onPreRender");
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if(!_eventTable.ContainsKey("_onRenderImage") || _eventTable["_onRenderImage"].Count == 0)
            {
                Graphics.Blit(src, dest);
                return;
            }
            RunEvent("_onRenderImage", ("src", src), ("dest", dest));
        }

        public void OnRenderObject()
        {
            RunEvent("_onRenderObject");
        }

        public void OnTransformChildrenChanged()
        {
            RunEvent("_onTransformChildrenChanged");
        }

        public void OnTransformParentChanged()
        {
            RunEvent("_onTransformParentChanged");
        }

        public void OnTriggerEnter(Collider other)
        {
            RunEvent("_onTriggerEnter", ("other", other));
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            RunEvent("_onTriggerEnter2D", ("other", other));
        }

        public void OnTriggerExit(Collider other)
        {
            RunEvent("_onTriggerExit", ("other", other));
        }

        public void OnTriggerExit2D(Collider2D other)
        {
            RunEvent("_onTriggerExit2D", ("other", other));
        }

        public void OnTriggerStay(Collider other)
        {
            RunEvent("_onTriggerStay", ("other", other));
        }

        public void OnTriggerStay2D(Collider2D other)
        {
            RunEvent("_onTriggerStay2D", ("other", other));
        }

        public void OnValidate()
        {
            RunEvent("_onValidate");
        }

        public void OnWillRenderObject()
        {
            RunEvent("_onWillRenderObject");
        }

        #endregion

        #region VRCSDK Events

        #if VRC_CLIENT
        private void OnNetworkReady()
        {
            _isNetworkReady = true;
        }
        #endif

        //Called through Interactable interface
        public override void Interact()
        {
            RunEvent("_interact");
        }
        
        public override void OnDrop()
        {
            RunEvent("_onDrop");
        }

        public override void OnPickup()
        {
            RunEvent("_onPickup");
        }

        public override void OnPickupUseDown()
        {
            RunEvent("_onPickupUseDown");
        }
        
        public override void OnPickupUseUp()
        {
            RunEvent("_onPickupUseUp");
        }
        
        //Called via delegate by UdonSync
        [PublicAPI]
        public void OnPreSerialization()
        {
            RunEvent("_onPreSerialization");
        }

        //Called via delegate by UdonSync
        [PublicAPI]
        public void OnDeserialization()
        {
            RunEvent("_onDeserialization");
        }

        #endregion

        #region RunProgram Methods

        [PublicAPI]
        public void RunProgram(string eventName)
        {
            if(_program == null)
            {
                return;
            }

            foreach(string entryPoint in _program.EntryPoints.GetExportedSymbols())
            {
                if(entryPoint != eventName)
                {
                    continue;
                }

                uint address = _program.EntryPoints.GetAddressFromSymbol(entryPoint);
                RunProgram(address);
            }
        }

        private void RunProgram(uint entryPoint)
        {
            if(_hasError)
            {
                return;
            }

            if(_udonVM == null)
            {
                return;
            }

            uint originalAddress = _udonVM.GetProgramCounter();
            UdonBehaviour originalExecuting = UdonManager.Instance.currentlyExecuting;

            _udonVM.SetProgramCounter(entryPoint);
            UdonManager.Instance.currentlyExecuting = this;

            _udonVM.DebugLogging = UdonManager.Instance.DebugLogging;

            try
            {
                uint result = _udonVM.Interpret();
                if(result != 0)
                {
                    Core.Logger.LogError($"Udon VM execution errored, this UdonBehaviour will be halted.", _debugLevel, this);
                    _hasError = true;
                    enabled = false;
                }
            }
            catch(UdonVMException error)
            {
                Core.Logger.LogError($"An exception occurred during Udon execution, this UdonBehaviour will be halted.\n{error}", _debugLevel, this);
                _hasError = true;
                enabled = false;
            }

            UdonManager.Instance.currentlyExecuting = originalExecuting;
            if(originalAddress < 0xFFFFFFFC)
            {
                _udonVM.SetProgramCounter(originalAddress);
            }
        }

        [PublicAPI]
        public string[] GetPrograms()
        {
            return _program == null ? new string[0] : _program.EntryPoints.GetExportedSymbols();
        }

        #endregion

        #region Serialization

        [SerializeField]
        private string serializedPublicVariablesBytesString;

        [SerializeField]
        private List<UnityEngine.Object> publicVariablesUnityEngineObjects;

        [SerializeField]
        private DataFormat publicVariablesSerializationDataFormat = DataFormat.Binary;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            DeserializePublicVariables();
        }

        private void DeserializePublicVariables()
        {
            byte[] serializedPublicVariablesBytes = Convert.FromBase64String(serializedPublicVariablesBytesString ?? "");
            publicVariables = SerializationUtility.DeserializeValue<IUdonVariableTable>(
                                  serializedPublicVariablesBytes,
                                  publicVariablesSerializationDataFormat,
                                  publicVariablesUnityEngineObjects
                              ) ?? new UdonVariableTable();

            // Validate that the type of the value can actually be cast to the declaredType to avoid InvalidCastExceptions later.
            foreach(string publicVariableSymbol in publicVariables.VariableSymbols.ToArray())
            {
                if(!publicVariables.TryGetVariableValue(publicVariableSymbol, out object value))
                {
                    continue;
                }

                if(value == null)
                {
                    continue;
                }

                if(!publicVariables.TryGetVariableType(publicVariableSymbol, out Type declaredType))
                {
                    continue;
                }

                if(declaredType.IsInstanceOfType(value))
                {
                    continue;
                }

                if(declaredType.IsValueType)
                {
                    publicVariables.TrySetVariableValue(publicVariableSymbol, Activator.CreateInstance(declaredType));
                }
                else
                {
                    publicVariables.TrySetVariableValue(publicVariableSymbol, null);
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            SerializePublicVariables();
        }

        private void SerializePublicVariables()
        {
            byte[] serializedPublicVariablesBytes = SerializationUtility.SerializeValue(publicVariables, publicVariablesSerializationDataFormat, out publicVariablesUnityEngineObjects);
            serializedPublicVariablesBytesString = Convert.ToBase64String(serializedPublicVariablesBytes);
        }

        #endregion
        
        #region IUdonBehaviour Interface
        
        public void RunEvent(string eventName, params (string symbolName, object value)[] programVariables)
        {
            if(!_isNetworkReady)
            {
                return;
            }
            if(!_hasDoneStart)
            {
                return;
            }
            
            if (!_eventTable.TryGetValue(eventName, out List<uint> entryPoints))
            {
                return;
            }

            //TODO: Replace with a non-boxing interface before exposing to users
            foreach((string symbolName, object value) in programVariables)
            {
                if (!_symbolNameCache.TryGetValue((eventName, symbolName), out string newSymbolName))
                {
                    newSymbolName = $"{eventName.Substring(1)}{char.ToUpper(symbolName.First())}{symbolName.Substring(1)}";
                    _symbolNameCache.Add((eventName, symbolName), newSymbolName);
                }
                SetProgramVariable(newSymbolName, value);
            }

            foreach(uint entryPoint in entryPoints)
            {
                RunProgram(entryPoint);
            }
            
            foreach((string symbolName, object value) in programVariables)
            {
                SetProgramVariable(symbolName, null);    
            }
        }
        
        public void InitializeUdonContent()
        {
            if(_initialized)
            {
                return;
            }

            SetupLogging();

            UdonManager udonManager = UdonManager.Instance;
            if(udonManager == null)
            {
                enabled = false;
                VRC.Core.Logger.LogError($"Could not find the UdonManager; the UdonBehaviour on '{gameObject.name}' will not run.", _debugLevel, this);
                return;
            }

            if(!LoadProgram())
            {
                enabled = false;
                VRC.Core.Logger.Log($"Could not load the program; the UdonBehaviour on '{gameObject.name}' will not run.", _debugLevel, this);

                return;
            }

            IUdonSymbolTable symbolTable = _program?.SymbolTable;
            IUdonHeap heap = _program?.Heap;
            if(symbolTable == null || heap == null)
            {
                enabled = false;
                VRC.Core.Logger.Log($"Invalid program; the UdonBehaviour on '{gameObject.name}' will not run.", _debugLevel, this);
                return;
            }

            if(!ResolveUdonHeapReferences(symbolTable, heap))
            {
                enabled = false;
                VRC.Core.Logger.Log($"Failed to resolve a GameObject/Component Reference; the UdonBehaviour on '{gameObject.name}' will not run.", _debugLevel, this);
                return;
            }

            _udonVM = udonManager.ConstructUdonVM();

            if(_udonVM == null)
            {
                enabled = false;
                VRC.Core.Logger.LogError($"No UdonVM; the UdonBehaviour on '{gameObject.name}' will not run.", _debugLevel, this);
                return;
            }

            _udonVM.LoadProgram(_program);

            ProcessEntryPoints();

            #if !VRC_CLIENT
            _isNetworkReady = true;
            #endif

            _initialized = true;
        }

        [PublicAPI]
        public void RunOnInit()
        {
            if(OnInit == null)
            {
                return;
            }

            try
            {
                OnInit(this, _program);
            }
            catch(Exception exception)
            {
                enabled = false;
                VRC.Core.Logger.LogError(
                    $"An exception '{exception.Message}' occurred during initialization; the UdonBehaviour on '{gameObject.name}' will not run. Exception:\n{exception}",
                    _debugLevel,
                    this
                );
            }
        }

        #region IUdonEventReceiver and IUdonSyncTarget Interface

        #region IUdonEventReceiver Only

        public void SendCustomEvent(string eventName)
        {
            RunProgram(eventName);
        }

        public void SendCustomNetworkEvent(NetworkEventTarget target, string eventName)
        {
            SendCustomNetworkEventHook?.Invoke(this, target, eventName);
        }

        #endregion

        #region IUdonSyncTarget

        public IUdonSyncMetadataTable SyncMetadataTable => _program?.SyncMetadataTable;

        #endregion

        #region Shared
        
        public Type GetProgramVariableType(string symbolName)
        {
            if(!_program.SymbolTable.HasAddressForSymbol(symbolName))
            {
                return null;
            }

            uint symbolAddress = _program.SymbolTable.GetAddressFromSymbol(symbolName);
            return _program.Heap.GetHeapVariableType(symbolAddress);
        }

        public void SetProgramVariable<T>(string symbolName, T value)
        {
            if(_program == null)
            {
                return;
            }
            
            if(!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return;
            }

            _program.Heap.SetHeapVariable<T>(symbolAddress, value);
        }

        public void SetProgramVariable(string symbolName, object value)
        {
            if(_program == null)
            {
                return;
            }

            if(!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return;
            }

            _program.Heap.SetHeapVariable(symbolAddress, value);
        }

        public T GetProgramVariable<T>(string symbolName)
        {
            if(_program == null)
            {
                return default;
            }

            if(!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return default;
            }

            return _program.Heap.GetHeapVariable<T>(symbolAddress);
        }

        public object GetProgramVariable(string symbolName)
        {
            if(_program == null)
            {
                return null;
            }

            if(!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return null;
            }

            return _program.Heap.GetHeapVariable(symbolAddress);
        }

        public bool TryGetProgramVariable<T>(string symbolName, out T value)
        {
            value = default;
            if(_program == null)
            {
                return false;
            }

            if(!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return false;
            }

            return _program.Heap.TryGetHeapVariable(symbolAddress, out value);
        }

        public bool TryGetProgramVariable(string symbolName, out object value)
        {
            value = null;
            if(_program == null)
            {
                return false;
            }

            if(!_program.SymbolTable.TryGetAddressFromSymbol(symbolName, out uint symbolAddress))
            {
                return false;
            }

            return _program.Heap.TryGetHeapVariable(symbolAddress, out value);
        }

        #endregion

        #endregion

        #endregion

        #region Logging Methods

        private void SetupLogging()
        {
            _debugLevel = GetType().GetHashCode();
            if(VRC.Core.Logger.DebugLevelIsDescribed(_debugLevel))
            {
                return;
            }

            Core.Logger.DescribeDebugLevel(_debugLevel, "UdonBehaviour");
            Core.Logger.AddDebugLevel(_debugLevel);
        }

        #endregion

        #region Manual Initialization Methods
        public void AssignProgramAndVariables(VRC.Udon.AbstractSerializedUdonProgramAsset compiledAsset, IUdonVariableTable variables)
        {
            serializedProgramAsset = compiledAsset;
            publicVariables = variables;
        }
        #endregion
    }
}
