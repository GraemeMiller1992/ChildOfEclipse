using UnityEngine;

namespace ChildOfEclipse
{
    /// <summary>
    /// Interface for objects that can be interacted with via the InteractPointer.
    /// Implement this interface on any GameObject that should be clickable/interactable.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called when the object is clicked/hovered by the interact pointer.
        /// </summary>
        /// <param name="interactor">The GameObject that initiated the interaction.</param>
        /// <param name="hitInfo">Raycast hit information containing point of contact and normal.</param>
        void OnInteract(GameObject interactor, RaycastHit hitInfo);

        /// <summary>
        /// Called when the object is hovered over by the interact pointer.
        /// </summary>
        /// <param name="interactor">The GameObject that is hovering.</param>
        void OnHoverEnter(GameObject interactor);

        /// <summary>
        /// Called when the object is no longer being hovered over.
        /// </summary>
        /// <param name="interactor">The GameObject that was hovering.</param>
        void OnHoverExit(GameObject interactor);

        /// <summary>
        /// Returns whether the object can currently be interacted with.
        /// Useful for conditional interaction (e.g., locked doors, disabled buttons).
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// Optional: Returns a description of what will happen when interacted with.
        /// Used for UI tooltips or interaction prompts.
        /// </summary>
        string InteractionDescription { get; }
    }
}
