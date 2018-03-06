using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSPrewalker
{
    public interface ICrawlPolicy
    {
        bool Accept(string url);
    }
}
