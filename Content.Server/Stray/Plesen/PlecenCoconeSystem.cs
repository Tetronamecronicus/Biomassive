using Content.Shared.Stray.Plesen;
using Content.Shared.Damage;
using JetBrains.Annotations;

namespace Content.Server.Stray.Plesen;

[UsedImplicitly]
public sealed class PlesenCoconeSystem : SharedPlesenCoconeSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlesenCoconeComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, PlesenCoconeComponent component, DamageChangedEvent args)
    {
        var totalHealth = PlesenConfig.CoconeHealth - (int)args.Damageable.TotalDamage;
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
