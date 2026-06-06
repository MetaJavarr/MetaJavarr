using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class QualityAllowedByProfileSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public QualityAllowedByProfileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteMovie subject, SearchCriteriaBase searchCriteria)
        {
            _logger.Debug("Checking if report meets quality requirements. {0}", subject.ParsedMovieInfo.Quality);

            if (subject.MovieMatchType == MovieMatchType.Number &&
                subject.ParsedMovieInfo.Quality.Quality == Quality.Unknown)
            {
                _logger.Debug("Allowing unknown quality for exact movie number match");
                return DownloadSpecDecision.Accept();
            }

            var profile = subject.Movie.QualityProfile;
            var qualityIndex = profile.GetIndex(subject.ParsedMovieInfo.Quality.Quality);
            var qualityOrGroup = profile.Items[qualityIndex.Index];

            if (!qualityOrGroup.Allowed)
            {
                _logger.Debug("Quality {0} rejected by Movie's quality profile", subject.ParsedMovieInfo.Quality);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.QualityNotWanted, "{0} is not wanted in profile", subject.ParsedMovieInfo.Quality.Quality);
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
