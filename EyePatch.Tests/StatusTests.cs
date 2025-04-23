using EyePatch;
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
                    try
                    {
                        var mockSettings = new Mock<Settings>();

                        new Status().Execute(mockSettings.Object);
                    }
                    catch (EyePatchException ex)
                    {
                        Assert.IsInstanceOfType(ex.InnerException, typeof(RepositoryNotFoundException));
                        throw;
                    }
                });
        }

        [TestMethod]
        public void Status_ShouldWriteError_WhenMain()
        {
            Assert.ThrowsException<EyePatchException>(
                () =>
                {
                    try
                    {
                        var mockSettings = new Mock<Settings>();

                        var mockRepository = new Mock<IRepository>();
                        mockRepository.Setup(r => r.Branches["origin/main"].Tip).Returns((Commit)null!);

                        new Status().Execute(mockSettings.Object, "test.patch");
                    }
                    catch (EyePatchException ex)
                    {
                        Assert.IsInstanceOfType(ex.InnerException, typeof(RepositoryNotFoundException));
                        throw;
                    }
                });
        }
    }
}
