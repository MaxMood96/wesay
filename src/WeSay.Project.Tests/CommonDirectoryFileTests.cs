using NUnit.Framework;
using SIL.WritingSystems;
using SIL.WritingSystems.Migration;
using System;
using System.IO;

namespace WeSay.Project.Tests
{
	[TestFixture]
	public class CommonDirectoryFileTests
	{
		private class TestEnvironment : IDisposable
		{
			public string WritingSystemLdmlFolderPath
			{
				get
				{
					return WeSayWordsProject.GetPathToLdmlWritingSystemsFolder(WeSayWordsProject.ApplicationCommonDirectory);
				}
			}

			public void Dispose()
			{
			}
		}

		[Test]
		public void WritingSystemLdmlFiles_Version_IsLatestVersion()
		{
			using (var e = new TestEnvironment())
			{
				var ldmlVersionGetter = new WritingSystemLdmlVersionGetter();
				foreach (var filePath in Directory.GetFiles(e.WritingSystemLdmlFolderPath, "*.ldml"))
				{
					Assert.AreEqual(LdmlDataMapper.CurrentLdmlLibraryVersion, ldmlVersionGetter.GetFileVersion(filePath), String.Format("The file {0} did not have the correct version.", filePath));
				}
			}

		}
	}
}
