using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspacePrototypeCatalogBuilderTests
{
    [Test]
    public async Task Build_UsesInstallationAdjustedNameAndMessageBackedDescription(CancellationToken cancellationToken)
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "proto", "critters"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "oemes"));

        var prototype = WorkspaceCatalogTestData.MakePrototype(
            ObjectType.Npc,
            21,
            ObjectPropertyFactory.ForInt32(ObjectField.Description, 10),
            ObjectPropertyFactory.ForInt32(ObjectField.CurrentAid, unchecked((int)0x00000123u)),
            ObjectPropertyFactory.ForInt32(ObjectField.DestroyedAid, unchecked((int)0x00000456u))
        );
        ProtoFormat.WriteToFile(
            in prototype,
            Path.Combine(gameDirectory, "data", "proto", "critters", "000021 - Guard.pro")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1, "Translated Guard")] },
            Path.Combine(gameDirectory, "data", "oemes", "oname.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(10, "Patrols the bridge.")] },
            Path.Combine(gameDirectory, "data", "mes", "description.mes")
        );

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(
            gameDirectory,
            cancellationToken: cancellationToken
        );

        var entries = WorkspacePrototypeCatalogBuilder.Build(
            loadResult.GameData,
            ArcanumInstallationType.UniversalArcanumPatcher
        );

        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].ProtoNumber).IsEqualTo(21);
        await Assert.That(entries[0].ObjectType).IsEqualTo(ObjectType.Npc);
        await Assert.That(entries[0].AssetPath).IsEqualTo("proto/critters/000021 - Guard.pro");
        await Assert.That(entries[0].DisplayName).IsEqualTo("Translated Guard");
        await Assert.That(entries[0].Description).IsEqualTo("Patrols the bridge.");
        await Assert.That(entries[0].PaletteGroup).IsEqualTo("critters");
        await Assert.That(entries[0].CurrentArtId).IsEqualTo(new ArtId(0x00000123u));
        await Assert.That(entries[0].DestroyedArtId).IsEqualTo(new ArtId(0x00000456u));
        await Assert.That(entries[0].ArtAssetPath).IsNull();
        await Assert.That(entries[0].PortalFlags).IsNull();
        await Assert.That(entries[0].ContainerFlags).IsNull();
        await Assert.That(entries[0].SceneryFlags).IsNull();
    }

    [Test]
    public async Task Build_ProjectsTypeSpecificPrototypeFlags(CancellationToken cancellationToken)
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "proto", "portal"));

        var prototype = WorkspaceCatalogTestData.MakePrototype(
            ObjectType.Portal,
            1001,
            ObjectPropertyFactory.ForInt32(ObjectField.PortalFlags, unchecked((int)PortalFlags.Locked)),
            ObjectPropertyFactory.ForInt32(ObjectField.PortalLockDifficulty, 45),
            ObjectPropertyFactory.ForInt32(ObjectField.PortalKeyId, 17)
        );
        ProtoFormat.WriteToFile(
            in prototype,
            Path.Combine(gameDirectory, "data", "proto", "portal", "001001 - Door.pro")
        );

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(
            gameDirectory,
            cancellationToken: cancellationToken
        );

        var entries = WorkspacePrototypeCatalogBuilder.Build(loadResult.GameData);

        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].ObjectType).IsEqualTo(ObjectType.Portal);
        await Assert.That(entries[0].PortalFlags).IsEqualTo(PortalFlags.Locked);
        await Assert.That(entries[0].PortalLockDifficulty).IsEqualTo(45);
        await Assert.That(entries[0].PortalKeyId).IsEqualTo(17);
        await Assert.That(entries[0].ContainerFlags).IsNull();
        await Assert.That(entries[0].SceneryFlags).IsNull();
    }
}
