import { PureComponent } from 'preact/compat';
import IItem from '../../interfaces/IItem';
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
}

interface ComparisonEntry {
  key: string;
  leftText: string | null;
  rightText: string | null;
  leftRoll: IRollStat | null;
  rightRoll: IRollStat | null;
  rawDifference: number | null;
  qualityDifference: number | null;
}
interface MultiComparisonValue {
  text: string;
  roll: IRollStat | null;
  comparable: boolean;
}

interface ItemComparisonRow extends MultiComparisonValue {
  key: string;
}

interface MultiComparisonEntry {
  key: string;
  values: Array<MultiComparisonValue | null>;
  metrics: Array<number | null>;
  best: number | null;
  worst: number | null;
  hasDifference: boolean;
}

interface RankedItem {
  item: IItem;
  originalIndex: number;
  advantages: number;
}

class ItemComparer extends PureComponent<Props> {
  private _dialog: HTMLDivElement | null = null;

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

  private _getRowKey = (text: string, type?: number, section = 'item') => {
    return type === 5 ? 'item-name' : `${section}:${normalizeRollText(text)}`;
  };

  private _getRows = (item: IItem): ItemComparisonRow[] => {
    if (item.replicaStats && item.replicaStats.length > 0) {
      const rows: Array<{text: string; type: number; section: string}> = [];
      let section = 'item';
      let isGrantedSkill = false;
      let grantedSkillSeparators = 0;

      item.replicaStats.forEach((row, index) => {
        if (row.type === 36) {
          isGrantedSkill = true;
          grantedSkillSeparators = 0;
          section = 'granted-skill';
          const text = this._stripColorCodes(row.text);
          if (text.length > 0) {
            rows.push({ text, type: row.type, section });
          }
          return;
        }

        if (isGrantedSkill) {
          if (row.type === 0) {
            grantedSkillSeparators += 1;
            if (grantedSkillSeparators >= 2) {
              isGrantedSkill = false;
              section = 'item';
            }
            return;
          }

          const text = this._stripColorCodes(row.text);
          if (section === 'granted-skill' && text.length > 0) {
            section = `granted-skill:${normalizeRollText(text)}`;
          }
          if (text.length > 0) {
            rows.push({ text, type: row.type, section });
          }
          return;
        }

        if (row.type === 0 || row.type === 1) {
          section = 'item';
          return;
        }

        const nextType = item.replicaStats?.[index + 1]?.type;
        const isModifiedSkillHeader = (row.type === 37 && nextType === 26)
          || (row.type === 81 && nextType === 82);
        const text = this._stripColorCodes(row.text);
        if (isModifiedSkillHeader) {
          section = `modified-skill:${normalizeRollText(text)}`;
        }
        if (text.length > 0) {
          rows.push({ text, type: row.type, section });
        }
      });

      const rolls = this._getRollsForRows(item, rows.map((row) => row.text));
      return rows.map((row, index) => ({
        key: this._getRowKey(row.text, row.type, row.section),
        text: row.text,
        roll: rolls[index],
        comparable: row.type !== 5 && !row.section.startsWith('granted-skill'),
      }));
    }

    const rows = [
      ...item.headerStats.map((stat) => ({ text: statToString(stat), section: 'header' })),
      ...item.bodyStats.map((stat) => ({ text: statToString(stat), section: 'body' })),
      ...item.petStats.map((stat) => ({ text: statToString(stat), section: 'pet' })),
    ]
      .map((row) => ({ ...row, text: this._stripColorCodes(row.text) }))
      .filter((row) => row.text.length > 0);
    const rolls = this._getRollsForRows(item, rows.map((row) => row.text));
    return rows.map((row, index) => ({
      key: this._getRowKey(row.text, undefined, row.section),
      text: row.text,
      roll: rolls[index],
      comparable: true,
    }));
  };

  private _getRollsForRows = (item: IItem, rows: string[]) => matchRollStats(rows, item.rollStats);

  private _createComparison = (left: IItem, right: IItem) => {
    return this._createMultiComparison([left, right]).map((entry): ComparisonEntry => {
      const leftValue = entry.values[0] ?? null;
      const rightValue = entry.values[1] ?? null;
      const leftMetric = entry.metrics[0];
      const rightMetric = entry.metrics[1];
      const rawDifference = this._getRawDifference(leftMetric, rightMetric);
      let qualityDifference = rawDifference;

      if (leftMetric !== null && rightMetric === null) {
        qualityDifference = -1;
      } else if (leftMetric === null && rightMetric !== null) {
        qualityDifference = 1;
      } else if (rawDifference !== null && this._isLowerBetterText(`${leftValue?.text ?? ''} ${rightValue?.text ?? ''}`)) {
        qualityDifference = -rawDifference;
      }

      return {
        key: entry.key,
        leftText: leftValue?.text ?? null,
        rightText: rightValue?.text ?? null,
        leftRoll: leftValue?.roll ?? null,
        rightRoll: rightValue?.roll ?? null,
        rawDifference,
        qualityDifference,
      };
    });
  };

