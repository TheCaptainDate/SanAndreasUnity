using System.Collections;
using System.Collections.Generic;
using SanAndreasUnity.Behaviours;
using UnityEngine;

public class PedBasicState : EntityState<PedEntity>
{
    #region Overrides of EntityState

    /// <inheritdoc />
    public override void StateEnter()
    {
        
    }

    /// <inheritdoc />
    public override void StateUpdate()
    {
        
    }

    // todo: move somewhere
    private bool m_smoothMovement;

    /// <inheritdoc />
    public override void StateInputUpdate()
    {
        if (!GameManager.CanPlayerReadInput()) return;

        _entity.IsJumpOn = Input.GetKey(KeyCode.LeftShift); // todo: key settings system.

        Vector3 inputMove = Vector3.zero;
        if (m_smoothMovement)
            inputMove = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        else
            inputMove = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (inputMove.sqrMagnitude > 0f)
        {
            inputMove.Normalize();

             // todo: replace with state names.

             if (!_entity.CurrentStateIdEquals(1))
             {
                 _entity.SwitchState(1);
             }

             /*if (Input.GetKey (m_walkKey))
                 m_ped.IsWalking = true;
             else if (Input.GetKey (m_sprintKey))
                 m_ped.IsSprinting = true;
             else
                 m_ped.IsRunning = true;*/

        }
        else
        {
            if (!_entity.CurrentStateIdEquals(0))
            {
                _entity.SwitchState(0);
            }
        }

        _entity.Movement = Vector3.Scale(_entity.Camera.transform.TransformVector(inputMove),
            new Vector3(1f, 0f, 1f)).normalized;

        // player heading should be assigned here, not in Player class
        //	if (!_player.IsAiming)
        {
            if (_entity.Movement.sqrMagnitude > float.Epsilon)
            {
                _entity.Heading = _entity.Movement;
            }
        }
    }

    /// <inheritdoc />
    public override void StateExit()
    {
        
    }

    #endregion
}
