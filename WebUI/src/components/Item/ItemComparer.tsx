import { h } from 'preact';
import { PureComponent } from 'preact/compat';
import IItem from '../../interfaces/IItem';
import ICollectionItem from '../../interfaces/ICollectionItem';
import IItemType from '../../interfaces/IItemType';
import { statToString } from '../../interfaces/IStat';
import type { IRollStat } from '../../interfaces';
import { matchRollStats, normalizeRollText } from '../../utils';
import { isEmbedded } from '../../integration/integration';
// @ts-ignore: Missing @types
import { localize, translate } from '../../translations';
import styles from './ItemComparer.module.css';

interface Props {
  item: IItem[];
  transferSingle: (item: IItem) => void;
  onClose: () => void;
  showBackupCloudIcon: boolean;
  getItemName: (baseRecord: string) => ICollectionItem;
}

interface State {
  rightIndex: number;
}

interface ComparisonEntry {
  key: string;
  leftText: string | null;
  rightText: string | null;
  leftRoll: IRollStat | null;
  rightRoll: IRollStat | null;
}
interface MultiComparisonValue {
  text: string;
  roll: IRollStat | null;
}

interface MultiComparisonEntry {
  key: string;
  values: Array<MultiComparisonValue | null>;
}

interface RankedItem {
  item: IItem;
  originalIndex: number;
  advantages: number;
}

class ItemComparer extends PureComponent<Props, State> {
  private _dialog: HTMLDivElement | null = null;

  state: State = {
    rightIndex: 1,
  };

  componentDidMount() {
    this._dialog?.focus();
  }

  private _setDialog = (dialog: HTMLDivElement | null) => {
    this._dialog = dialog;
  };

