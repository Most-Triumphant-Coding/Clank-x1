using Godot;

public partial class LaserShot : CharacterBody3D
{
    private const float ImpactDamage = 25.0f;
    private const int MaxImpactsPerFrame = 8;

    [Export]
    public float SpeedUnitsPerSecond { get; set; } = 260.0f;

    [Export]
    public float LifetimeSeconds { get; set; } = 1.6f;

    public Vector3 Direction { get; set; } = -Vector3.Forward;

    private MeshInstance3D _mesh = default!;
    private float _timeRemaining;

    public override void _Ready()
    {
        _timeRemaining = LifetimeSeconds;
        CollisionLayer = 0;
        CollisionMask = 3;

        AddChild(new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 1.0f }
        });

        _mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh
            {
                Radius = 1.0f,
                Height = 2.0f,
                RadialSegments = 24,
                Rings = 12
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.18f, 0.1f, 0.5f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.2f, 0.12f),
            EmissionEnergyMultiplier = 3.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        _mesh.MaterialOverride = material;
        AddChild(_mesh);

        var core = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.5f, Height = 1.0f, RadialSegments = 24, Rings = 12 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = Colors.Yellow,
                EmissionEnabled = true,
                Emission = Colors.Yellow,
                EmissionEnergyMultiplier = 3.0f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(core);

        Direction = new Vector3(Direction.X, 0.0f, Direction.Z).Normalized();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        float travelTime = Mathf.Min(dt, _timeRemaining);
        Vector3 movement = Direction * SpeedUnitsPerSecond * travelTime;

        for (int impact = 0; impact < MaxImpactsPerFrame && movement.LengthSquared() > 0.000001f; impact++)
        {
            KinematicCollision3D collision = MoveAndCollide(movement);
            if (collision == null)
            {
                break;
            }

            GodotObject collider = collision.GetCollider();
            if (collider is PlayerHoverBikeController player)
            {
                player.ApplyDamage(ImpactDamage);
                QueueFree();
                return;
            }

            if (collider is AIDummyController enemy)
            {
                enemy.ApplyDamage(ImpactDamage);
                QueueFree();
                return;
            }

            if (collider is not StaticBody3D || GD.Randf() < 0.2f)
            {
                QueueFree();
                return;
            }

            Vector3 normal = collision.GetNormal();
            Vector3 horizontalNormal = new Vector3(normal.X, 0.0f, normal.Z);
            Direction = horizontalNormal.IsZeroApprox()
                ? -Direction
                : Direction.Bounce(horizontalNormal.Normalized()).Normalized();
            movement = Direction * collision.GetRemainder().Length();
        }

        _timeRemaining -= dt;
        if (_timeRemaining <= 0.0f)
        {
            QueueFree();
        }
    }
}
