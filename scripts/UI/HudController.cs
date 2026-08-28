using Godot;

public partial class HudController : CanvasLayer
{
    private Label _debugLabel = default!;
    private GameRoot? _gameRoot;
    private PlayerHoverBikeController? _player;
    private CombatLoadoutController? _combat;

    public override void _Ready()
    {
        _debugLabel = GetNode<Label>("DebugLabel");
        _gameRoot = GetNodeOrNull<GameRoot>("..");
        _player = GetNodeOrNull<PlayerHoverBikeController>("../World/Player1");
        _combat = _player?.GetNodeOrNull<CombatLoadoutController>("Combat");
    }

    public override void _Process(double delta)
    {
        if (_player == null || _combat == null)
        {
            _debugLabel.Text = "Waiting for player systems...";
            return;
        }

        int minutes = _gameRoot != null ? Mathf.FloorToInt(_gameRoot.MatchTimeRemaining / 60.0f) : 0;
        int seconds = _gameRoot != null ? Mathf.FloorToInt(_gameRoot.MatchTimeRemaining % 60.0f) : 0;
        int kills = _gameRoot?.Kills ?? 0;
        int deaths = _gameRoot?.Deaths ?? 0;

        _debugLabel.Text =
            "Clank-x1 MVP Prototype\n"
            + $"Match Time: {minutes:00}:{seconds:00}   Kills: {kills}   Deaths: {deaths}\n"
            + $"Speed: {_player.CurrentSpeed:0.0} m/s\n"
            + $"Health: {_player.Health:0}   Armor: {_player.Armor:0}   Alive: {_player.IsAlive}\n"
            + $"Primary Ammo: {_combat.PrimaryAmmo}   Missiles: {_combat.MissileAmmo}\n"
            + "Controls: Auto-forward at 10 units/s, hold S to slow by 5 units/s each second, A rotate left at 5 deg/s, D rotate right at 5 deg/s, LMB fire, Space missile";
    }
}
