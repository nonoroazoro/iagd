import {h} from "preact";
import {PureComponent} from "preact/compat";
import { localize } from '../translations';
import styles from "./NumericFilterBanner.module.css";
import {darkFilterScreenshot, lightFilterScreenshot} from "./NumericFilterBanner.images";

interface Props {
  close: () => void;
}

class NumericFilterBanner extends PureComponent<Props, object> {
  render() {
    return (
      <div className={styles.backdrop}>
        <div className={styles.modal}>
          <h2>{localize('Advanced filtering', '高级筛选')}</h2>
          <p>
            {localize('You can now filter your items on the numeric value of a stat, not just whether the item has it at all.', '现在可以按属性数值筛选物品，而不只是筛选物品是否拥有该属性。')}<br/>
            <br/>
            {localize('Hover a stat in the filter panel on the left and click the funnel button that appears, then pick a comparison such as ">= 30". Only items whose value matches will be shown.', '将鼠标悬停在左侧筛选面板的属性上，单击出现的漏斗按钮，然后选择“>= 30”之类的比较条件。只有数值符合条件的物品才会显示。')}
          </p>

          <img className={styles.screenshot + " " + styles.lightScreenshot} src={lightFilterScreenshot}
               alt={localize('The funnel button next to a stat, and the value filter dialog it opens', '属性旁的漏斗按钮及其打开的数值筛选对话框')}/>
          <img className={styles.screenshot + " " + styles.darkScreenshot} src={darkFilterScreenshot}
               alt={localize('The funnel button next to a stat, and the value filter dialog it opens', '属性旁的漏斗按钮及其打开的数值筛选对话框')}/>

          <br/>
          <p className={styles.btnConfirm} onClick={() => this.props.close()}>{localize('Got it', '知道了')}</p>
        </div>
      </div>);
  }
}

export default NumericFilterBanner;
