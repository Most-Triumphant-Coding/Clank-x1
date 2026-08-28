using Godot;

public partial class CombatLoadoutController : Node
{
    [Signal]
    public delegate void PrimaryFiredEventHandler(int remainingInMagazine, float damage, float effectiveRange);

    [Signal]
    public delegate void MissileFiredEventHandler(int remainingMissiles, float damage, float blastRadius);

    private GunConfig _primary = new();
    private MissileConfig _missile = new();
    private int _primaryAmmo;
    private int _missileAmmo;
    private double _primaryCooldownRemaining;
    private double _missileCooldownRemaining;

    public int PrimaryAmmo => _primaryAmmo;
    public int MissileAmmo => _missileAmmo;

    public override void _Process(double delta)
    {
        _primaryCooldownRemaining = Mathf.Max(0.0, _primaryCooldownRemaining - delta);
        _missileCooldownRemaining = Mathf.Max(0.0, _missileCooldownRemaining - delta);
    }

    public void Configure(BikeLoadout loadout)
    {
        _primary = loadout.PrimaryGun;
        _missile = loadout.Missile;
        _primaryAmmo = _primary.MagazineSize;
        _missileAmmo = _missile.Ammo;
    }

    public bool TryFirePrimary()
    {
        if (_primaryAmmo <= 0 || _primaryCooldownRemaining > 0.0)
        {
            return false;
        }

        _primaryAmmo -= 1;
        _primaryCooldownRemaining = 1.0 / Mathf.Max(0.01f, _primary.FireRatePerSecond);
        EmitSignal(SignalName.PrimaryFired, _primaryAmmo, _primary.Damage, _primary.EffectiveRangeMeters);
        return true;
    }

    public bool TryFireMissile()
    {
        if (_missileAmmo <= 0 || _missileCooldownRemaining > 0.0)
        {
            return false;
        }

        _missileAmmo -= 1;
        _missileCooldownRemaining = _missile.CooldownSeconds;
        EmitSignal(SignalName.MissileFired, _missileAmmo, _missile.Damage, _missile.BlastRadius);
        return true;
    }
}
