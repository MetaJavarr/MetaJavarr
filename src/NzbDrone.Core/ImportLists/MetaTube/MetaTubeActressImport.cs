using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.MetaTube
{
    public class MetaTubeActressImport : ImportListBase<MetaTubeActressSettings>
    {
        private readonly MetaTubeActressImportProxy _proxy;

        public MetaTubeActressImport(MetaTubeActressImportProxy proxy,
                                     IImportListStatusService importListStatusService,
                                     IConfigService configService,
                                     IParsingService parsingService,
                                     Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _proxy = proxy;
        }

        public override string Name => "MetaTube Actress";
        public override ImportListType ListType => ImportListType.Program;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);
        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public override ImportListFetchResult Fetch()
        {
            try
            {
                var movies = _proxy.Fetch(Settings);
                _importListStatusService.RecordSuccess(Definition.Id);

                return new ImportListFetchResult(CleanupListItems(movies), false)
                {
                    SyncedLists = 1
                };
            }
            catch (Exception ex)
            {
                _importListStatusService.RecordFailure(Definition.Id);
                _logger.Warn(ex, "Unable to fetch MetaTube actress import list for {0}", Settings.ActressName);

                return new ImportListFetchResult(new List<ImportListMovie>(), true);
            }
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            try
            {
                _proxy.Fetch(Settings);
            }
            catch (Exception ex)
            {
                failures.Add(new ValidationFailure(string.Empty, $"Unable to connect to MetaTube: {ex.Message}"));
            }
        }
    }
}
