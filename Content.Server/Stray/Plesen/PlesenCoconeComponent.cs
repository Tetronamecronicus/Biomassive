using Robust.Shared.GameStates;//
using Content.Shared.Stray.Plesen;//
namespace Content.Server.Stray.Plesen
{
    //[Serializable]
    [RegisterComponent]
    [AutoGenerateComponentState(true)]
    [Access(typeof(SharedPlesenCoconeSystem))]
    public sealed partial class PlesenCoconeComponent : Component
    {
        [DataField]
        public float Health = PlesenConfig.CoconeHealth;
        [DataField]
        public List<EntityUid> AttachedCores = new();

        [DataField]
        [AutoNetworkedField]
        public bool Growth;
    }
}
