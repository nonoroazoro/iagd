import './Help.css';
import {openUrl} from '../../integration/integration';
import {Fragment} from "preact";
import {PureComponent} from "preact/compat";

function toNumberedList(s: string) {
    return <ol>
        {s.trim().split('\n').map(e => <li key={e}>{e}</li>)}
    </ol>;
}

enum IHelpEntryType {
    Informational, Help
}

interface IHelpEntry {
    title: string;
    tag: string;
    body: () => any;
    type: IHelpEntryType;
}

const typicalParseDbMessage = <Fragment>{toNumberedList(`
    关闭 Grim Dawn
    打开 IA
    在 IA 中单击“Grim Dawn”页
    选择“本体”（或 Forgotten Gods/AoM）
    单击“加载数据库”
    `)}
    <i>如果问题仍未解决，请尝试重启 IA。</i>
</Fragment>;

const helpEntries = [
    {
        title: `IA 已存入物品，但游戏中始终没有出现`,
        tag: 'ExpansionsDisabledItemsMissing',
        body: () => <div>
            IA 可以正常收取和存入物品，但物品始终没有出现在游戏中？ <br/>
            最可能的原因是右上角的“Mod 筛选器”设置错误。 <br/>
            例如你正在游玩资料片，但 Mod 筛选器却设成了“无资料片”。<br/><br/>
            此筛选器让你在为了和朋友联机而禁用资料片时，仍可正常使用 IA。
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `IA 显示“Stash: Error”`,
        tag: 'StashError',
        body: () => <div>
            检测游戏内仓库状态时出现问题。 <br/>
            这通常表示权限问题导致 IA DLL 无法注入游戏。 <br/>
            请尝试以管理员身份运行 IA。
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `安装路径存在错误`,
        tag: 'PathError',
        body: () => <div>
            Item Assistant 向 Grim Dawn 注入物品监控代码时遇到错误。<br/>
            <br/>
            IA 的安装路径或文件夹似乎存在问题。 <br/>
			请尝试把 IA 安装到其他文件夹，或以管理员身份运行 IA。如果仍然失败，请到 Discord 寻求帮助。 <br/>
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `IA 显示“Stash: NOT64BIT”`,
        tag: 'No32Bit',
        body: () => <div>
            Item Assistant 仅支持 64-bit 版本的 Grim Dawn。 <br/>
            为了确保始终运行 64-bit 版本，可以在 Steam 启动选项中添加“/x64”。
            <img src="./x64steam.png"/> <br/>
            即使你认为自己已在运行 Grim Dawn x64，也请添加该启动参数。游戏右下角会显示“(x64)”。
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `IA 显示“Base stats”`,
        tag: 'GenericStatNoReplica',
        body: () => <div>
            物品显示“Base stats”时，表示 IA 暂时无法<u>显示</u>该物品的真实属性。 <br/>
            Item Assistant 始终可以精确还原物品，但在 IA 中显示真实属性需要 Grim Dawn 提供部分数据。 <br/>
            游玩过程中，IA 会自动获取物品的真实显示属性。 <br/>
            获取完成前，IA 会显示物品的基础数值。<br/><br/>
            把物品传回游戏时，IA 始终会精确还原你存入的物品。
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `IA 立即退回物品`,
        tag: 'ItemClassificationReturn',
        body: () => <div>
            如果遇到“Deposited item back in-game, did not pass item classification.”错误，通常表示需要解析数据库。 <br/>
            IA 会退回无法识别的物品，因为这类物品无法按属性搜索。 <br/> <br/>
            也可能是 DLL 注入问题，请在日志中检查与注入有关的消息。
            <br/>
            {typicalParseDbMessage}
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `物品图标不见了！`,
        tag: 'MissingIcons',
        body: () => <div>
            IA 会在后台解析图标，因此可能需要一段时间才会显示。<br/>
            可能需要重启 IA 才能彻底完成图标解析。
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `IA 何时适配最新的 GD 更新？`,
        tag: 'GrimDawnUpdated',
        body: () => <div>
            Item Assistant 很少需要专门更新，但你可能需要重新解析数据库。<br/>
            IA 已完全兼容 Fangs of Asterkarn
          <br/><br/>
            {typicalParseDbMessage}
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `IA 会归还属性完全相同的物品吗？`,
        tag: 'ReproduceStats',
        body: () => <div>
            IA 不会修改任何物品，并且始终会精确还原它们。 <br/>
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `在多台 PC 上使用 IA 是什么意思？`,
        tag: 'MultiplePcs',
        body: () => <div>
            Item Assistant 中的“<i>多台 PC</i>”复选框是备份系统的附加功能。<br/>
            如果你已使用 e-mail 登录并启用备份，就可以在
            多台 PC 之间近乎实时地同步物品。<br/><br/>
            如果你在多台 PC 上游玩 Grim Dawn，或者与亲友共享仓库，此功能会很有用，因为不同安装会
            使用同一个 IA 仓库。<br/><br/>

            设置中的该复选框会让 IA 更积极地同步物品，优先实现近乎即时的同步
            而不是降低带宽占用。
            IA 只同步物品，不同步角色。<br/>
            <br/>
            <b>注意：</b>切换此功能后需要重启 IA。
        </div>,
        type: IHelpEntryType.Informational
    },
    {
      title: `设置中的“搜索时延迟”是什么？`,
      tag: 'DelayWhenSearching',
      body: () => <div>
        使用左侧复选框时，Item Assistant 中的“<i>搜索时延迟</i>”选项会增加短暂延迟。<br/>
        当物品很多（例如 100k）或 PC 性能较低时，这可能有助于改善性能。
      </div>,
      type: IHelpEntryType.Informational
    },
    {
        title: `找不到我的物品！`,
        tag: 'CantFindItemsMod',
        body: () => <div>
            {toNumberedList(`
      重启 IA
      确认已选择正确的 Mod（右上角，通常是“No Mod”）`)}

            通常是 Mod 选择错误导致找不到物品，因为 IA 可以区分
            不同 Mod 以及普通模式和专家模式的物品。
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `在哪里查找日志文件？`,
        tag: 'FindLogFiles',
        body: () => <div>
            {toNumberedList(`
      单击“设置”页
      单击“查看日志”
      最新日志是“log.txt”`)}
            日志文件夹位于 <i>&lt;IA 安装目录&gt;\UserData</i>
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `在哪里查找备份？`,
        tag: 'FindBackups',
        body: () => <div>
            {toNumberedList(`
      单击“设置”页
      单击“查看备份”`)}

            此文件夹包含 Item Assistant 的每日备份以及之前所有的公共仓库
            文件。<br/>
            <span className="attention">强烈建议</span>启用其他备份方式。 <br/>
            硬件可能损坏，恶意软件可能破坏数据，重装 Windows 时也可能忘记复制 IA 数据。 <br/><br/>
            <span className="attention">请使用额外的备份方式！</span>
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `userdata.db 在哪里？IA 把物品存在哪里？`,
        tag: 'FindUserdataDb',
        body: () => <div>
            {toNumberedList(`
      单击“设置”页
      单击“查看日志”
      进入“data”文件夹`)}
            data 文件夹位于 <i>&lt;IA 安装目录&gt;\UserData\data</i>
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `无法创建 SSL/TLS 安全通道`,
        tag: 'SSLAntiVirusIssues',
        body: () => <div>
            "The request was aborted: Could not create SSL/TLS secure channel." <br/>
            如果日志中出现此错误，通常是 anti-virus 阻止了在线备份和自动备份。 <br/><br/>
            <a href={"https://social.msdn.microsoft.com/Forums/vstudio/en-US/9e0bbf83-78ae-4f5c-9ebb-dbb75c928929/problems-could-not-create-ssl-tls-secure-channel?forum=csharpgeneral"}>尤其是 Kaspersky anti-virus</a> 会对网络流量执行 man-in-the-middle 检查，导致加密（SSL）验证失败。
            <br/><br/>
            另一个原因可能是 Windows 7 的 TLS 版本过旧。如果你使用 Windows 7， <a href={"https://stackoverflow.com/questions/70674832/windows-7-could-not-create-ssl-tls-secure-channel-system-net-webexception"}>请查看此链接</a>
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `如何从备份恢复？`,
        tag: 'RestoreBackup',
        body: () => <div>

            <b>IA 备份通常有两种方式：</b><br/>
            <br/>
            <b>如果通过云备份或自定义文件夹备份 IA：</b><br/>

            <ol>
                <li>从备份中解压 <i>export.ias</i> 文件</li>
                <li>打开 IA</li>
                <li>单击“设置”页</li>
                <li>单击“导入/导出”</li>
                <li>选择“导入 / IAS”</li>
                <li>导入 export.ias 文件</li>
            </ol>
            备份文件的“save”目录中还包含 Grim Dawn 角色。<br/>
            <br/>
            <b>如果通过复制 <i>userdata.db</i> 手动备份 IA：</b><br/>
            <ol>
                <li>打开 IA</li>
                <li>单击“设置”页</li>
                <li>单击“查看日志”</li>
                <li>进入“data”文件夹</li>
                <li>
                    <b>关闭 IA</b>
                </li>
                <li>把 <i>userdata.db 复制到 data 文件夹</i></li>
            </ol>
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `Item Assistant 提示部分文件丢失，并认为可能是 Avast 导致的！？`,
        tag: 'Avasted',
        body: () => <div>
            Item Assistant 通过向 Grim Dawn 注入代码来检测仓库是否打开。<br/>
            某些过度敏感的 anti-virus 或 anti-malware 程序会直接删除 Item
            Assistant。<br/>
            它们通常不会通知你，因此很难发现真正原因。<br/><br/>
            最常见的是 Avast，但其他 anti-virus 程序有时也会误报。
             <br/>
            只能查看 anti-virus 日志，确认它是否在未通知你的情况下进行了干预。
            <br/><br/>
            要继续使用 IA，必须在所用的 anti-virus 程序中把它加入白名单，
            然后完整重装 IA。<br/>
            如果你正在阅读此消息，anti-virus 程序很可能已经删除了 IA 正常运行所需的部分文件，
            <br/><br/>
            问题不在 IA，请先处理 anti-virus 的拦截。
        </div>,
        type: IHelpEntryType.Help
    },
    {
        title: `我要删除全部物品并从头开始！`,
        tag: 'StartFromZero',
        body: () => <div>
            {toNumberedList(`
      如果使用在线备份，请进入备份页并单击“删除备份”
      进入设置并单击“查看数据”
      关闭 IA
      删除 userdata.db 文件
      确认你确实想永久清空数据
      打开 IA，确认所有物品已永久删除
      `)}
            <br/>

            如果物品又逐渐出现，说明你漏掉了第 1 步。<br/>
            请从第 1 步重新开始。
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `此工具支持专家模式吗？可以同时玩专家模式和普通模式吗？`,
        tag: 'SupportsHardcore',
        body: () => <div>
            支持。物品会按 Mod、专家模式和普通模式分开保存。 <br/>
            可以在 IA 右上角选择要显示的物品类型。 <br/>
          <br/>
          只有各游戏模式都已有物品时，此选项才会出现。
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `设置：“好友物品”是什么？`,
        tag: 'BuddyItems',
        body: () => <div>
            好友物品功能可让你查看好友的全部物品。<br/>
            与朋友或家人一起游玩时，此功能很有用。 <br/>
            双方都能搜索并查看对方拥有的物品，但不能直接取用。
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `好友物品：好友 ID 是什么？`,
        tag: 'WhatIsBuddyId',
        body: () => <div>
            登录在线备份时，系统会为你的帐户分配一个随机编号。<br/>
            把这个编号发给好友后，他们无需知道你的
            e-mail 就能搜索你的物品。
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `好友物品：好友昵称是什么？`,
        tag: 'WhatIsBuddyNickname',
        body: () => <div>
            好友昵称是用于区分好友的文本标签。<br/>
            可以填写任意内容，但不能为空。
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `找不到 Grim Dawn 安装目录`,
        tag: 'CannotFindGrimdawn',
        body: () => <div>
            IA 会通过以下方式自动检测 Grim Dawn：
            <ul>
                <li>检查 Grim Dawn 是否正在运行</li>
                <li>读取 Steam 配置</li>
                <li>读取 Registry</li>
                <li>查找 GOG Galaxy</li>
            </ul>
            <b>让 IA 检测到 Grim Dawn 最简单的方法，是在 IA 运行时启动游戏。</b>
        </div>,
        type: IHelpEntryType.Informational
    },
    {
        title: `在线备份如何工作？`,
        tag: 'OnlineBackups',
        body: () => <div>
            <h2>IA 内置了将物品备份到云端的功能。</h2>
            要启用物品自动备份，只需输入 e-mail 地址，然后输入
            收到的一次性验证码。 <br/>
            <br/>
            每次 IA 收取物品时都会同步到云端。如果发生问题需要重装
            IA，只需使用同一个 e-mail 地址登录，IA 就会
            自动重新下载全部物品。
            <br/>
            也可以通过“使用多台
            PC”设置在多台计算机之间共享物品。
        </div>
    },
    {
        title: `仓库页数量不足`,
        tag: 'NotEnoughStashTabs',
        body: () => <div>
            IA 至少需要两个<u>公共仓库</u>页才能工作。 <br/>
            一页用于收取物品，另一页用于存入物品。 <br/>
            默认情况下，IA 会收取放在公共仓库<u>最后一页</u>的物品，并把物品存入<u>倒数
            第二页</u>。<br/><br/>
            如果已有两个或更多仓库页却仍看到此错误，很可能购买的是<u>个人
            仓库</u>页，而不是<u>公共仓库</u>页。<br/>
            <img src="./smuggler.png"/> <br/>
            如果看到此图标，当前查看的是<u>个人仓库</u>，而不是公共仓库。
        </div>
    },
    {
        title: `可以把物品存到其他硬盘吗？`,
        tag: 'StoreOnDifferentDisk',
        body: () => <div>
            可以，但需要手动设置。<br/><br/>
            这是面向 power user 的用法，操作步骤如下： <br/>
            <ol>
                <li>关闭 IA</li>
                <li>打开 IA 安装目录</li>
                <li>把“UserData”文件夹复制到“D:\mystuff\UserData”</li>
                <li>把原“UserData”文件夹重命名为“UserData-backup”</li>
                <li>在 IA 安装目录中打开命令行</li>
                <li>"cd /d &lt;IA 安装目录&gt;"</li>
                <li>"mklink /j UserData D:\mystuff\UserData"</li>
                <li>启动 IA</li>
                <li>确认一切正常后，删除“UserData-backup”文件夹。</li>
            </ol>
            这会创建一个 IA 无需感知的目录链接。物品等数据实际位于“D:\mystuff\UserData”，但 IA 仍可从
            安装目录中的“UserData”路径读取数据。 <br/>

          以后更换硬盘时，记得先删除此 symlink！
        </div>
    },
    {
        title: `可以在 4K 显示器上使用吗？`,
        tag: '4K',
        body: () => <div>
            Item Assistant 在 4K 显示器和 UI scaling 下可能无法正常显示。 <br/>
            右键单击 Item Assistant，打开属性，然后进入兼容性设置。 <br/>
            <img src="./4k.png"/>
        </div>
    },
    {
        title: `IA 为什么要求提供 e-mail？`,
        tag: 'email-whiners',
        body: () => <div>
            IA 使用 e-mail 来识别在线备份帐户。可以在在线页单击“退出在线功能”来禁用此功能。 <br/>
            IA 不会将 e-mail 用于其他目的，backend code 也和 IA 一样开源。如果不想使用，可直接退出在线备份。 <br/>
            <span style="display: none;">电子邮件、隐私、登录。</span>
        </div>
    },
] as IHelpEntry[];

