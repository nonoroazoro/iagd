import {h} from "preact";
import {PureComponent} from "preact/compat";
import { localize } from '../translations';
import styles from "./GdSeasonError.module.css";

interface Props {
  close: () => void;
}

class GdSeasonError extends PureComponent<Props, object> {
  render() {
    return (
      <div className={styles.center + " " + styles.yellowmodal}>
      <h2>{localize('Grim League detected', '检测到 Grim League')}</h2>
      <p>
        {localize('The use of Item Assistant is not permitted when playing Grim League.', '游玩 Grim League 时不允许使用 Item Assistant。')}<br/>
      <br/>
        {localize('You can safely keep Item Assistant running. IA will not interfere with the game while Grim League is running.', '你可以让 Item Assistant 保持运行。只要 Grim League 正在运行，IA 就不会干预游戏。')}
        <br/>
      <br/>
        <p className={styles.btnClose} onClick={() => this.props.close()}>{localize('Close', '关闭')}</p>
      </p>
    </div>);
  }
}

export default GdSeasonError;
