using EyePatch;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace EyePatch.Tests
{
    [TestClass]
    public class ViewTests
    {
        [TestMethod]
        public void View_ThrowsException_WhenPatchFilePathIsNullOrEmpty()
        {
            var view = new Mock<View> { CallBase = true };
            var settings = new Mock<Settings>().Object;
            Assert.ThrowsException<EyePatchException>(() => view.Object.Execute(settings, null!));
            Assert.ThrowsException<EyePatchException>(() => view.Object.Execute(settings, ""));
        }

        [TestMethod]
        public void View_ThrowsException_WhenPatchFileDoesNotExist()
        {
            var view = new Mock<View> { CallBase = true };
            var settings = new Mock<Settings>().Object;
            Assert.ThrowsException<EyePatchException>(() => view.Object.Execute(settings, "does_not_exist.patch"));
        }

        [TestMethod]
        public void View_LaunchesDiffTool_WhenPatchExists()
        {
            const string patchContent = "diff --git a/foo.txt b/foo.txt\nindex 1234567..89abcde 100644\n--- a/foo.txt\n+++ b/foo.txt\n@@ -1,2 +1,2 @@\n-old line\n+new line\n unchanged";
            const string tempFolder = "mockTempFolder";

            var mockBlob = new Mock<Blob>();
            mockBlob.Setup(b => b.GetContentText()).Returns("old line\nunchanged");

            mockBlob.Setup(b => b.GetContentText()).Returns("old line\nunchanged");
            var mockView = new Mock<View>();
            mockView.Setup(v => v.CreateTempFolder()).Returns(tempFolder);
            mockView.Setup(v => v.DeleteTempFolder(tempFolder));
            mockView.Setup(v => v.WriteBaseBlobToFile(It.IsAny<string>(), It.IsAny<Blob>()));
            mockView.Setup(v => v.WritePatchedBlobToFile(It.IsAny<string>(), It.IsAny<string>()));
            mockView.Setup(v => v.LaunchDiffTool(It.IsAny<Settings>(), tempFolder, It.IsAny<List<string>>()));
            mockView.Setup(v => v.LookupBlobByIndexHash(It.IsAny<IRepository>(), It.IsAny<FilePatch>()))
                    .Returns(mockBlob.Object);

            var mockRepository = new Mock<IRepository>();

            var settings = new Mock<Settings>().Object;

            mockView.Object.ExecuteWithRepo(patchContent, settings, mockRepository.Object);

            mockView.Verify(v => v.CreateTempFolder(), Times.Once);
            mockView.Verify(v => v.DeleteTempFolder(tempFolder), Times.Once);
            mockView.Verify(v => v.WriteBaseBlobToFile(It.IsAny<string>(), It.IsAny<Blob>()), Times.Once);
            mockView.Verify(v => v.WritePatchedBlobToFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            mockView.Verify(v => v.LaunchDiffTool(It.IsAny<Settings>(), tempFolder, It.IsAny<List<string>>()), Times.Once);
        }
    }
}
