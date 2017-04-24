namespace Nancy.Responses.Negotiation
{
    using System;
    using System.Linq;

    /// <summary>
    /// Represents a media range from an accept header
    /// </summary>
    public class MediaRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaRange"/> class from a string representation of a media range
        /// </summary>
        /// <param name="contentType">the content type</param>
        public MediaRange(string contentType) : this()
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentException("inputString cannot be null or empty", contentType);
            }

            if (contentType.Equals("*"))
            {
                contentType = "*/*";
            }

            var parts = contentType.Split('/', ';');

            if (parts.Length < 2)
            {
                {
                    throw new ArgumentException("inputString not in correct Type/SubType format", contentType);
                }
            }

            this.Type = parts[0];
            this.Subtype = parts[1].TrimEnd();

            if (parts.Length > 2)
            {
                var separator = contentType.IndexOf(';');
                this.Parameters = MediaRangeParameters.FromString(contentType.Substring(separator));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaRange"/> class.
        /// </summary>
        public MediaRange()
        {
            this.Parameters = new MediaRangeParameters();
        }

        /// <summary>
        /// Media range type
        /// </summary>
        public MediaType Type { get; private set; }

        /// <summary>
        /// Media range subtype
        /// </summary>
        public MediaType Subtype { get; private set; }

        /// <summary>
        /// Media range parameters
        /// </summary>
        public MediaRangeParameters Parameters { get; private set; }

        /// <summary>
        /// Gets a value indicating if the media range is the */* wildcard
        /// </summary>
        public bool IsWildcard
        {
            get
            {
                var matches = this.Type.IsWildcard && this.Subtype.IsWildcard;
                return matches;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the media type is a vendor tree type or not
        /// </summary>
        /// <value><see langword="true" /> if the media type is a vendor tree type, otherwise <see langword="false" />.</value>
        public bool IsVendorTreeType
        {
            get
            {
                return this.Type != null && ((string)this.Subtype).StartsWith("vnd.", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the vendor tree name, returns an empty string, if this is not a vendor tree type.
        /// </summary>
        /// <value><see langword="true" /> if the media type is a vendor tree type, otherwise <see langword="false" />.</value>
        public string VendorTreeName
        {
            get
            {
                if (!this.IsVendorTreeType)
                {
                    return "";
                }

                var subType = (string)this.Subtype;
                var vnd = subType.IndexOf("vnd.", StringComparison.Ordinal) + 4;
                var plus = subType.IndexOf('+', vnd);
                var vendorTreeName = subType.Substring(vnd, plus - vnd);

                return vendorTreeName;
            }
        }

        /// <summary>
        /// Returns the media type suffix (the [+suffix]), which will be empty where there is no suffix
        /// </summary>
        /// <example>"application/xhtml+xml", "image/png will return "+xml" and "" respectively</example>
        public string Suffix
        {
            get
            {
                var subType = (string) this.Subtype;
                var plus = subType.IndexOf('+');

                return plus > 0 ? subType.Substring(plus) : "";
            }
        }

        /// <summary>
        /// Whether or not a media range matches another, taking into account wildcards
        /// </summary>
        /// <param name="other">Other media range</param>
        /// <returns>True if matching, false if not</returns>
        public bool Matches(MediaRange other)
        {
            var matches = this.Type.Matches(other.Type) && this.Subtype.Matches(other.Subtype);
            return matches;
        }

        /// <summary>
        /// Whether or not a media range matches another and whether the parameters match.
        /// </summary>
        /// <param name="other">Other media range</param>
        /// <returns>True if matching, false if not</returns>
        public bool MatchesWithParameters(MediaRange other)
        {
            var matches = this.Matches(other) && this.Parameters.Matches(other.Parameters);
            return matches;
        }

        /// <summary>
        /// Whether or not a media range matches another exactly on type, subtype and parameters.
        /// </summary>
        /// <param name="other">Other media range</param>
        /// <returns>True if matching, false if not</returns>
        public bool MatchesExactlyWithParameters(MediaRange other)
        {
            var matches = this.MatchesExactly(other) && this.Parameters.Matches(other.Parameters);
            return matches;
        }

        /// <summary>
        /// Whether or not a media range matches another exactly on type and subtype.
        /// </summary>
        /// <param name="other">Other media range</param>
        /// <returns>True if matching, false if not</returns>
        public bool MatchesExactly(MediaRange other)
        {
            var matches = this.Type.MatchesExactly(other.Type) && this.Subtype.MatchesExactly(other.Subtype);
            return matches;
        }

        /// <summary>
        /// Creates a MediaRange from a "Type/SubType" string
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        [Obsolete("Please use the constructor")]
        public static MediaRange FromString(string contentType)
        {
            return new MediaRange(contentType);
        }

        public static implicit operator MediaRange(string contentType)
        {
            return new MediaRange(contentType);
        }

        public static implicit operator string(MediaRange mediaRange)
        {
            if (mediaRange.Parameters.Any())
            {
                return string.Format("{0}/{1};{2}", mediaRange.Type, mediaRange.Subtype, mediaRange.Parameters);
            }

            return string.Format("{0}/{1}", mediaRange.Type, mediaRange.Subtype);
        }

        ///// <summary>
        ///// Compares the current object with another object of the same type.
        ///// </summary>
        ///// <returns>
        ///// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>.
        ///// </returns>
        ///// <param name="other">An object to compare with this object.</param>
        //public int CompareTo(MediaRange other)
        //{
        //	if (this.MatchesExactlyWithParameters(other))
        //	{
        //		return 0;
        //	}

        //	if (this.IsWildcard && !other.IsWildcard)
        //	{
        //		return -1;
        //	}

        //	if (this.Type.IsWildcard)
        //	{
        //		if (other.Type.IsWildcard)
        //		{

        //		}
        //	}
        //}

        public override string ToString()
        {
            return this;
        }
    }
}
