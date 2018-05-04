import * as React from 'react';
import { NavLink, Link } from 'react-router-dom';

export class NavMenu extends React.Component<{}, {}> {
    public render() {
        return <nav className="navbar navbar-expand-lg navbar-dark bg-dark">
            <a className="navbar-brand" href="#">T-SQL Strong</a>
            <button className="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarNav" aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
                <span className="navbar-toggler-icon"></span>
            </button>
            <div className="collapse navbar-collapse" id="navbarNav">
                <ul className="navbar-nav">
                    <li className="nav-item">
                        <NavLink className="nav-link" exact to={'/'} activeClassName='active'>Home</NavLink>
                    </li>
                    <li className="nav-item">
                        <NavLink className="nav-link" to={'/tsqleditor'} activeClassName='active'>Try It On-Line!</NavLink>
                    </li>
                </ul>
            </div>
        </nav>;
    }
}
