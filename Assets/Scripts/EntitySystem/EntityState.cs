using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actually i don't want this to be behaviour.
/// States shouldn't contains much of code and it's always better to call methods on entity.
///
/// For us it's pretty tricky due to some hardcoded calculations of aiming and etc.
/// So currently states are behaviours, but i am really hope that we can move to plain c# classes in future :)
///
/// TODO: all methods probably should be virtual instead of abstract.
/// </summary>
public abstract class EntityState : MonoBehaviour
{
    /// <summary>
    /// This method runs when we switch to this state.
    /// </summary>
    public abstract void StateEnter();
    
    /// <summary>
    /// Update runs just like unity's update but only when state is active.
    /// </summary>
    public abstract void StateUpdate();

    /// <summary>
    /// Update that run before StateUpdate. Use this method to split input/state update logic. 
    /// </summary>
    public abstract void StateInputUpdate();
    
    /// <summary>
    /// This method runs when we leave this state.
    /// TODO: this method probably usselss and should be removed.
    /// </summary>
    public abstract void StateExit();

    /// <summary>
    /// It is internal init method, called by entity at spawn. 
    /// </summary>
    public abstract void InitState<T>(T entity) where T: StatedEntity;

}

/// <summary>
/// <inheritdoc cref="EntityState"/>
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class EntityState<T> : EntityState where T : StatedEntity
{
    /// <summary>
    /// Link to entity object.
    /// </summary>
    protected T _entity;

    #region Overrides of EntityState

    /// <inheritdoc />
    /// TODO: I AM NOT SURE RIGHT NOW how to did it properly.
    /// Leave it like this for now..
    public override void InitState<T1>(T1 entity)
    {
        _entity = entity as T;
    }

    #endregion
}