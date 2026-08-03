import type { IRollStat } from '../../interfaces';
import { localize } from '../../translations';

interface Props {
  roll: IRollStat | null | undefined;
}

export function MaximumRollBadge({ roll }: Props) {
  if (!roll?.isMaximum) {
    return null;
  }

  return (
    <span className="roll-maximum-badge" title={localize(`Affix range: ${roll.minimum} to ${roll.maximum}`, `词缀范围：${roll.minimum} 至 ${roll.maximum}`)}>
      {localize('MAX', '满')}
    </span>
  );
}
