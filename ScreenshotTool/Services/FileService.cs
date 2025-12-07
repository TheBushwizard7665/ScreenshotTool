using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenshotTool
{
    public class FileService : IFileService
    {
        public List<IMediaItem> LoadMedia(string folderPath, string searchPattern, bool isScreenshot)
        {
            var result = new List<IMediaItem>();
            if (!Directory.Exists(folderPath))
                return result;

            var files = Directory.GetFiles(folderPath, searchPattern, SearchOption.AllDirectories);
            Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));

            foreach (var f in files)
            {
                if (isScreenshot)
                    result.Add(new ScreenshotItem(f));
                else
                    result.Add(new RecordingItem(f));
            }
            return result;
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void MoveFiles(IEnumerable<string> filePaths, string destinationFolder)
        {
            foreach (var path in filePaths)
            {
                if (!File.Exists(path)) continue;

                string fileName = Path.GetFileName(path);
                string destPath = Path.Combine(destinationFolder, fileName);

                if (File.Exists(destPath))
                {
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    string ext = Path.GetExtension(fileName);
                    string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    destPath = Path.Combine(destinationFolder, $"{name}_{stamp}{ext}");
                }

                File.Move(path, destPath);
            }
        }

        public void RenameFile(string currentPath, string newName)
        {
            if (!File.Exists(currentPath)) return;

            string dir = Path.GetDirectoryName(currentPath) ?? "";
            string ext = Path.GetExtension(currentPath);
            string destPath = Path.Combine(dir, newName + ext);

            if (File.Exists(destPath))
                throw new IOException("A file with that name already exists.");

            File.Move(currentPath, destPath);
        }

        public void CreateFolder(string parentFolder, string newFolderName)
        {
            string path = Path.Combine(parentFolder, newFolderName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public void RunAutoCleanup(string folderPath, int maxFileCount, int maxAgeDays)
        {
            if (!Directory.Exists(folderPath)) return;

            // Simple pattern matching for screenshots
            var files = Directory.GetFiles(folderPath, "*.png", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.CreationTimeUtc)
                .ToList();

            // 1. Count-based cleanup
            if (files.Count > maxFileCount)
            {
                foreach (var fi in files.Skip(maxFileCount))
                {
                    try { fi.Delete(); } catch { }
                }
            }

            // 2. Age-based cleanup
            // Re-fetch list or filter remaining? Filter remaining is safer.
            // But we already deleted some. Let's iterate what's left.
            // Actually, best to do age check on all *remaining* files.
            DateTime cutoff = DateTime.Now.AddDays(-maxAgeDays);

            // Note: files list still contains deleted items objects, but we can't delete them again.
            // Ideally we re-scan or just try/catch.
            // Better logic: Filter 'files' that were NOT deleted in step 1.
            var remainingFiles = files.Take(maxFileCount).ToList();

            foreach (var fi in remainingFiles)
            {
                if (fi.CreationTime < cutoff)
                {
                    try { fi.Delete(); } catch { }
                }
            }
        }
    }
}
