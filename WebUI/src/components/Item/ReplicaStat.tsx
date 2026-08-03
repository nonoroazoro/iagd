import { h } from 'preact';
import { PureComponent } from 'preact/compat';
import { IReplicaRow } from '../../interfaces/IReplicaRow';
import type { IRollStat } from '../../interfaces';
import { MaximumRollBadge } from '.';

interface Props extends IReplicaRow {
  rollStat?: IRollStat | null;
}

/**
 * Renders one game-provided replica stat row.
 */
class ReplicaStat extends PureComponent<Props, object> {
  render() {
    const { text, type, rollStat } = this.props;

    let result = '';
    for (let index = text.length - 1; index >= 0; index--) {
      const character = text.charAt(index);
      if (character === '^') {
        result = `<span class="replica-letter-${text.charAt(index + 1)}">` + result.substr(1) + '</span>';
      } else {
        result = character + result;
      }
    }

    const className = `replica-type-${type}${rollStat?.isMaximum ? ' roll-maximum-stat' : ''}`;
    return (
      <p class={className}>
        <span dangerouslySetInnerHTML={{ __html: result }} />
        <MaximumRollBadge roll={rollStat} />
      </p>
    );
  }
}

export default ReplicaStat;
