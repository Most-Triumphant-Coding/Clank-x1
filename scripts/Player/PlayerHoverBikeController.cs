using Godot;
using System.Collections.Generic;

public partial class PlayerHoverBikeController : CharacterBody3D
{
    private const string FirePrimaryAction = "fire_primary";
    private const string FireMissileAction = "fire_missile";
    private const float LaserSpawnForwardOffset = 2.15f;
    private const float LaserSpawnHeightOffset = 0.2f;
    private const float CollisionDamage = 50.0f;

    [Export]
    public float HoverHeightMeters { get; set; } = 1.8f;

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
    public float CurrentSpeed => _forwardSpeed;
    public bool IsAlive { get; private set; } = true;
    public Vector3 ViewOrigin => _camera?.GlobalPosition ?? GlobalPosition;
    public Vector3 ViewForward => -(_camera?.GlobalTransform.Basis.Z ?? GlobalTransform.Basis.Z);

    private Node3D _headPivot = default!;
    private MeshInstance3D _headMesh = default!;
    private Camera3D _camera = default!;
    private CombatLoadoutController _combat = default!;
    private float _forwardSpeed;
    private readonly HashSet<ulong> _obstacleContacts = new();
    private readonly BoxShape3D _contactShape = new() { Size = new Vector3(1.3f, 0.55f, 2.3f) };

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

        Health = MaxHealth;
        _forwardSpeed = ForwardSpeedUnitsPerSecond;

        SetProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive)
        {
            return;
        }

        float dt = (float)delta;
        float turnInput = GetTurnInput();

        RotateOnPlane(turnInput, dt);
        UpdateSpeeds(dt);
        if (MoveOnPlane(dt))
        {
            return;
        }
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
        _headMesh.Position = Vector3.Zero;
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

    private bool MoveOnPlane(float dt)
    {
        Vector3 move = -GlobalTransform.Basis.Z * _forwardSpeed * dt;
        KinematicCollision3D collision = MoveAndCollide(move);
        var contacts = new Dictionary<ulong, StaticBody3D>();
        if (collision?.GetCollider() is StaticBody3D collider)
        {
            contacts[collider.GetInstanceId()] = collider;
        }

        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = _contactShape,
            Transform = GlobalTransform,
            CollisionMask = CollisionMask,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() }
        };

        foreach (Godot.Collections.Dictionary result in GetWorld3D().DirectSpaceState.IntersectShape(query, 512))
        {
            if (result["collider"].AsGodotObject() is StaticBody3D obstacle)
            {
                contacts[obstacle.GetInstanceId()] = obstacle;
            }
        }

        foreach (KeyValuePair<ulong, StaticBody3D> contact in contacts)
        {
            if (!_obstacleContacts.Contains(contact.Key))
            {
                Vector3 impactPoint;
                if (collision?.GetCollider() is StaticBody3D hit && hit.GetInstanceId() == contact.Key)
                {
                    impactPoint = collision.GetPosition();
                }
                else
                {
                    var excludedBodies = new Godot.Collections.Array<Rid> { GetRid() };
                    foreach (KeyValuePair<ulong, StaticBody3D> other in contacts)
                    {
                        if (other.Key != contact.Key)
                        {
                            excludedBodies.Add(other.Value.GetRid());
                        }
                    }

                    query.Exclude = excludedBodies;
                    Godot.Collections.Dictionary restInfo = GetWorld3D().DirectSpaceState.GetRestInfo(query);
                    impactPoint = restInfo.ContainsKey("point")
                        ? restInfo["point"].AsVector3()
                        : GlobalPosition;
                }

                SpawnImpactFlash(impactPoint);
                bool destroyed = Health <= CollisionDamage;
                ApplyDamage(CollisionDamage);
                if (destroyed)
                {
                    return true;
                }
            }
        }

        _obstacleContacts.Clear();
        _obstacleContacts.UnionWith(contacts.Keys);
        return false;
    }

    private void SpawnImpactFlash(Vector3 position)
    {
        var flash = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 1.4f, Height = 2.8f, RadialSegments = 12, Rings = 6 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1.0f, 1.0f, 1.0f, 0.5f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        GetTree().CurrentScene.AddChild(flash);
        flash.GlobalPosition = position;
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(flash, "scale", Vector3.Zero, 0.18);
        tween.TweenCallback(Callable.From(flash.QueueFree));
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

        Health = Mathf.Max(0.0f, Health - rawDamage);

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
        IsAlive = true;
        _obstacleContacts.Clear();
    }
}
