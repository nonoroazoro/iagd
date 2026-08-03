import {h} from "preact";
import {PureComponent} from "preact/compat";
import { localize } from '../translations';
import styles from "./EasterEgg.module.css";

interface Props {
  close: () => void;
}

class EasterEgg extends PureComponent<Props, object> {
  render() {
    return Math.random() < 0.5 ? this.render2() : this.render3();
  }
  render3() {
    return (
      <div className={styles.center + " " + styles.yellowmodal}>
      <h1>{localize('Free item limit reached!', '免费物品数量已达上限！')}</h1>
      <p>
        {localize('Your free item limit has been reached.', '免费物品数量已达上限。')}<br/>
      <br/>
        {localize('To continue using Item Assistant, purchase the full version and unlock unlimited items.', '如需继续使用 Item Assistant，请购买完整版以解锁无限物品容量。')}
        <br/>
      <br/>
        <p className={styles.btnSubscribe} onClick={() => this.props.close()}>{localize('Subscribe now for only $19.99/mo', '立即订阅，每月仅需 $19.99')}</p>
      </p>
    </div>);
  }

render2() {
  return (
    <div className={styles.center + " " + styles.yellowmodal}>
    <h1>{localize('Your free trial has expired!', '免费试用已到期！')}</h1>
    <br/>
    <p>
      {localize('Your free trial of Item Assistant has expired.', 'Item Assistant 免费试用已到期。')}<br/>
    <br/>
      {localize('To continue using Item Assistant, please purchase the full version.', '如需继续使用 Item Assistant，请购买完整版。')}
      <br/>
    <br/>
      <p className={styles.btnSubscribe} onClick={() => this.props.close()}>{localize('Unlock now for only $69.95', '立即解锁，仅需 $69.95')}</p>
    </p>
  </div>);
}
}

export default EasterEgg;
