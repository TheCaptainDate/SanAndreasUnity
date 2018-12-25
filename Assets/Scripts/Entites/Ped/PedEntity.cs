using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SanAndreasUnity.Behaviours;
using SanAndreasUnity.Behaviours.World;
using UnityEngine;
using SanAndreasUnity;
using SanAndreasUnity.Behaviours.Vehicles;
using SanAndreasUnity.Importing.Animation;
using SanAndreasUnity.Utilities;

public class PedEntity : StatedEntity
{
    private WeaponHolder m_weaponHolder;

    public WeaponHolder WeaponHolder
    {
        get { return m_weaponHolder; }
    }

    private int jumpTimer;


    #region Public Vars

    public Camera Camera;
    public PedModel PlayerModel;

    public bool shouldPlayAnims = true;

    public float TurnSpeed = 10f;

    public bool enableFlying = false;
    public bool enableNoclip = false;

    public CharacterController characterController;

    public float jumpSpeed = 8.0f;

    // Small amounts of this results in bumping when walking down slopes, but large amounts results in falling too fast
    public float antiBumpFactor = .75f;

    // Player must be grounded for at least this many physics frames before being able to jump again; set to 0 to allow bunny hopping
    public int antiBunnyHopFactor = 1;

    #endregion Inspector Fields

    #region Properties

    public Cell Cell
    {
        get { return Cell.Instance; }
    }

    public Animation AnimComponent
    {
        get { return PlayerModel.AnimComponent; }
    }

    public SanAndreasUnity.Importing.Items.Definitions.PedestrianDef PedDef
    {
        get { return this.PlayerModel.Definition; }
    }

    public static int RandomPedId
    {
        get
        {
            int count = SanAndreasUnity.Importing.Items.Item
                .GetNumDefinitions<SanAndreasUnity.Importing.Items.Definitions.PedestrianDef>();
            if (count < 1)
                throw new System.Exception("No ped definitions found");

            int index = Random.Range(0, count);
            return SanAndreasUnity.Importing.Items.Item
                .GetDefinitions<SanAndreasUnity.Importing.Items.Definitions.PedestrianDef>().ElementAt(index).Id;
        }
    }

    public Vector3 Position
    {
        get { return transform.localPosition; }
        set { transform.localPosition = value; }
    }

    public bool IsGrounded
    {
        get { return characterController.isGrounded; }
    }

