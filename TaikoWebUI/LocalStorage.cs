// LocalStorage.cs
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Microsoft.JSInterop;
namespace TaikoWebUI
{

    public interface ILocalStorage
    {
        /// <summary>
        /// Remove a key from browser local storage.
        /// </summary>
        /// <param name="key">The key previously used to save to local storage.</param>
        public Task RemoveAsync(string key);

        /// <summary>
        /// Save a string value to browser local storage.
        /// </summary>
        /// <param name="key">The key to use to save to and retrieve from local storage.</param>
        /// <param name="value">The string value to save to local storage.</param>
        public Task SaveStringAsync(string key, string value);

        /// <summary>
        /// Get a string value from browser local storage.
        /// </summary>
        /// <param name="key">The key previously used to save to local storage.</param>
        /// <returns>The string previously saved to local storage.</returns>
        public Task<string> GetStringAsync(string key);

        /// <summary>
        /// Save an array of string values to browser local storage.
        /// </summary>
        /// <param name="key">The key previously used to save to local storage.</param>
        /// <param name="values">The array of string values to save to local storage.</param>
        public Task SaveStringArrayAsync(string key, string[] values);

        /// <summary>
        /// Get an array of string values from browser local storage.
        /// </summary>
        /// <param name="key">The key previously used to save to local storage.</param>
        /// <returns>The array of string values previously saved to local storage.</returns>
        public Task<string[]> GetStringArrayAsync(string key);
    }

    public class LocalStorage : ILocalStorage
    {
        private readonly IJSRuntime jsruntime;
        public LocalStorage(IJSRuntime jSRuntime)
        {
            jsruntime = jSRuntime;
        }

        public async Task RemoveAsync(string key)
        {
            await jsruntime.InvokeVoidAsync("localStorage.removeItem", key).ConfigureAwait(false);
        }

        public async Task SaveStringAsync(string key, string value)
        {
            var compressedBytes = await Compressor.CompressBytesAsync(Encoding.UTF8.GetBytes(value));
            await jsruntime.InvokeVoidAsync("localStorage.setItem", key, Convert.ToBase64String(compressedBytes)).ConfigureAwait(false);
        }

        public async Task<string> GetStringAsync(string key)
        {
            var str = await jsruntime.InvokeAsync<string>("localStorage.getItem", key).ConfigureAwait(false);
            if (str == null)
                return "";
            var bytes = await Compressor.DecompressBytesAsync(Convert.FromBase64String(str));
            return Encoding.UTF8.GetString(bytes);
        }

        public async Task SaveStringArrayAsync(string key, string[] values)
        {
            await SaveStringAsync(key, values == null ? "" : string.Join('\0', values));
        }

        public async Task<string[]> GetStringArrayAsync(string key)
        {
            var data = await GetStringAsync(key);
            if (!string.IsNullOrEmpty(data))
                return data.Split('\0');
            return Array.Empty<string>();
        }
    }

    internal class Compressor
    {
        public static async Task<byte[]> CompressBytesAsync(byte[] bytes)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    await compressionStream.WriteAsync(bytes, 0, bytes.Length);
                }
                return outputStream.ToArray();
            }
        }

        public static async Task<byte[]> DecompressBytesAsync(byte[] bytes)
        {
            using (var inputStream = new MemoryStream(bytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        await compressionStream.CopyToAsync(outputStream);
                    }
                    return outputStream.ToArray();
                }
            }
        }
    }
}
