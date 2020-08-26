using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using VRC.Udon.Graph;
using VRC.Udon.Serialization;
using EditorUI = UnityEditor.Experimental.UIElements;
using EngineUI = UnityEngine.Experimental.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonParameterProperty : EngineUI.VisualElement
	{
		protected UdonGraph graph;
		protected UdonNodeData nodeData;
		protected UdonNodeDefinition definition;

		//public ExposedParameter parameter { get; private set; }

		public EngineUI.Toggle isPublic { get; private set; }
		public EngineUI.Toggle isSynced { get; private set; }
		public EngineUI.VisualElement defaultValueContainer { get; private set; }
		public EditorUI.PopupField<string> syncField { get; private set; }
		private VisualElement _inputField;

		public enum ValueIndices
		{
			value = 0,
			name = 1,
			isPublic = 2,
			isSynced = 3,
			syncType = 4
		}

		private static SerializableObjectContainer[] GetDefaultNodeValues()
		{
			return new[]
			{
				SerializableObjectContainer.Serialize("", typeof(string)),
				SerializableObjectContainer.Serialize("newVariableName", typeof(string)),
				SerializableObjectContainer.Serialize(false, typeof(bool)),
				SerializableObjectContainer.Serialize(false, typeof(bool)),
				SerializableObjectContainer.Serialize("none", typeof(string))
			};
		}
		
		// 0 = Value, 1 = name, 2 = public, 3 = synced, 4 = syncType
		public UdonParameterProperty(UdonGraph graphView, UdonNodeDefinition definition, UdonNodeData nodeData)
		{
			this.graph = graphView;
			this.definition = definition;
			this.nodeData = nodeData;

			// Make sure the incoming nodeData has the right number of nodeValues (super old graphs didn't have sync info)
			if (this.nodeData.nodeValues.Length != 5)
			{
				this.nodeData.nodeValues = GetDefaultNodeValues();
				for (int i = 0; i < nodeData.nodeValues.Length; i++)
				{
					this.nodeData.nodeValues[i] = nodeData.nodeValues[i];
				}
			}

			// Public Toggle
			isPublic = new EngineUI.Toggle
			{
				text = "public",
				value = (bool)GetValue(ValueIndices.isPublic)
			};
			isPublic.OnValueChanged(e =>
			{
				SetNewValue(e.newValue, ValueIndices.isPublic);
			});
			Add(isPublic);

			// Is Synced Field
			isSynced = new EngineUI.Toggle
			{
				text = "synced",
				value = (bool)GetValue(ValueIndices.isSynced),
			};

			isSynced.OnValueChanged(e =>
			{
				SetNewValue(e.newValue, ValueIndices.isSynced);
				syncField.visible = e.newValue;
			});
			Add(isSynced);

			// Sync Field, add to isSynced
			List<string> choices = new List<string>()
			{
				"none", "linear", "smooth"
			};
			syncField = new EditorUI.PopupField<string>(choices, 0)
			{
				visible = isSynced.value
			};
			syncField.OnValueChanged(e =>
			{
				SetNewValue(e.newValue, ValueIndices.syncType);
			});
			isSynced.Add(syncField);

			// Container to show/edit Default Value
			var friendlyName = UdonGraphExtensions.FriendlyTypeName(definition.type).FriendlyNameify();
			defaultValueContainer = new VisualElement
			{
				new Label("default value") { name = "default-value-label" }
			};
			var field = GetTheRightField(definition.type);
			if(field != null)
			{
				// TODO: need to handle cases where we can't generate the field above
				defaultValueContainer.Add(field);
				Add(defaultValueContainer);
			}
		}

		private object GetValue(ValueIndices index)
		{
			if((int)index >= nodeData.nodeValues.Length)
			{
				Debug.LogWarning($"Can't get {index} from {definition.name} variable");
				return null;
			}
			return nodeData.nodeValues[(int)index].Deserialize();
		}

		private bool TryGetValueObject(out object result)
		{
			result = null;

			var container = nodeData.nodeValues[0];
			if (container == null)
			{
				return false;
			}

			result = container.Deserialize();
			if (result == null)
			{
				return false;
			}

			return true;
		}

		private void SetNewValue(object newValue, ValueIndices index)
		{
			nodeData.nodeValues[(int)index] = SerializableObjectContainer.Serialize(newValue);
			graph.ReSerializeData();
			graph.SaveGraphToDisk();
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
			else if (portType == typeof(System.String))
				return SetupField<TextField, string>();
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
				_editArrayButton = new Button(() => EditArray(portType.GetElementType()))
				{
					text = "Edit",
					name = "array-editor",
				};
				return _editArrayButton;
			}
			else return null;
		}

		// TODO: centralize this logic so it's not copied between here and UdonPort
		private IArrayProvider _inspector;
		private Button _editArrayButton;
		private void EditArray(Type elementType)
		{
			// Update Values when 'Save' is clicked
			if (_inspector != null)
			{
				// Update Values
				SetNewValue(_inspector.GetValues(), ValueIndices.value);

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

			defaultValueContainer.Add(_inspector as VisualElement);
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
				field.value = (TType)result;
			}
			field.OnValueChanged((e) => SetNewValue(e.newValue, ValueIndices.value));
			_inputField = field;
			return _inputField;
		}
	}
}