    public bool IsWalking { get; set; }
    public bool IsRunning { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsJumpOn { get; set; }

    public Vector3 Velocity { get; private set; }

    /// <summary> Current movement input. </summary>
    public Vector3 Movement { get; set; }

    /// <summary> Direction towards which the player turns. </summary>
    public Vector3 Heading { get; set; }

    public bool IsAiming
    {
        get { return m_weaponHolder.IsAiming; }
    }

    public Weapon CurrentWeapon
    {
        get { return m_weaponHolder.CurrentWeapon; }
    }

    public bool IsFiring
    {
        get { return m_weaponHolder.IsFiring; }
        set { m_weaponHolder.IsFiring = value; }
    }

    public Vector3 AimDirection
    {
        get { return m_weaponHolder.AimDirection; }
    }

    private static bool makeGPUAdjustments;

    private Coroutine m_findGroundCoroutine;

    #endregion Properties


    // TODO: Wrong, remove
    public static Ped Instance { get; private set; }

    /// <summary>Position of player instance.</summary>
    public static Vector3 InstancePos
    {
        get { return Instance.transform.position; }
    }

    // TODO: some old thing for netwoking? seems to be wrong keep it here.
    public bool IsLocalPlayer { get; private set; }

    #region Overrides of Entity

    /// <inheritdoc />
    public override void EntInit()
    {
        // todo: what is that? Why we need this?
        if (null == Instance)
        {
            //Instance = this;
            IsLocalPlayer = true;
        }

        characterController = GetComponent<CharacterController>();
        m_weaponHolder = GetComponent<WeaponHolder>();
        
        // TODO: this stuff should be handled by camera module.
        Camera = Camera.main;
    }

    /// <inheritdoc />
    public override void EntOnSpawn()
    {
        jumpTimer = antiBunnyHopFactor;

        if (!IsGrounded)
        {
            // Find the ground (instead of falling)
            FindGround();
        }
    }


    /// <inheritdoc />
    public override void EntUpdate()
    {
        if (!Loader.HasLoaded)
            return;

        // Reset to a valid (and solid!) start position when falling below the world
        if (transform.position.y < -300)
        {
            Velocity = new Vector3(0, 0, 0);
            Transform spawn = GameObject.Find("Player Spawns").GetComponentsInChildren<Transform>()[1];
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;
        }

        ConstrainPosition();

        ConstrainRotation();

        // TODO: commented part. Replace vehicle logic
        //if (IsDrivingVehicle)
        //    UpdateWheelTurning();

        //If player falls from the map
        if (IsGrounded && transform.position.y < -50)
        {
            Vector3 t = transform.position;
            transform.position = new Vector3(t.x, 150, t.z);
            FindGround();
        }

        // TODO: update camera should be translated to camera module.
        UpdateCamera();
        
        // TODO: commented code, implement damage system
        //this.UpdateDamageStuff ();
    }

    /// <inheritdoc />
    public override void EntFixedUpdate()
    {
        if (!Loader.HasLoaded)
            return;

        // TODO: commented part. Replace vehicle logic
        //if (IsInVehicle) return;


        if (this.IsAiming && this.CurrentWeapon != null && !this.CurrentWeapon.CanTurnInDirectionOtherThanAiming)
        {
            // ped heading can only be the same as ped direction
            this.Heading = this.WeaponHolder.AimDirection;
        }

        // player can look only along X and Z axis
        this.Heading = this.Heading.WithXAndZ().normalized;


        // rotate player towards his heading
        Vector3 forward = Vector3.RotateTowards(this.transform.forward, Heading, TurnSpeed * Time.deltaTime, 0.0f);
        this.transform.rotation = Quaternion.LookRotation(forward);


        if (enableFlying || enableNoclip)
        {
            Heading = Vector3.Scale(Movement, new Vector3(1f, 0f, 1f)).normalized;
            Velocity = Movement * Time.fixedDeltaTime;
            if (enableNoclip)
            {
                transform.position += Velocity;
            }
            else
            {
                characterController.Move(Velocity);
            }
        }
        else
        {

            // movement can only be done on X and Z axis
            this.Movement = this.Movement.WithXAndZ();

            // change heading to match movement input
            //if (Movement.sqrMagnitude > float.Epsilon)
            //{
            //	Heading = Vector3.Scale(Movement, new Vector3(1f, 0f, 1f)).normalized;
            //}

            // change velocity based on movement input and current speed extracted from anim

            float modelVel = Mathf.Abs(PlayerModel.Velocity[PlayerModel.VelocityAxis]);
            //Vector3 localMovement = this.transform.InverseTransformDirection (this.Movement);
            //Vector3 globalMovement = this.transform.TransformDirection( Vector3.Scale( localMovement, modelVel ) );

            Vector3 vDiff = this.Movement * modelVel - new Vector3(Velocity.x, 0f, Velocity.z);
            Velocity += vDiff;

            // apply gravity
            Velocity = new Vector3(Velocity.x, characterController.isGrounded
                ? 0f
                : Velocity.y - 9.81f * 2f * Time.fixedDeltaTime, Velocity.z);

            // Jump! But only if the jump button has been released and player has been grounded for a given number of frames
            if (!this.IsJumpOn)
                jumpTimer++;
            else if (jumpTimer >= antiBunnyHopFactor && this.IsGrounded)
            {
                Velocity += Vector3.up * jumpSpeed;
                jumpTimer = 0;
            }

            // finally, move the character
            characterController.Move(Velocity * Time.fixedDeltaTime);
        }
    }

    #endregion


    #region Internal Functions

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        // TODO: vehicle is partial class now.
        // This should be optimized.. Make vehicle another entity, and allow to interact with ped.
        //if (this.IsInVehicle)
        //	return;

        this.transform.position = position;
        this.transform.rotation = rotation;

        this.FindGround();
    }

    public void Teleport(Vector3 position)
    {
        this.Teleport(position, this.transform.rotation);
    }

    public void FindGround()
    {
        if (m_findGroundCoroutine != null)
        {
            StopCoroutine(m_findGroundCoroutine);
            m_findGroundCoroutine = null;
        }

        m_findGroundCoroutine = StartCoroutine(FindGroundCoroutine());
    }

