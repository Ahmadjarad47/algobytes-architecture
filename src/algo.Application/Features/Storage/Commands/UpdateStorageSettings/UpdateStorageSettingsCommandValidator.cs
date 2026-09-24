using FluentValidation;

namespace algo.Application.Features.Storage.Commands.UpdateStorageSettings;

public sealed class UpdateStorageSettingsCommandValidator : AbstractValidator<UpdateStorageSettingsCommand>
{
    public UpdateStorageSettingsCommandValidator()
    {
        RuleFor(x => x.EndpointUrl).NotEmpty().MaximumLength(512);
        RuleFor(x => x.AccessKey).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SecretKey).MaximumLength(512);
        RuleFor(x => x.BucketName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Region).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Folder).NotEmpty().MaximumLength(512);
        RuleFor(x => x.ScannerProvider).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ScannerEndpointUrl).MaximumLength(512);
        RuleFor(x => x.ScannerApiKey).MaximumLength(512);
        RuleFor(x => x.QuarantineFolder).MaximumLength(512);
    }
}
