using System.IO;

namespace MiscTools
{
    public static class Utilities
    {
        public static long DirectorySize(DirectoryInfo dirInfo)
        {
            if (!dirInfo.Exists)
                return 0;

            long size = 0;

            // Add file sizes
            foreach (FileInfo fileInfo in dirInfo.GetFiles())
                size += fileInfo.Length;

            // Add subdirectory sizes
            foreach (DirectoryInfo directoryInfo in dirInfo.GetDirectories())
                size += DirectorySize(directoryInfo);

            return size;
        }
    }
}
