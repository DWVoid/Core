using Config.Net;

namespace Akarin
{
    public static class Config
    {
        private static string Path;
        
        public static void Init(string path)
        {
            Path = System.IO.Path.GetFullPath(path);
        }

        public static T GetPool<T>(string pool) where T: class
        {
            return new ConfigurationBuilder<T>().UseJsonConfig($"{Path}/{pool}.json").Build();
        }
    }
}
