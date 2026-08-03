import {h} from "preact";
import {PureComponent} from "preact/compat";
import { localize } from '../translations';
import styles from "./ModFilterWarning.module.css";

interface Props {
  numOtherItems: number;
}

class ModFilterWarning extends PureComponent<Props, object> {
  state = {
    isHidden: false,
  }
  render() {
  if (this.state.isHidden) {
    return null;
  }
  return (
  <div className={styles.outer}>
        <div className={styles.large +" "+ styles.large +" "+ styles.yellow +" "+ styles.border +" "+ styles.panel + " " + styles.container}>
          <span className={styles.button +" "+ styles.large +" "+ styles.topright} onClick={() => this.setState({isHidden: true})}>×</span>
          <h3>{localize('Warning!', '警告！')}</h3>
          <p>{localize(`You have an additional ${this.props.numOtherItems} items which were filtered out due to the mod filter.`, `另有 ${this.props.numOtherItems} 件物品被 Mod 筛选器隐藏。`)}</p>
          <p>{localize('If you are having trouble finding your items, check the mod filter in the top right corner.', '如果找不到物品，请检查右上角的 Mod 筛选器。')}</p>
          <p>{localize('It differentiates between softcore and hardcore stashes, as well as items from various mods.', '它用于区分普通模式、专家模式以及不同 Mod 的仓库物品。')}</p>
        </div>
        </div>
    );
  }
}

export default ModFilterWarning;
