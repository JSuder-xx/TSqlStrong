import * as React from 'react';
import { withRouter, RouteComponentProps } from 'react-router-dom';

export default class Home extends React.Component<RouteComponentProps<any>, {}> {
    public render() {
        return <div>
            <p>
                T-SQL Strong is a T-SQL type checker/verifier that
                <ul>
                    <li>Verifies the correctness of T-Sql code before it executes against a database.</li>
                    <li>
                        <span>Provides advanced type checking features such as</span>
                        <ul>
                            <li>Key Column Comparison - Protects against incorrect joins.</li>
                            <li>Null Type Checking - Guard against run-time errors of assigning a null value to a non-null column.</li>
                            <li>Check Constraints As Enumerations - Ensure only valid literals are assigned or compared against check constraint columns.</li>
                            <li>Insert Into Select Alias Matching - Protect against positional mistakes made with insert-select where you have many columns.</li>
                            <li>Verify the correctness of Temporary Table structure usage between different stored procedures.</li>
                            <li>Flow Typing. TSqlStrong can learn more about types (refinement) by analyzing flow control (IF/ELSEIF, CASE).</li>
                        </ul>
                    </li>
                </ul>
            </p>
            <p>
                The project is currently a proof of concept. 
            </p>
            <p>
                Take it for a spin on the Try It Online tab.
            </p>
        </div>;
    }
}
