namespace NAnt.VisualCpp.Types {
    /// <summary>
    /// Defines the character sets that can be used by the C++ compiler.
    /// </summary>
    public enum CharacterSet {
        /// <summary>
        /// Have the compiler determine the character set.
        /// </summary>
        NotSet = 0,

        /// <summary>
        /// Unicode character set.
        /// </summary>
        Unicode = 1,

        /// <summary>
        /// Multi-byte character set.
        /// </summary>
        MultiByte = 2
    }
}
