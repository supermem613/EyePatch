using LibGit2Sharp;
using Moq;

namespace EyePatch.Tests
{
    [TestClass]
    public class StatusTests
    {
        [TestMethod]
        public void Status_ShouldWriteError_WhenNotInGitRepository()
        {
            Assert.ThrowsException<EyePatchException>(
                () =>
                {
                    var mockSettings = new Mock<Settings>();

                    var mockStatus = new Mock<Status> { CallBase = true };
                    mockStatus.Setup(d => d.FindRepository())
                        .Throws(new EyePatchException("Not in a Git repository.", new RepositoryNotFoundException()));

                    mockStatus.Object.Execute(mockSettings.Object);
                });
        }

        [TestMethod]
        public void Status_ShouldWriteError_WhenMain()
        {
            Assert.ThrowsException<EyePatchException>(
                () =>
                {
                    var mockSettings = new Mock<Settings>();

                    var mockRepository = new Mock<IRepository>();
                    mockRepository.Setup(r => r.Branches["origin/main"].Tip).Returns((Commit)null!);

                    var mockStatus = new Mock<Status> { CallBase = true };
                    mockStatus.Setup(d => d.FindRepository())
                        .Returns(mockRepository.Object);

                    mockStatus.Object.Execute(mockSettings.Object, "test.patch");
                });
        }
    }
}
