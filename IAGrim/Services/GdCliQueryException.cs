namespace IAGrim.Services {
    internal sealed class GdCliQueryException : Exception {
        public GdCliQueryException(string message) : base(message) {
        }

        public GdCliQueryException(string message, Exception innerException)
            : base(message, innerException) {
        }
    }
}
