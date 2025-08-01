using Content.Shared.Access.Systems;
using System.Text.RegularExpressions;
using Content.Shared.StationRecords;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Shared.Access.Components;

/// <summary>
/// Stores access levels necessary to "use" an entity
/// and allows checking if something or somebody is authorized with these access levels.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AccessReaderSystem))]
public sealed partial class AccessReaderComponent : Component
{
    // Sunrise added start
    #region ExtendedAccess

    /// <summary>
    /// Именно от Group происходит проверка аварийных доступов
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AccessGroupPrototype>? Group;

    [DataField, ViewVariables]
    public Dictionary<string, ProtoId<AccessGroupPrototype>> AlertAccesses = new();

    #endregion
    // Sunrise added end

    /// <summary>
    /// Whether or not the access reader is enabled.
    /// If not, it will always let people through.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// The set of tags that will automatically deny an allowed check, if any of them are present.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> DenyTags = new();

    /// <summary>
    /// List of access groups that grant access to this reader. Only a single matching group is required to gain access.
    /// A group matches if it is a subset of the set being checked against.
    /// </summary>
    [DataField("access")]
    public List<HashSet<ProtoId<AccessLevelPrototype>>> AccessLists = new();

    /// <summary>
    /// A list of <see cref="StationRecordKey"/>s that grant access. Only a single matching key is required to gain access.
    /// </summary>
    [DataField]
    public HashSet<StationRecordKey> AccessKeys = new();

    /// <summary>
    /// If specified, then this access reader will instead pull access requirements from entities contained in the
    /// given container.
    /// </summary>
    /// <remarks>
    /// This effectively causes <see cref="DenyTags"/>, <see cref="AccessLists"/>, and <see cref="AccessKeys"/> to be
    /// ignored, though <see cref="Enabled"/> is still respected. Access is denied if there are no valid entities or
    /// they all deny access.
    /// </remarks>
    [DataField]
    public string? ContainerAccessProvider;

    /// <summary>
    /// A list of past authentications.
    /// </summary>
    [DataField]
    public Queue<AccessRecord> AccessLog = new();

    /// <summary>
    /// A limit on the max size of <see cref="AccessLog"/>
    /// </summary>
    [DataField]
    public int AccessLogLimit = 20;

    /// <summary>
    /// If true logging on successful access uses will be disabled.
    /// Can be set by LOG wire.
    /// </summary>
    [DataField]
    public bool LoggingDisabled;

    /// <summary>
    /// Whether or not emag interactions have an effect on this.
    /// </summary>
    [DataField]
    public bool BreakOnAccessBreaker = true;
}

[DataDefinition, Serializable, NetSerializable]
public readonly partial record struct AccessRecord(
    [property: DataField, ViewVariables(VVAccess.ReadWrite)]
    TimeSpan AccessTime,
    [property: DataField, ViewVariables(VVAccess.ReadWrite)]
    string Accessor)
{
    public AccessRecord() : this(TimeSpan.Zero, string.Empty)
    {
    }
}

[Serializable, NetSerializable]
public sealed class AccessReaderComponentState : ComponentState
{
    public bool Enabled;
    public HashSet<ProtoId<AccessLevelPrototype>> DenyTags;
    public List<HashSet<ProtoId<AccessLevelPrototype>>> AccessLists;

    public ProtoId<AccessGroupPrototype>? Group; // Sunrise-alertAccesses, нужно для связывания клиента с сервером

    public List<(NetEntity, uint)> AccessKeys;
    public Queue<AccessRecord> AccessLog;
    public int AccessLogLimit;

    public AccessReaderComponentState(bool enabled, HashSet<ProtoId<AccessLevelPrototype>> denyTags,
        List<HashSet<ProtoId<AccessLevelPrototype>>> accessLists,
        ProtoId<AccessGroupPrototype>? group, //Sunrise added
        List<(NetEntity, uint)> accessKeys,
        Queue<AccessRecord> accessLog,
        int accessLogLimit)
    {
        Enabled = enabled;
        DenyTags = denyTags;
        AccessLists = accessLists;
        AccessKeys = accessKeys;
        AccessLog = accessLog;
        AccessLogLimit = accessLogLimit;
        Group = group; // Sunrise added for alertAccesses
    }
}

public sealed class AccessReaderConfigurationChangedEvent : EntityEventArgs;
