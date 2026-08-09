namespace IAGrim.Services {
    public sealed class GrimToolsBuildResult {
        public GrimToolsBuildResult(IReadOnlyList<string> baseRecords, int equipmentCount, int resolvedEquipmentCount) {
            BaseRecords = baseRecords;
            EquipmentCount = equipmentCount;
            ResolvedEquipmentCount = resolvedEquipmentCount;
        }

        public IReadOnlyList<string> BaseRecords { get; }
        public int EquipmentCount { get; }
        public int ResolvedEquipmentCount { get; }
    }
}
