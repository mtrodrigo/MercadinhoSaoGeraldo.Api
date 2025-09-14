using System;
using System.IO;
using System.Threading.Tasks;
using Supabase;

namespace MercadinhoSaoGeraldo.Api.Storage
{
    public class SupabaseStorageService
    {
        private readonly Client _client;
        private readonly string _bucket;

        public SupabaseStorageService(string url, string serviceKey, string bucket)
        {
            var options = new SupabaseOptions { AutoConnectRealtime = false };
            _client = new Client(url, serviceKey, options);
            _bucket = bucket;
        }

        public async Task<string> UploadProductImageAsync(string fileName, Stream stream, string contentType)
        {
            await _client.InitializeAsync();
            var storage = _client.Storage;

            if (stream.CanSeek)
                stream.Position = 0;

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var path = $"products/{fileName}";

            await storage.From(_bucket).Upload(bytes, path, new Supabase.Storage.FileOptions
            {
                ContentType = contentType,
                Upsert = true
            });

            return storage.From(_bucket).GetPublicUrl(path);

        }
    }
}
