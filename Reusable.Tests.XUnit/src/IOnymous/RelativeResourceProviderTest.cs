using System;
using System.Threading.Tasks;
using Reusable.IOnymous;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;
using Xunit;

namespace Reusable.Tests.XUnit.IOnymous
{
    public class RelativeResourceProviderTest
    {
        [Fact]
        public void Throws_when_base_uri_not_absolute()
        {
            Assert.Throws<ArgumentException>(() => new InMemoryResourceProvider().DecorateWith(RelativeResourceProvider.Factory("blub")));
        }

        [Fact]
        public async Task Creates_new_absolute_uri_when_relative_one_specified()
        {
            var mockProvider = Mock.Create<IResourceProvider>();

            mockProvider
                .Arrange(x => x.Metadata)                
                .Returns(ResourceMetadata.Empty.AddScheme("blub"));

            mockProvider
                .Arrange(x => x.GetAsync(Arg.Matches<UriString>(uri => uri == new UriString("blub:base/relative")), Arg.IsAny<ResourceMetadata>()))
                .Returns<UriString, ResourceMetadata>((uri, metadata) => Task.FromResult<IResourceInfo>(new InMemoryResourceInfo(uri)));
                        

            var relativeProvider = mockProvider.DecorateWith(RelativeResourceProvider.Factory("blub:base"));
            var resource = await relativeProvider.GetAsync("relative");
            
            Assert.False(resource.Exists);
            Assert.Equal("blub:base/relative", resource.Uri);
        }
    }
}