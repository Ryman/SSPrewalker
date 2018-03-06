using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSPrewalker;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;

namespace SSPrewalkerTests
{
    [TestClass]
    public class WalkerTests
    {
        class TestHtmlSource : IHtmlSource
        {
            public Dictionary<string, string> mappings = new Dictionary<string, string>();

            Task<Stream> IHtmlSource.GetStreamAsync(string requestUri)
            {
                var html = mappings[requestUri];
                var bytes = Encoding.UTF8.GetBytes(html);

                return Task.FromResult((Stream) new MemoryStream(bytes));
            }
        }

        class RejectAllPolicy : ICrawlPolicy
        {
            public bool Accept(string url)
            {
                return false;
            }
        }

        [TestMethod]
        public void HandlesNoLinks()
        {
            var root = @"http://foo.bar/";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, @"<html></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(0, results[root].Count);
        }

        [TestMethod]
        public void HandlesSelfLinks()
        {
            var root = @"http://foo.bar/";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href={root}></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);
            
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(root, results[root][0]);
        }

        [TestMethod]
        public void SkipsForeignContent()
        {
            var root = @"http://foo.bar/";
            var foreign = "http://example.com/";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href='{foreign}'></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(foreign, results[root][0]);
        }

        [TestMethod]
        public void FollowsLinksUnderRootDomain()
        {
            var root = @"http://foo.bar/";
            var other = @"http://foo.bar/other";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href='{other}'></html>");
            htmlSource.mappings.Add(other, "<html />");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Console.WriteLine(string.Join(",", results.Keys));
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(other, results[root][0]);
            Assert.AreEqual(0, results[other].Count);
        }

        [TestMethod]
        public void HandlesBrokenLinks()
        {
            var root = @"http://foo.bar/";
            var other = @"http://foo.bar/other";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href='{other}'></html>");
            /* No content for other */

            var w = new Walker(htmlSource);
            var results = w.Crawl(root);
            Assert.AreEqual(results[other], null);
        }

        [TestMethod]
        public void HandlesBrokenRoot()
        {
            var root = @"http://foo.bar/";
            var htmlSource = new TestHtmlSource();
            /* No content for root */

            var w = new Walker(htmlSource);
            var results = w.Crawl(root);
            Assert.AreEqual(results[root], null);
        }

        [TestMethod]
        public void NoInfiniteLoops()
        {
            var root = @"http://foo.bar/";
            var other = @"http://foo.bar/other";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href={other}></html>");
            htmlSource.mappings.Add(other, $@"<html><a href={root}></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("http://foo.bar/other", results[root][0]);
            Assert.AreEqual("http://foo.bar/", results[other][0]);
        }

        [TestMethod]
        public void FollowsRelativePaths()
        {
            var root = @"http://foo.bar/";
            var b = @"http://foo.bar/baz/aca";
            var c = @"http://foo.bar/baz/other";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href='{b}'></html>");
            htmlSource.mappings.Add(b, $@"<html><a href='other'></html>");
            htmlSource.mappings.Add(c, $@"<html></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(c, results[b][0]);
        }

        [TestMethod]
        public void RequireRootUrl()
        {
            var root = @"http://foo.bar/notaroot";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html></html>");
            var w = new Walker(htmlSource);

            Assert.ThrowsException<AggregateException>(() => w.Crawl(root));
        }

        [TestMethod]
        public void FollowsAbsolutePaths()
        {
            var root = @"http://foo.bar/";
            var b = @"http://foo.bar/baz/aca";
            var c = @"http://foo.bar/other";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href='{b}'></html>");
            htmlSource.mappings.Add(b, $@"<html><a href='/other'></html>");
            htmlSource.mappings.Add(c, $@"<html></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(c, results[b][0]);
        }

        [TestMethod]
        public void Canonicalization()
        {
            var root = @"http://foo.bar/";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href='{root.TrimEnd('/')}'></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(root, results[root][0]);
        }

        [TestMethod]
        public void CustomPolicy()
        {
            var root = @"http://foo.bar/";
            var other = @"http://foo.bar/other";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href={other}><a href={root}></html>");
            htmlSource.mappings.Add(other, $@"<html><a href={root}></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root, new RejectAllPolicy());

            // Only includes the root page, no further crawling
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(2, results[root].Count);
        }

        [TestMethod]
        public void IgnoresFragments()
        {
            var root = @"http://foo.bar/";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href='#boom'></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(0, results[root].Count);
        }

        [TestMethod]
        public void IgnoresEmpty()
        {
            var root = @"http://foo.bar";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href=''></html>");
            var w = new Walker(htmlSource);
            var results = w.Crawl(root);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(0, results[root].Count);
        }

        [TestMethod]
        public void DeduplicatedResults()
        {
            var root = @"http://foo.bar";
            var htmlSource = new TestHtmlSource();
            htmlSource.mappings.Add(root, $@"<html><a href='{root}'><a href='{root}'><a href='{root}'></html>");
            /* No content for other */

            var w = new Walker(htmlSource);
            var results = w.Crawl(root);
            Assert.AreEqual(1, results[root].Count);
        }
    }
}
