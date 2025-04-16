using EyePatch;

namespace test
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
    }
}
