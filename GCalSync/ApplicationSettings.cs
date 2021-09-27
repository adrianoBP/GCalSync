using Google.Apis.Calendar.v3;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace GCalSync
{
    public class ApplicationSettings
    {
        public static readonly string Prefix = "[University] - ";
        public static readonly string ApplicationName = "GCalsSync";
        public static readonly string[] CalendarScopes = new[] { CalendarService.Scope.Calendar };

        public const int MAX_NUMBER_OF_EVENTS = 5;

        private static IConfiguration configuration;

        public static void Init(IConfiguration config)
        {
            configuration = config;
        }

        public static List<string> FromAccountIdsSync
        {
            get { return Setting<List<string>>(configuration.GetSection("FromAccountIdsSync")); }
        }
        public static string ToAccountIdSync
        {
            get { return Setting<string>(configuration.GetSection("ToAccountIdSync")); }
        }

        public static T Setting<T>(IConfigurationSection section)
        {
            T appSettings = section.Get<T>();
            return appSettings;
        }
    };

   
}
