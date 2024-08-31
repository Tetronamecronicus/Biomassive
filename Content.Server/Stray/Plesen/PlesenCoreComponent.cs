using Robust.Shared.GameStates;
using Content.Shared.Stray.Plesen;
namespace Content.Server.Stray.Plesen
{
    //[Serializable]
    [RegisterComponent]
    [AutoGenerateComponentState(true)]
    [Access(typeof(SharedPlesenCoreSystem), typeof(SharedPlesenWallSystem), typeof(SharedPlesenFloorSystem), typeof(SharedPlesenCoconeSystem))]
    public sealed partial class PlesenCoreComponent : Component
    {
        [DataField]
        public float Health = PlesenConfig.CoreHealth;
        [DataField]
        public List<EntityUid> AttachedCores = new();
        [DataField]
        public float Agro { get; set; }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan NextUpdateTime = TimeSpan.Zero;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float NextUpdateAfter = 2;

        [DataField]
        [AutoNetworkedField]
        public bool Growth;
    }
}
