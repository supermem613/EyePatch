using EyePatch;
using LibGit2Sharp;
using Moq;

namespace EyePatch.Tests
{
    [TestClass]
    public class DiffTests
    {
        [TestMethod]
        public void FilePatchParser_ParsesSinglePatchCorrectly()
        {
            const string patch = """
                                 diff --git a/foo.txt b/foo.txt
                                 index 1234567..89abcde 100644
                                 --- a/foo.txt
                                 +++ b/foo.txt
                                 @@ -1,2 +1,2 @@
                                 -old line
                                 +new line
                                  unchanged
                                 """;

            var parser = new FilePatchParser(patch);
            var patches = parser.Parse();
            Assert.AreEqual(1, patches.Count);
            Assert.AreEqual("foo.txt", patches[0].BaseFilePath);
            Assert.AreEqual("1234567", patches[0].BaseIndex);
            StringAssert.Contains(patches[0].DiffContent, "@@ -1,2 +1,2 @@");
        }

        [TestMethod]
        public void FilePatchApplier_AppliesSimplePatchCorrectly()
        {
            const string baseContent = "old line\nunchanged";
            const string diffContent = "@@ -1,2 +1,2 @@\n-old line\n+new line\n unchanged";
            const string expected = "new line\nunchanged";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_AppliesAdditionAndRemoval()
        {
            const string baseContent = "a\nb\nc";
            const string diffContent = "@@ -2,2 +2,3 @@\n-b\n+c\n+d";
            const string expected = "a\nc\nd\nc";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_InsertsLineInMiddle()
        {
            const string baseContent = "line1\nline2\nline3\nline4\nline5";
            const string diffContent = "@@ -3,2 +3,3 @@\n line3\n+new line\n line4";
            const string expected = "line1\nline2\nline3\nnew line\nline4\nline5";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_InsertsLineAtTop()
        {
            const string baseContent = "line1\nline2\nline3";
            const string diffContent = "@@ -0,0 +1,1 @@\n+new line";
            const string expected = "new line\nline1\nline2\nline3";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_InsertsLineAtBottom()
        {
            const string baseContent = "line1\nline2\nline3";
            const string diffContent = "@@ -3,1 +3,2 @@\n line3\n+new line";
            const string expected = "line1\nline2\nline3\nnew line";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_RemovesTopLine()
        {
            const string baseContent = "line1\nline2\nline3\nline4\nline5";
            const string diffContent = "@@ -1,1 +0,0 @@\n-line1";
            const string expected = "line2\nline3\nline4\nline5";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_RemovesMiddleLine()
        {
            const string baseContent = "line1\nline2\nline3\nline4\nline5";
            const string diffContent = "@@ -3,1 +0,0 @@\n-line3";
            const string expected = "line1\nline2\nline4\nline5";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_RemovesBottomLine()
        {
            const string baseContent = "line1\nline2\nline3\nline4\nline5";
            const string diffContent = "@@ -5,1 +0,0 @@\n-line5";
            const string expected = "line1\nline2\nline3\nline4";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_RemovesAndInsertsTopLine()
        {
            const string baseContent = "line1\nline2\nline3\nline4\nline5";
            const string diffContent = "@@ -1,1 +1,1 @@\n-line1\n+new line";
            const string expected = "new line\nline2\nline3\nline4\nline5";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_RemovesAndInsertsMiddleLine()
        {
            const string baseContent = "line1\nline2\nline3\nline4\nline5";
            const string diffContent = "@@ -3,1 +3,1 @@\n-line3\n+new line";
            const string expected = "line1\nline2\nnew line\nline4\nline5";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_RemovesAndInsertsBottomLine()
        {
            const string baseContent = "line1\nline2\nline3\nline4\nline5";
            const string diffContent = "@@ -5,1 +5,1 @@\n-line5\n+new line";
            const string expected = "line1\nline2\nline3\nline4\nnew line";
            var result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void Diff_ShouldThrowError_WhenNotInGitRepository()
        {
            Assert.ThrowsException<EyePatchException>(
                () =>
                {
                    try
                    {
                        var mockSettings = new Mock<Settings>();

                        new Diff().Execute(mockSettings.Object);
                    }
                    catch (EyePatchException ex)
                    {
                        Assert.IsInstanceOfType(ex.InnerException, typeof(RepositoryNotFoundException));
                        throw;
                    }
                });
        }

        [TestMethod]
        public void Diff_ShouldThrowError_WhenPatchFileNotFound()
        {
            Assert.ThrowsException<EyePatchException>(
                () =>
                {
                    var mockSettings = new Mock<Settings>();

                    new Diff().Execute(mockSettings.Object);
                });
        }

        [TestMethod]
        public void Diff_ShouldNotLaunchDiffTool_WhenNoParentCommit()
        {
            var mockSettings = new Mock<Settings>();
            var mockRepository = new Mock<IRepository>();
            var mockDiffLauncher = new Mock<DiffLauncher>();
            var mockBranch = new Mock<Branch>();
            var mockObjectDatabase = new Mock<ObjectDatabase>(); // Added this line to define mockObjectDatabase

            mockRepository.Setup(r => r.Head).Returns(mockBranch.Object);

            // Simulate no changes in the repository
            var mockPatch = new Mock<Patch>();
            mockRepository.Setup(r => r.Diff.Compare<Patch>(It.IsAny<Tree>(), It.IsAny<Tree>()))
                .Returns(mockPatch.Object);

            var mockDiff = new Mock<Diff>();
            mockDiff.Setup(d => d.CreateTempFolder())
                .Returns(string.Empty); // Mock CreateTempFolder to return an empty string instead of creating a folder

            var mockTip = new Mock<Commit>();
            var mockOriginMainBranch = new Mock<Branch>();
            var mockOriginMainTip = new Mock<Commit>();

            mockRepository.Setup(r => r.ObjectDatabase).Returns(mockObjectDatabase.Object);
            mockRepository.Setup(r => r.Branches["origin/main"]).Returns(mockOriginMainBranch.Object);
            mockBranch.Setup(b => b.Tip).Returns(mockTip.Object);
            mockOriginMainBranch.Setup(b => b.Tip).Returns(mockOriginMainTip.Object);

            Assert.ThrowsException<EyePatchException>(
                () =>
                {
                    mockDiff.Object.ExecuteWithRepo(mockSettings.Object, mockRepository.Object);
                });
        }

        [TestMethod]
        public void Diff_ShouldNotLaunchDiffTool_WhenNoChanges()
        {
            var mockSettings = new Mock<Settings>();
            var mockRepository = new Mock<IRepository>();
            var mockBranch = new Mock<Branch>();
            var mockObjectDatabase = new Mock<ObjectDatabase>(); // Added this line to define mockObjectDatabase

            mockRepository.Setup(r => r.Head).Returns(mockBranch.Object);

            // Simulate no changes in the repository
            var mockPatch = new Mock<Patch>();
            mockRepository.Setup(r => r.Diff.Compare<Patch>(It.IsAny<Tree>(), It.IsAny<Tree>()))
                .Returns(mockPatch.Object);

            var mockDiff = new Mock<Diff>();
            mockDiff.Setup(d => d.CreateTempFolder())
                .Returns(string.Empty); // Mock CreateTempFolder to return an empty string instead of creating a folder

            var mockTip = new Mock<Commit>();
            var mockOriginMainBranch = new Mock<Branch>();
            var mockOriginMainTip = new Mock<Commit>();

            mockRepository.Setup(r => r.ObjectDatabase).Returns(mockObjectDatabase.Object);
            mockRepository.Setup(r => r.Branches["origin/main"]).Returns(mockOriginMainBranch.Object);
            mockBranch.Setup(b => b.Tip).Returns(mockTip.Object);
            mockOriginMainBranch.Setup(b => b.Tip).Returns(mockOriginMainTip.Object);

            // Mock ObjectDatabase.Merge to return a specific object
            var mockBaseCommit = new Mock<Commit>();
            mockObjectDatabase.Setup(db => db.FindMergeBase(It.IsAny<Commit>(), It.IsAny<Commit>()))
                .Returns(mockBaseCommit.Object);

            mockDiff.Object.ExecuteWithRepo(mockSettings.Object, mockRepository.Object);

            mockDiff.Verify(s => s.LaunchDiffTool(It.IsAny<Settings>(), It.IsAny<String>(), It.IsAny<List<string>>()), Times.Never);
        }


        [TestMethod]
        public void Diff_ShouldNotLaunchDiffTool_NoBlobs()
        {
            var mockSettings = new Mock<Settings>();
            var mockRepository = new Mock<IRepository>();
            var mockBranch = new Mock<Branch>();
            var mockObjectDatabase = new Mock<ObjectDatabase>(); // Added this line to define mockObjectDatabase

            mockRepository.Setup(r => r.Head).Returns(mockBranch.Object);

            // Simulate no changes in the repository
            var mockPatch = new Mock<Patch>();
            mockRepository.Setup(r => r.Diff.Compare<Patch>(It.IsAny<Tree>(), It.IsAny<Tree>()))
                .Returns(mockPatch.Object);

            var mockDiff = new Mock<Diff>();
            mockDiff.Setup(d => d.CreateTempFolder())
                .Returns(string.Empty); // Mock CreateTempFolder to return an empty string instead of creating a folder

            var mockTip = new Mock<Commit>();
            var mockOriginMainBranch = new Mock<Branch>();
            var mockOriginMainTip = new Mock<Commit>();

            mockRepository.Setup(r => r.Info.WorkingDirectory).Returns("C:/mock/repo/path");
            mockRepository.Setup(r => r.ObjectDatabase).Returns(mockObjectDatabase.Object);
            mockRepository.Setup(r => r.Branches["origin/main"]).Returns(mockOriginMainBranch.Object);
            mockBranch.Setup(b => b.Tip).Returns(mockTip.Object);
            mockOriginMainBranch.Setup(b => b.Tip).Returns(mockOriginMainTip.Object);

            // Mock ObjectDatabase.Merge to return a specific object
            var mockBaseCommit = new Mock<Commit>();
            mockObjectDatabase.Setup(db => db.FindMergeBase(It.IsAny<Commit>(), It.IsAny<Commit>()))
                .Returns(mockBaseCommit.Object);

            var mockTreeChanges = new Mock<TreeChanges>();
            var mockAddedChange = new Mock<TreeEntryChanges>();
            var mockModifiedChange = new Mock<TreeEntryChanges>();
            var mockDeletedChange = new Mock<TreeEntryChanges>();

            mockAddedChange.Setup(c => c.Status).Returns(ChangeKind.Added);
            mockAddedChange.Setup(c => c.Path).Returns("addedFile.txt");

            mockModifiedChange.Setup(c => c.Status).Returns(ChangeKind.Modified);
            mockModifiedChange.Setup(c => c.Path).Returns("modifiedFile.txt");

            mockDeletedChange.Setup(c => c.Status).Returns(ChangeKind.Deleted);
            mockDeletedChange.Setup(c => c.Path).Returns("deletedFile.txt");

            mockTreeChanges.Setup(tc => tc.Added).Returns([mockAddedChange.Object]);
            mockTreeChanges.Setup(tc => tc.Modified).Returns([mockModifiedChange.Object]);
            mockTreeChanges.Setup(tc => tc.Deleted).Returns([mockDeletedChange.Object]);
            mockTreeChanges.Setup(tc => tc.Count).Returns(3);

            mockTreeChanges.Setup(tc => tc.GetEnumerator())
                .Returns(new List<TreeEntryChanges>
                {
                    mockAddedChange.Object,
                    mockModifiedChange.Object,
                    mockDeletedChange.Object
                }.GetEnumerator());

            mockRepository.Setup(r => r.Diff.Compare<TreeChanges>(
                It.IsAny<Tree>(),
                It.IsAny<Tree>()))
                .Returns(mockTreeChanges.Object);

            mockDiff.Object.ExecuteWithRepo(mockSettings.Object, mockRepository.Object);

            mockDiff.Verify(s => s.LaunchDiffTool(It.IsAny<Settings>(), It.IsAny<String>(), It.IsAny<List<string>>()), Times.Once);
        }

        [TestMethod]
        public void Diff_ShouldLaunchDiffTool_WhenValidPatchProvided()
        {
            var mockSettings = new Mock<Settings>();
            var mockRepository = new Mock<IRepository>();
            var mockBranch = new Mock<Branch>();
            var mockObjectDatabase = new Mock<ObjectDatabase>(); // Added this line to define mockObjectDatabase

            mockRepository.Setup(r => r.Head).Returns(mockBranch.Object);

            // Simulate no changes in the repository
            var mockPatch = new Mock<Patch>();
            mockRepository.Setup(r => r.Diff.Compare<Patch>(It.IsAny<Tree>(), It.IsAny<Tree>()))
                .Returns(mockPatch.Object);

            var mockDiff = new Mock<Diff>();
            mockDiff.Setup(d => d.CreateTempFolder())
                .Returns(string.Empty); // Mock CreateTempFolder to return an empty string instead of creating a folder

            var mockTip = new Mock<Commit>();
            var mockOriginMainBranch = new Mock<Branch>();
            var mockOriginMainTip = new Mock<Commit>();

            mockRepository.Setup(r => r.Info.WorkingDirectory).Returns("C:/mock/repo/path");
            mockRepository.Setup(r => r.ObjectDatabase).Returns(mockObjectDatabase.Object);
            mockRepository.Setup(r => r.Branches["origin/main"]).Returns(mockOriginMainBranch.Object);
            mockBranch.Setup(b => b.Tip).Returns(mockTip.Object);
            mockOriginMainBranch.Setup(b => b.Tip).Returns(mockOriginMainTip.Object);

            var mockTreeChanges = new Mock<TreeChanges>();
            var mockAddedChange = new Mock<TreeEntryChanges>();
            var mockModifiedChange = new Mock<TreeEntryChanges>();
            var mockDeletedChange = new Mock<TreeEntryChanges>();

            mockAddedChange.Setup(c => c.Status).Returns(ChangeKind.Added);
            mockAddedChange.Setup(c => c.Path).Returns("addedFile.txt");

            mockModifiedChange.Setup(c => c.Status).Returns(ChangeKind.Modified);
            mockModifiedChange.Setup(c => c.Path).Returns("modifiedFile.txt");

            mockDeletedChange.Setup(c => c.Status).Returns(ChangeKind.Deleted);
            mockDeletedChange.Setup(c => c.Path).Returns("deletedFile.txt");

            mockTreeChanges.Setup(tc => tc.Added).Returns([mockAddedChange.Object]);
            mockTreeChanges.Setup(tc => tc.Modified).Returns([mockModifiedChange.Object]);
            mockTreeChanges.Setup(tc => tc.Deleted).Returns([mockDeletedChange.Object]);
            mockTreeChanges.Setup(tc => tc.Count).Returns(3);

            mockTreeChanges.Setup(tc => tc.GetEnumerator())
                .Returns(new List<TreeEntryChanges>
                {
                    mockAddedChange.Object,
                    mockModifiedChange.Object,
                    mockDeletedChange.Object
                }.GetEnumerator());

            // Mock ObjectDatabase.Merge to return a specific object
            var mockBaseCommit = new Mock<Commit>();
            mockObjectDatabase.Setup(db => db.FindMergeBase(It.IsAny<Commit>(), It.IsAny<Commit>()))
                .Returns(mockBaseCommit.Object);

            var mockTreeEntry = new Mock<TreeEntry>();
            mockBaseCommit.Setup(commit => commit[It.IsAny<string>()]).Returns(mockTreeEntry.Object);

            var mockBlob = new Mock<Blob>();
            mockTreeEntry.Setup(te => te.Target).Returns(mockBlob.Object);

            // Add verification step to ensure the mock setup is being used
            mockRepository.Setup(r => r.Diff.Compare<TreeChanges>(
                It.IsAny<Tree>(),
                It.IsAny<Tree>()))
                .Returns(mockTreeChanges.Object);

            mockDiff.Setup(d => d.WriteBlobAsFile(It.IsAny<string>(), It.IsAny<Blob>())).Verifiable();
            int callCount = 0;
            mockDiff.Setup(d => d.AreFilesIdentical(It.IsAny<string>(), It.IsAny<Blob>(), It.IsAny<string>()))
               .Returns(() =>
               {
                   callCount++;
                   return callCount == 1;
               });

            mockDiff.Object.ExecuteWithRepo(mockSettings.Object, mockRepository.Object);

            mockDiff.Verify(d => d.WriteBlobAsFile(It.IsAny<string>(), It.IsAny<Blob>()), Times.Exactly(3));
            mockDiff.Verify(s => s.LaunchDiffTool(It.IsAny<Settings>(), It.IsAny<String>(), It.IsAny<List<string>>()), Times.Once);
        }
    }
}
