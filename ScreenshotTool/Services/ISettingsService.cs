using System.Collections.Generic;

namespace ScreenshotTool.Services
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
