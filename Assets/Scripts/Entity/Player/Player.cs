using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Entity
{
    [SerializeField] private InputActionReference actionInput;
    public InputActionReference ActionInput => actionInput;
    [SerializeField] private InputActionReference moveInput;
    public InputActionReference MoveInput => moveInput;

    public TerrainGrassCutter GrassCutter;
    [SerializeField] private LayerBasedParticleSpawner ParticleSpawner;

    public enum ToolType
    {
        Plough,
        Blood,
        Seeds,
        Basket
    }

    public ToolType CurrentTool;

    public Transform toolIndicator;

    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerManagementState ManagementState { get; private set; }

    public Vector2 cachedMoveInput;

    protected override void Awake()
    {
        base.Awake();

        StateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, StateMachine, "Idle");
        MoveState = new PlayerMoveState(this, StateMachine, "Move");
        ManagementState = new PlayerManagementState(this, StateMachine, "Idle");
    }

    protected override void Start()
    {
        base.Start();

        StateMachine.Initialize(IdleState);
    }

    private void OnEnable() {
        
    }
    
    protected override void Update()
    {
        base.Update();

        StateMachine.CurrentState?.Update();

        if (actionInput.action.WasPressedThisFrame())
        {
            UseTool();
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            CurrentTool++;
            if ((int)CurrentTool >= 4)
            {
                CurrentTool = ToolType.Plough;
            }
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            UseTool();
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryCutGrass(LastFacingDir);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        StateMachine.CurrentState?.FixedUpdate();
    }

    private void UseTool()
    {
        GrowBlock block = GetBlock();

        if (block == null) return;

        switch (CurrentTool)
        {
            case ToolType.Plough:
                block.PloughSoil();
                break;
            case ToolType.Blood:
                block.WaterSoil();
                break;
            case ToolType.Seeds:
                block.PlantCrop();
                break;
            case ToolType.Basket:
                block.HarvestCrop();
                break;
        }
    }

    public GrowBlock GetBlock()
    {
        GrowBlock block = GridManager.Instance.GetBlock(toolIndicator.position.x, toolIndicator.position.z);
        return block;
    }

    private void TryCutGrass(Vector3 hitPoint)
    {
        // if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 2f, TerrainLayer))
        // {
        //     ParticleSpawner.SpawnFromHit(hit);
        // }
        GrassCutter.CutGrass(LastFacingDir);
    }
}
