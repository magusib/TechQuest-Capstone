namespace TechQuestBackend.Services
{
    public static class NpcImageStorage
    {
        public static string GetFolder(IWebHostEnvironment environment)
        {
            string folder = Path.GetFullPath(Path.Combine(
                environment.ContentRootPath,
                "..",
                "TechQuestFrontend",
                "Items",
                "NPC"));
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
