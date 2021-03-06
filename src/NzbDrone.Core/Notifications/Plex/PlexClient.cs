﻿using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Plex
{
    public class PlexClient : NotificationBase<PlexClientSettings>
    {
        private readonly IPlexService _plexService;

        public PlexClient(IPlexService plexService)
        {
            _plexService = plexService;
        }

        public override string Link
        {
            get { return "http://www.plexapp.com/"; }
        }

        public override void OnGrab(string message)
        {
            const string header = "Sonarr [TV] - Grabbed";
            _plexService.Notify(Settings, header, message);
        }

        public override void OnDownload(DownloadMessage message)
        {
            const string header = "Sonarr [TV] - Downloaded";
            _plexService.Notify(Settings, header, message.Message);
        }

        public override void AfterRename(Series series)
        {
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_plexService.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
