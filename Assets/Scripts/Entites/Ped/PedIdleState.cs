﻿using System.Collections;
using System.Collections.Generic;
using SanAndreasUnity.Importing.Animation;
using UnityEngine;

public class PedIdleState : PedBasicState
{
    #region Overrides of EntityState

    /// <inheritdoc />
    public override void StateEnter()
    {
        base.StateEnter();
    }

    /// <inheritdoc />
    public override void StateUpdate()
    {
        base.StateUpdate();
        
        _entity.PlayerModel.PlayAnim(AnimGroup.WalkCycle, AnimIndex.Idle, PlayMode.StopAll);  
    }

    /// <inheritdoc />
    public override void StateInputUpdate()
    {
        base.StateInputUpdate();
        
    }

    /// <inheritdoc />
    public override void StateExit()
    {
        base.StateExit();
    }

    #endregion
}