  private _handleKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'Escape' && event.code !== 'Space') {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    this.props.onClose();
  };

  private _stripColorCodes = (text: string) => {
    return text.replace(/\{?\^.[}]?/g, '').trim();
  };

  private _getRows = (item: IItem) => {
    if (item.replicaStats && item.replicaStats.length > 0) {
      return item.replicaStats
        .filter((row) => row.type !== 0 && !(row.type >= 20 && row.type <= 26))
        .map((row) => this._stripColorCodes(row.text))
        .filter((text) => text.length > 0);
    }

    return [...item.headerStats, ...item.bodyStats, ...item.petStats]
      .map(statToString)
      .map(this._stripColorCodes)
      .filter((text) => text.length > 0);
  };

  private _getRowKey = (text: string) => normalizeRollText(text);

  private _getRollsForRows = (item: IItem, rows: string[]) => matchRollStats(rows, item.rollStats);

  private _createComparison = (left: IItem, right: IItem) => {
    const leftRows = this._getRows(left);
    const rightRows = this._getRows(right);
    const leftRolls = this._getRollsForRows(left, leftRows);
    const rightRolls = this._getRollsForRows(right, rightRows);
    const rightBuckets = new Map<string, number[]>();

    rightRows.forEach((text, index) => {
      const key = this._getRowKey(text);
      const bucket = rightBuckets.get(key) ?? [];
      bucket.push(index);
      rightBuckets.set(key, bucket);
    });

    const usedRightRows = new Set<number>();
    const entries: ComparisonEntry[] = leftRows.map((leftText, index) => {
      const key = this._getRowKey(leftText);
      const bucket = rightBuckets.get(key) ?? [];
      const rightIndex = bucket.find((candidate) => !usedRightRows.has(candidate));

      if (rightIndex === undefined) {
        return {
          key: `left-${key}-${index}`,
          leftText,
          rightText: null,
          leftRoll: leftRolls[index],
          rightRoll: null,
        };
      }

      usedRightRows.add(rightIndex);
      return {
        key: `pair-${key}-${index}`,
        leftText,
        rightText: rightRows[rightIndex],
        leftRoll: leftRolls[index],
        rightRoll: rightRolls[rightIndex],
      };
    });

    rightRows.forEach((rightText, index) => {
      if (!usedRightRows.has(index)) {
        entries.push({
          key: `right-${this._getRowKey(rightText)}-${index}`,
          leftText: null,
          rightText,
          leftRoll: null,
          rightRoll: rightRolls[index],
        });
      }
    });

    return entries;
  };

  private _createMultiComparison = (items: IItem[]) => {
    const orderedKeys: string[] = [];
    const knownKeys = new Set<string>();
    const valuesByItem = items.map((item) => {
      const rows = this._getRows(item);
      const rolls = this._getRollsForRows(item, rows);
      const occurrences = new Map<string, number>();
      const values = new Map<string, MultiComparisonValue>();

      rows.forEach((text, index) => {
        const rowKey = this._getRowKey(text);
        const occurrence = occurrences.get(rowKey) ?? 0;
        occurrences.set(rowKey, occurrence + 1);
        const key = `${rowKey}\u0000${occurrence}`;
        values.set(key, { text, roll: rolls[index] });
        if (!knownKeys.has(key)) {
          knownKeys.add(key);
          orderedKeys.push(key);
        }
      });

      return values;
    });

    return orderedKeys.map((key): MultiComparisonEntry => ({
      key,
      values: valuesByItem.map((values) => values.get(key) ?? null),
    }));
  };

  private _isLowerBetterText = (text: string) => {
    return /冷却时间|施法间隔|攻击间隔|等级需求|属性需求|cooldown|interval|requirement/i.test(text);
  };

  private _getMultiRanking = (items: IItem[], entries: MultiComparisonEntry[]) => {
    const advantages = items.map(() => 0);

    entries.forEach((entry) => {
      const lowerIsBetter = this._isLowerBetterText(entry.values.map((value) => value?.text ?? '').join(' '));
      const metrics = entry.values.map((value) => this._getMetric(value?.text ?? null));
      const comparable = metrics.filter((metric): metric is number => metric !== null);
      if (comparable.length !== items.length) {
        return;
      }

      const best = lowerIsBetter ? Math.min(...comparable) : Math.max(...comparable);
      const worst = lowerIsBetter ? Math.max(...comparable) : Math.min(...comparable);
      if (Math.abs(best - worst) < 0.000001) {
        return;
      }

      metrics.forEach((metric, index) => {
        if (metric !== null && Math.abs(metric - best) < 0.000001) {
          advantages[index] += 1;
        }
      });
    });

    return items
      .map((item, originalIndex): RankedItem => ({ item, originalIndex, advantages: advantages[originalIndex] }))
      .sort((left, right) => right.advantages - left.advantages || left.originalIndex - right.originalIndex);
  };

  private _getMultiValueClass = (entry: MultiComparisonEntry, itemIndex: number) => {
    const lowerIsBetter = this._isLowerBetterText(entry.values.map((value) => value?.text ?? '').join(' '));
    const metrics = entry.values.map((value) => this._getMetric(value?.text ?? null));
    const metric = metrics[itemIndex];
    const comparable = metrics.filter((value): value is number => value !== null);
    if (metric === null || comparable.length !== entry.values.length) {
      return styles.neutral;
    }

    const best = lowerIsBetter ? Math.min(...comparable) : Math.max(...comparable);
    const worst = lowerIsBetter ? Math.max(...comparable) : Math.min(...comparable);
    if (Math.abs(best - worst) < 0.000001) {
      return styles.neutral;
    }
    if (Math.abs(metric - best) < 0.000001) {
      return styles.better;
    }
    if (Math.abs(metric - worst) < 0.000001) {
      return styles.worse;
    }
    return styles.neutral;
  };

  private _renderMultiComparison = (items: IItem[]) => {
    const entries = this._createMultiComparison(items);
    const rankedItems = this._getMultiRanking(items, entries);
    const minimumWidth = Math.max(900, rankedItems.length * 290);

    return (
      <div
        ref={this._setDialog}
        className={styles.itemComparer}
        role="dialog"
        aria-modal="true"
        aria-labelledby="item-comparer-title"
        tabIndex={-1}
        onKeyDown={this._handleKeyDown}
      >
        <header className={styles.itemHeader}>
          <div>
            <h2 id="item-comparer-title">{localize('Item Comparison', '物品比较')}</h2>
            <p>{localize('Items are ordered by advantage count. Ties preserve their original order.', '按优势项数量从左到右排列；并列优势保持原始顺序')}</p>
          </div>
          <button type="button" className={styles.closeButton} aria-label={localize('Close item comparison', '关闭物品比较')} onClick={() => this.props.onClose()}>
            ×
          </button>
        </header>

        <div className={styles.content}>
          <div className={styles.tableContainer}>
            <table className={styles.multiComparisonTable} style={{ minWidth: `${minimumWidth}px` }}>
              <thead>
                <tr>
                  {rankedItems.map((ranked) => (
                    <th key={`multi-head-${ranked.item.uniqueIdentifier || ranked.originalIndex}`}>
                      {this._renderItemSummary(ranked.item, localize(`${ranked.advantages} advantages`, `${ranked.advantages} 项优势`))}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <tr key={entry.key}>
                    {rankedItems.map((ranked) => {
                      const value = entry.values[ranked.originalIndex];
                      return (
                        <td
                          key={`multi-value-${entry.key}-${ranked.originalIndex}`}
                          className={this._getMultiValueClass(entry, ranked.originalIndex)}
                        >
                          {this._renderValue(value?.text ?? null, value?.roll ?? null)}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    );
  };

  private _getMetric = (text: string | null) => {
    if (text === null) {
      return null;
    }

    const range = text.match(/([+-]?\d+(?:\.\d+)?)\s*(?:-|~|至)\s*([+-]?\d+(?:\.\d+)?)/);
    if (range) {
      return (Number(range[1]) + Number(range[2])) / 2;
    }

    const value = text.match(/[+-]?\d+(?:\.\d+)?/);
    return value ? Number(value[0]) : null;
  };

  private _getRawDifference = (entry: ComparisonEntry) => {
    const left = this._getMetric(entry.leftText);
    const right = this._getMetric(entry.rightText);

    if (left === null || right === null || Math.abs(left) < 0.000001 || Math.abs(left - right) < 0.000001) {
      return null;
    }

    return ((right - left) / Math.abs(left)) * 100;
  };

  private _isLowerBetter = (entry: ComparisonEntry) => {
    const text = `${entry.leftText ?? ''} ${entry.rightText ?? ''}`;
    return this._isLowerBetterText(text);
  };

  private _getQualityDifference = (entry: ComparisonEntry) => {
    const difference = this._getRawDifference(entry);
    if (difference === null) {
      return null;
    }

    return this._isLowerBetter(entry) ? -difference : difference;
  };

  private _formatDifference = (difference: number | null) => {
    if (difference === null) {
      return '-';
    }

    const absolute = Math.abs(difference);
    const precision = absolute >= 100 ? 0 : 1;
    return `${difference > 0 ? '+' : ''}${difference.toFixed(precision)}%`;
  };

  private _getValueClass = (qualityDifference: number | null, side: 'left' | 'right') => {
    if (qualityDifference === null) {
      return styles.neutral;
    }

    const rightIsBetter = qualityDifference > 0;
    const isBetter = side === 'right' ? rightIsBetter : !rightIsBetter;
    return isBetter ? styles.better : styles.worse;
  };

  private _renderValue = (text: string | null, roll: IRollStat | null) => {
    if (text === null) {
      return <span className={styles.missing}>-</span>;
    }

    if (!roll?.isMaximum) {
      return text;
    }

    return (
      <span className={styles.maximumValue} title={localize(`Affix range: ${roll.minimum} to ${roll.maximum}`, `词缀范围：${roll.minimum} 至 ${roll.maximum}`)}>
        {text}
        <span className={styles.maximumBadge}>{localize('MAX', '满')}</span>
      </span>
    );
  };

  private _getIcon = (item: IItem) => {
    const icon = item.icon && item.icon.length > 0 ? item.icon : 'weapon1h_focus02a.tex.png';
    return isEmbedded ? icon : `http://static.iagd.evilsoft.net/img/${icon}`;
  };

  private _renderItemSummary = (item: IItem, label: string) => {
    const name = this._stripColorCodes(item.name) || localize('Unknown', '未知');

    return (
      <section className={styles.itemSummary}>
        <img className={styles.itemIcon} src={this._getIcon(item)} alt="" />
        <div className={styles.itemIdentity}>
          <span className={styles.sideLabel}>{label}</span>
          <strong className={`item-quality-${item.quality.toLowerCase()}`}>{name}</strong>
          {item.socket && <span className={styles.socket}>{item.socket}</span>}
        </div>
        {item.type === IItemType.Player && (
          <button className={styles.transferButton} type="button" onClick={() => this.props.transferSingle(item)}>
            {translate('item.label.transferSingle')}
          </button>
        )}
      </section>
    );
  };

  render() {
    const items = this.props.item;
    if (items.length > 2) {
      return this._renderMultiComparison(items);
    }

    const leftItem = items[0];
    const safeRightIndex = Math.min(Math.max(this.state.rightIndex, 1), Math.max(items.length - 1, 1));
    const rightItem = items[safeRightIndex];

    if (!leftItem || !rightItem) {
      return null;
    }

    const entries = this._createComparison(leftItem, rightItem);
    const leftWins = entries.filter((entry) => {
      const difference = this._getQualityDifference(entry);
      return difference !== null && difference < 0;
    }).length;
    const rightWins = entries.filter((entry) => {
      const difference = this._getQualityDifference(entry);
      return difference !== null && difference > 0;
    }).length;

    return (
      <div
        ref={this._setDialog}
        className={styles.itemComparer}
        role="dialog"
        aria-modal="true"
        aria-labelledby="item-comparer-title"
        tabIndex={-1}
        onKeyDown={this._handleKeyDown}
      >
        <header className={styles.itemHeader}>
          <div>
            <h2 id="item-comparer-title">{localize('Item Comparison', '物品比较')}</h2>
            <p>{localize('Differences use the left item as baseline. Green is better, red is weaker, and MAX marks the true roll cap.', '以左侧为基准计算差异，绿色更优，红色较弱；“满”表示达到真实 roll 上限')}</p>
          </div>
          <button
            type="button"
            className={styles.closeButton}
            aria-label={localize('Close item comparison', '关闭物品比较')}
            onClick={() => this.props.onClose()}
          >
            ×
          </button>
        </header>

        <div className={styles.content}>
          {items.length > 2 && (
            <nav className={styles.candidateTabs} aria-label={localize('Select the right-side candidate item', '选择右侧候选物品')}>
              {items.slice(1).map((item, index) => {
                const candidateIndex = index + 1;
                return (
                  <button
                    type="button"
                    key={`${item.uniqueIdentifier || item.mergeIdentifier}-${candidateIndex}`}
                    className={candidateIndex === safeRightIndex ? styles.activeCandidate : ''}
                    onClick={() => this.setState({ rightIndex: candidateIndex })}
                  >
                    {localize(`Candidate ${candidateIndex}`, `候选 ${candidateIndex}`)}
                  </button>
                );
              })}
            </nav>
          )}

          <div className={styles.summaryGrid}>
            {this._renderItemSummary(leftItem, localize('Left', '左侧'))}
            <div className={styles.score}>
              <span className={leftWins > rightWins ? styles.winningScore : leftWins < rightWins ? styles.losingScore : ''}>
                <b>{leftWins}</b> {localize('left-side advantages', '项左侧更优')}
              </span>
              <span className={rightWins > leftWins ? styles.winningScore : rightWins < leftWins ? styles.losingScore : ''}>
                <b>{rightWins}</b> {localize('right-side advantages', '项右侧更优')}
              </span>
            </div>
            {this._renderItemSummary(rightItem, localize('Right', '右侧'))}
          </div>

          <div className={styles.tableContainer}>
            <table className={styles.comparisonTable}>
              <thead>
                <tr>
                  <th>{localize('Left Stat', '左侧属性')}</th>
                  <th className={styles.differenceColumn}>{localize('Difference', '差异')}</th>
                  <th>{localize('Right Stat', '右侧属性')}</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => {
                  const rawDifference = this._getRawDifference(entry);
                  const qualityDifference = this._getQualityDifference(entry);

                  return (
                    <tr key={entry.key}>
                      <td className={this._getValueClass(qualityDifference, 'left')}>
                        {this._renderValue(entry.leftText, entry.leftRoll)}
                      </td>
                      <td className={`${styles.difference} ${this._getValueClass(qualityDifference, 'right')}`}>
                        {this._formatDifference(rawDifference)}
                      </td>
                      <td className={this._getValueClass(qualityDifference, 'right')}>
                        {this._renderValue(entry.rightText, entry.rightRoll)}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    );
  }
}

export default ItemComparer;
