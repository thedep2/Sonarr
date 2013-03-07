using NLog;
using NzbDrone.Core.History;
using NzbDrone.Core.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class UpgradeHistorySpecification : IFetchableSpecification
    {
        private readonly IHistoryService _historyService;
        private readonly QualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly Logger _logger;

        public UpgradeHistorySpecification(IHistoryService historyService, QualityUpgradableSpecification qualityUpgradableSpecification, Logger logger)
        {
            _historyService = historyService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _logger = logger;
        }

        public string RejectionReason
        {
            get
            {
                return "Higher quality report exists in history";
            }
        }

        public virtual bool IsSatisfiedBy(EpisodeParseResult subject)
        {
            foreach (var episode in subject.Episodes)
            {
                var bestQualityInHistory = _historyService.GetBestQualityInHistory(subject.Series.Id, episode.SeasonNumber, episode.EpisodeNumber);
                if (bestQualityInHistory != null)
                {
                    _logger.Trace("Comparing history quality with report. History is {0}", bestQualityInHistory);
                    if (!_qualityUpgradableSpecification.IsUpgradable(subject.Series.QualityProfile, bestQualityInHistory, subject.Quality))
                        return false;
                }
            }

            return true;
        }
    }
}