using System;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using VRC.Udon.Graph;
using VRC.Udon.Serialization;
using EditorUI = UnityEditor.Experimental.UIElements;
using EngineUI = UnityEngine.Experimental.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    [Serializable]
    public class UdonPort : Port
    {
        public string FullName;
        private UdonNodeData _udonNodeData;
        private int _nodeValueIndex;

        private VisualElement _inputField;
        private VisualElement _inputFieldTypeLabel;

        private IArrayProvider _inspector;

        private bool _waitToReserialize = false;

        protected UdonPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
        }

        public static Port Create(string portName, Direction portDirection, IEdgeConnectorListener connectorListener, Type type, UdonNodeData data, int index, Orientation orientation = Orientation.Horizontal)
        {
            Capacity capacity = Capacity.Single;
            if(portDirection == Direction.Input && type == null || portDirection == Direction.Output && type != null)
            {
                capacity = Capacity.Multi;
            }
            var port = new UdonPort(orientation, portDirection, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<Edge>(connectorListener),
            };

            port.portName = portName;
            port._udonNodeData = data;
            port._nodeValueIndex = index;

            port.SetupPort();
            return port;
        }

        public int GetIndex()
        {
            return _nodeValueIndex;
        }

        private void SetupPort()
        {
            this.AddManipulator(m_EdgeConnector);
            if (portType == null || direction == Direction.Output)
            {
                return;
            }
            tooltip = UdonGraphExtensions.FriendlyTypeName(portType);
            var field = GetTheRightField(portType);
            if(field != null)
            {
                Add(field);
            }
            
            if (_udonNodeData.fullName.StartsWith("Const"))
            {
                RemoveConnector();
            }

            UpdateLabel(connected);
        }

        // Made its own method for now as we have issues auto-converting between string and char in a TextField
        // TODO: refactor SetupField so we can do just the field.value part separately to combine with this
        private VisualElement SetupCharField()
        {
            TextField field = new TextField();
            field.AddToClassList("portField");
            if (TryGetValueObject(out object result))
            {
                field.value = UdonGraphExtensions.UnescapeLikeALiteral((char)result);
            }

            field.isDelayed = true;

            // Special handling for escaping char value
            field.OnValueChanged((e) =>
            {
                if(e.newValue[0] == '\\' && e.newValue.Length > 1)
                {
                    SetNewValue(UdonGraphExtensions.EscapeLikeALiteral(e.newValue.Substring(0, 2)));
                }
                else
                {
                    SetNewValue(e.newValue[0]);
                }
            });
            _inputField = field;

            // Add label, shown when input is connected. Not shown by default
            var friendlyName = UdonGraphExtensions.FriendlyTypeName(typeof(char)).FriendlyNameify();
            var label = new EngineUI.Label(friendlyName);
            _inputFieldTypeLabel = label;

            return _inputField;
        }

        // Convenience wrapper for field types that don't need special initialization
        private VisualElement SetupField<TField, TType>() where TField : VisualElement, INotifyValueChanged<TType>, new()
        {
            var field = new TField();
            return SetupField<TField, TType>(field);
        }

        // Works for any TextValueField types, needs to know fieldType and object type
        private VisualElement SetupField<TField, TType>(TField field) where TField : VisualElement, INotifyValueChanged<TType>
        {
            field.AddToClassList("portField");
            if (TryGetValueObject(out object result))
            {
                try
                {
                    field.value = (TType)(result as object);
                }
                catch (Exception e)
                {
                    // Quietly catch this and continue
                }
            }

            // Delay text inputs
            if(typeof(TField).IsAssignableFrom(typeof(TextField)))
            {
                (field as TextField).isDelayed = true;
            }

            // Don't update color fields right away, breaks the UI
            if(typeof(TField).IsAssignableFrom(typeof(EditorUI.ColorField)))
            {
                _waitToReserialize = true;
            }

            // Custom Event fields need their event names sanitized after input and their connectors removed
            if (_udonNodeData.fullName.CompareTo("Event_Custom") == 0)
            {
                var tfield = field as TextField;
                tfield.OnValueChanged((e) =>
                {
                    string newValue = e.newValue.SanitizeVariableName();
                    tfield.value = newValue;
                    SetNewValue(newValue);
                });
                RemoveConnector();
            }
            else
            {
                field.OnValueChanged((e) => SetNewValue(e.newValue));
            }
            _inputField = field;

            // Add label, shown when input is connected. Not shown by default
            var friendlyName = UdonGraphExtensions.FriendlyTypeName(typeof(TType)).FriendlyNameify();
            var label = new EngineUI.Label(friendlyName);
            _inputFieldTypeLabel = label;

            return _inputField;
        }

        private void RemoveConnector()
        {
            this.Q("connector")?.RemoveFromHierarchy();
            this.Q(null, "connectorText")?.RemoveFromHierarchy();
        }

        private VisualElement GetTheRightField(Type portType)
        {
            // Handle normal input fields
            if (portType == typeof(Bounds))
                return SetupField<EditorUI.BoundsField, Bounds>();
            else if (portType == typeof(Color))
                return SetupField<EditorUI.ColorField, Color>();
            else if (portType == typeof(AnimationCurve))
                return SetupField<EditorUI.CurveField, AnimationCurve>();
            else if (portType == typeof(double))
                return SetupField<EditorUI.DoubleField, double>();
            else if (portType == typeof(float))
                return SetupField<EditorUI.FloatField, float>();
            else if (portType == typeof(Gradient))
                return SetupField<EditorUI.GradientField, Gradient>();
            else if (portType == typeof(int))
                return SetupField<EditorUI.IntegerField, int>();
            else if (portType == typeof(long))
                return SetupField<EditorUI.LongField, long>();
            else if (portType == typeof(Rect))
                return SetupField<EditorUI.RectField, Rect>();
            else if (portType == typeof(RectInt))
                return SetupField<EditorUI.RectIntField, RectInt>();
            else if (portType == typeof(string))
                return SetupField<TextField, string>();
            else if (portType == typeof(char))
                return SetupCharField();
            else if (portType == typeof(Vector2))
                return SetupField<EditorUI.Vector2Field, Vector2>();
            else if (portType == typeof(Vector3))
                return SetupField<EditorUI.Vector3Field, Vector3>();
            else if (portType == typeof(Vector4))
                return SetupField<EditorUI.Vector4Field, Vector4>();
            else if (portType == typeof(Vector2Int))
                return SetupField<EditorUI.Vector2IntField, Vector2Int>();
            else if (portType == typeof(Vector3Int))
                return SetupField<EditorUI.Vector3IntField, Vector3Int>();
            else if (portType == typeof(bool))
                return SetupField<EngineUI.Toggle, bool>();
            else if (portType != null && portType.IsEnum)
                return SetupField<EditorUI.EnumField, Enum>(new EditorUI.EnumField(portType.GetEnumValues().GetValue(0) as Enum));
            else if (portType != null && portType.IsArray)
            {
                _editArrayButton = new Button(()=>EditArray(portType.GetElementType()))
                {
                    text = "Edit",
                    name = "array-editor",
                };
                return _editArrayButton;
            }
            else return null;
        }

        private Button _editArrayButton;
        private void EditArray(Type elementType)
        {
            // Update Values when 'Save' is clicked
            if(_inspector != null)
            {
                // Update Values
                SetNewValue(_inspector.GetValues());

                // Remove Inspector
                _inspector.RemoveFromHierarchy();
                _inspector = null;

                // Update Button Text
                _editArrayButton.text = "Edit";
                return;
            }

            // Otherwise set up the inspector
            _editArrayButton.text = "Save";
            
            // Get value object, null is ok
            TryGetValueObject(out object value);

            // Create it new
            Type typedArrayInspector = (typeof(UdonArrayInspector<>)).MakeGenericType(elementType);
            _inspector = (Activator.CreateInstance(typedArrayInspector, value) as IArrayProvider);

            parent.Add(_inspector as VisualElement);
        }

        // Update elements on connect
        public override void Connect(Edge edge)
        {
            base.Connect(edge);

            // The below logic is just for Output ports
            if (edge.input.Equals(this)) return;

            // hide field, show label
            var input = ((UdonPort)edge.input);
            input.UpdateLabel(true);
            
            if (IsReloading())
            {
                return;
            }
            
            // update data
            if (portType == null)
            {
                // We are a flow port
                SetFlowUID(((UdonNode)input.node).uid);
            }
            else
            {
                // We are a value port, we need to send our info over to the OTHER node
                string myNodeUid = ((UdonNode)node).uid;
                input.SetDataFromNewConnection($"{myNodeUid}|{_nodeValueIndex}", input.GetIndex());
            }

            // in this case, we catch the method on the left node, with input as the right node
            SendReserializeEvent();
        }

        public override void OnStopEdgeDragging()
        {
            base.OnStopEdgeDragging();

            if (edgeConnector.edgeDragHelper.draggedPort == this)
            {
                if (capacity == Capacity.Single && connections.Count() > 0)
                {
                    // This port could only have one connection. Fixed in Reserialize, need to reload to show the change
                    this.Reload();
                }
            }
        }

        private void SetFlowUID(string newValue)
        {
            if (_udonNodeData.flowUIDs.Length <= _nodeValueIndex)
            {
                // If we don't have space for this flow value, create a new array
                // TODO: handle this elsewhere?
                var newFlowArray = new string[_nodeValueIndex + 1];
                for (int i = 0; i < _udonNodeData.flowUIDs.Length; i++)
                {
                    newFlowArray[i] = _udonNodeData.flowUIDs[i];
                }
                _udonNodeData.flowUIDs = newFlowArray;

                _udonNodeData.flowUIDs.SetValue(newValue, _nodeValueIndex);
            } 
            else
            {
                _udonNodeData.flowUIDs.SetValue(newValue, _nodeValueIndex);
            }
        }

        public bool IsReloading()
        {
            if(node is UdonNode)
            {
                return ((UdonNode)node).Graph.IsReloading;
            }
            else if(node is UdonStackNode)
            {
                return ((UdonStackNode)node).Graph.IsReloading;
            }
            else
            {
                return false;
            }
        }

        public void SetDataFromNewConnection(string uidAndPort, int index)
        {
            // can't do this for Reg stack nodes yet so skipping for demo
            if (_udonNodeData == null) return;

            if (_udonNodeData.nodeUIDs.Length <= _nodeValueIndex)
            {
                Debug.Log("Couldn't set it");
            }
            else
            {
                _udonNodeData.nodeUIDs.SetValue(uidAndPort, index);
            }
        }

        // Update elements on disconnect
        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);

            // hide label, show field
            if(direction == Direction.Input)
            {
                UpdateLabel(false);
            }

            if (IsReloading())
            {
                return;
            }

            // update data
            if (direction == Direction.Output && portType == null)
            {
                // We are a flow port
                SetFlowUID("");
            }
            else if (direction == Direction.Input && portType != null)
            {
                // Direction is input
                // We are a value port
                SetDataFromNewConnection("", GetIndex());
            }

            // in this case, we catch the method on the left node, with input as the right node
            SendReserializeEvent();
        }

        public void UpdateLabel(bool isConnected)
        {
            // Port has a 'connected' bool but it doesn't seem to update, so passing 'isConnected' for now

            if (isConnected)
            {
                if (_inputField != null && Contains(_inputField))
                {
                    _inputField.RemoveFromHierarchy();
                }
                if (_inputFieldTypeLabel != null && !Contains(_inputFieldTypeLabel))
                {
                    Add(_inputFieldTypeLabel);
                }
                if(_editArrayButton != null && Contains(_editArrayButton))
                {
                    _editArrayButton.RemoveFromHierarchy();
                }
            }
            else
            {
                if (_inputField != null && !Contains(_inputField))
                {
                    Add(_inputField);
                }
                if (_inputFieldTypeLabel != null && Contains(_inputFieldTypeLabel))
                {
                    _inputFieldTypeLabel.RemoveFromHierarchy();
                }
                if(_editArrayButton != null && !Contains(_editArrayButton))
                {
                    Add(_editArrayButton);
                }
            }
        }

        private bool TryGetValueObject(out object result)
        {
            result = null;
            SerializableObjectContainer container = null;
            // For Get and Set variable nodes, reach into the actual value of the linked variable
            if (_udonNodeData.fullName.Contains("et_Variable"))
            {
                container = _udonNodeData.nodeValues[1];
            }
            else
            {
                container = _udonNodeData.nodeValues[_nodeValueIndex];
            }
           
            if (container == null)
            {
                return false;
            }

            result = container.Deserialize();
            if(result == null)
            {
                return false;
            }

            return true;
        }

        private void SetNewValue(object newValue)
        {
            _udonNodeData.nodeValues[_nodeValueIndex] = SerializableObjectContainer.Serialize(newValue, portType);
            if (!_waitToReserialize)
            {
                SendReserializeEvent();
            }
        }

        private void SendReserializeEvent()
        {
            if (!IsReloading())
            {
                this.Reserialize();
            }
        }
    }
}