    private IEnumerator FindGroundCoroutine()
    {
        yield return null;

        // set y pos to high value, so that higher grounds can be loaded
        this.transform.SetY(150);

        Vector3 startingPos = this.transform.position;

        // wait for loader to finish, in case he didn't
        while (!Loader.HasLoaded)
            yield return null;

        // yield until you find ground beneath or above the player, or until timeout expires

        float timeStarted = Time.time;
        int numAttempts = 1;

        while (true)
        {
            if (Time.time - timeStarted > 4.0f)
            {
                // timeout expired
                Debug.LogWarning("Failed to find ground - timeout expired");
                yield break;
            }

            // maintain starting position
            this.transform.position = startingPos;
            this.Velocity = Vector3.zero;

            RaycastHit hit;
            float raycastDistance = 1000f;
            // raycast against all layers, except player
            int raycastLayerMask = ~ LayerMask.GetMask("Player");

            Vector3[] raycastPositions = new Vector3[]
            {
                this.transform.position, this.transform.position + Vector3.up * raycastDistance
            }; //transform.position - Vector3.up * characterController.height;
            Vector3[] raycastDirections = new Vector3[] {Vector3.down, Vector3.down};
            string[] customMessages = new string[] {"from center", "from above"};

            for (int i = 0; i < raycastPositions.Length; i++)
            {
                if (Physics.Raycast(raycastPositions[i], raycastDirections[i], out hit, raycastDistance,
                    raycastLayerMask))
                {
                    // ray hit the ground
                    // we can move there

                    this.OnFoundGround(hit, numAttempts, customMessages[i]);

                    yield break;
                }
            }


            numAttempts++;
            yield return null;
        }
    }

    private void OnFoundGround(RaycastHit hit, int numAttempts, string customMessage)
    {
        this.transform.position = hit.point + Vector3.up * characterController.height * 1.5f;
        this.Velocity = Vector3.zero;

        Debug.LogFormat("Found ground at {0}, distance {1}, object name {2}, num attempts {3}, {4}", hit.point,
            hit.distance,
            hit.transform.name, numAttempts, customMessage);
    }

    #endregion

    #region InternalFuctions2

    private void ConstrainPosition()
    {
        // Constrain to stay inside map

        if (transform.position.x < -3000)
        {
            var t = transform.position;
            t.x = -3000;
            transform.position = t;
        }

        if (transform.position.x > 3000)
        {
            var t = transform.position;
            t.x = 3000;
            transform.position = t;
        }

        if (transform.position.z < -3000)
        {
            var t = transform.position;
            t.z = -3000;
            transform.position = t;
        }

        if (transform.position.z > 3000)
        {
            var t = transform.position;
            t.z = 3000;
            transform.position = t;
        }
    }

    private void ConstrainRotation()
    {
        // TODO: commented part. Replace vehicle logic
        //if (IsInVehicle)
        //   return;

        // ped can only rotate around Y axis

        Vector3 eulers = this.transform.eulerAngles;
        if (eulers.x != 0f || eulers.z != 0f)
        {
            eulers.x = 0f;
            eulers.z = 0f;
            this.transform.eulerAngles = eulers;
        }
    }

    private void UpdateAnims()
    {
        if (!this.shouldPlayAnims)
            return;

        // TODO: commented part. Replace vehicle logic
        // if (IsInVehicle || m_weaponHolder.IsHoldingWeapon)
        //    return;

        if (IsRunning)
        {
            PlayerModel.PlayAnim(AnimGroup.WalkCycle, AnimIndex.Run, PlayMode.StopAll);
        }
        else if (IsWalking)
        {
            PlayerModel.PlayAnim(AnimGroup.WalkCycle, AnimIndex.Walk, PlayMode.StopAll);
        }
        else if (IsSprinting)
        {
            PlayerModel.PlayAnim(AnimGroup.MyWalkCycle, AnimIndex.sprint_civi);
        }
        else
        {
            // player is standing
            PlayerModel.PlayAnim(AnimGroup.WalkCycle, AnimIndex.Idle, PlayMode.StopAll);
        }
    }

    #endregion

    #region Camera Module --> move me

    
    private float _pitch;
    private float _yaw;

    public static bool _showVel = true;

    // Alpha speedometer
    private const float velTimer = 1 / 4f;

    private static float velCounter = velTimer;

    private static Vector3 lastPos = Vector3.zero,
        deltaPos = Vector3.zero;

    private Vector2 _mouseAbsolute;
    private Vector2 _smoothMouse = Vector2.zero;
    private Vector3 targetDirection = Vector3.forward;

        
    public Vector2 CursorSensitivity = new Vector2(2f, 2f);

    public float CarCameraDistance = 6.0f;
    public float PlayerCameraDistance = 3.0f;

    //public Vector2 PitchClamp = new Vector2(-89f, 89f);
    public Vector2 clampInDegrees = new Vector2(90, 60);

    public Vector2 smoothing = new Vector2(10, 10);
    public bool m_doSmooth = true;

    [SerializeField] private bool m_smoothMovement = false;

