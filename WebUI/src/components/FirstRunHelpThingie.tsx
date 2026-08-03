import {h} from "preact";
import {PureComponent} from "preact/compat";
import { localize } from '../translations';
import styles from "./FirstRunHelpThingie.module.css";

class FirstRunHelpThingie extends PureComponent<object, object> {
  render() {
    return (
      <div className={styles.center}>
      <h2>{localize('Welcome to Grim Dawn Item Assistant', '欢迎使用 Grim Dawn Item Assistant')}</h2>
      <p>
        {localize("It seems you have already parsed the Grim Dawn database, so you're ready to get started.", '看起来你已经解析了 Grim Dawn 数据库，现在可以开始使用了。')} <br/>
        <br/>
        <br/>
        {localize('Step 1: Start Grim Dawn', '第 1 步：启动 Grim Dawn')}<br/>
        {localize('Step 2: Walk to the smuggler', '第 2 步：前往走私者处')}<br/>
        {localize('Step 3: Open the shared stash tab', '第 3 步：打开公共仓库')}<br/>
        <br/>
        {localize("This is where most people get it wrong, so make sure you're opening the SHARED stash, and not the PRIVATE stash.", '大多数人会在这里出错，请确认你打开的是公共仓库，而不是个人仓库。')}<br/>
        {localize('Remember there are two types of stashes at the smuggler.', '走私者处有两种不同的仓库。')}<br/>
        <br/>
        {localize('With the shared stash open, make sure you own at least two tabs. If you only own one, you need to purchase another before you can use IA.', '打开公共仓库后，请确认至少拥有两个仓库页。如果只有一个，需要再购买一个才能使用 IA。')}<br/>
        <br/>
        {localize('Once you have at least two stash tabs, place an item in the last tab and watch it disappear from the game.', '拥有至少两个仓库页后，把物品放进最后一页，它会从游戏中消失并存入 IA。')}<br/>
        <br/>
        <br/>
        {localize('The item will not immediately appear in Item Assistant. Search for items to refresh the item view.', '物品不会立刻出现在 Item Assistant 中，需要执行一次搜索来刷新物品列表。')}<br/>
        {localize('The easiest way is to select and unselect any checkbox on the left side.', '最简单的方法是勾选再取消左侧任意一个复选框。')}<br/>
        <br/>
        {localize('The item should now appear inside Item Assistant. This walkthrough will not be shown again.', '现在物品应该已经出现在 Item Assistant 中，这段入门说明以后也不会再显示。')}
        <br/>
        <br/>
        {localize('If you run into any issues, you can usually find the answer on the Help tab.', '如果遇到问题，通常可以在“帮助”页找到答案。')}

      </p>
    </div>);
  }
}

export default FirstRunHelpThingie;
