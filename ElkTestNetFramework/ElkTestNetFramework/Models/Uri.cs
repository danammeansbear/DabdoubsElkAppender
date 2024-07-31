using System;
using System.Collections.Specialized;
using ElkTestNetFramework.Infrastructure;

#if NETFRAMEWORK
using System.Web;
#else
using System.Net;
#endif          

namespace ElkTestNetFramework.Models
{
    public class Uri
    {
        private static string indexNameDateFormat;

        readonly StringDictionary connectionStringParts;

        public string UserInfo { get; private set; }

        static Uri()
        {
            indexNameDateFormat = "yyyy.MM.dd";
        }

        public static void Init(string rollingIndexNameDateFormat)
        {
            indexNameDateFormat = rollingIndexNameDateFormat;
        }

        Uri(StringDictionary connectionStringParts)
        {
            this.connectionStringParts = connectionStringParts;
            UserInfo = CreateUserInfo();
        }

        private string CreateUserInfo()
        {
            var user = connectionStringParts[Keys.User];
            var password = connectionStringParts[Keys.Password];
            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
            {
#if NETFRAMEWORK
                return $"{HttpUtility.UrlEncode(user)}:{HttpUtility.UrlEncode(password)}";
#else
                return $"{WebUtility.UrlEncode(user)}:{WebUtility.UrlEncode(password)}";
#endif
            }
            return null;
        }

        public static implicit operator System.Uri(Uri uri)
        {
            string userInfo = uri.UserInfo;
            if (!string.IsNullOrWhiteSpace(userInfo))
            {
                return new System.Uri($"{uri.Scheme()}://{userInfo}@{uri.Server()}:{uri.Port()}/{uri.Index()}{uri.Routing()}{uri.Bulk()}");
            }
            return string.IsNullOrEmpty(uri.Port())
                ? new System.Uri($"{uri.Scheme()}://{uri.Server()}/{uri.Index()}{uri.Routing()}{uri.Bulk()}")
                : new System.Uri($"{uri.Scheme()}://{uri.Server()}:{uri.Port()}/{uri.Index()}{uri.Routing()}{uri.Bulk()}");
        }

        public static Uri For(string connectionString)
        {
            return new Uri(connectionString.ConnectionStringParts());
        }

        string User()
        {
            return connectionStringParts[Keys.User];
        }

        string Password()
        {
            return connectionStringParts[Keys.Password];
        }

        string Scheme()
        {
            return connectionStringParts[Keys.Scheme] ?? "http";
        }

        string Server()
        {
            return connectionStringParts[Keys.Server];
        }

        string Port()
        {
            return connectionStringParts[Keys.Port];
        }

        string Routing()
        {
            var routing = connectionStringParts[Keys.Routing];
            if (!string.IsNullOrWhiteSpace(routing))
            {
                return string.Format("?routing={0}", routing);
            }

            return string.Empty;
        }

        string Doc()
        {
            var bufferSize = connectionStringParts[Keys.BufferSize];
            if (Convert.ToInt32(bufferSize) > 1)
            {
                return "/_bulk";
            }
            else
            {
                return "/_doc";
            }
        }

        string Bulk()
        {
            var bufferSize = connectionStringParts[Keys.BufferSize];
            if (Convert.ToInt32(bufferSize) > 1)
            {
                return "/_bulk";
            }
            else
            {
                return "/_doc";
            }
        }

        public string Index()
        {
            var index = connectionStringParts[Keys.Index];

            return IsRollingIndex(connectionStringParts)
                       ? $"{index}-{Clock.Date.ToString(indexNameDateFormat)}"
                       : index;
        }

        static bool IsRollingIndex(StringDictionary parts)
        {
            return parts.Contains(Keys.Rolling) && parts[Keys.Rolling].ToBool();
        }

        private static class Keys
        {
            public const string Scheme = "Scheme";
            public const string User = "User";
            public const string Password = "Pwd";
            public const string Server = "Server";
            public const string Port = "Port";
            public const string Index = "Index";
            public const string Rolling = "Rolling";
            public const string BufferSize = "BufferSize";
            public const string Routing = "Routing";
        }
    }
}
