namespace SportConnect.API.Services
{
    public static class CacheKeys
    {
        public static string UserSports(Guid userId) => $"user:{userId}:sports";
        public static string AllSports() => "sports:all";
        public static string UserProfile(Guid userId) => $"user:{userId}:profile";
    }
}
