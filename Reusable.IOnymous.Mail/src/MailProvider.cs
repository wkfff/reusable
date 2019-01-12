using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Reusable.Exceptionizer;

namespace Reusable.IOnymous
{
    public abstract class MailProvider : ResourceProvider
    {
        public new static readonly string DefaultScheme = "mailto";

        protected MailProvider(ResourceMetadata metadata)
            : base(new SoftString[] { DefaultScheme }, metadata)
        {
        }

        protected async Task<string> ReadBodyAsync(Stream value, ResourceMetadata metadata)
        {
            using (var bodyReader = new StreamReader(value, metadata.Scope<MailProvider>().BodyEncoding()))
            {
                return await bodyReader.ReadToEndAsync();
            }
        }
    }

    internal class MailResourceInfo : ResourceInfo
    {
        private readonly Stream _response;

        public MailResourceInfo([NotNull] UriString uri, Stream response, MimeType format)
            : base(uri, format)
        {
            _response = response;
        }

        public override bool Exists => !(_response is null);

        public override long? Length => _response?.Length;

        public override DateTime? CreatedOn { get; }

        public override DateTime? ModifiedOn { get; }

        protected override async Task CopyToAsyncInternal(Stream stream)
        {
            await _response.Rewind().CopyToAsync(stream);
        }

        public override void Dispose()
        {
            _response.Dispose();
        }
    }    
}