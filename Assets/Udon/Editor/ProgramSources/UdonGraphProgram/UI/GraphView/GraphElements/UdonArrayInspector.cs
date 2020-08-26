using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Graph;
using VRC.Udon.Serialization;
using UnityEditor.Experimental.UIElements;
using System.Collections;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonArrayInspector<T> : VisualElement, IArrayProvider
    {
        private ScrollView _scroller;
        private VisualContainer _container;
        private List<INotifyValueChanged<T>> _fields = new List<INotifyValueChanged<T>>();
        private IntegerField _sizeField;

        public UdonArrayInspector(object value)
        {
            AddToClassList("input-inspector");
            var resizeContainer = new VisualElement()
            {
                name = "resize-container",
            };
            resizeContainer.Add(new Label("size"));

            _sizeField = new IntegerField();
            _sizeField.isDelayed = true;
            _sizeField.OnValueChanged((evt) =>
            {
                ResizeTo(evt.newValue);
            });
            resizeContainer.Add(_sizeField);
            Add(resizeContainer);

            _scroller = new ScrollView()
            {
                name = "array-scroll",
            };
            Add(_scroller);

            _container = new VisualContainer();
            _scroller.SetContents(_container);

            if (value == null)
            {
                // Can't show if array is empty
                _sizeField.value = 0;
                return;
            }
            else
            {
                IEnumerable<T> values = (value as IEnumerable<T>);
                if (values == null)
                {
                    Debug.LogError($"Couldn't convert {value} to {typeof(T).ToString()} Array");
                    return;
                }

                // Populate fields and their values from passed-in array
                _fields = new List<INotifyValueChanged<T>>();
                foreach (var item in values)
                {
                    var field = GetValueField() as INotifyValueChanged<T>;
                    field.value = (T)item;

                    _fields.Add(field);

                    _container.Add(field as VisualElement);
                }

                _sizeField.value = values.Count();
            }
            
        }

        private void ResizeTo(int newValue)
        {
            _sizeField.value = newValue;

            // Create from scratch if currentFields are null
            if(_fields == null)
            {
                Debug.Log($"Creating from Scratch");
                _fields = new List<INotifyValueChanged<T>>();
                for (int i = 0; i < newValue; i++)
                {
                    var field = GetValueField() as INotifyValueChanged<T>;
                    _fields.Add(field);
                    _container.Add(field as VisualElement);
                }
                return;
            }

            // Shrink list if new value is less than old one
            if(newValue < _fields.Count)
            {
                for (int i = _fields.Count - 1; i >= newValue; i--)
                {
                    (_fields[i] as VisualElement).RemoveFromHierarchy();
                    _fields.RemoveAt(i);
                }
                MarkDirtyRepaint();
                return;
            }

            // Expand list if new value is more than old one.
            if(newValue > _fields.Count)
            {
                int numberToAdd = newValue - _fields.Count;
                for (int i = 0; i < numberToAdd; i++)
                {
                    var field = GetValueField() as INotifyValueChanged<T>;
                    if (field == null)
                    {
                        Debug.LogWarning($"Sorry, can't edit object of type {typeof(T).ToString()} yet.");
                        return;
                    }
                    _fields.Add(field);

                    _container.Add(field as VisualElement);
                }
                MarkDirtyRepaint();
                return;
            }
        }

        private INotifyValueChanged<T> GetValueField()
        {
            var typeString = typeof(T).ToString();
            switch (typeString)
            {
                case "UnityEngine.Bounds":
                    return new BoundsField() as INotifyValueChanged<T>;
                case "UnityEngine.Color":
                    return new ColorField() as INotifyValueChanged<T>;
                case "UnityEngine.AnimationCurve":
                    return new CurveField() as INotifyValueChanged<T>;
                case "System.Double":
                    return new DoubleField() as INotifyValueChanged<T>;
                case "System.Single":
                    return new FloatField() as INotifyValueChanged<T>;
                case "UnityEngine.Gradient":
                    return new GradientField() as INotifyValueChanged<T>;
                case "System.Int32":
                    return new IntegerField() as INotifyValueChanged<T>;
                case "System.Int64":
                    return new LongField() as INotifyValueChanged<T>;
                case "UnityEngine.Rect":
                    return new RectField() as INotifyValueChanged<T>;
                case "System.String":
                    return new TextField() as INotifyValueChanged<T>;
                case "UnityEngine.Vector2":
                    return new Vector2Field() as INotifyValueChanged<T>;
                case "UnityEngine.Vector3":
                    return new Vector3Field() as INotifyValueChanged<T>;
                case "UnityEngine.Vector4":
                    return new Vector4Field() as INotifyValueChanged<T>;
                case "System.Boolean":
                    return new Toggle() as INotifyValueChanged<T>;
                default:
                    Debug.LogWarning($"Couldn't find field for type {typeString}");
                    return null;
            }
        }

        public object GetValues()
        {
            var result = new List<T>();
            for (int i = 0; i < _fields.Count; i++)
            {
                result.Add(_fields[i].value);
            }
            return result.ToArray();
        }

    }

    public interface IArrayProvider
    {
        object GetValues();
        void RemoveFromHierarchy(); // in VisualElement
    }
}