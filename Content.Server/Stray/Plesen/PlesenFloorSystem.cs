using Content.Shared.Stray.Plesen;
using Content.Shared.Damage;
using JetBrains.Annotations;

namespace Content.Server.Stray.Plesen;

[UsedImplicitly]
public sealed class PlesenFloorSystem : SharedPlesenFloorSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlesenFloorComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, PlesenFloorComponent component, DamageChangedEvent args)
    {
        var totalHealth = PlesenConfig.FloorHealth - (int)args.Damageable.TotalDamage;
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
