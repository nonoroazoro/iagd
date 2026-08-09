namespace IAGrim.Services {
    internal sealed class GrimToolsItemMetadata {
        public required string ItemId { get; init; }
        public string? NameTag { get; init; }
        public int? RequiredLevel { get; init; }
        public int? ItemLevel { get; init; }
        public string? ItemClass { get; init; }
        public string? Rarity { get; init; }
        public string? Bitmap { get; init; }
    }
}
