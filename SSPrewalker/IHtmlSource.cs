using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSPrewalker
{
    public interface IHtmlSource
    {
        Task<Stream> GetStreamAsync(string requestUri);
    }
}
