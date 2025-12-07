using System.IO;
using ScreenshotTool.Services;
using Xunit;

namespace ScreenshotTool.Tests
{
    public class SettingsServiceTests
    {
        [Fact]
        public void Load_ReturnsDefaultSettings_WhenFileDoesNotExist()
        {
            // Arrange
            string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);
            var service = new SettingsService(tempFolder);

            // Act
            var settings = service.Load();

            // Assert
            Assert.NotNull(settings);
            Assert.False(settings.CompactMode); // Default check

            // Cleanup
            Directory.Delete(tempFolder, true);
        }

        [Fact]
        public void Save_And_Load_PersistsSettings()
        {
            // Arrange
            string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);
            var service = new SettingsService(tempFolder);

            var settingsToSave = new AppSettings
            {
                CompactMode = true,
                MaxFileCount = 42
            };

            // Act
            service.Save(settingsToSave);
            var loadedSettings = service.Load();

            // Assert
            Assert.True(loadedSettings.CompactMode);
            Assert.Equal(42, loadedSettings.MaxFileCount);

            // Cleanup
            Directory.Delete(tempFolder, true);
        }
    }
}