    [SerializeField] private KeyCode m_walkKey = KeyCode.LeftAlt;
    [SerializeField] private KeyCode m_sprintKey = KeyCode.Space;
    [SerializeField] private KeyCode m_jumpKey = KeyCode.LeftShift;
    
    public float CurVelocity
    {
        get
        {
            return deltaPos.magnitude * 3.6f / velTimer;
        }
    }

    public Vector3 CameraFocusPos { get { return transform.position + Vector3.up * 0.5f; } }
    //public Vector3 CameraFocusPosVehicle { get { return m_ped.CurrentVehicle.transform.position; } }

    
    private void UpdateCamera ()
		{

			if (GameManager.CanPlayerReadInput())
			{
				// rotate camera

				var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

				mouseDelta = Vector2.Scale(mouseDelta, CursorSensitivity);


				if (m_doSmooth)
				{
					_smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
					_smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

					_mouseAbsolute += _smoothMouse;
				}
				else
					_mouseAbsolute += mouseDelta;

				// Waiting for an answer: https://stackoverflow.com/questions/50837685/camera-global-rotation-clamping-issue-unity3d

				/*if (clampInDegrees.x > 0)
                    _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x, clampInDegrees.x);*/

				if (clampInDegrees.y > 0)
					_mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y, clampInDegrees.y);


//				Vector3 eulers = Camera.transform.eulerAngles;
//				eulers.x += - mouseDelta.y;
//				eulers.y += mouseDelta.x;
//
//				// no rotation around z axis
//				eulers.z = 0;
//
//				// clamp rotation
//				if(eulers.x > 180)
//					eulers.x -= 360;
//				eulers.x = Mathf.Clamp (eulers.x, -clampInDegrees.x, clampInDegrees.x);
//
//				// apply new rotation
//				Camera.transform.eulerAngles = eulers;

			}

			Camera.transform.rotation = Quaternion.AngleAxis(_mouseAbsolute.x, Vector3.up)
				* Quaternion.AngleAxis(-_mouseAbsolute.y, Vector3.right);


			// this must be called from here (right after the camera transform is changed), otherwise camera will shake
			WeaponHolder.RotatePlayerInDirectionOfAiming ();


			// cast a ray from player to camera to see if it hits anything
			// if so, then move the camera to hit point

			float distance;
			Vector3 castFrom;
			Vector3 castDir = -Camera.transform.forward;

			float scrollValue = Input.mouseScrollDelta.y;
			if (!GameManager.CanPlayerReadInput ())
				scrollValue = 0;

			// TODO: vehicle and aiming stuff not implemented currently due to refactor
			/*if (m_ped.IsInVehicle)
			{
				CarCameraDistance = Mathf.Clamp (CarCameraDistance - scrollValue, 2.0f, 32.0f);
				distance = CarCameraDistance;
				castFrom = this.CameraFocusPosVehicle;
				// cast towards current camera position
			//	castDir = (Camera.transform.position - castFrom).normalized;
			}
			else if (m_ped.IsAiming)
			{
				castFrom = this.CameraFocusPos;

				// use distance from gun aiming offset ?
				if (m_ped.CurrentWeapon.GunAimingOffset != null) {
				//	Vector3 desiredCameraPos = this.transform.TransformPoint (- _player.CurrentWeapon.GunAimingOffset.Aim) + Vector3.up * .5f;
				//	Vector3 desiredCameraPos = this.transform.TransformPoint( new Vector3(0.8f, 1.0f, -1) );
					Vector3 desiredCameraPos = this.CameraFocusPos + Camera.transform.TransformVector( m_ped.WeaponHolder.cameraAimOffset );
					Vector3 diff = desiredCameraPos - castFrom;
					distance = diff.magnitude;
					castDir = diff.normalized;
				}
				else
					distance = PlayerCameraDistance;
			}
			else*/
			{
				PlayerCameraDistance = Mathf.Clamp(PlayerCameraDistance - scrollValue, 2.0f, 32.0f);
				distance = PlayerCameraDistance;
				castFrom = this.CameraFocusPos;
			}

			var castRay = new Ray(castFrom, castDir);

			RaycastHit hitInfo;

			if (Physics.SphereCast(castRay, 0.25f, out hitInfo, distance,
				-1 ^ (1 << MapObject.BreakableLayer) ^ (1 << Vehicle.Layer)))
			{
				distance = hitInfo.distance;
			}

			Camera.transform.position = castRay.GetPoint(distance);
		}

    #endregion
}


// TODO:
// Things i should not forget about while doing this LARGE refactoring.
// 1. _shouldPlayAnimations - not working for now, but used in debug menu to stop execution of state animations.