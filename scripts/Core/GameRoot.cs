using Godot;
using System.Collections.Generic;

public partial class GameRoot : Node3D
{
    private const float GroundPlaneSizeMeters = 900.0f;
    private const float GroundY = 0.0f;
    private const float ArenaHalfExtent = GroundPlaneSizeMeters * 0.5f;
    private const float WallHeightMeters = 24.0f;
    private const float WallThicknessMeters = 5.0f;
    private const float PlayerSpawnHeight = 1.8f;
    private const float DummySpawnRadius = 120.0f;
    private const float MatchLengthSeconds = 300.0f;
    private const float AIDummyAttackRange = 14.0f;
    private const float AIDummyDps = 9.0f;
    private const int RockCount = 260;

    private Node3D _world = default!;
    private PlayerHoverBikeController _player = default!;
    private CombatLoadoutController _combat = default!;
    private readonly List<AIDummyController> _dummies = new();
    private float _matchTimeRemaining = MatchLengthSeconds;
    private int _kills;
    private int _deaths;

    public float MatchTimeRemaining => _matchTimeRemaining;
    public int Kills => _kills;
    public int Deaths => _deaths;

    public override void _Ready()
    {
        EnsureInputActions();

        _world = GetNode<Node3D>("World");
        _player = GetNode<PlayerHoverBikeController>("World/Player1");
        _combat = GetNode<CombatLoadoutController>("World/Player1/Combat");

        _combat.PrimaryFired += OnPrimaryFired;
        _combat.MissileFired += OnMissileFired;
        _player.PlayerDestroyed += OnPlayerDestroyed;

        RemoveLegacySphereNodes();
        BuildGroundPlane();
        BuildArenaWall();

        SpawnGroundRocks();

        SpawnAIDummies();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _matchTimeRemaining = Mathf.Max(0.0f, _matchTimeRemaining - dt);

        if (_player.IsAlive)
        {
            ApplyAIDummyPressure(dt);
        }
    }

