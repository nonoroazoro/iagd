import { h } from 'preact';
import { PureComponent } from 'preact/compat';
import { IStat } from '../../interfaces/IStat';
import type { IRollStat } from '../../interfaces';
import { MaximumRollBadge } from '.';

interface Props extends IStat {
  rollStat?: IRollStat | null;
}

function statToString(text: string, stat: IStat) {
  return text
    .replace('{0}', stat.param0)
    .replace('{1}', stat.param1)
    .replace('{2}', stat.param2)
    .replace('{3}', stat.param3)
    .replace('{4}', stat.param4)
    .replace('{5}', stat.param5)
    .replace('{6}', stat.param6);
}

class ItemStat extends PureComponent<Props, object> {
  render() {
    if (this.props.text === '') {
      return null;
    }

    const className = this.props.rollStat?.isMaximum ? 'roll-maximum-stat' : undefined;
    if (this.props.extras) {
      const text = statToString(this.props.text.replace('{3}', ' '), this.props);
      const modifier = text.substr(0, text.indexOf(' '));
      const label = text.substr(text.indexOf(' ') + 1);

      return (
        <p className={className}>
          <a data-tip={this.props.extras} className="skill-trigger">
            <span className="modifier">{modifier}</span>&nbsp;
            <span className="label">{label}</span>
            <span className="modified-skill">{this.props.param3}</span>
          </a>
          <MaximumRollBadge roll={this.props.rollStat} />
        </p>
      );
    }

    const text = statToString(this.props.text, this.props);
    const modifier = text.substr(0, text.indexOf(' '));
    const label = text.substr(text.indexOf(' ') + 1);
    return (
      <p className={className}>
        <span className="modifier">{modifier}</span>&nbsp;
        <span className="label">{label}</span>
        <MaximumRollBadge roll={this.props.rollStat} />
      </p>
    );
  }
}

export default ItemStat;
