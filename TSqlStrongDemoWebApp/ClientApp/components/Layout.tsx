import * as React from 'react';
import { NavMenu } from './NavMenu';

export class Layout extends React.Component<{}, {}> {
    public render() {
        return <div>
            <NavMenu />
            <div className="container-fluid">
                {this.props.children}
            </div>
        </div>;        
    }
}
