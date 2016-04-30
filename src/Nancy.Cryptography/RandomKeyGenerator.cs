namespace Nancy.Cryptography
{
    using System.Security.Cryptography;

    /// <summary>
    /// Generates random secure keys using RNGCryptoServiceProvider
    /// </summary>
    public class RandomKeyGenerator : IKeyGenerator
    {
        private readonly RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();

        /// <summary>
        /// Generate a sequence of bytes
        /// </summary>
        /// <returns>Returns an array of <paramref name="count"/> bytes</returns>
        /// <returns></returns>
        public byte[] GetBytes(int count)
        {
            var buffer = new byte[count];

            this.provider.GetBytes(buffer);

            return buffer;
        }
    }
}