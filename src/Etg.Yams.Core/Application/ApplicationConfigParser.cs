using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Etg.Yams.Install;
using Etg.Yams.Json;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Application
{
    public class ApplicationConfigParser : IApplicationConfigParser
    {
        private readonly IApplicationConfigSymbolResolver _symbolResolver;
        private readonly IJsonSerializer _jsonSerializer;

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ApplicationConfigData
        {
#pragma warning disable 649
            public string ExeName;
            public string ExeArgs;
#pragma warning restore 649
        }

        public ApplicationConfigParser(IApplicationConfigSymbolResolver symbolResolver, IJsonSerializer jsonSerializer)
        {
            _symbolResolver = symbolResolver;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<ApplicationConfig> ParseFile(string path, AppInstallConfig appInstallConfig)
        {
            using (StreamReader r = new StreamReader(path))
            {
                return await Parse(await _jsonSerializer.DeserializeAsync<ApplicationConfigData>(await r.ReadToEndAsync()), appInstallConfig);
            }
        }

        private async Task<ApplicationConfig> Parse(ApplicationConfigData appConfigData, AppInstallConfig appInstallConfig)
        {
            string args = await SubstituteSymbols(appConfigData.ExeArgs, appInstallConfig);
            return new ApplicationConfig(appInstallConfig.AppIdentity, appConfigData.ExeName, args);
        }

        private async Task<string> SubstituteSymbols(string str, AppInstallConfig appInstallConfig)
        {
            ISet<string> symbols = new HashSet<string>();
            const string pattern = @"\{(.*?)\}";
            foreach (Match m in Regex.Matches(str, pattern))
            {
                symbols.Add(m.Groups[1].ToString());
            }

            foreach (string symbol in symbols)
            {
                str = await SubstitueSymbol(str, symbol, appInstallConfig);
            }
            return str;
        }

        private async Task<string> SubstitueSymbol(string str, string symbol, AppInstallConfig appInstallConfig)
        {
            string symbolValue = await _symbolResolver.ResolveSymbol(appInstallConfig, symbol);
            return str.Replace(string.Format("${{{0}}}", symbol), symbolValue);
        }
    }
}
