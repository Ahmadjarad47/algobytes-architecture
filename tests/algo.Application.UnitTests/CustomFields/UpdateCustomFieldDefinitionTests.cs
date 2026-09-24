using algo.Application.Features.CustomFields.Commands.UpdateCustomFieldDefinition;
using algo.Domain.CustomFields;

namespace algo.Application.UnitTests.CustomFields;

public sealed class UpdateCustomFieldDefinitionRequestTests
{
    [Fact]
    public void ToCommand_MapsAllFieldsIncludingId()
    {
        var id = Guid.NewGuid();
        var request = new UpdateCustomFieldDefinitionRequest(
            Label: "  Updated Label  ",
            Type: CustomFieldType.Number,
            Required: true,
            Searchable: false,
            Filterable: true,
            Sortable: true,
            VisibleInTable: false,
            VisibleInForm: true,
            VisibleInDetails: false,
            Options: null,
            DefaultValue: null,
            Validation: null);

        var command = request.ToCommand(id);

        Assert.Equal(id, command.Id);
        Assert.Equal("  Updated Label  ", command.Label);
        Assert.Equal(CustomFieldType.Number, command.Type);
        Assert.True(command.Required);
        Assert.False(command.Searchable);
        Assert.True(command.Filterable);
        Assert.True(command.Sortable);
        Assert.False(command.VisibleInTable);
        Assert.True(command.VisibleInForm);
        Assert.False(command.VisibleInDetails);
    }
}
