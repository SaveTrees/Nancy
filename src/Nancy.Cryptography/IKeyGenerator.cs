namespace Nancy.Cryptography
{
    /// <summary>
    /// Provides key byte generation
    /// </summary>
    public interface IKeyGenerator
    {
        /// <summary>
        /// Generate a sequence of bytes
        /// </summary>
        /// <param name="count">Number of bytes to return</param>
        /// <returns>Returns an array of <paramref name="count"/> bytes</returns>
        byte[] GetBytes(int count);
    }
}