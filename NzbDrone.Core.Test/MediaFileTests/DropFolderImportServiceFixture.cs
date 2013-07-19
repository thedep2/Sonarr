using System;
using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFileTests
{
    [TestFixture]
    public class DropFolderImportServiceFixture : CoreTest<DownloadedEpisodesImportService>
    {
        private EpisodeFile _fakeEpisodeFile;

        private string[] _subFolders = new[] { "c:\\root\\foldername" };
        private string[] _videoFiles = new[] { "c:\\root\\foldername\\video.ext" };

        [SetUp]
        public void Setup()
        {
            _fakeEpisodeFile = Builder<EpisodeFile>.CreateNew().Build();

            Mocker.GetMock<IDiskScanService>().Setup(c => c.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(_videoFiles);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.GetDirectories(It.IsAny<string>()))
                  .Returns(_subFolders);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IConfigService>().SetupGet(c => c.DownloadedEpisodesFolder)
                  .Returns("c:\\drop\\");

            Mocker.GetMock<IImportApprovedEpisodes>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true))
                  .Returns(new List<ImportDecision>());
        }

        private void GivenValidSeries()
        {
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetSeries(It.IsAny<String>()))
                  .Returns(Builder<Series>.CreateNew().Build());
        }

        [Test]
        public void should_search_for_series_using_folder_name()
        {
            Subject.Execute(new DownloadedEpisodesScanCommand());


            Mocker.GetMock<IParsingService>().Verify(c => c.GetSeries("foldername"), Times.Once());
        }

        [Test]
        public void should_skip_import_if_dropfolder_doesnt_exist()
        {
            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(It.IsAny<string>())).Returns(false);
            
            Subject.Execute(new DownloadedEpisodesScanCommand());

            Mocker.GetMock<IDiskProvider>().Verify(c => c.GetDirectories(It.IsAny<string>()), Times.Never());
            Mocker.GetMock<IDiskProvider>().Verify(c => c.GetFiles(It.IsAny<string>(), It.IsAny<SearchOption>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_skip_if_file_is_in_use_by_another_process()
        {
            Mocker.GetMock<IDiskProvider>().Setup(c => c.IsFileLocked(It.IsAny<FileInfo>()))
                  .Returns(true);

            Subject.Execute(new DownloadedEpisodesScanCommand());

            VerifyNoImport();
        }

        [Test]
        public void should_not_import_if_folder_is_a_series_path()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.SeriesPathExists(It.IsAny<String>()))
                  .Returns(true);

            Mocker.GetMock<IDiskScanService>()
                  .Setup(c => c.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(new string[0]);

            Subject.Execute(new DownloadedEpisodesScanCommand());

            Mocker.GetMock<IParsingService>()
                  .Verify(v => v.GetSeries(It.IsAny<String>()), Times.Never());
        }

        [Test]
        public void should_not_delete_folder_if_no_files_were_imported()
        {
            Mocker.GetMock<IImportApprovedEpisodes>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), false))
                  .Returns(new List<ImportDecision>());

            Subject.Execute(new DownloadedEpisodesScanCommand());

            Mocker.GetMock<DiskProvider>()
                  .Verify(v => v.GetFolderSize(It.IsAny<String>()), Times.Never());
        }

        [Test]
        public void should_delete_folder_if_files_were_imported()
        {
            GivenValidSeries();

            var localEpisode = new LocalEpisode();

            var imported = new List<ImportDecision>();
            imported.Add(new ImportDecision(localEpisode));

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<IEnumerable<String>>(), It.IsAny<Series>()))
                  .Returns(imported);

            Mocker.GetMock<IImportApprovedEpisodes>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true))
                  .Returns(imported);

            Subject.Execute(new DownloadedEpisodesScanCommand());

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<String>(), true), Times.Once());
        }

        private void VerifyNoImport()
        {
            Mocker.GetMock<IImportApprovedEpisodes>().Verify(c => c.Import(It.IsAny<List<ImportDecision>>(), true),
                Times.Never());
        }

        private void VerifyImport()
        {
            Mocker.GetMock<IImportApprovedEpisodes>().Verify(c => c.Import(It.IsAny<List<ImportDecision>>(), true),
                Times.Once());
        }
    }
}