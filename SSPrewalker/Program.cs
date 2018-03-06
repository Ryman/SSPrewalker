using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSPrewalker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SSPrewalker.exe <URL> <parallelism>");
                Console.ReadLine();
                return;
            }

            var parallelism = 200u;
            if (args.Length == 2) {
                parallelism = UInt32.Parse(args[1]);
            }
            var walker = new Walker(null, parallelism);
            var results = walker.Crawl(args[0]);

            var linksFound = 0;
            var pagesFailed = 0;

            foreach (var page in results.Keys)
            {
                var links = results[page];

                if (links == null)
                {
                    Console.WriteLine($"{page}: Failed to load");
                    pagesFailed += 1;
                    continue;
                }
                
                Console.WriteLine($"{page}: {links.Count} links");

                foreach(var link in links)
                {
                    Console.WriteLine($"\t{link}");
                }

                linksFound += links.Count;
            }

            Console.Error.WriteLine($"Crawled {results.Count} pages and found {linksFound} links and had {pagesFailed} failed page loads.");

            Console.Read();
        }
    }
}
