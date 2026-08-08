using IAGrim.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using IAGrim.Database;
using IAGrim.Database.Dto;

namespace IAGrim.Services {
    class ItemPaginationService {
        private readonly int _limit;
        private readonly Comparison<List<PlayerHeldItem>> _comparer;

        private int _skip;
        private ItemSortMode _sortMode;
        private List<List<PlayerHeldItem>> _items = new List<List<PlayerHeldItem>>();

        /// <summary>
        /// Total item count after aggregation
        /// </summary>
        public int NumItems => _items.Count;

        /// <summary>
        /// Total item count before aggregation, across every DB page (not just what's currently buffered).
        /// </summary>
        public int NumTotalItems { get; private set; }

        private int Remaining {
            get {
                return Math.Min(_limit, NumItems - _skip);
            }
        }

        /// <summary>
        /// True once every buffered (in-memory) item has been served to the UI. When this is true and
        /// the DB still has further pages, the caller should fetch and Append the next DB batch.
        /// </summary>
        public bool BufferExhausted => _skip >= NumItems;


        public ItemPaginationService(int limit) {
            this._limit = limit;


            this._comparer = (a, b) => a?[0].Name?.CompareTo(b?[0]?.Name) ?? 0;

            // Keep batches aligned to complete four-column rows on high resolutions.
            System.Diagnostics.Debug.Assert(limit % 4 == 0);
        }

        private int CompareToMinimumLevel(List<PlayerHeldItem> itemA, List<PlayerHeldItem> itemB) {
            if (itemA != null && itemB != null) {
                var order = itemA[0].MinimumLevel.CompareTo(itemB[0].MinimumLevel);
                if (order == 0) {
                    return itemA[0].CompareTo(itemB[0]);
                }
                else {
                    return order;
                }
            }

            return 0;
        }

        private int CompareToQuantity(List<PlayerHeldItem> itemA, List<PlayerHeldItem> itemB) {
            var playerA = itemA[0] as PlayerItem;
            var playerB = itemB[0] as PlayerItem;
            if (playerA == null || playerB == null) {
                if (playerA != null) {
                    return -1;
                }

                if (playerB != null) {
                    return 1;
                }

                return _comparer(itemA, itemB);
            }

            var countA = playerA.DuplicateCount;
            var countB = playerB.DuplicateCount;
            var order = countB.CompareTo(countA);
            if (order != 0) {
                return order;
            }

            order = string.Compare(playerA.DuplicateIdentity, playerB.DuplicateIdentity, StringComparison.Ordinal);
            return order != 0 ? order : _comparer(itemA, itemB);
        }

        private Comparison<List<PlayerHeldItem>> GetComparer() {
            return _sortMode switch {
                ItemSortMode.Level => CompareToMinimumLevel,
                ItemSortMode.Quantity => CompareToQuantity,
                _ => _comparer
            };
        }

        public bool Update(List<List<PlayerHeldItem>> items, ItemSortMode sortMode, int numTotalItems) {
            this._skip = 0;
            this._sortMode = sortMode;
            this._items = items;
            _items.Sort(GetComparer());
            this.NumTotalItems = numTotalItems;
            return true;
        }

        /// <summary>
        /// Append a freshly fetched DB page to the buffer without disturbing how far the UI has already
        /// scrolled (_skip). Only the not-yet-served tail ([_skip..end)) is (re)sorted, so items the UI
        /// has already rendered are never reordered away (which would otherwise skip/duplicate rows if
        /// the SQLite and .NET orderings disagree). MergeStackSize does not preserve order, hence the sort.
        /// </summary>
        public void Append(List<List<PlayerHeldItem>> items) {
            this._items.AddRange(items);
            var tailStart = Math.Min(_skip, _items.Count);
            _items.Sort(tailStart, _items.Count - tailStart, Comparer<List<PlayerHeldItem>>.Create(GetComparer()));
        }

        public List<List<PlayerHeldItem>> Fetch() {
            var remaining = Remaining;
            var batch = _items?.Skip(_skip).Take(remaining);
            this._skip += remaining;
            return batch?.ToList() ?? new List<List<PlayerHeldItem>>();
        }
    }
}
