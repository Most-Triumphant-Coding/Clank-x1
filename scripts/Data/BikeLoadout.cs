public sealed class BikeLoadout
{
    public ArmorPlate FrontArmor { get; init; } = new();
    public ArmorPlate LeftArmor { get; init; } = new();
    public ArmorPlate RightArmor { get; init; } = new();
    public ArmorPlate RearArmor { get; init; } = new();
    public GunConfig PrimaryGun { get; init; } = new();
    public GunConfig SecondaryGun { get; init; } = new();
    public MissileConfig Missile { get; init; } = new();

    public static BikeLoadout CreateStarter()
    {
        return new BikeLoadout
        {
            FrontArmor = new ArmorPlate { Durability = 80.0f, Weight = 20.0f, Resistance = 1.1f },
            LeftArmor = new ArmorPlate { Durability = 70.0f, Weight = 15.0f, Resistance = 1.0f },
            RightArmor = new ArmorPlate { Durability = 70.0f, Weight = 15.0f, Resistance = 1.0f },
            RearArmor = new ArmorPlate { Durability = 60.0f, Weight = 12.0f, Resistance = 0.95f },
            PrimaryGun = new GunConfig
            {
                Name = "Ripper Auto",
                Damage = 8.0f,
                FireRatePerSecond = 8.0f,
                MagazineSize = 40,
                ReloadSeconds = 1.6f,
                EffectiveRangeMeters = 140.0f
            },
            SecondaryGun = new GunConfig
            {
                Name = "Bolt Cannon",
                Damage = 40.0f,
                FireRatePerSecond = 1.2f,
                MagazineSize = 6,
                ReloadSeconds = 2.0f,
                EffectiveRangeMeters = 180.0f
            },
            Missile = new MissileConfig
            {
                Name = "Homing Sting",
                Damage = 70.0f,
                BlastRadius = 5.0f,
                CooldownSeconds = 2.5f,
                Ammo = 12
            }
        };
    }
}

public sealed class ArmorPlate
{
    public float Durability { get; init; } = 50.0f;
    public float Weight { get; init; } = 10.0f;
    public float Resistance { get; init; } = 1.0f;
}

public sealed class GunConfig
{
    public string Name { get; init; } = "Gun";
    public float Damage { get; init; } = 10.0f;
    public float FireRatePerSecond { get; init; } = 5.0f;
    public int MagazineSize { get; init; } = 30;
    public float ReloadSeconds { get; init; } = 1.5f;
    public float EffectiveRangeMeters { get; init; } = 120.0f;
}

public sealed class MissileConfig
{
    public string Name { get; init; } = "Missile";
    public float Damage { get; init; } = 50.0f;
    public float BlastRadius { get; init; } = 4.0f;
    public float CooldownSeconds { get; init; } = 2.0f;
    public int Ammo { get; init; } = 8;
}
