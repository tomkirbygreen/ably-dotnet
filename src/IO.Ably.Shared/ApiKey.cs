﻿using System;
using System.Net;
using System.Text.RegularExpressions;

namespace IO.Ably
{
    /// <summary>
    /// Internal class used to parse ApiKeys. The api key has the following parts {keyName}:{KeySecret}
    /// The app and key parts form the KeyId
    /// </summary>
    public class ApiKey
    {
        private static readonly Regex KeyRegex = new Regex(@"^[\w-]{2,}\.[\w-]{2,}:[\w-]{2,}$");

        internal string AppId { get; private set; }

        public string KeyName { get; private set; }

        public string KeySecret { get; private set; }

        public override string ToString()
        {
            return $"{KeyName}:{KeySecret}";
        }

        private ApiKey() { }

        public static ApiKey Parse(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new AblyException("Ably key was empty. Ably key must be in the following format [AppId].[keyId]:[keyValue]", 40101, HttpStatusCode.Unauthorized);
            }

            var trimmedKey = key.Trim();

            if (IsValidFormat(trimmedKey))
            {
                var parts = trimmedKey.Trim().Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                {
                    var keyParts = parts[0].Split(".".ToCharArray());
                    return new ApiKey()
                    {
                        AppId = keyParts[0],
                        KeyName = keyParts[0] + "." + keyParts[1],
                        KeySecret = parts[1]
                    };
                }
            }

            throw new AblyException("Invalid Ably key. Ably key must be in the following format [AppId].[keyId]:[keyValue]", 40101, HttpStatusCode.Unauthorized);
        }

        internal static bool IsValidFormat(string key)
        {
            return KeyRegex.Match(key.Trim()).Success;
        }
    }
}
