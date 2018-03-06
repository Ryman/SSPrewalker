using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSPrewalker;

namespace SSPrewalkerTests
{
    [TestClass]
    public class PolicyTests
    {
        WhiteListDomainPolicy policy = new WhiteListDomainPolicy("http://foo.bar");

        [TestMethod]
        public void SameDomain()
        {
            Assert.IsTrue(policy.Accept("http://foo.bar/baz"));
        }

        [TestMethod]
        public void RelativeUrl()
        {
            Assert.IsTrue(policy.Accept("bar/baz"));
        }

        [TestMethod]
        public void Subdomain()
        {
            var policy = new WhiteListDomainPolicy("http://qux.foo.bar", "http://foo.bar");
            Assert.IsTrue(policy.Accept("http://qux.foo.bar"));
        }

        [TestMethod]
        public void MultipleSchemes()
        {
            var policy = new WhiteListDomainPolicy("https://foo.bar", "http://foo.bar");
            Assert.IsTrue(policy.Accept("https://foo.bar/aza"));
            Assert.IsTrue(policy.Accept("http://foo.bar/aza"));
        }

        [TestMethod]
        public void RejectsForeignDomains()
        {
            Assert.IsFalse(policy.Accept("http://bing.bar/baz"));
        }

        [TestMethod]
        [Ignore]
        // FIXME: This is probably being too permissive by allowing these as relative urls!
        public void NotAUrl()
        {
            var badurl = "foo\nbar";
            // Sanitycheck
            Assert.ThrowsException<UriFormatException>(() => new Uri(badurl, UriKind.RelativeOrAbsolute));
            Assert.IsFalse(policy.Accept(badurl));
        }
    }
}
