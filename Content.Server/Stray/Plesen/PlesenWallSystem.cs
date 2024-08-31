using Content.Shared.Stray.Plesen;
using Content.Shared.Damage;
using JetBrains.Annotations;

namespace Content.Server.Stray.Plesen;

[UsedImplicitly]
public sealed class PlesenWallSystem : SharedPlesenWallSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlesenWallComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, PlesenWallComponent component, DamageChangedEvent args)
    {
        var totalHealth = PlesenConfig.WallHealth - (int)args.Damageable.TotalDamage;
        var difference = MathF.Abs(component.Health - totalHealth);
        component.Health = totalHealth;
        foreach (EntityUid coreUid in component.AttachedCores)
        {
            if (!TryComp(coreUid, out PlesenCoreComponent? coreCom))
                continue;
            coreCom.Agro += difference * PlesenConfig.CoreDamageAgroMultiplyer;
        }
    }
}
