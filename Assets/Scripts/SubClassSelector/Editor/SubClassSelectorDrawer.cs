using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Actions.Editor
{
    /// <summary>
    /// UIElements-based PropertyDrawer for the SubClassSelector attribute.
    /// Provides a dropdown menu for selecting subclasses or interface implementations.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubClassSelectorAttribute))]
    public class SubClassSelectorDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SubClassSelectorAttribute attribute = (SubClassSelectorAttribute)this.attribute;
            
            // Get the field type from fieldInfo
            Type baseType = fieldInfo.FieldType;
            
            // If it's a list or array, get the element type
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
            {
                baseType = baseType.GetGenericArguments()[0];
            }
            else if (baseType.IsArray)
            {
                baseType = baseType.GetElementType();
            }

            // Create a container for the property
            VisualElement container = new VisualElement(); 
            
            // Get all assignable types using Unity's TypeCache
            List<Type> assignableTypes = GetAssignableTypes(baseType, attribute.IncludeAbstractClasses);
            
            // Create the dropdown
            PopupField<string> dropdown = new PopupField<string>
            {
                label = string.IsNullOrEmpty(attribute.Label) ? $"Choose {baseType.Name}" : attribute.Label,
                choices = new List<string>(),
                value = GetCurrentValue(property, assignableTypes, attribute.ShowFullTypeName)
            };

            // Populate the dropdown choices
            List<string> choices = new List<string> { "None" };
            foreach (Type type in assignableTypes)
            {
                string displayName = attribute.ShowFullTypeName ? type.FullName : type.Name;
                choices.Add(displayName);
            }
            dropdown.choices = choices;

            // Handle dropdown value changes
            dropdown.RegisterValueChangedCallback(evt =>
            {
                Type selectedType = null;
                if (evt.newValue != "None")
                {
                    selectedType = assignableTypes.FirstOrDefault(t => 
                        (attribute.ShowFullTypeName ? t.FullName : t.Name) == evt.newValue);
                }

                SetManagedReferenceValue(property, selectedType);
                property.serializedObject.ApplyModifiedProperties();
            });

            container.Add(dropdown);

            // If a value is selected, show its inspector using PropertyField
            if (property.managedReferenceValue != null)
            {
                // Get the actual type of the managed reference
                Type actualType = property.managedReferenceValue.GetType();
                
                // Create a Foldout with the class type name
                Foldout foldout = new Foldout
                {
                    text = actualType.Name,
                    value = true
                };
                
                // Create a PropertyField for the managed reference itself
                // This will automatically show all the fields of the object
                PropertyField propertyField = new PropertyField(property);
                foldout.Add(propertyField);
                container.Add(foldout);
            }

            return container;
        }

        /// <summary>
        /// Gets all types that are assignable from the base type using Unity's TypeCache.
        /// </summary>
        private List<Type> GetAssignableTypes(Type baseType, bool includeAbstract)
        {
            List<Type> types = new List<Type>();
            
            // Use Unity's TypeCache to get all types in the project
            TypeCache.TypeCollection allTypes = TypeCache.GetTypesDerivedFrom(baseType);
            
            foreach (Type type in allTypes)
            {
                if (type != baseType && 
                    !type.IsInterface &&
                    (includeAbstract || !type.IsAbstract))
                {
                    types.Add(type);
                }
            }

            // Sort by name for consistent ordering
            return types.OrderBy(t => t.FullName).ToList();
        }

        /// <summary>
        /// Gets the current value display string for the dropdown.
        /// </summary>
        private string GetCurrentValue(SerializedProperty property, List<Type> assignableTypes, bool showFullTypeName)
        {
            if (property.managedReferenceValue == null)
            {
                return "None";
            }

            Type currentType = property.managedReferenceValue.GetType();
            return showFullTypeName ? currentType.FullName : currentType.Name;
        }

        /// <summary>
        /// Sets the managed reference value to an instance of the specified type.
        /// </summary>
        private void SetManagedReferenceValue(SerializedProperty property, Type type)
        {
            object instance = null;
            if (type != null)
            {
                instance = Activator.CreateInstance(type);
            }
            property.managedReferenceValue = instance;
        }
    }
}
