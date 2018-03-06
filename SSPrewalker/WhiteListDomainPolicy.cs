using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSPrewalker
{
    public class WhiteListDomainPolicy : ICrawlPolicy
    {
        private List<Uri> allowed;

        public WhiteListDomainPolicy(params string[] domains)
        {
            this.allowed = new List<Uri>(domains.Select((d) => new Uri(d)));
        }

        public bool Accept(string url)
        {
            Uri testUrl;
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out testUrl))
            {
                return false;
            }

            return this.allowed.Any((root) => root.IsBaseOf(testUrl));
        }
    }
}
