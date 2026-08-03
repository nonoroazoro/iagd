import type { IRollStat } from '../interfaces';

export function normalizeRollText(text: string): string {
  return text
    .replace(/\{?\^.[}]?/g, '')
    .toLocaleLowerCase()
    .replace(/([+-]?\d+(?:\.\d+)?)\s*(?:-|~|至)\s*([+-]?\d+(?:\.\d+)?)/g, '#')
    .replace(/[+-]?\d+(?:\.\d+)?/g, '#')
    .replace(/\s+/g, ' ')
    .trim();
}

export function matchRollStats(rows: string[], rollStats: IRollStat[] | undefined): Array<IRollStat | null> {
  const buckets = new Map<string, IRollStat[]>();
  (rollStats ?? []).forEach((roll) => {
    const key = normalizeRollText(roll.text);
    const bucket = buckets.get(key) ?? [];
    bucket.push(roll);
    buckets.set(key, bucket);
  });

  return rows.map((text) => {
    const bucket = buckets.get(normalizeRollText(text));
    return bucket && bucket.length > 0 ? bucket.shift() ?? null : null;
  });
}
