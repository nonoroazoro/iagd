import { PureComponent } from 'preact/compat';
import { localize } from '../../translations';
import { HelpChinese } from './HelpChinese';
import { HelpEnglish } from './HelpEnglish';

interface Props {
  searchString: string;
  onSearch: (search: string) => void;
}

export class Help extends PureComponent<Props, object> {
  render() {
    const Content = localize(HelpEnglish, HelpChinese);
    return <Content searchString={this.props.searchString} onSearch={this.props.onSearch} />;
  }
}
