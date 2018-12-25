using UnityEngine;

/// <summary>
/// Stated entity is like entity, but has state machine inside :)
/// </summary>
public abstract class StatedEntity : Entity
{ 
    /// <summary>
    /// Array of states of this entity.
    /// We can probably take all EntityState type components from current gameobject and auto-fill this,
    /// but as mentioned in <see cref="EntityState"/> they probably should be converted to plain c# classes.
    /// So we did it unity-way for now. Easy and pretty visual way.
    /// </summary>
    public EntityState[] States;

    /// <summary>
    /// Current state of entity.
    /// </summary>
    private EntityState _currentState;

    /// <summary>
    /// Previous state of entity.
    /// </summary>
    private EntityState _prevState;
    
    #region Overrides of Entity

    /// <inheritdoc />
    public override void _____EntOnSpawnInternal()
    {
        if (States.Length == 0)
        {
            Debug.LogError($"[Entity] There is no any states at {this.GetType().Name} object. " +
                           $"So why to use StatedEntity? :)");
            return;
        }
        
        foreach (EntityState state in States)
        {
            state.InitState(this);
        }
        
        // by default open first state
        SwitchState(0);
    }

    /// <inheritdoc />
    public override void _____EntUpdateInternal()
    {
        _currentState.StateUpdate();
    }
    
    #endregion
    
    #region States logic

    /// <summary>
    /// Switch entity's state by id.
    /// </summary>
    /// <param name="id">Id of the state.</param>
    public void SwitchState(int id)
    {
        if (id < 0 || id >= States.Length)
        {
            Debug.LogError($"[Entity] Wrong state id: {id}");
            return;
        }

        // I guess better to show this warning than just ignore such case.
        if (States[id] == _currentState)
        {
            Debug.LogError("[Entity] Trying to change state on same. This is wrong behaviour");
            return;
        }
        
        if (_currentState != null)
        {
            _currentState.StateExit();
        }

        _prevState = _currentState;
        _currentState = States[id];
        
        _currentState.StateEnter();
    }

    #endregion
    
}

