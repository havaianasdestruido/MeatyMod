namespace MeatyMod.Core
{
    public static class FileSizeGuard
    {
        public const long DefaultMaxBytes = 100L * 1024 * 1024;

        public static bool IsAllowed(long length)
        {
            return IsAllowed(length, DefaultMaxBytes);
        }

        public static bool IsAllowed(long length, long maxBytes)
        {
            if (maxBytes <= 0)
            {
                maxBytes = DefaultMaxBytes;
            }

            return length <= maxBytes;
        }
    }
}
