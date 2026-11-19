using Godot;

public partial class LaserShot : Node3D
{
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
            AlbedoColor = new Color(1.0f, 0.18f, 0.1f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.2f, 0.12f),
            EmissionEnergyMultiplier = 3.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        _mesh.MaterialOverride = material;
        AddChild(_mesh);

        Direction = Direction.Normalized();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        GlobalPosition += Direction * SpeedUnitsPerSecond * dt;

        _timeRemaining -= dt;
        if (_timeRemaining <= 0.0f)
        {
            QueueFree();
        }
    }
}
