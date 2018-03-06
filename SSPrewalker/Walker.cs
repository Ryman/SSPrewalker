using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SSPrewalker
{
    public class Walker
    {
        class RealInternetSource : IHtmlSource
        {
            HttpClient http;

            public RealInternetSource(uint jobs)
            {
                ServicePointManager.DefaultConnectionLimit = (int)jobs;
                this.http = new HttpClient();
            }

            public Task<Stream> GetStreamAsync(string requestUri)
            {
                return this.http.GetStreamAsync(requestUri);
            }
        }

        IHtmlSource htmlSource;
        // TODO: Use core relative value
        uint jobs;

        public Walker(IHtmlSource htmlSource = null, uint jobs = 30)
        {
            this.htmlSource = htmlSource ?? new RealInternetSource(jobs);
            this.jobs = jobs;
        }

        /// <summary>
        /// Returns a Dictionary mapping Urls to pages that they link to.
        /// 
        /// If the page was not fetched successfully then the Value for its key will be null.
        /// </summary>
        /// <param name="root">The root url to start the crawl</param>
        /// <param name="policy">Policy which dictates which urls may be crawled</param>
        /// <returns></returns>
        public async Task<IDictionary<string, List<string>>> CrawlAsync(string root, ICrawlPolicy policy = null)
        {
            // FIXME: Use Uri throughout
            var rootUri = new Uri(root, UriKind.Absolute);
            if (rootUri.GetLeftPart(UriPartial.Authority) != root.TrimEnd('/')
                || rootUri.AbsolutePath != "/") 
            {
                throw new ArgumentException("Must provide a root url with a trailing slash.");
            }

            policy = policy ?? new WhiteListDomainPolicy(root);
            var results = new ConcurrentDictionary<string, List<string>>();

            var initialLinks = await GetLinks(root);
            if (initialLinks == null)
            {
                Debug.Assert(results.TryAdd(root, null));
                return results;
            }

            Debug.Assert(results.TryAdd(root, initialLinks));

            // No point spawning a bunch of threads for this
            if (initialLinks.Count == 0)
            {
                return results;
            }

            var remainingLinks = new ConcurrentBag<string>(initialLinks);

            var tasks = new Task[this.jobs];
            for(var i = 0; i < this.jobs; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    string url;
                    while (!remainingLinks.IsEmpty)
                    {
                        // Skip if we don't want to do this url or we  already have a thread doing
                        // the job
                        if (!remainingLinks.TryTake(out url) || !policy.Accept(url) || !results.TryAdd(url, null))
                        {
                            continue;
                        }

                        // Now we *should* be the only Task trying to get `url`
                        var links = await GetLinks(url);
                            
                        if (links != null)
                        {
                            // Add all the links to the joblist
                            foreach (var link in links)
                            {
                                remainingLinks.Add(link);
                            }
                            Debug.Assert(results.TryUpdate(url, links, null));
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            return results;
        }

        public IDictionary<string, List<string>> Crawl(string root, ICrawlPolicy policy = null)
        {
            var task = CrawlAsync(root, policy);
            task.Wait();
            return task.Result;
        }

        // TODO: Don't swallow the error & allow retrying the connection
        async Task<List<string>> GetLinks(string baseUrl)
        {
            Stream html;
            var doc = new HtmlDocument();

            try
            {
                html = await htmlSource.GetStreamAsync(baseUrl);
                doc.Load(html);
            }
            catch (Exception)
            {
                return null;
            }

            var links = doc.DocumentNode.SelectNodes(@"//a");

            if (links == null)
            {
                return new List<string>();
            }

            return links.Select((anchor) =>
            {
                var href = anchor.GetAttributeValue("href", "#");
                // We don't want the current culture messing with canonicalization.
                href = href.ToLowerInvariant();

                // Cut the fragment off
                return href.Split('#')[0];
            })
            // Ignore fragments and empty links
            .Where((href) => href.Length > 0)
            .Select((href) =>
            {
                // We want to ensure that relative and absolute paths are adjusted
                var baseee = new Uri(baseUrl);
                Uri u = new Uri(baseee, href);
                return u.AbsoluteUri;
            })
            // Ignore multiple links from the same page
            .Distinct()
            .ToList();
        }

    }
}
