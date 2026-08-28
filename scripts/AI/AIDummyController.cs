using Godot;

public partial class AIDummyController : Node3D
{
    [Signal]
    public delegate void DestroyedEventHandler(AIDummyController dummy);

    [Export]
    public float OrbitSpeedDegreesPerSecond { get; set; } = 12.0f;

    [Export]
    public float HoverHeightMeters { get; set; } = 1.8f;

    [Export]
    public float OrbitRadiusMeters { get; set; } = 120.0f;

    [Export]
    public float MaxHealth { get; set; } = 100.0f;

    public float Health { get; private set; } = 100.0f;
    public bool IsAlive { get; private set; } = true;
    private float _orbitAngle;

    public override void _Ready()
    {
        Health = MaxHealth;

        OrbitRadiusMeters = new Vector2(GlobalPosition.X, GlobalPosition.Z).Length();
        if (OrbitRadiusMeters < 1.0f)
        {
            OrbitRadiusMeters = 120.0f;
        }

        _orbitAngle = Mathf.Atan2(GlobalPosition.Z, GlobalPosition.X);
        GlobalPosition = new Vector3(GlobalPosition.X, HoverHeightMeters, GlobalPosition.Z);
    }

    public override void _Process(double delta)
    {
        if (!IsAlive)
        {
            return;
        }

        float dt = (float)delta;
        _orbitAngle += Mathf.DegToRad(OrbitSpeedDegreesPerSecond) * dt;
        GlobalPosition = new Vector3(
            Mathf.Cos(_orbitAngle) * OrbitRadiusMeters,
            HoverHeightMeters,
            Mathf.Sin(_orbitAngle) * OrbitRadiusMeters);

        LookAt(new Vector3(0.0f, HoverHeightMeters, 0.0f), Vector3.Up);
    }

    public void ApplyDamage(float amount)
    {
        if (!IsAlive || amount <= 0.0f)
        {
            return;
        }

        Health = Mathf.Max(0.0f, Health - amount);
        if (Health <= 0.0f)
        {
            IsAlive = false;
            Visible = false;
            EmitSignal(SignalName.Destroyed, this);
        }
    }

    public void Revive()
    {
        Health = MaxHealth;
        IsAlive = true;
        Visible = true;
        OrbitRadiusMeters = new Vector2(GlobalPosition.X, GlobalPosition.Z).Length();
        _orbitAngle = Mathf.Atan2(GlobalPosition.Z, GlobalPosition.X);
        GlobalPosition = new Vector3(GlobalPosition.X, HoverHeightMeters, GlobalPosition.Z);
    }
}
