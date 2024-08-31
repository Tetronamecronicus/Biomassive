using Robust.Shared.GameStates;//
using Content.Shared.Stray.Plesen;//
namespace Content.Server.Stray.Plesen
{
    //[Serializable]
    [RegisterComponent]
    [AutoGenerateComponentState(true)]
    [Access(typeof(SharedPlesenWallSystem))]
    public sealed partial class PlesenWallComponent : Component
    {
        [DataField]
        public float Health = PlesenConfig.WallHealth;
        [DataField]
        public List<EntityUid> AttachedCores = new();

        [DataField]
        [AutoNetworkedField]
        public bool Growth;
    }
}
