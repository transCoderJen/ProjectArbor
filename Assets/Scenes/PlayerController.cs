using UnityEngine;
using UnityEngine.AI;
using Unity.Cinemachine;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;
    public Transform cameraTransform;
    private Vector2 inputMove;
    private NavMeshAgent agent;
    public bool movementEnabled = false;
    public PlayerControls controls;
    public Vector3 LastMoveDirection { get; private set; } = Vector3.forward;


    void Awake()
    {
        controls = new PlayerControls();
        controls.Player.Move.performed += ctx => inputMove = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += _ => inputMove = Vector2.zero;

    }

    public void Start()
    {
        // Setup navmesh agent
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.autoBraking = false;
        agent.autoTraverseOffMeshLink = false;
        agent.acceleration = float.MaxValue;


        EnableMovement();
    }


    private void UpdatePlayerPosition()
    {

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camRight * inputMove.x + camForward * inputMove.y;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Vector3 normalizedMoveDir = moveDir.normalized;
            agent.Move(agent.speed * Time.deltaTime * normalizedMoveDir);
            Quaternion targetRotation = Quaternion.LookRotation(normalizedMoveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    public void EnableMovement()
    {
        movementEnabled = true;
        controls.Player.Move.Enable();
    }

    public void DisableMovement()
    {
        movementEnabled = false;

        inputMove = Vector2.zero;

        controls.Player.Move.Disable();

    }

    void Update()
    {
        if (!movementEnabled)
            return;

        UpdatePlayerPosition();
    }
    void FixedUpdate()
    {
        Vector3 moveDir = transform.forward;
        LastMoveDirection = moveDir.normalized;
    }

}