using UnityEngine;

namespace Actions
{
    /// <summary>
    /// Attribute that enables a dropdown menu in the Unity Inspector for selecting
    /// subclasses or classes implementing the field's type.
    /// When a class is selected from the dropdown, it creates an instance of that class
    /// for the managed reference field.
    /// 
    /// Usage:
    /// [SerializeReference, SubClassSelector]
    /// public IAction myAction;
    /// </summary>
    public class SubClassSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Optional custom label for the dropdown field.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Whether to show the full type name (including namespace) in the dropdown.
        /// Default is false (shows only the class name).
        /// </summary>
        public bool ShowFullTypeName { get; set; } = false;

        /// <summary>
        /// Whether to include abstract classes in the dropdown.
        /// Default is false.
        /// </summary>
        public bool IncludeAbstractClasses { get; set; } = false;
    }
}
