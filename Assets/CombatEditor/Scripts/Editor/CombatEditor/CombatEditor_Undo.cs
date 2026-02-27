using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{
    public partial class CombatEditor
    {
        // Register undo operations for CombatEditor
        
        /// <summary>
        /// Register undo operation for event track changes
        /// </summary>
        /// <param name="abilityObj">The AbilityScriptableObject being modified</param>
        /// <param name="operationName">Name of the operation for the undo stack</param>
        public void RegisterUndo(Object obj, string operationName)
        {
            Undo.RecordObject(obj, operationName);
        }
        
        /// <summary>
        /// Register undo for a collection of objects
        /// </summary>
        /// <param name="objects">Collection of objects to record for undo</param>
        /// <param name="operationName">Name of the operation for the undo stack</param>
        public void RegisterUndoForObjects(Object[] objects, string operationName)
        {
            Undo.RecordObjects(objects, operationName);
        }
        
        /// <summary>
        /// Register undo operation when creating a new event
        /// </summary>
        /// <param name="abilityObj">The AbilityScriptableObject</param>
        /// <param name="eventObj">The newly created event object</param>
        public void RegisterUndoForCreatedEvent(AbilityScriptableObject abilityObj, AbilityEventObj eventObj)
        {
            Undo.RegisterCreatedObjectUndo(eventObj, "Create Event");
            Undo.RecordObject(abilityObj, "Add Event");
        }
        
        /// <summary>
        /// Register undo operation when destroying an event object
        /// </summary>
        /// <param name="abilityObj">The AbilityScriptableObject</param>
        /// <param name="eventObj">The event object being destroyed</param>
        public void RegisterUndoForDestroyedEvent(AbilityScriptableObject abilityObj, AbilityEventObj eventObj)
        {
            Undo.RecordObject(abilityObj, "Delete Event");
            Undo.DestroyObjectImmediate(eventObj);
        }
    }
} 