using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(243)]
    public class add_movie_number : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("MovieMetadata").AddColumn("Number").AsString().Nullable();
            Execute.Sql("UPDATE \"NamingConfig\" SET \"MovieFolderFormat\" = '{Movie CleanNumber}', \"StandardMovieFormat\" = '{Movie CleanNumber}'");
        }
    }
}
