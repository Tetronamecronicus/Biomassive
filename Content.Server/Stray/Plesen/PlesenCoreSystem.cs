using Content.Shared.Stray.Plesen;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using JetBrains.Annotations;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Server.Atmos.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Tag;
using Content.Server.Spreader;
using Content.Shared.Spreader;
using Robust.Shared.Collections;
using Content.Server.Stray.Utilitys.LogMessageSender;

namespace Content.Server.Stray.Plesen;

[UsedImplicitly]
public sealed class PlesenCoreSystem : SharedPlesenCoreSystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;



    private readonly Dictionary<EntityUid, Dictionary<string, int>> _gridUpdates = [];
    private Dictionary<string, int> _prototypeUpdates = default!;

    private static readonly ProtoId<TagPrototype> PlesenTag = "Plesen";
    private static readonly ProtoId<TagPrototype> IgnoredTag = "Plesen";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlesenCoreComponent, DamageChangedEvent>(OnDamageChanged);

        SetupPrototypes();
    }

    private void OnDamageChanged(EntityUid uid, PlesenCoreComponent component, DamageChangedEvent args)
    {
        var totalHealth = PlesenConfig.CoreHealth - (int)args.Damageable.TotalDamage;
        var difference = MathF.Abs(component.Health - totalHealth);
        component.Health = totalHealth;
        foreach (EntityUid coreUid in component.AttachedCores)
        {
            if (!TryComp(coreUid, out PlesenCoreComponent? coreCom))
                continue;
            coreCom.Agro += difference * PlesenConfig.CoreDamageAgroMultiplyer;
        }
    }

    private void SetupPrototypes()
    {
        _prototypeUpdates = [];
        foreach (var proto in _prototype.EnumeratePrototypes<EdgeSpreaderPrototype>())
        {
            _prototypeUpdates.Add(proto.ID, proto.UpdatesPerSecond);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var anomalyQuery = EntityQueryEnumerator<PlesenCoreComponent>();
        while (anomalyQuery.MoveNext(out var uid, out var plesen))
        {
            if (Timing.CurTime > plesen.NextUpdateTime)
            {
                _entManager.System<LogMessageSenderSystem>().LogDebug("UpdateStart");

                if (!TryComp(uid, out TransformComponent? xform))
                {
                    RemComp(uid, plesen);
                    continue;
                }

                _entManager.System<LogMessageSenderSystem>().LogDebug("UpdateTransformGet");

                if (!TryComp(uid, out EdgeSpreaderComponent? spreader))
                {
                    RemComp(uid, plesen);
                    continue;
                }

                _entManager.System<LogMessageSenderSystem>().LogDebug("UpdateEdgeSpreaderGet");

                if (xform.GridUid == null)
                {
                    RemComp(uid, plesen);
                    continue;
                }

                _entManager.System<LogMessageSenderSystem>().LogDebug("UpdateGridCheckPass");

                if (!_gridUpdates.TryGetValue(xform.GridUid.Value, out var groupUpdates))
                    continue;

                _entManager.System<LogMessageSenderSystem>().LogDebug("Initialize Grow");

                UpdatePlesen(uid, xform, plesen, spreader.Id);
            }
        }
    }

    public void UpdatePlesen(EntityUid uid, TransformComponent xform, PlesenCoreComponent? core, ProtoId<EdgeSpreaderPrototype> prototype)
    {
        if (!Resolve(uid, ref core))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        core.NextUpdateTime += TimeSpan.FromSeconds(core.NextUpdateAfter);

        object[]? nearby = GetNeighbors(uid, xform, prototype);

        if (nearby == null)
            return;

        foreach (object[]? neighbor in nearby)
        {
            if (neighbor == null)
                continue;

            TileRef tileRef = (TileRef)neighbor[0];
            MapGridComponent gridCom = (MapGridComponent)neighbor[0];

            Spawn(prototype, _map.GridTileToLocal(tileRef.GridUid, gridCom, tileRef.GridIndices));
        }
    }
    //   { freeTiles, neighbors, occupiedTiles }
    public object[]? GetNeighbors(EntityUid uid, TransformComponent comp, ProtoId<EdgeSpreaderPrototype> prototype)
    {
        List<(MapGridComponent, TileRef)> freeTiles = [];
        List<Vector2i> occupiedTiles = [];
        List<EntityUid> neighbors = [];

        if (!_prototype.TryIndex(prototype, out var spreaderPrototype))
            return null;

        if (!TryComp<MapGridComponent>(comp.GridUid, out var grid))
            return null;

        var tile = _map.TileIndicesFor(comp.GridUid.Value, grid, comp.Coordinates);
        var airtightQuery = GetEntityQuery<AirtightComponent>();
        var dockQuery = GetEntityQuery<DockingComponent>();
        var spreaderQuery = GetEntityQuery<EdgeSpreaderComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var blockedAtmosDirs = AtmosDirection.Invalid;

        // Due to docking ports they may not necessarily be opposite directions.
        var neighborTiles = new List<(EntityUid entity, MapGridComponent grid, Vector2i Indices, AtmosDirection OtherDir, AtmosDirection OurDir)>();

        // Check if anything on our own tile blocking that direction.
        var ourEnts = _map.GetAnchoredEntitiesEnumerator(comp.GridUid.Value, grid, tile);

        while (ourEnts.MoveNext(out var ent))
        {
            // Spread via docks in a special-case.
            if (dockQuery.TryGetComponent(ent, out var dock) &&
                dock.Docked &&
                xformQuery.TryGetComponent(ent, out var xform) &&
                xformQuery.TryGetComponent(dock.DockedWith, out var dockedXform) &&
                TryComp<MapGridComponent>(dockedXform.GridUid, out var dockedGrid))
            {
                neighborTiles.Add((dockedXform.GridUid.Value, dockedGrid, _map.CoordinatesToTile(dockedXform.GridUid.Value, dockedGrid, dockedXform.Coordinates), xform.LocalRotation.ToAtmosDirection(), dockedXform.LocalRotation.ToAtmosDirection()));
            }

            // If we're on a blocked tile work out which directions we can go.
            if (!airtightQuery.TryGetComponent(ent, out var airtight) || !airtight.AirBlocked ||
                _tag.HasTag(ent.Value, IgnoredTag) || _tag.HasTag(ent.Value, PlesenTag))
            {
                continue;
            }

            foreach (var value in new[] { AtmosDirection.North, AtmosDirection.East, AtmosDirection.South, AtmosDirection.West })
            {
                if ((value & airtight.AirBlockedDirection) == 0x0)
                    continue;

                blockedAtmosDirs |= value;
                break;
            }
            break;
        }

        // Add the normal neighbors.
        for (var i = 0; i < 4; i++)
        {
            var atmosDir = (AtmosDirection) (1 << i);
            var neighborPos = tile.Offset(atmosDir);
            neighborTiles.Add((comp.GridUid.Value, grid, neighborPos, atmosDir, i.ToOppositeDir()));
        }

        foreach (var (neighborEnt, neighborGrid, neighborPos, ourAtmosDir, otherAtmosDir) in neighborTiles)
        {
            // This tile is blocked to that direction.
            if ((blockedAtmosDirs & ourAtmosDir) != 0x0)
                continue;

            if (!_map.TryGetTileRef(neighborEnt, neighborGrid, neighborPos, out var tileRef) || tileRef.Tile.IsEmpty)
                continue;

            if (tileRef.Tile.IsSpace())
                continue;

            var directionEnumerator = _map.GetAnchoredEntitiesEnumerator(neighborEnt, neighborGrid, neighborPos);
            var occupied = false;

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (!airtightQuery.TryGetComponent(ent, out var airtight) || !airtight.AirBlocked || _tag.HasTag(ent.Value, IgnoredTag))
                {
                    continue;
                }

                if ((airtight.AirBlockedDirection & otherAtmosDir) == 0x0)
                    continue;

                occupied = true;
                break;
            }

            if (occupied)
                continue;

            var oldCount = occupiedTiles.Count;
            directionEnumerator = _map.GetAnchoredEntitiesEnumerator(neighborEnt, neighborGrid, neighborPos);

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (!spreaderQuery.TryGetComponent(ent, out var spreader))
                    continue;

                if (spreader.Id != prototype)
                    continue;

                neighbors.Add(ent.Value);
                occupiedTiles.Add(neighborPos);
                break;
            }

            if (oldCount == occupiedTiles.Count)
                freeTiles.Add((neighborGrid, tileRef));
        }
        return new object[] { freeTiles, neighbors, occupiedTiles };
    }
}
