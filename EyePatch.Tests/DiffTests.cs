using EyePatch;

namespace test
{
    [TestClass]
    public class DiffTests
    {
        [TestMethod]
        public void FilePatchParser_ParsesSinglePatchCorrectly()
        {
            string patch = """
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
            string baseContent = "old line\nunchanged";
            string diffContent = "@@ -1,2 +1,2 @@\n-old line\n+new line\n unchanged";
            string expected = "new line\nunchanged";
            string result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }

        [TestMethod]
        public void FilePatchApplier_AppliesAdditionAndRemoval()
        {
            string baseContent = "a\nb\nc";
            string diffContent = "@@ -2,2 +2,3 @@\n-b\n+c\n+d";
            string expected = "a\nc\nd";
            string result = FilePatchApplier.Apply(baseContent, diffContent);
            Assert.AreEqual(expected.Replace("\n", Environment.NewLine), result);
        }
    }
}