    private void BuildGroundPlane()
    {
        if (_world.GetNodeOrNull<MeshInstance3D>("GroundPlane") != null)
        {
            return;
        }

        var mesh = new PlaneMesh
        {
            Size = new Vector2(GroundPlaneSizeMeters, GroundPlaneSizeMeters)
        };

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.27f, 0.55f, 0.23f),
            Roughness = 0.95f,
            CullMode = BaseMaterial3D.CullModeEnum.Back
        };

        var ground = new MeshInstance3D
        {
            Name = "GroundPlane",
            Mesh = mesh,
            MaterialOverride = material
        };
        ground.Position = new Vector3(0.0f, GroundY, 0.0f);

        _world.AddChild(ground);
    }

    private void BuildArenaWall()
    {
        if (_world.GetNodeOrNull<Node3D>("ArenaWall") != null)
        {
            return;
        }

        var wallRoot = new Node3D
        {
            Name = "ArenaWall"
        };
        _world.AddChild(wallRoot);

        var wallMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.74f, 0.76f, 0.80f),
            Roughness = 0.92f
        };

        float centerY = GroundY + (WallHeightMeters * 0.5f);
        float wallOffset = ArenaHalfExtent + (WallThicknessMeters * 0.5f);

        AddWallSegment(
            wallRoot,
            wallMaterial,
            "NorthWall",
            new Vector3(GroundPlaneSizeMeters, WallHeightMeters, WallThicknessMeters),
            new Vector3(0.0f, centerY, -wallOffset));

        AddWallSegment(
            wallRoot,
            wallMaterial,
            "SouthWall",
            new Vector3(GroundPlaneSizeMeters, WallHeightMeters, WallThicknessMeters),
            new Vector3(0.0f, centerY, wallOffset));

        AddWallSegment(
            wallRoot,
            wallMaterial,
            "WestWall",
            new Vector3(WallThicknessMeters, WallHeightMeters, GroundPlaneSizeMeters),
            new Vector3(-wallOffset, centerY, 0.0f));

        AddWallSegment(
            wallRoot,
            wallMaterial,
            "EastWall",
            new Vector3(WallThicknessMeters, WallHeightMeters, GroundPlaneSizeMeters),
            new Vector3(wallOffset, centerY, 0.0f));
    }

    private static void AddWallSegment(Node3D parent, StandardMaterial3D material, string name, Vector3 size, Vector3 position)
    {
        var wallMesh = new BoxMesh
        {
            Size = size
        };

        var wall = new MeshInstance3D
        {
            Name = name,
            Mesh = wallMesh,
            MaterialOverride = material,
            Position = position
        };

        parent.AddChild(wall);
    }

    private void RemoveLegacySphereNodes()
    {
        _world.GetNodeOrNull<Node3D>("InnerSphere")?.QueueFree();
        _world.GetNodeOrNull<Node3D>("OuterSphere")?.QueueFree();
    }

    private void SpawnAIDummies()
    {
        for (int i = 0; i < 3; i++)
        {
            string dummyName = $"AIDummy{i + 1}";
            if (_world.GetNodeOrNull<AIDummyController>(dummyName) != null)
            {
                continue;
            }

            var dummy = new AIDummyController
            {
                Name = dummyName
            };

            float angle = Mathf.DegToRad(i * 120.0f);
            dummy.Position = new Vector3(Mathf.Sin(angle), 0.0f, Mathf.Cos(angle)) * DummySpawnRadius;
            dummy.Position = new Vector3(dummy.Position.X, PlayerSpawnHeight, dummy.Position.Z);
            _world.AddChild(dummy);

            dummy.Destroyed += OnAIDummyDestroyed;
            _dummies.Add(dummy);

            var meshInstance = new MeshInstance3D
            {
                Name = "Body",
                Mesh = new SphereMesh { Radius = 0.8f, Height = 1.6f, RadialSegments = 18, Rings = 12 },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1.0f, 0.35f, 0.25f) }
            };
            dummy.AddChild(meshInstance);
        }
    }

    private void SpawnGroundRocks()
    {
        if (_world.GetNodeOrNull<Node3D>("GroundRocks") != null)
        {
            return;
        }

        var rocksRoot = new Node3D
        {
            Name = "GroundRocks"
        };
        _world.AddChild(rocksRoot);

        float halfExtent = GroundPlaneSizeMeters * 0.5f - 14.0f;

        var rng = new RandomNumberGenerator();
        rng.Seed = 1337;

        for (int i = 0; i < RockCount; i++)
        {
            float baseScale = rng.RandfRange(0.18f, 0.9f);
            var rockMesh = new SphereMesh
            {
                Radius = 6f,
                Height = 12f,
                RadialSegments = 10,
                Rings = 8
            };

            var rockMaterial = new StandardMaterial3D
            {
                AlbedoColor = new Color(
                    rng.RandfRange(0.42f, 0.58f),
                    rng.RandfRange(0.42f, 0.58f),
                    rng.RandfRange(0.42f, 0.58f)),
                Roughness = 0.95f
            };

            var rock = new MeshInstance3D
            {
                Name = $"Rock{i + 1}",
                Mesh = rockMesh,
                MaterialOverride = rockMaterial
            };

            Basis basis = Basis.Identity.Rotated(Vector3.Up, rng.RandfRange(0.0f, Mathf.Tau));
            Vector3 scale = new Vector3(baseScale, baseScale * rng.RandfRange(0.65f, 1.4f), baseScale);
            float x = rng.RandfRange(-halfExtent, halfExtent);
            float z = rng.RandfRange(-halfExtent, halfExtent);
            float y = GroundY + (0.24f * scale.Y);

            rock.Transform = new Transform3D(
                basis.Scaled(scale),
                new Vector3(x, y, z));

            rocksRoot.AddChild(rock);
        }
    }

    private void ApplyAIDummyPressure(float dt)
    {
        float damageThisFrame = 0.0f;

        foreach (AIDummyController dummy in _dummies)
        {
            if (!IsInstanceValid(dummy) || !dummy.IsAlive)
            {
                continue;
            }

            float distance = dummy.GlobalPosition.DistanceTo(_player.GlobalPosition);
            if (distance <= AIDummyAttackRange)
            {
                damageThisFrame += AIDummyDps * dt;
            }
        }

        if (damageThisFrame > 0.0f)
        {
            _player.ApplyDamage(damageThisFrame);
        }
    }

    private void OnPrimaryFired(int remainingInMagazine, float damage, float effectiveRange)
    {
        AIDummyController? target = FindBestTarget(effectiveRange, 0.9659f);
        target?.ApplyDamage(damage);
    }

    private void OnMissileFired(int remainingMissiles, float damage, float blastRadius)
    {
        AIDummyController? centerTarget = FindBestTarget(220.0f, 0.90f);
        if (centerTarget == null)
        {
            return;
        }

        Vector3 center = centerTarget.GlobalPosition;
        foreach (AIDummyController dummy in _dummies)
        {
            if (!IsInstanceValid(dummy) || !dummy.IsAlive)
            {
                continue;
            }

            float distance = dummy.GlobalPosition.DistanceTo(center);
            if (distance <= blastRadius)
            {
                float falloff = 1.0f - Mathf.Clamp(distance / Mathf.Max(0.01f, blastRadius), 0.0f, 1.0f);
                dummy.ApplyDamage(damage * (0.3f + 0.7f * falloff));
            }
        }
    }

    private AIDummyController? FindBestTarget(float maxRange, float minDot)
    {
        Vector3 origin = _player.ViewOrigin;
        Vector3 viewForward = _player.ViewForward;

        AIDummyController? best = null;
        float bestDistance = float.MaxValue;

        foreach (AIDummyController dummy in _dummies)
        {
            if (!IsInstanceValid(dummy) || !dummy.IsAlive)
            {
                continue;
            }

            Vector3 toTarget = dummy.GlobalPosition - origin;
            float distance = toTarget.Length();
            if (distance > maxRange || distance < 0.001f)
            {
                continue;
            }

            float alignment = toTarget.Normalized().Dot(viewForward);
            if (alignment < minDot)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = dummy;
            }
        }

        return best;
    }

    private void OnAIDummyDestroyed(AIDummyController dummy)
    {
        _kills += 1;
        RespawnDummy(dummy);
    }

    private void RespawnDummy(AIDummyController dummy)
    {
        if (!IsInstanceValid(dummy))
        {
            return;
        }

        float angle = GD.Randf() * Mathf.Tau;
        float radius = (float)GD.RandRange(25.0f, DummySpawnRadius);
        dummy.GlobalPosition = new Vector3(Mathf.Sin(angle), PlayerSpawnHeight, Mathf.Cos(angle)) * radius;
        dummy.Revive();
    }

    private void OnPlayerDestroyed()
    {
        _deaths += 1;
        _player.RespawnAt(new Vector3(0.0f, PlayerSpawnHeight, 0.0f));
    }

    private void EnsureInputActions()
    {
        EnsureActionWithPhysicalKey("accelerate", Key.W);
        EnsureActionWithPhysicalKey("turn_left", Key.A);
        EnsureActionWithPhysicalKey("turn_right", Key.D);
        EnsureActionWithPhysicalKey("boost", Key.Shift);
        EnsureActionWithPhysicalKey("drift", Key.Ctrl);
        EnsureActionWithPhysicalKey("fire_missile", Key.Space);
        EnsureActionWithPhysicalKey("fire_primary", Key.Q);
        EnsureActionWithMouseButton("fire_primary", MouseButton.Left);
    }

    private static void EnsureActionWithPhysicalKey(string action, Key key)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        foreach (InputEvent evt in InputMap.ActionGetEvents(action))
        {
            if (evt is InputEventKey keyEvent && keyEvent.PhysicalKeycode == key)
            {
                return;
            }
        }

        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    private static void EnsureActionWithMouseButton(string action, MouseButton button)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        foreach (InputEvent evt in InputMap.ActionGetEvents(action))
        {
            if (evt is InputEventMouseButton buttonEvent && buttonEvent.ButtonIndex == button)
            {
                return;
            }
        }

        InputMap.ActionAddEvent(action, new InputEventMouseButton { ButtonIndex = button });
    }
}
