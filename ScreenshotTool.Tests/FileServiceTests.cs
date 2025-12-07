using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScreenshotTool.Services;
using Xunit;

namespace ScreenshotTool.Tests
{
    public class FileServiceTests
    {
        [Fact]
        public void LoadMedia_ReturnsEmptyList_WhenFolderDoesNotExist()
        {
            var service = new FileService();
            var result = service.LoadMedia("NonExistentFolder", "*.png", true);
            Assert.Empty(result);
        }

        [Fact]
        public void LoadMedia_ReturnsSortedFiles()
        {
            // Arrange
            string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);

            string file1 = Path.Combine(tempFolder, "old.png");
            string file2 = Path.Combine(tempFolder, "new.png");

            File.WriteAllText(file1, "content");
            File.WriteAllText(file2, "content");

            // Set timestamps
            File.SetCreationTime(file1, DateTime.Now.AddHours(-2));
            File.SetCreationTime(file2, DateTime.Now);

            var service = new FileService();

            // Act
            var result = service.LoadMedia(tempFolder, "*.png", true);

            // Assert
            Assert.Equal(2, result.Count);
            // Expect newest first
            Assert.Contains("new.png", result[0].FileName);
            Assert.Contains("old.png", result[1].FileName);

            // Cleanup
            Directory.Delete(tempFolder, true);
        }

        [Fact]
        public void RenameFile_RenamesSuccessfully()
        {
            // Arrange
            string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);
            string original = Path.Combine(tempFolder, "test.png");
            File.WriteAllText(original, "data");

            var service = new FileService();

            // Act
            service.RenameFile(original, "renamed");

            // Assert
            Assert.False(File.Exists(original));
            Assert.True(File.Exists(Path.Combine(tempFolder, "renamed.png")));

            // Cleanup
            Directory.Delete(tempFolder, true);
        }

        [Fact]
        public void RunAutoCleanup_DeletesOldFiles_BasedOnCount()
        {
            // Arrange
            string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);

            // Create 5 files
            for (int i = 0; i < 5; i++)
            {
                string p = Path.Combine(tempFolder, $"img{i}.png");
                File.WriteAllText(p, "x");
                // Ensure unique timestamps so sort order is deterministic
                File.SetCreationTimeUtc(p, DateTime.UtcNow.AddMinutes(i));
            }
            // img4 is newest, img0 is oldest

            var service = new FileService();

            // Act: Keep only 3 latest
            service.RunAutoCleanup(tempFolder, 3, 30);

            // Assert
            var files = Directory.GetFiles(tempFolder, "*.png");
            Assert.Equal(3, files.Length);

            // Should have kept img2, img3, img4
            Assert.Contains("img4.png", string.Join(" ", files));
            Assert.Contains("img3.png", string.Join(" ", files));
            Assert.Contains("img2.png", string.Join(" ", files));
            Assert.DoesNotContain("img0.png", string.Join(" ", files));

            // Cleanup
            Directory.Delete(tempFolder, true);
        }
    }
}