// Converts a JSX element tree to an array of text (extract text content from <div>mytext</div> for example)
function elementToText(arg: any) {
    const children = arg?.children || [];

    if (typeof children === 'string') {
        return [children];
    }

    let result = [] as string[];
    for (const idx in children) {
        const child = children[idx];

        if (typeof child === 'string') {
            result.push(child);
        } else if (child) {
            result = result.concat(elementToText(child.props));
        }
    }

    return result;
}

// Checks if a target string contains all the words in a search string, ala "mystrike LIKE %word%word%word%"
const contains = (target: string, what: string) => {
    const args = what.split(' ');
    for (const idx in args) {
        const arg = args[idx];
        if (target.toLowerCase().indexOf(arg.toLowerCase()) === -1) {
            return false;
        }
    }

    return true;
};

interface Props {
    searchString: string;
    onSearch: (s: string) => void;
}

export class HelpChinese extends PureComponent<Props, object> {
    renderHelpEntry(entry: IHelpEntry) {

        const onSelectTag = (tag: string | undefined) => {
            if (tag) {
                this.props.onSearch(tag);
            }
        };

        return (
            <div className="container" key={entry.tag}>
                <div className="header" data-helptag={entry.tag} onClick={() => onSelectTag(entry.tag)}>
                    {entry.title}
                    {entry.type === IHelpEntryType.Informational &&
                    <span className="informational">
            说明
          </span>}
                    {entry.type === IHelpEntryType.Help &&
                    <span className="needhelp">
            帮助
          </span>}
                </div>
                <div className="content">
                    {entry.body()}
                </div>
            </div>
        );
    }

    filteredEntries() {
        if (this.props.searchString.trim() === '') {
            return helpEntries.map(e => this.renderHelpEntry(e));
        }

        // Convert a JSX element to searchable text
        const toSearchableText = (elem: IHelpEntry) => [elem.title, elem.tag]
            .concat(elementToText(elem.body().props))
            .join(' ');

        // Filter items
        return helpEntries
            .filter(s => contains(toSearchableText(s), this.props.searchString))
            .map(e => this.renderHelpEntry(e));
    }

    render() {
        return <div className="help">
            <div className="form-group">
                <h2>搜索：</h2>
                <input type="text" className="form-control" placeholder="搜索..."
                       onInput={(e: any) => this.props.onSearch(e.target.value)} value={this.props.searchString}/>
            </div>
            {this.filteredEntries()}

            <h2>仍然没有找到需要的答案？</h2>
            <a href="#" onClick={() => openUrl('https://discord.gg/5wuCPbB')} target="_blank" rel="noreferrer">前往
                IA Discord 寻求帮助！</a>
        </div>;
    }
}
