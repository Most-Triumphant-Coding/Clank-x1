using Godot;

public partial class PlayerHoverBikeController : Node3D
{
    private const string FirePrimaryAction = "fire_primary";
    private const string FireMissileAction = "fire_missile";
    private const float LaserSpawnForwardOffset = 1.35f;
    private const float LaserSpawnHeightOffset = 0.2f;

    [Export]
    public float HoverHeightMeters { get; set; } = 1.8f;

    [Export]
    public float WorldHalfExtentMeters { get; set; } = 430.0f;

    [Export]
    public float TurnRateDegreesPerSecond { get; set; } = 35.0f;

    [Export]
    public float ForwardSpeedUnitsPerSecond { get; set; } = 80.0f;

    [Export]
    public float BrakeDecelerationUnitsPerSecondSquared { get; set; } = 40.0f;

    [Export]
    public float MaxHealth { get; set; } = 200.0f;

    [Export]
    public float ThirdPersonCameraHeight { get; set; } = 1.0f;

    [Export]
    public float ThirdPersonCameraDistance { get; set; } = 5.0f;

    [Export]
    public float ThirdPersonCameraPitchDegrees { get; set; } = -10.0f;

    [Signal]
    public delegate void PlayerDestroyedEventHandler();

    public float Health { get; private set; } = 200.0f;
    public float Armor { get; private set; } = 100.0f;
    public float CurrentSpeed => _forwardSpeed;
    public bool IsAlive { get; private set; } = true;
    public Vector3 ViewOrigin => _camera?.GlobalPosition ?? GlobalPosition;
    public Vector3 ViewForward => -(_camera?.GlobalTransform.Basis.Z ?? GlobalTransform.Basis.Z);

    private Node3D _headPivot = default!;
    private MeshInstance3D _headMesh = default!;
    private Camera3D _camera = default!;
    private CombatLoadoutController _combat = default!;
    private float _forwardSpeed;
    private float _maxArmor;

    public override void _Ready()
    {
        _headPivot = GetNode<Node3D>("HeadPivot");
        _headMesh = GetNode<MeshInstance3D>("HeadPivot/HeadMesh");
        _camera = GetNode<Camera3D>("HeadPivot/Camera3D");
        _combat = GetNode<CombatLoadoutController>("Combat");

        ConfigureHeadVisuals();
        _camera.Current = true;
        _camera.Position = new Vector3(0.0f, ThirdPersonCameraHeight, ThirdPersonCameraDistance);
        _camera.RotationDegrees = new Vector3(ThirdPersonCameraPitchDegrees, 0.0f, 0.0f);

        var starterLoadout = BikeLoadout.CreateStarter();
        _combat.Configure(starterLoadout);

        _maxArmor = starterLoadout.FrontArmor.Durability
            + starterLoadout.LeftArmor.Durability
            + starterLoadout.RightArmor.Durability
            + starterLoadout.RearArmor.Durability;
        Armor = _maxArmor;
        Health = MaxHealth;
        _forwardSpeed = ForwardSpeedUnitsPerSecond;

        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (!IsAlive)
        {
            return;
        }

        float dt = (float)delta;
        float turnInput = GetTurnInput();

        RotateOnPlane(turnInput, dt);
        UpdateSpeeds(dt);
        MoveOnPlane(dt);
        HandleCombatInput();
    }

    private static float GetTurnInput()
    {
        float actionTurn = Input.GetActionStrength("turn_left") - Input.GetActionStrength("turn_right");

        float keyTurn = 0.0f;
        if (Input.IsPhysicalKeyPressed(Key.A))
        {
            keyTurn += 1.0f;
        }

        if (Input.IsPhysicalKeyPressed(Key.D))
        {
            keyTurn -= 1.0f;
        }

        return Mathf.Abs(keyTurn) > Mathf.Abs(actionTurn) ? keyTurn : actionTurn;
    }

    private void ConfigureHeadVisuals()
    {
        _headMesh.Visible = true;

        var headMesh = new BoxMesh
        {
            Size = new Vector3(1.2f, 0.45f, 2.2f)
        };

        var headMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.22f, 0.22f, 0.22f),
            Metallic = 0.0f,
            Roughness = 0.9f,
            CullMode = BaseMaterial3D.CullModeEnum.Back
        };

        _headMesh.Mesh = headMesh;
        _headMesh.MaterialOverride = headMaterial;
        _headMesh.Position = new Vector3(0.0f, -0.2f, 0.0f);
    }

    private void RotateOnPlane(float turnInput, float dt)
    {
        if (!Mathf.IsZeroApprox(turnInput))
        {
            float turnRadians = Mathf.DegToRad(TurnRateDegreesPerSecond) * dt * turnInput;
            RotateY(turnRadians);
        }
    }

    private void UpdateSpeeds(float dt)
    {
        if (Input.IsPhysicalKeyPressed(Key.S))
        {
            _forwardSpeed = Mathf.Max(0.0f, _forwardSpeed - (BrakeDecelerationUnitsPerSecondSquared * dt));
            return;
        }

        _forwardSpeed = ForwardSpeedUnitsPerSecond;
    }

    private void MoveOnPlane(float dt)
    {
        Vector3 move = -GlobalTransform.Basis.Z * _forwardSpeed * dt;
        Vector3 nextPos = GlobalPosition + move;
        nextPos.X = Mathf.Clamp(nextPos.X, -WorldHalfExtentMeters, WorldHalfExtentMeters);
        nextPos.Z = Mathf.Clamp(nextPos.Z, -WorldHalfExtentMeters, WorldHalfExtentMeters);
        nextPos.Y = HoverHeightMeters;
        GlobalPosition = nextPos;
    }

    private void HandleCombatInput()
    {
        if (Input.IsActionJustPressed(FirePrimaryAction))
        {
            if (_combat.TryFirePrimary())
            {
                SpawnPrimaryLaser();
            }
        }

        if (Input.IsActionJustPressed(FireMissileAction))
        {
            _combat.TryFireMissile();
        }
    }

    private void SpawnPrimaryLaser()
    {
        Node? currentScene = GetTree().CurrentScene;
        if (currentScene == null)
        {
            return;
        }

        var laser = new LaserShot();
        Vector3 forward = -GlobalTransform.Basis.Z;
        laser.GlobalPosition = GlobalPosition + (forward * LaserSpawnForwardOffset) + (Vector3.Up * LaserSpawnHeightOffset);
        laser.Direction = forward;
        currentScene.AddChild(laser);
    }

    public void ApplyDamage(float rawDamage)
    {
        if (!IsAlive || rawDamage <= 0.0f)
        {
            return;
        }

        float damageRemaining = rawDamage;

        if (Armor > 0.0f)
        {
            float armorAbsorb = Mathf.Min(Armor, damageRemaining * 0.7f);
            Armor -= armorAbsorb;
            damageRemaining -= armorAbsorb;
        }

        if (damageRemaining > 0.0f)
        {
            Health = Mathf.Max(0.0f, Health - damageRemaining);
        }

        if (Health <= 0.0f)
        {
            IsAlive = false;
            _forwardSpeed = 0.0f;
            EmitSignal(SignalName.PlayerDestroyed);
        }
    }

    public void RespawnAt(Vector3 position)
    {
        position.Y = HoverHeightMeters;
        GlobalPosition = position;
        Rotation = new Vector3(0.0f, Rotation.Y, 0.0f);

        _forwardSpeed = ForwardSpeedUnitsPerSecond;
        Health = MaxHealth;
        Armor = _maxArmor;
        IsAlive = true;
    }
}
