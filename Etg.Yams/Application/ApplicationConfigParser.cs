using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Etg.Yams.Utils;

namespace Etg.Yams.Application
{
    public class ApplicationConfigParser : IApplicationConfigParser
    {
        private readonly IApplicationConfigSymbolResolver _symbolResolver;

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ApplicationConfigData
        {
#pragma warning disable 649
            public string ExeName;
            public string ExeArgs;
#pragma warning restore 649
        }

        public ApplicationConfigParser(IApplicationConfigSymbolResolver symbolResolver)
        {
            _symbolResolver = symbolResolver;
        }

        public async Task<ApplicationConfig> ParseFile(string path, AppIdentity identity)
        {
            return await Parse(await JsonUtils.ParseFile<ApplicationConfigData>(path), identity);
        }

        private async Task<ApplicationConfig> Parse(ApplicationConfigData appConfigData, AppIdentity identity)
        {
            string id = identity.Id;
            Version version = new Version(identity.Version.ToString());
            string args = await SubstituteSymbols(appConfigData.ExeArgs, identity);

            return new ApplicationConfig(new AppIdentity(id, version), appConfigData.ExeName, args);
        }

        private async Task<string> SubstituteSymbols(string str, AppIdentity appIdentity)
        {
            ISet<string> symbols = new HashSet<string>();
            const string pattern = @"\{(.*?)\}";
            foreach (Match m in Regex.Matches(str, pattern))
            {
                symbols.Add(m.Groups[1].ToString());
            }

            foreach (string symbol in symbols)
            {
                str = await SubstitueSymbol(str, symbol, appIdentity);
            }
            return str;
        }

        private async Task<string> SubstitueSymbol(string str, string symbol, AppIdentity appIdentity)
        {
            string symbolValue = await _symbolResolver.ResolveSymbol(appIdentity, symbol);
            return str.Replace(string.Format("${{{0}}}", symbol), symbolValue);
        }
    }
}
