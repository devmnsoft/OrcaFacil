using OrcaFacil.Application.Documents;
using Xunit;
namespace OrcaFacil.UnitTests;

public sealed class DocumentSnapshotSerializerTests
{
    private readonly DocumentSnapshotSerializer _serializer = new();

    [Fact]
    public void Equivalent_snapshots_have_the_same_canonical_hash()
    {
        var first = Snapshot([Item(" Instalação ", 2), Item("Cabo", 1)]);
        var second = Snapshot([Item("Cabo", 1), Item("Instalação", 2)]);
        var left = _serializer.Serialize(first);
        var right = _serializer.Serialize(second);

        Assert.Equal(left.Json, right.Json);
        Assert.Equal(left.Hash, right.Hash);
        Assert.Equal(64, left.Hash.Length);
    }

    [Fact]
    public void Commercial_change_changes_hash()
    {
        var left = _serializer.Serialize(Snapshot([Item("Instalação", 2)]));
        var right = _serializer.Serialize(Snapshot([Item("Instalação", 3)]));
        Assert.NotEqual(left.Hash, right.Hash);
    }

    private static DocumentSnapshot Snapshot(IReadOnlyList<QuoteItemSnapshot> items) => new(
        new(" Orça Fácil ", null, "COMERCIAL@EXAMPLE.COM", null, null, " São Paulo ", "sp", null, null, null),
        new("Cliente", "PF", null, null, "CLIENTE@EXAMPLE.COM", null, null, null),
        new("ORC-1", new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc), null, null, null, " À vista ", null,
            "ESSENTIAL", "#ffffff", null, true, 100, 0, 100), items);

    private static QuoteItemSnapshot Item(string description, decimal quantity) =>
        new(description, "UN", quantity, 50, 0, quantity * 50, quantity * 50);
}