  private _mergeRowOrder = (sequences: string[][]) => {
    const longest = sequences.reduce(
      (current, sequence) => sequence.length > current.length ? sequence : current,
      [] as string[],
    );
    const orderedKeys = [...longest];
    const knownKeys = new Set(orderedKeys);

    sequences.forEach((sequence) => {
      sequence.forEach((key, index) => {
        if (knownKeys.has(key)) {
          return;
        }

        let nextKey: string | undefined;
        for (let candidateIndex = index + 1; candidateIndex < sequence.length; candidateIndex += 1) {
          if (knownKeys.has(sequence[candidateIndex])) {
            nextKey = sequence[candidateIndex];
            break;
          }
        }

        if (nextKey !== undefined) {
          orderedKeys.splice(orderedKeys.indexOf(nextKey), 0, key);
        } else {
          let previousKey: string | undefined;
          for (let candidateIndex = index - 1; candidateIndex >= 0; candidateIndex -= 1) {
            if (knownKeys.has(sequence[candidateIndex])) {
              previousKey = sequence[candidateIndex];
              break;
            }
          }
          const insertionIndex = previousKey === undefined
            ? orderedKeys.length
            : orderedKeys.indexOf(previousKey) + 1;
          orderedKeys.splice(insertionIndex, 0, key);
        }
        knownKeys.add(key);
      });
    });

    return orderedKeys;
  };

  private _createMultiComparison = (items: IItem[]) => {
    const sequences: string[][] = [];
    const valuesByItem = items.map((item) => {
      const rows = this._getRows(item);
      const occurrences = new Map<string, number>();
      const values = new Map<string, MultiComparisonValue>();
      const sequence: string[] = [];

      rows.forEach((row) => {
        const rowKey = row.key;
        const occurrence = occurrences.get(rowKey) ?? 0;
        occurrences.set(rowKey, occurrence + 1);
        const key = `${rowKey}\u0000${occurrence}`;
        values.set(key, row);
        sequence.push(key);
      });

      sequences.push(sequence);
      return values;
    });
    const orderedKeys = this._mergeRowOrder(sequences);

    const entries = orderedKeys.reduce<MultiComparisonEntry[]>((result, key) => {
      const values = valuesByItem.map((itemValues) => itemValues.get(key) ?? null);
      const metrics = values.map((value) => value?.comparable
        ? this._getMetric(value.text)
        : null);
      const comparable = metrics.filter((metric): metric is number => metric !== null);
      const lowerIsBetter = this._isLowerBetterText(values.map((value) => value?.text ?? '').join(' '));
      const best = comparable.length === 0
        ? null
        : lowerIsBetter ? Math.min(...comparable) : Math.max(...comparable);
      const worst = comparable.length === 0
        ? null
        : lowerIsBetter ? Math.max(...comparable) : Math.min(...comparable);
      const hasDifference = best !== null
        && worst !== null
        && (comparable.length < values.length || Math.abs(best - worst) >= 0.000001);

      result.push({ key, values, metrics, best, worst, hasDifference });
      return result;
    }, []);

    const priority = (entry: MultiComparisonEntry) => {
      if (entry.key.startsWith('item-name\u0000')) {
        return 0;
      }
      if (entry.hasDifference) {
        return 1;
      }
      if (entry.metrics.some((metric) => metric !== null)) {
        return 2;
      }
      return 3;
    };

    return entries
      .map((entry, sourceIndex) => ({entry, sourceIndex}))
      .sort((left, right) => priority(left.entry) - priority(right.entry) || left.sourceIndex - right.sourceIndex)
      .map(({entry}) => entry);
  };

  private _isLowerBetterText = (text: string) => {
    return /冷却时间|施法间隔|攻击间隔|等级需求|属性需求|cooldown|interval|requirement/i.test(text);
  };

  private _getMultiRanking = (items: IItem[], entries: MultiComparisonEntry[]) => {
    const advantages = items.map(() => 0);

    entries.forEach((entry) => {
      if (!entry.hasDifference || entry.best === null) {
        return;
      }

      entry.metrics.forEach((metric, index) => {
        if (metric !== null && Math.abs(metric - entry.best) < 0.000001) {
          advantages[index] += 1;
        }
      });
    });

    return items
      .map((item, originalIndex): RankedItem => ({ item, originalIndex, advantages: advantages[originalIndex] }))
      .sort((left, right) => right.advantages - left.advantages || left.originalIndex - right.originalIndex);
  };

  private _getMultiValueClass = (entry: MultiComparisonEntry, itemIndex: number) => {
    if (!entry.hasDifference || entry.best === null || entry.worst === null) {
      return styles.neutral;
    }
    const metric = entry.metrics[itemIndex];
    if (metric === null) {
      return styles.worse;
    }
    if (Math.abs(metric - entry.best) < 0.000001) {
      return styles.better;
    }
    if (Math.abs(metric - entry.worst) < 0.000001) {
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

  private _getRawDifference = (left: number | null, right: number | null) => {
    if (left === null || right === null || Math.abs(left) < 0.000001 || Math.abs(left - right) < 0.000001) {
      return null;
    }

    return ((right - left) / Math.abs(left)) * 100;
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
    const rightItem = items[1];

    if (!leftItem || !rightItem) {
      return null;
    }

    const entries = this._createComparison(leftItem, rightItem);
    const leftWins = entries.filter((entry) => entry.qualityDifference !== null && entry.qualityDifference < 0).length;
    const rightWins = entries.filter((entry) => entry.qualityDifference !== null && entry.qualityDifference > 0).length;

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
                  return (
                    <tr key={entry.key}>
                      <td className={this._getValueClass(entry.qualityDifference, 'left')}>
                        {this._renderValue(entry.leftText, entry.leftRoll)}
                      </td>
                      <td className={`${styles.difference} ${this._getValueClass(entry.qualityDifference, 'right')}`}>
                        {this._formatDifference(entry.rawDifference)}
                      </td>
                      <td className={this._getValueClass(entry.qualityDifference, 'right')}>
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
