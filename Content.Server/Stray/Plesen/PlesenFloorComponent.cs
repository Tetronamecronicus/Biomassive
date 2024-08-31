using Robust.Shared.GameStates;//
using Content.Shared.Stray.Plesen;//
namespace Content.Server.Stray.Plesen
{
    //[Serializable]
    [RegisterComponent]
    [AutoGenerateComponentState(true)]
    [Access(typeof(SharedPlesenFloorSystem))]
    public sealed partial class PlesenFloorComponent : Component
    {
        [DataField]
        public float Health = PlesenConfig.FloorHealth;
        [DataField]
        public List<EntityUid> AttachedCores = new();

        [DataField]
        [AutoNetworkedField]
        public bool Growth;
    }
}
