using System.IO;

namespace MeatyMod.Core
{
    public class BackupManager
    {
        private readonly string _backupRoot;

        public BackupManager(string backupRoot)
        {
            _backupRoot = backupRoot;
        }

        public void BackupFile(string sourcePath)
        {
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(sourcePath));
            var destPath = Path.Combine(_backupRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            File.Copy(sourcePath, destPath, overwrite: true);
        }

        public void RestoreFile(string gamePath)
        {
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(gamePath));
            var backupPath = Path.Combine(_backupRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(gamePath));
            File.Copy(backupPath, gamePath, overwrite: true);
        }
    }
}