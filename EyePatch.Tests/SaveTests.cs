using LibGit2Sharp;
using Moq;

namespace EyePatch.Tests
{
    [TestClass]
    public class SaveTests
    {
        [TestMethod]
        public void Save_ShouldWriteError_WhenNotInGitRepository()
        {
            Assert.ThrowsException<EyePatchException>(
                () =>
                {
                    try
                    {
                        var mockSettings = new Mock<Settings>();

                        new Save().Execute(mockSettings.Object, "test.patch");
                    }
                    catch (EyePatchException ex)
                    {
                        Assert.IsInstanceOfType(ex.InnerException, typeof(RepositoryNotFoundException));
                        throw;
                    }
                });
        }

        [TestMethod]
        public void Save_ShouldWriteError_WhenParentCommitNotFound()
        {
            Assert.ThrowsException<EyePatchException>(
                () =>
                {
                    try
                    {
                        var mockSettings = new Mock<Settings>();

                        var mockRepository = new Mock<IRepository>();
                        mockRepository.Setup(r => r.Branches["origin/main"].Tip).Returns((Commit)null!);

                        new Save().Execute(mockSettings.Object, "test.patch");
                    }
                    catch (EyePatchException ex)
                    {
                        Assert.IsInstanceOfType(ex.InnerException, typeof(RepositoryNotFoundException));
                        throw;
                    }
                });
        }

        [TestMethod]
        public void Save_ShouldWriteWarning_WhenNoChangesToSave()
        {
            var mockSettings = new Mock<Settings>();
            var mockRepository = new Mock<IRepository>();
            var mockBranch = new Mock<Branch>();
            var mockCommit = new Mock<Commit>();
            var mockParentCommit = new Mock<Commit>();
            var mockPatch = new Mock<Patch>();

            mockRepository.Setup(r => r.Head).Returns(mockBranch.Object);
            mockRepository.Setup(r => r.Branches["origin/main"].Tip).Returns(mockCommit.Object);
            mockRepository.Setup(r => r.ObjectDatabase.FindMergeBase(mockBranch.Object.Tip, mockCommit.Object))
                .Returns(mockParentCommit.Object);
            mockRepository.Setup(r => r.Diff.Compare<Patch>(
                mockParentCommit.Object.Tree,
                DiffTargets.Index | DiffTargets.WorkingDirectory))
                .Returns(mockPatch.Object);

            List<PatchEntryChanges> mockPatchEntries = [];
            Assert.IsNotNull(mockPatchEntries);

            mockPatch.As<IEnumerable<PatchEntryChanges>>().Setup(p => p.GetEnumerator())
                .Returns(mockPatchEntries.GetEnumerator());

            var mockSave = new Mock<Save>();
            mockSave.Setup(s => s.WriteAndVerifyPatchFile(It.IsAny<string>(), It.IsAny<Patch>()));

            mockSave.Object.ExecuteWithRepo("test.patch", mockSettings.Object, mockRepository.Object);

            mockSave.Verify(s => s.WriteAndVerifyPatchFile(It.IsAny<string>(), It.IsAny<Patch>()), Times.Never);
        }

        [TestMethod]
        public void Save_ShouldWritePatchFile_WhenChangesExist()
        {
            var mockSettings = new Mock<Settings>();
            var mockRepository = new Mock<IRepository>();
            var mockBranch = new Mock<Branch>();
            var mockCommit = new Mock<Commit>();
            var mockParentCommit = new Mock<Commit>();
            var mockPatch = new Mock<Patch>();

            mockRepository.Setup(r => r.Head).Returns(mockBranch.Object);
            mockRepository.Setup(r => r.Branches["origin/main"].Tip).Returns(mockCommit.Object);
            mockRepository.Setup(r => r.ObjectDatabase.FindMergeBase(mockBranch.Object.Tip, mockCommit.Object))
                .Returns(mockParentCommit.Object);
            mockRepository.Setup(r => r.Diff.Compare<Patch>(
                    mockParentCommit.Object.Tree,
                    DiffTargets.Index | DiffTargets.WorkingDirectory))
                .Returns(mockPatch.Object);

            // Mocking the patch to return two entries
            var mockPatchEntries = new List<PatchEntryChanges>
            {
                new Mock<PatchEntryChanges>().Object,
                new Mock<PatchEntryChanges>().Object
            };
            mockPatch.As<IEnumerable<PatchEntryChanges>>().Setup(p => p.GetEnumerator())
                .Returns(mockPatchEntries.GetEnumerator());

            var mockSave = new Mock<Save>();
            mockSave.Setup(s => s.WriteAndVerifyPatchFile(It.IsAny<string>(), It.IsAny<Patch>()));
            mockSave.Setup(s => s.EnsurePatchesDirectoryExists(It.IsAny<Settings>())).Returns(string.Empty);

            mockSave.Object.ExecuteWithRepo("test.patch", mockSettings.Object, mockRepository.Object);

            mockSave.Verify(s => s.WriteAndVerifyPatchFile(It.IsAny<string>(), It.IsAny<Patch>()), Times.Once);
        }
    }
}
