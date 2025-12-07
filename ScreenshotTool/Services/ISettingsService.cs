using System.Collections.Generic;

namespace ScreenshotTool
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
