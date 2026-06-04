using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.MetaTube
{
    public class MetaTubeActressSettingsValidator : AbstractValidator<MetaTubeActressSettings>
    {
        public MetaTubeActressSettingsValidator()
        {
            RuleFor(c => c.ActressName).NotEmpty();
        }
    }

    public class MetaTubeActressSettings : ImportListSettingsBase<MetaTubeActressSettings>
    {
        private static readonly MetaTubeActressSettingsValidator Validator = new ();

        public MetaTubeActressSettings()
        {
            ActressName = string.Empty;
            Provider = string.Empty;
            ActorId = string.Empty;
        }

        [FieldDefinition(0, Label = "Actress Name", HelpText = "Exact actress name to follow in MetaTube movie results")]
        public string ActressName { get; set; }

        [FieldDefinition(1, Label = "Provider", HelpText = "Optional MetaTube actor provider id", Advanced = true)]
        public string Provider { get; set; }

        [FieldDefinition(2, Label = "Actor Id", HelpText = "Optional MetaTube actor id", Advanced = true)]
        public string ActorId { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
