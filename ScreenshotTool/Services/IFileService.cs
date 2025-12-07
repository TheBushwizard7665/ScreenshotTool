using System.Collections.Generic;

namespace ScreenshotTool
{
    public interface IFileService
    {
        List<IMediaItem> LoadMedia(string folderPath, string searchPattern, bool isScreenshot);
        void DeleteFiles(IEnumerable<string> filePaths);
        void MoveFiles(IEnumerable<string> filePaths, string destinationFolder);
        void RenameFile(string currentPath, string newName);
        void CreateFolder(string parentFolder, string newFolderName);
        void RunAutoCleanup(string folderPath, int maxFileCount, int maxAgeDays);
    }
}
