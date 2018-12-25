using System.Collections;
using System.Collections.Generic;
using SanAndreasUnity.Importing.Animation;
using UnityEngine;

public class PedRunState : PedBasicState
{
    #region Overrides of PedBasicState

    /// <inheritdoc />
    public override void StateEnter()
    {
        base.StateEnter();
    }

    /// <inheritdoc />
    public override void StateUpdate()
    {
        base.StateUpdate();
    }

    /// <inheritdoc />
    public override void StateInputUpdate()
    {
        base.StateInputUpdate();
        _entity.PlayerModel.PlayAnim(AnimGroup.WalkCycle, AnimIndex.Run, PlayMode.StopAll);  
    }

    /// <inheritdoc />
    public override void StateExit()
    {
        base.StateExit();
    }

    #endregion
}
