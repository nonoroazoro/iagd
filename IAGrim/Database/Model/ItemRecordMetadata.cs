namespace IAGrim.Database.Model {
    public sealed class ItemRecordMetadata {
        public string RecordId { get; set; } = string.Empty;
        public string? NameTag { get; set; }
        public double? RequiredLevel { get; set; }
        public double? ItemLevel { get; set; }
        public string? ItemClass { get; set; }
        public string? Rarity { get; set; }
        public string? Bitmap { get; set; }
    }
}
