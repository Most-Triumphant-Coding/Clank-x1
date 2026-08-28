using Godot;

public partial class WorldController : Node3D
{
    private const string TurnLeftAction = "turn_left";
    private const string TurnRightAction = "turn_right";

    private const float OuterSphereDiameterMeters = 800.0f;
    private const float InnerSphereDiameterMeters = 200.0f;
    private const float HeadDiameterMeters = 0.2f;
    private const float HoverHeightMeters = 5.0f;
    private const float HeadTurnSpeedDegrees = 5.0f;
    private const float HeadViewPitchDegrees = -20.0f;

    private Node3D _player1HeadPivot;
    private MeshInstance3D _player1Head;
    private Camera3D _player1Camera;
    private float _player1ViewYawDegrees;

    public override void _Ready()
    {
        EnsureTurnInputActions();
        CreateOuterSphere();
        CreateInnerSphere();
        CreatePlayer1Head();
        AttachPlayer1Camera();
        ApplyHeadFacing();
    }

    private void CreateOuterSphere()
    {
        if (GetNodeOrNull<MeshInstance3D>("OuterSphere") != null)
        {
            return;
        }

        float outerRadius = OuterSphereDiameterMeters * 0.5f;

        var outerSphereMesh = new SphereMesh
        {
            Radius = outerRadius,
            Height = OuterSphereDiameterMeters,
            RadialSegments = 64,
            Rings = 32
        };

        var outerMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.85f, 0.85f, 0.85f),
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };

        var outerSphere = new MeshInstance3D
        {
            Name = "OuterSphere",
            Mesh = outerSphereMesh,
            MaterialOverride = outerMaterial
        };

        AddChild(outerSphere);
    }

    public override void _Process(double delta)
    {
        float turnInput = Input.GetActionStrength(TurnLeftAction) - Input.GetActionStrength(TurnRightAction);

        if (Mathf.IsZeroApprox(turnInput) || _player1HeadPivot == null)
        {
            return;
        }

        _player1ViewYawDegrees += HeadTurnSpeedDegrees * (float)delta * turnInput;
        ApplyHeadFacing();
    }

    private void EnsureTurnInputActions()
    {
        if (!InputMap.HasAction(TurnLeftAction))
        {
            InputMap.AddAction(TurnLeftAction);
        }

        if (!InputMap.HasAction(TurnRightAction))
        {
            InputMap.AddAction(TurnRightAction);
        }

        AddPhysicalKeyBindingIfMissing(TurnLeftAction, Key.A);
        AddPhysicalKeyBindingIfMissing(TurnRightAction, Key.D);
    }

    private static void AddPhysicalKeyBindingIfMissing(string actionName, Key physicalKey)
    {
        foreach (InputEvent actionEvent in InputMap.ActionGetEvents(actionName))
        {
            if (actionEvent is InputEventKey existingKey && existingKey.PhysicalKeycode == physicalKey)
            {
                return;
            }
        }

        var keyEvent = new InputEventKey
        {
            PhysicalKeycode = physicalKey,
            Pressed = false
        };
        InputMap.ActionAddEvent(actionName, keyEvent);
    }

    private void CreateInnerSphere()
    {
        if (GetNodeOrNull<MeshInstance3D>("InnerSphere") != null)
        {
            return;
        }

        float innerRadius = InnerSphereDiameterMeters * 0.5f;

        var innerSphereMesh = new SphereMesh
        {
            Radius = innerRadius,
            Height = InnerSphereDiameterMeters,
            RadialSegments = 64,
            Rings = 32
        };

        var tanMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("d2b48c")
        };

        var innerSphere = new MeshInstance3D
        {
            Name = "InnerSphere",
            Mesh = innerSphereMesh,
            MaterialOverride = tanMaterial
        };

        AddChild(innerSphere);
    }

    private void CreatePlayer1Head()
    {
        _player1HeadPivot = GetNodeOrNull<Node3D>("Player1HeadPivot");
        if (_player1HeadPivot != null)
        {
            _player1Head = _player1HeadPivot.GetNodeOrNull<MeshInstance3D>("Player1Head");
            return;
        }

        float innerRadius = InnerSphereDiameterMeters * 0.5f;
        float headRadius = HeadDiameterMeters * 0.5f;

        _player1HeadPivot = new Node3D
        {
            Name = "Player1HeadPivot"
        };
        AddChild(_player1HeadPivot);

        var headMesh = new SphereMesh
        {
            Radius = headRadius,
            Height = HeadDiameterMeters,
            RadialSegments = 24,
            Rings = 12
        };

        var headMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.9f, 0.9f, 0.95f)
        };

        var player1Head = new MeshInstance3D
        {
            Name = "Player1Head",
            Mesh = headMesh,
            MaterialOverride = headMaterial,
            Position = new Vector3(0.0f, 0.0f, innerRadius + HoverHeightMeters + headRadius)
        };

        _player1Head = player1Head;
        _player1HeadPivot.AddChild(player1Head);
    }

    private void AttachPlayer1Camera()
    {
        if (_player1HeadPivot == null)
        {
            _player1HeadPivot = GetNodeOrNull<Node3D>("Player1HeadPivot");
            if (_player1HeadPivot == null)
            {
                return;
            }
        }

        _player1Camera = GetNodeOrNull<Camera3D>("Camera3D") ?? FindChild("Camera3D", true, false) as Camera3D;
        if (_player1Camera == null)
        {
            return;
        }

        float innerRadius = InnerSphereDiameterMeters * 0.5f;
        float headRadius = HeadDiameterMeters * 0.5f;

        (_player1Camera.GetParent() as Node)?.RemoveChild(_player1Camera);
        _player1HeadPivot.AddChild(_player1Camera);

        // Place camera at the head center. Orientation is handled by ApplyHeadFacing.
        _player1Camera.Position = new Vector3(0.0f, 0.0f, innerRadius + HoverHeightMeters + headRadius);
        _player1Camera.Current = true;
    }

    private void ApplyHeadFacing()
    {
        if (_player1HeadPivot != null)
        {
            _player1HeadPivot.RotationDegrees = new Vector3(0.0f, _player1ViewYawDegrees, 0.0f);
        }

        _player1Head?.LookAt(_player1Head.GlobalPosition + _player1HeadPivot.Basis.Z, Vector3.Up);

        if (_player1Camera != null)
        {
            _player1Camera.RotationDegrees = new Vector3(HeadViewPitchDegrees, 0.0f, 0.0f);
        }
    }
}
